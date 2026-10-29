// Align the first packet with QPC, then append complete packets by sample count.
// Drivers can expose repeating/non-frame-scaled positions after engine conversion.
// Neither timestamp jitter nor those counters may cut samples or abort capture.
class PacketTimeline {
  constructor(sampleRate, origin100ns) {
    this.sampleRate = sampleRate;
    this.origin100ns = origin100ns;
    this.nextFrame = 0;
    this.expectedPosition = null;
    this.hasPackets = false;
    this.statistics = { missingDeviceFrames: 0, positionResets: 0, positionFallbacks: 0, timestampFallbacks: 0, maxQpcDeviationFrames: 0 };
  }
  place(packet, frameCount) {
    const timestampValid = !(packet.flags & 4) && Number.isFinite(packet.timestamp) && packet.timestamp > 0;
    const positionValid = !(packet.flags & 4) && Number.isSafeInteger(packet.position) && packet.position >= 0;
    const qpcFrame = timestampValid ? Math.round((packet.timestamp - this.origin100ns) * this.sampleRate / 1e7) : this.nextFrame;
    let start = this.hasPackets ? this.nextFrame : qpcFrame;
    if (!timestampValid) this.statistics.timestampFallbacks++;
    const deviceGap = positionValid && this.expectedPosition !== null ? packet.position - this.expectedPosition : 0;
    if (this.hasPackets && deviceGap !== 0) this.statistics.positionFallbacks++;
    if (this.hasPackets && (packet.flags & 1)) {
      if (deviceGap < 0) this.statistics.positionResets++;
      const timeGap = qpcFrame - this.nextFrame;
      // Only a flagged discontinuity with corroborating time/position evidence
      // creates silence. Unflagged driver counter quirks remain diagnostics.
      const agrees = deviceGap > 0 && Math.abs(deviceGap - timeGap) <= this.sampleRate / 1000;
      if (timestampValid && timeGap > 0 && timeGap <= this.sampleRate * 4 && (agrees || deviceGap < 0)) {
        const gap = agrees ? deviceGap : timeGap;
        start += gap;
        this.statistics.missingDeviceFrames += gap;
      }
    }
    if (timestampValid) this.statistics.maxQpcDeviationFrames = Math.max(this.statistics.maxQpcDeviationFrames, Math.abs(qpcFrame - start));
    if (positionValid) this.expectedPosition = packet.position + frameCount;
    else if (this.expectedPosition !== null) this.expectedPosition += frameCount;
    this.hasPackets = true;
    this.nextFrame = start + frameCount;
    return start;
  }
}

// Capture may omit silent packets. Pace each stream against the same monotonic
// origin, with 300 ms headroom, so silence does not shorten a separate output file.
class PcmClock {
  constructor(metadata, write, origin = performance.now()) {
    this.metadata = metadata; this.write = write; this.origin = origin;
    this.frameBytes = metadata.channelsPerFrame * metadata.bitsPerChannel / 8;
    this.queue = []; this.buffered = 0; this.frames = 0; this.nextFrame = 0;
  }
  push(data, startFrame = this.nextFrame) {
    if (data.length % this.frameBytes) throw new Error('Unaligned native audio packet.');
    if (this.buffered + data.length > this.metadata.sampleRate * this.frameBytes * 4) throw new Error('Native audio exceeded its four-second queue.');
    startFrame = Math.round(startFrame);
    const packetFrames = data.length / this.frameBytes;
    if (startFrame + packetFrames <= 0) return;
    if (startFrame < 0) { data = data.subarray(-startFrame * this.frameBytes); startFrame = 0; }
    if (startFrame < this.frames) throw new Error('Native audio arrived after its 300 ms capture deadline. Partial files were retained.');
    this.queue.push({ data, startFrame }); this.buffered += data.length;
    this.nextFrame = startFrame + data.length / this.frameBytes;
  }
  drain(final = false) {
    const elapsed = Math.max(0, performance.now() - this.origin - (final ? 0 : 300));
    const target = Math.floor(elapsed * this.metadata.sampleRate / 1000);
    while (this.frames < target) {
      const frames = Math.min(target - this.frames, Math.ceil(this.metadata.sampleRate / 50));
      const output = Buffer.alloc(frames * this.frameBytes, !this.metadata.isFloat && this.metadata.bitsPerChannel === 8 ? 128 : 0);
      let copied = 0;
      while (copied < output.length && this.queue.length) {
        const first = this.queue[0];
        const currentFrame = this.frames + copied / this.frameBytes;
        if (first.startFrame > currentFrame) {
          copied += Math.min(output.length - copied, (first.startFrame - currentFrame) * this.frameBytes);
          continue;
        }
        const skip = Math.max(0, currentFrame - first.startFrame) * this.frameBytes;
        const count = Math.min(output.length - copied, first.data.length - skip);
        if (count > 0) first.data.copy(output, copied, skip, skip + count);
        const consumed = Math.min(first.data.length, skip + Math.max(0, count));
        copied += Math.max(0, count); this.buffered -= consumed;
        if (consumed === first.data.length) this.queue.shift();
        else { first.data = first.data.subarray(consumed); first.startFrame += consumed / this.frameBytes; }
      }
      this.frames += frames;
      this.write(output);
    }
  }
}
module.exports = { PcmClock, PacketTimeline };
