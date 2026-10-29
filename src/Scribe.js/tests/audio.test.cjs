const test = require('node:test');
const assert = require('node:assert/strict');
const fs = require('node:fs');
const os = require('node:os');
const path = require('node:path');
const { fork, spawnSync } = require('node:child_process');
const { once } = require('node:events');
const ffmpeg = require('ffmpeg-static');
const { AudioSession } = require('../app/audio-session.cjs');
const { Encoder, pcmFormat } = require('../app/encoder.cjs');
const wait = ms => new Promise(resolve => setTimeout(resolve, ms));
function decode(file) {
  const result = spawnSync(ffmpeg, ['-hide_banner', '-loglevel', 'error', '-i', file, '-f', 'f32le', '-ac', '1', '-ar', '48000', 'pipe:1'], { maxBuffer: 20 * 1024 * 1024, windowsHide: true });
  assert.equal(result.status, 0, result.stderr.toString());
  return new Float32Array(result.stdout.buffer, result.stdout.byteOffset, result.stdout.length / 4);
}
function amplitude(samples, hz) {
  let sine = 0, cosine = 0;
  const count = Math.min(48000, samples.length - 2400);
  for (let index = 0; index < count; index++) {
    const value = samples[index + 2400];
    sine += value * Math.sin(2 * Math.PI * hz * index / 48000);
    cosine += value * Math.cos(2 * Math.PI * hz * index / 48000);
  }
  return 2 * Math.hypot(sine, cosine) / count;
}
test('separate source MP3s and live mixed MP3 retain both tones in the correct proportions', { timeout: 20000 }, async t => {
  const root = fs.mkdtempSync(path.join(os.tmpdir(), 'scribe-js-audio-'));
  t.after(() => fs.rmSync(root, { recursive: true, force: true }));
  const session = new AudioSession({ root, synthetic: true });
  t.after(() => session.stop());
  await session.start();
  await wait(1600);
  const result = await session.stop();
  assert.equal(result.completed, true, result.error);
  for (const [name, own, other] of [['microphone', 440, 880], ['playback', 880, 440]]) {
    const file = path.join(result.directory, `${name}.mp3`);
    const samples = decode(file);
    assert.ok(samples.length > 48000, `${name} has a playable duration`);
    assert.ok(amplitude(samples, own) > .15, `${name} retains its own source tone`);
    assert.ok(amplitude(samples, other) < .005, `${name} does not mix the other source`);
    const probe = spawnSync(ffmpeg, ['-hide_banner', '-i', file], { windowsHide: true, encoding: 'utf8' });
    assert.match(probe.stderr, /48000 Hz, mono/);
    assert.match(probe.stderr, /256 kb\/s/);
  }
  const mixed = decode(path.join(result.directory, 'recording.mp3'));
  assert.ok(Math.abs(amplitude(mixed, 440) - .1) < .01, 'microphone has fixed half gain after mono averaging');
  assert.ok(Math.abs(amplitude(mixed, 880) - .1) < .01, 'playback has fixed half gain after mono averaging');
  const mixedProbe = spawnSync(ffmpeg, ['-hide_banner', '-i', path.join(result.directory, 'recording.mp3')], { windowsHide: true, encoding: 'utf8' });
  assert.match(mixedProbe.stderr, /48000 Hz, mono/);
  assert.match(mixedProbe.stderr, /256 kb\/s/);
  const journal = fs.readFileSync(path.join(result.directory, 'events.jsonl'), 'utf8').trim().split('\n').map(JSON.parse);
  assert.equal(journal.at(-1).kind, 'completed');
  assert.equal(journal.filter(event => event.kind === 'final-track').length, 3);
});
test('forced process termination retains the mixed MP3 and both source MP3s', { timeout: 22000 }, async t => {
  const root = fs.mkdtempSync(path.join(os.tmpdir(), 'scribe-js-crash-'));
  const child = fork(path.join(__dirname, 'audio-child.cjs'), [root], { stdio: ['ignore', 'pipe', 'pipe', 'ipc'], windowsHide: true });
  let errors = '';
  child.stderr.on('data', data => { errors += data.toString(); });
  t.after(async () => { if (child.exitCode === null && child.signalCode === null) { child.kill(); await once(child, 'exit'); } fs.rmSync(root, { recursive: true, force: true }); });
  const [{ directory }] = await Promise.race([once(child, 'message'), once(child, 'exit').then(() => { throw new Error(errors); })]);
  await wait(5600);
  child.kill('SIGKILL');
  await once(child, 'exit');
  for (const name of ['recording', 'microphone', 'playback']) {
    const samples = decode(path.join(directory, `${name}.mp3`));
    assert.ok(samples.length > 48000 * 4, `${name} survives without repair`);
    if (name === 'recording') { assert.ok(amplitude(samples, 440) > .08); assert.ok(amplitude(samples, 880) > .08); }
  }
  const events = fs.readFileSync(path.join(directory, 'events.jsonl'), 'utf8').trim().split('\n').map(JSON.parse);
  for (const name of ['recording', 'microphone', 'playback']) assert.ok(events.some(event => event.kind === 'checkpoint' && event.detail.file === `${name}.mp3` && event.detail.bytes > 0));
  assert.ok(!events.some(event => event.kind === 'completed'));
});
test('unsupported or implausible PCM formats fail explicitly', () => {
  assert.throws(() => pcmFormat({ sampleRate: 0, channelsPerFrame: 1, bitsPerChannel: 16 }));
  assert.throws(() => pcmFormat({ sampleRate: 48000, channelsPerFrame: 1, bitsPerChannel: 12 }));
});

