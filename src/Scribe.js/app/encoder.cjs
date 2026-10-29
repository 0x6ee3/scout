const fs = require('node:fs');
const { spawn } = require('node:child_process');
const { EventEmitter } = require('node:events');

function pcmFormat(metadata) {
  const rate = metadata.sampleRate;
  const channels = metadata.channelsPerFrame;
  const bits = metadata.bitsPerChannel;
  if (!Number.isInteger(rate) || rate < 8000 || rate > 384000 || !Number.isInteger(channels) || channels < 1 || channels > 32) {
    throw new Error('The capture device returned an unsupported audio format.');
  }
  const format = metadata.isFloat ? ({ 32: 'f32le', 64: 'f64le' })[bits] : ({ 8: 'u8', 16: 's16le', 24: 's24le', 32: 's32le' })[bits];
  if (!format) throw new Error(`Unsupported ${bits}-bit PCM input.`);
  return { format, rate, channels, frameBytes: channels * bits / 8 };
}

class Encoder extends EventEmitter {
  constructor(file, metadata, executable = require('ffmpeg-static')) {
    super();
    this.file = file;
    this.inputFormats = (Array.isArray(metadata) ? metadata : [metadata]).map(pcmFormat);
    if (this.inputFormats.length < 1 || this.inputFormats.length > 2) throw new Error('Expected one source or a microphone/playback pair.');
    this.format = this.inputFormats[0];
    this.inputBytes = 0;
    this.inputBytesByStream = this.inputFormats.map(() => 0);
    this.outputBytes = 0;
    this.stderr = '';
    this.failure = null;
    this.ending = false;
    this.fd = fs.openSync(file, 'wx');
    const inputs = this.inputFormats.flatMap(({ format, rate, channels }, index) => [
      '-thread_queue_size', '128', '-probesize', '32', '-analyzeduration', '0', '-f', format,
      '-ar', String(rate), '-ac', String(channels), '-i', index === 0 ? 'pipe:0' : 'pipe:3'
    ]);
    const mono = index => `pan=mono|c0=${Array.from({ length: this.inputFormats[index].channels }, (_, channel) => `${1 / this.inputFormats[index].channels}*c${channel}`).join('+')}`;
    const mapping = this.inputFormats.length === 2 ? ['-filter_complex',
      `[0:a]${mono(0)},aformat=sample_fmts=fltp:sample_rates=48000:channel_layouts=mono[mic];` +
      `[1:a]${mono(1)},aformat=sample_fmts=fltp:sample_rates=48000:channel_layouts=mono[pc];` +
      '[mic][pc]amix=inputs=2:duration=longest:dropout_transition=0:weights=0.5 0.5:normalize=0[mix]', '-map', '[mix]'] : ['-map', '0:a:0'];
    this.child = spawn(executable, ['-hide_banner', '-loglevel', 'error', ...inputs, ...mapping,
      '-ar', '48000', '-ac', '1', '-c:a', 'libmp3lame', '-b:a', '256k',
      '-flush_packets', '1', '-write_xing', '0', '-f', 'mp3', 'pipe:1'],
    { windowsHide: true, stdio: this.inputFormats.length === 2 ? ['pipe', 'pipe', 'pipe', 'pipe'] : ['pipe', 'pipe', 'pipe'] });
    this.inputs = [this.child.stdin];
    if (this.inputFormats.length === 2) this.inputs.push(this.child.stdio[3]);
    this.done = new Promise(resolve => { this.resolveDone = resolve; });
    this.child.stdout.on('data', data => {
      try { fs.writeSync(this.fd, data); this.outputBytes += data.length; }
      catch (error) { this.fail(error); }
    });
    this.child.stderr.on('data', data => { this.stderr = (this.stderr + data.toString()).slice(-4000); });
    this.inputs.forEach(input => input.on('error', error => this.fail(error)));
    this.child.on('error', error => this.fail(error));
    this.child.on('close', code => {
      if (code !== 0 || !this.ending) this.fail(new Error(`MP3 encoder stopped (${code}): ${this.stderr}`));
      this.resolveDone();
    });
  }
  fail(error) {
    if (this.failure) return;
    this.failure = error;
    this.emit('fault', error);
  }
  write(data, index = 0) {
    if (this.ending || this.failure) return;
    const format = this.inputFormats[index], input = this.inputs[index];
    if (!format || !input) return this.fail(new Error('Unknown encoder source.'));
    if (data.length % format.frameBytes !== 0) return this.fail(new Error('Unaligned audio packet.'));
    const limit = format.rate * format.frameBytes * 4;
    if (input.writableLength + data.length > limit) return this.fail(new Error('Audio encoding fell more than four seconds behind. Partial files were retained.'));
    this.inputBytes += data.length;
    this.inputBytesByStream[index] += data.length;
    input.write(data);
  }
  checkpoint() {
    const origin = performance.now();
    fs.fsyncSync(this.fd);
    return { file: require('node:path').basename(this.file), bytes: this.outputBytes,
      inputBytes: this.inputBytes, inputBytesByStream: this.inputBytesByStream,
      audioSeconds: Math.max(...this.inputFormats.map((format, index) => this.inputBytesByStream[index] / format.frameBytes / format.rate)),
      queuedBytes: this.inputs.reduce((total, input) => total + input.writableLength, 0), flushMilliseconds: performance.now() - origin };
  }
  async finish() {
    this.ending = true;
    this.inputs.forEach(input => input.end());
    let timeout;
    try {
      await Promise.race([this.done, new Promise((_, reject) => {
        timeout = setTimeout(() => { this.child.kill(); reject(new Error('MP3 finalization timed out.')); }, 12000);
      })]);
      if (this.failure) throw this.failure;
      return this.checkpoint();
    } finally {
      clearTimeout(timeout);
      if (this.fd !== null) { fs.closeSync(this.fd); this.fd = null; }
    }
  }
}
module.exports = { Encoder, pcmFormat };
