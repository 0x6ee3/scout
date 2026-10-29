// Explicit diagnostic: opens exactly one source, discards all PCM, and prints
// only device/policy/packet-header statistics. Never runs as part of npm test.
const { openMicrophone, openPlayback } = require('../app/wasapi.cjs');
const { PcmClock, PacketTimeline } = require('../app/pcm-clock.cjs');
const { clock100ns } = require('../app/wasapi.cjs');
const args = process.argv.slice(2);
const option = name => { const index = args.indexOf(name); return index < 0 ? undefined : args[index + 1]; };
async function main() {
  const mode = option('--source');
  const seconds = Number(option('--seconds') || 3);
  if (!['microphone', 'playback', 'both'].includes(mode) || seconds < 1 || seconds > 30) throw new Error('Use --source microphone|playback|both --seconds 1..30 [--device endpoint-id].');
  if (mode === 'both') {
    const sources = [];
    let timer;
    const failures = [];
    try {
      sources.push(await openPlayback(process.pid));
      sources.push(openMicrophone(option('--device')));
      const origin = performance.now(), nativeOrigin = clock100ns();
      const clocks = [];
      for (const source of sources) {
        source.discardedBytes = 0;
        source.timeline = new PacketTimeline(source.metadata.sampleRate, nativeOrigin);
        const clock = new PcmClock(source.metadata, data => { source.discardedBytes += data.length; }, origin);
        clocks.push(clock);
        source.on('packet', (data, packet) => {
          try { clock.push(data, source.timeline.place(packet, data.length / source.metadata.frameBytes)); }
          catch (error) { failures.push(error.message); }
        });
        source.on('fault', error => failures.push(error.message));
        source.start();
      }
      timer = setInterval(() => { try { clocks.forEach(clock => clock.drain()); } catch (error) { failures.push(error.message); } }, 20);
      await new Promise(resolve => setTimeout(resolve, seconds * 1000));
      sources.forEach(source => source.stop());
      clocks.forEach(clock => clock.drain(true));
      console.log(JSON.stringify({ source: mode, failures, sources: sources.map(source => ({ name: source.name, device: source.device, discardedBytes: source.discardedBytes, statistics: source.statistics, timeline: source.timeline.statistics })) }));
      if (failures.length) throw new Error(failures.join('; '));
    } finally { clearInterval(timer); sources.forEach(source => source.close()); }
    return;
  }
  const source = mode === 'microphone' ? openMicrophone(option('--device')) : await openPlayback(process.pid);
  let failure;
  source.on('fault', error => { failure = error; });
  source.on('packet', () => {});
  try {
    console.log(JSON.stringify({ phase: 'starting', source: mode, device: source.device, format: source.metadata, audioPolicy: source.audioPolicy }));
    source.start();
    await new Promise(resolve => setTimeout(resolve, seconds * 1000));
    source.stop();
    console.log(JSON.stringify({ phase: 'released', source: mode, statistics: source.statistics, error: failure?.message || null }));
    if (failure) throw failure;
  } finally { source.close(); }
}
main().catch(error => { console.error(error); process.exitCode = 1; });
