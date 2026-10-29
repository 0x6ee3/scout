const fs = require('node:fs');
const path = require('node:path');
const { randomUUID } = require('node:crypto');
const { Encoder } = require('./encoder.cjs');
const { PcmClock, PacketTimeline } = require('./pcm-clock.cjs');

// This service owns capture and storage. The Electron main process only coordinates
// lifecycle; neither UI window has filesystem or audio access.
class AudioSession {
  constructor({ root, excludedPid, microphoneDeviceId = null, synthetic = false, notify = () => {} }) {
    this.root = root;
    this.excludedPid = excludedPid;
    this.microphoneDeviceId = microphoneDeviceId;
    this.synthetic = synthetic;
    this.notify = notify;
    this.encoders = new Map();
    this.sources = [];
    this.stopping = false;
    this.failure = null;
    this.startedAt = null;
    this.directory = path.join(root, `${new Date().toISOString().replace(/[:.]/g, '-')}_${randomUUID().slice(0, 8)}`);
  }
  event(kind, detail = {}) {
    const event = { utc: new Date().toISOString(), kind, detail };
    fs.writeSync(this.journal, `${JSON.stringify(event)}\n`);
    this.notify({ type: 'event', event, directory: this.directory });
  }
  fail(error) {
    this.failure ??= error.message || String(error);
    // The owner serializes Stop, including failures during asynchronous startup.
    this.notify({ type: 'fault', error: this.failure });
  }
  addTrack(name, metadata) {
    if (this.encoders.has(name)) return;
    const encoder = new Encoder(path.join(this.directory, `${name}.mp3`), metadata);
    encoder.on('fault', error => this.fail(error));
    this.encoders.set(name, encoder);
    this.event('format', { source: name, input: metadata, output: { sampleRate: 48000, channels: 1, bitrate: 256000 } });
  }
  addMergedTrack(formats) {
    this.addTrack('recording', formats);
    this.event('mix', { file: 'recording.mp3', sources: ['microphone', 'playback'], weights: [0.5, 0.5], normalization: false });
  }
  writeTrack(name, data) {
    this.encoders.get(name).write(data);
    this.encoders.get('recording').write(data, name === 'microphone' ? 0 : 1);
  }
  async start() {
    fs.mkdirSync(this.directory, { recursive: true });
    this.journal = fs.openSync(path.join(this.directory, 'events.jsonl'), 'wx');
    this.event('starting', { synthetic: this.synthetic, excludedPid: this.excludedPid, platform: process.platform,
      windows: require('node:os').release(), versions: process.versions, checkpointSeconds: 5 });
    this.startedAt = Date.now();
    if (this.synthetic) this.startSynthetic();
    else await this.startNative();
    const deadline = Date.now() + 10000;
    while (this.encoders.size !== 3 || [...this.encoders.values()].some(track => track.outputBytes === 0)) {
      if (this.failure) throw new Error(this.failure);
      if (Date.now() > deadline) throw new Error('The audio sources did not become ready within ten seconds.');
      await new Promise(resolve => setTimeout(resolve, 25));
    }
    this.event('recording', { startedAt: this.startedAt, primaryFile: 'recording.mp3', files: ['recording.mp3', 'microphone.mp3', 'playback.mp3'] });
    this.checkpoints = setInterval(() => {
      try {
        for (const track of this.encoders.values()) this.event('checkpoint', track.checkpoint());
        fs.fsyncSync(this.journal);
      } catch (error) { this.fail(error); }
    }, 5000);
    return { directory: this.directory, startedAt: this.startedAt };
  }
  async startNative() {
    if (process.platform !== 'win32' || process.arch !== 'x64' || Number(require('node:os').release().split('.')[2]) < 22000) throw new Error('This experiment requires Windows 11 x64.');
    const { openMicrophone, openPlayback, clock100ns } = require('./wasapi.cjs');
    // Activate playback first; failed virtual activation never opens the microphone.
    this.sources.push(await openPlayback(this.excludedPid));
    this.sources.push(openMicrophone(this.microphoneDeviceId));
    const origin = performance.now(), nativeOrigin = clock100ns();
    this.startedAt = Date.now();
    this.clocks = [];
    for (const source of this.sources) {
      const name = source.name;
      this.event('device', { source: name, ...source.device, nativeFormat: source.metadata, audioPolicy: source.audioPolicy });
      this.addTrack(name, source.metadata);
      const clock = new PcmClock(source.metadata, data => this.writeTrack(name, data), origin);
      const timeline = new PacketTimeline(source.metadata.sampleRate, nativeOrigin);
      source.timeline = timeline;
      this.clocks.push(clock);
      source.on('packet', (data, packet) => {
        try {
          const frame = timeline.place(packet, data.length / source.metadata.frameBytes);
          clock.push(data, frame);
        } catch (error) { this.fail(error); }
      });
      source.on('fault', error => this.fail(error));
    }
    this.addMergedTrack(['microphone', 'playback'].map(name => this.sources.find(source => source.name === name).metadata));
    for (const source of this.sources) source.start();
    this.captureTimer = setInterval(() => {
      try { for (const clock of this.clocks) clock.drain(); }
      catch (error) { this.fail(error); }
    }, 20);
  }
  startSynthetic() {
    const formats = [
      ['microphone', { sampleRate: 44100, channelsPerFrame: 1, bitsPerChannel: 16, isFloat: false }, 440],
      ['playback', { sampleRate: 48000, channelsPerFrame: 2, bitsPerChannel: 32, isFloat: true }, 880]
    ];
    const positions = [0, 0];
    formats.forEach(([name, metadata]) => this.addTrack(name, metadata));
    this.addMergedTrack(formats.map(([, metadata]) => metadata));
    // Catch up from elapsed monotonic time so CPU scheduling does not shorten files.
    const origin = performance.now();
    this.syntheticTimer = setInterval(() => {
      try {
        formats.forEach(([name, metadata, hz], index) => {
          const end = Math.floor((performance.now() - origin) * metadata.sampleRate / 1000);
          const frames = end - positions[index];
          if (frames <= 0) return;
          if (frames > metadata.sampleRate * 4) throw new Error('Synthetic audio exceeded its four-second queue.');
          const sampleBytes = metadata.bitsPerChannel / 8;
          const data = Buffer.alloc(frames * metadata.channelsPerFrame * sampleBytes);
          for (let frame = 0; frame < frames; frame++) {
            const value = 0.2 * Math.sin(2 * Math.PI * hz * (positions[index] + frame) / metadata.sampleRate);
            for (let channel = 0; channel < metadata.channelsPerFrame; channel++) {
              const offset = (frame * metadata.channelsPerFrame + channel) * sampleBytes;
              if (metadata.isFloat) data.writeFloatLE(value, offset);
              else data.writeInt16LE(Math.round(value * 32767), offset);
            }
          }
          positions[index] = end;
          this.writeTrack(name, data);
        });
      } catch (error) { this.fail(error); }
    }, 20);
  }
  stop() {
    return this.stopPromise ??= this.finish();
  }
  async finish() {
    this.stopping = true;
    clearInterval(this.syntheticTimer);
    clearInterval(this.checkpoints);
    clearInterval(this.captureTimer);
    for (const source of this.sources) {
      try { source.stop(); this.event('capture-statistics', { source: source.name, ...source.statistics, timeline: source.timeline?.statistics }); }
      catch (error) { this.failure ??= error.message; }
      finally { try { source.close(); } catch (error) { this.failure ??= error.message; } }
    }
    try { for (const clock of this.clocks || []) clock.drain(true); }
    catch (error) { this.failure ??= error.message; }
    const results = await Promise.allSettled([...this.encoders.values()].map(track => track.finish()));
    results.forEach(result => {
      if (result.status === 'fulfilled') this.event('final-track', result.value);
      else this.failure ??= result.reason.message;
    });
    if (this.journal !== undefined) {
      this.event(this.failure ? 'failed' : 'completed', { error: this.failure });
      fs.fsyncSync(this.journal);
      fs.closeSync(this.journal);
      this.journal = undefined;
    }
    return { directory: this.directory, completed: !this.failure, error: this.failure };
  }
}
module.exports = { AudioSession };
