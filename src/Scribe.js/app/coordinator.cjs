const { EventEmitter } = require('node:events');

class Coordinator extends EventEmitter {
  constructor({ createWorker, preferences, synthetic = false, excludedPid = process.pid }) {
    super();
    Object.assign(this, { createWorker, preferences, synthetic, excludedPid });
    this.state = { phase: 'idle', startedAt: null, directory: null, error: preferences.error, lastResult: null };
    this.events = [];
  }
  get busy() { return ['starting', 'recording', 'finalizing'].includes(this.state.phase); }
  snapshot() { return { ...this.state, busy: this.busy, recordingFolder: this.preferences.value.recordingFolder, settingsError: this.preferences.error, events: this.events }; }
  update(patch) { Object.assign(this.state, patch); this.emit('change', this.snapshot()); }
  log(event) {
    this.events.push(event);
    if (this.events.length > 250) this.events.shift();
    this.emit('change', this.snapshot());
  }
  start() {
    if (this.busy) return;
    if (this.preferences.error) throw new Error(this.preferences.error);
    this.events = [];
    this.update({ phase: 'starting', startedAt: null, directory: null, error: null, lastResult: null });
    try {
      const worker = this.createWorker();
      this.worker = worker;
      this.stopped = new Promise(resolve => { this.resolveStopped = resolve; });
      worker.on('message', message => {
        if (worker !== this.worker) return;
        if (message.type === 'event') {
          if (message.directory) this.state.directory = message.directory;
          this.log(message.event);
        } else if (message.type === 'started') {
          const patch = { directory: message.directory, startedAt: message.startedAt };
          if (this.state.phase === 'starting') patch.phase = 'recording';
          this.update(patch);
        } else if (message.type === 'fault') {
          this.update({ error: message.error });
          void this.stop();
        } else if (message.type === 'stopped') this.complete(message.result);
      });
      worker.once('exit', code => {
        if (this.worker === worker) this.complete({ completed: false, error: `Audio worker exited unexpectedly (${code}). Any checkpointed MP3 data was retained.`, directory: this.state.directory });
      });
      this.watchdog = setTimeout(() => {
        if (this.state.phase === 'starting') {
          this.update({ error: 'Audio startup timed out.' });
          void this.stop();
        }
      }, 20000);
      worker.postMessage({ type: 'start', options: { root: this.preferences.value.recordingFolder, microphoneDeviceId: this.preferences.value.microphoneDeviceId, excludedPid: this.excludedPid, synthetic: this.synthetic } });
    } catch (error) {
      this.complete({ completed: false, error: error.message });
    }
  }
  complete(result) {
    clearTimeout(this.watchdog);
    clearTimeout(this.stopTimeout);
    this.worker = null;
    const error = result?.error || this.state.error;
    this.update({ phase: error ? 'failed' : 'saved', error: error || null,
      directory: result?.directory || this.state.directory, lastResult: result || null });
    this.resolveStopped?.();
  }
  async stop() {
    if (!this.worker) return;
    if (this.state.phase !== 'finalizing') {
      clearTimeout(this.watchdog);
      this.update({ phase: 'finalizing' });
      this.worker.postMessage({ type: 'stop' });
      this.stopTimeout = setTimeout(() => {
        const worker = this.worker;
        this.complete({ completed: false, error: 'Audio finalization timed out. Partial files were retained.', directory: this.state.directory });
        worker?.kill();
      }, 18000);
    }
    await this.stopped;
  }
}
module.exports = { Coordinator };