function tone(seconds, hz, volume, channels = 1, audible = () => true) {
  const frames = Math.round(seconds * 48000), data = Buffer.alloc(frames * channels * 4);
  for (let frame = 0; frame < frames; frame++) {
    const value = audible(frame / 48000) ? volume * Math.sin(2 * Math.PI * hz * frame / 48000) : 0;
    for (let channel = 0; channel < channels; channel++) data.writeFloatLE(value, (frame * channels + channel) * 4);
  }
  return data;
}
const floatFormat = channels => ({ sampleRate: 48000, channelsPerFrame: channels, bitsPerChannel: 32, isFloat: true });
test('merged timeline retains source silence, the longer tail and fixed gain after one source ends', { timeout: 15000 }, async t => {
  const root = fs.mkdtempSync(path.join(os.tmpdir(), 'scribe-js-mix-timing-'));
  t.after(() => fs.rmSync(root, { recursive: true, force: true }));
  const file = path.join(root, 'recording.mp3');
  const encoder = new Encoder(file, [floatFormat(1), floatFormat(2)]);
  encoder.on('fault', () => {});
  encoder.write(tone(2, 440, .6), 0);
  encoder.write(tone(1, 880, .6, 2, time => time >= .4 && time < .8), 1);
  await encoder.finish();
  const samples = decode(file);
  assert.ok(Math.abs(samples.length / 48000 - 2) < .1, 'merged duration follows the longer source');
  const window = (start, end) => samples.subarray(Math.round(start * 48000), Math.round(end * 48000));
  assert.ok(amplitude(window(.05, .35), 880) < .005, 'playback leading silence is retained');
  assert.ok(Math.abs(amplitude(window(.45, .75), 440) - .3) < .02);
  assert.ok(Math.abs(amplitude(window(.45, .75), 880) - .3) < .02, 'playback tone arrives at the correct time');
  assert.ok(Math.abs(amplitude(window(1.2, 1.8), 440) - .3) < .02, 'microphone tail is retained without a volume jump');
  assert.ok(amplitude(window(1.2, 1.8), 880) < .005);
});
test('coincident full-level stereo sources mix with headroom instead of doubling or boosting channels', { timeout: 15000 }, async t => {
  const root = fs.mkdtempSync(path.join(os.tmpdir(), 'scribe-js-mix-level-'));
  t.after(() => fs.rmSync(root, { recursive: true, force: true }));
  const file = path.join(root, 'recording.mp3');
  const encoder = new Encoder(file, [floatFormat(2), floatFormat(2)]);
  encoder.on('fault', () => {});
  encoder.write(tone(1.5, 440, .95, 2), 0);
  encoder.write(tone(1.5, 440, .95, 2), 1);
  await encoder.finish();
  const samples = decode(file);
  assert.ok(Math.abs(amplitude(samples, 440) - .95) < .02, 'two .95 sources produce .95, not 1.9 or a stereo sum boost');
  assert.ok(samples.every(value => Math.abs(value) < 1.02), 'decoded output is not clipped');
});
