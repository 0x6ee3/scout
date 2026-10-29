const test = require('node:test');
const assert = require('node:assert/strict');
const { parseWaveFormat, floatPlaybackFormat, guid, openPlayback, WasapiSource } = require('../app/wasapi.cjs');
const { PcmClock, PacketTimeline } = require('../app/pcm-clock.cjs');
const koffi = require('koffi');

test('virtual playback initializes twice with explicit format and never starts capture', { timeout: 20000 }, async () => {
  for (let attempt = 0; attempt < 2; attempt++) {
    const source = await openPlayback(process.pid);
    try {
      assert.equal(source.running, undefined);
      assert.deepEqual(source.metadata, { sampleRate: 48000, channelsPerFrame: 2, bitsPerChannel: 32, isFloat: true, frameBytes: 8 });
    } finally { source.close(); }
  }
});
test('native formats retain PCM/extensible subtype, width, channels and rate', () => {
  assert.equal(parseWaveFormat(floatPlaybackFormat()).isFloat, true);
  const format = Buffer.alloc(40);
  format.writeUInt16LE(0xfffe); format.writeUInt16LE(2, 2); format.writeUInt32LE(44100, 4);
  format.writeUInt16LE(6, 12); format.writeUInt16LE(24, 14); format.writeUInt16LE(22, 16);
  guid('00000001-0000-0010-8000-00aa00389b71').copy(format, 24);
  assert.deepEqual(parseWaveFormat(format), { sampleRate: 44100, channelsPerFrame: 2, bitsPerChannel: 24, isFloat: false, frameBytes: 6 });
  format[24] = 9;
  assert.throws(() => parseWaveFormat(format), /unsupported/);
});
test('timestamped PCM preserves missing silence and places later audio at its original time', () => {
  const chunks = [];
  const metadata = { sampleRate: 1000, channelsPerFrame: 1, bitsPerChannel: 16, isFloat: false };
  const clock = new PcmClock(metadata, data => chunks.push(data), performance.now() - 1000);
  const first = Buffer.alloc(200); first.fill(1);
  const second = Buffer.alloc(200); second.fill(2);
  clock.push(first, 100); clock.push(second, 500);
  clock.drain(true);
  const result = Buffer.concat(chunks);
  assert.deepEqual(result.subarray(0, 200), Buffer.alloc(200));
  assert.deepEqual(result.subarray(200, 400), first);
  assert.deepEqual(result.subarray(400, 1000), Buffer.alloc(600));
  assert.deepEqual(result.subarray(1000, 1200), second);
});
test('PCM queues reject excessive lag and late packets instead of silently dropping audio', () => {
  const metadata = { sampleRate: 1000, channelsPerFrame: 1, bitsPerChannel: 16, isFloat: false };
  const clock = new PcmClock(metadata, () => {}, performance.now() - 1000);
  assert.throws(() => clock.push(Buffer.alloc(10000), 0), /four-second/);
  clock.drain(true);
  assert.throws(() => clock.push(Buffer.alloc(200), 0), /capture deadline/);
});

test('jittering packet timestamps never cut samples or inject silence into a continuous microphone tone', () => {
  const rate = 48000, packetFrames = 480, count = 100, origin100ns = 1e7, leadFrames = 4800;
  const output = [];
  const timeline = new PacketTimeline(rate, origin100ns);
  const clock = new PcmClock({ sampleRate: rate, channelsPerFrame: 1, bitsPerChannel: 32, isFloat: true },
    data => output.push(data), performance.now() - 1500);
  const expected = Buffer.alloc(count * packetFrames * 4);
  for (let packet = 0; packet < count; packet++) {
    const data = Buffer.alloc(packetFrames * 4);
    for (let frame = 0; frame < packetFrames; frame++) data.writeFloatLE(.2 * Math.sin(2 * Math.PI * 440 * (packet * packetFrames + frame) / rate), frame * 4);
    data.copy(expected, packet * data.length);
    const jitterFrames = packet === 0 ? 0 : packet % 2 ? 38 : -38;
    const start = timeline.place({ position: packet * packetFrames, flags: packet === 0 ? 1 : 0,
      timestamp: origin100ns + (leadFrames + packet * packetFrames + jitterFrames) * 1e7 / rate }, packetFrames);
    clock.push(data, start);
  }
  clock.drain(true);
  assert.deepEqual(Buffer.concat(output).subarray(leadFrames * 4, (leadFrames + count * packetFrames) * 4), expected);
  assert.equal(timeline.statistics.maxQpcDeviationFrames, 38);
  assert.equal(timeline.statistics.missingDeviceFrames, 0);
});
test('device frame gaps remain silence while normal timestamps cannot create artificial gaps', () => {
  const timeline = new PacketTimeline(48000, 1e7);
  assert.equal(timeline.place({ position: 0, timestamp: 1e7, flags: 1 }, 480), 0);
  assert.equal(timeline.place({ position: 480, timestamp: 1e7 + 104000, flags: 0 }, 480), 480);
  assert.equal(timeline.place({ position: 1440, timestamp: 1e7 + 300000, flags: 1 }, 480), 1440);
  assert.equal(timeline.statistics.missingDeviceFrames, 480);
});
test('timestamp errors keep sample continuity and flagged device counter resets reanchor safely', () => {
  const timeline = new PacketTimeline(48000, 1e7);
  assert.equal(timeline.place({ position: 0, timestamp: 1e7, flags: 1 }, 480), 0);
  assert.equal(timeline.place({ position: 999999, timestamp: 0, flags: 4 }, 480), 480);
  assert.equal(timeline.place({ position: 960, timestamp: 1e7 + 200000, flags: 0 }, 480), 960);
  assert.equal(timeline.place({ position: 0, timestamp: 1e7 + 400000, flags: 1 }, 480), 1920);
  assert.equal(timeline.statistics.timestampFallbacks, 1);
  assert.equal(timeline.statistics.positionResets, 1);
  assert.equal(timeline.place({ position: 0, timestamp: 1e7 + 500000, flags: 0 }, 480), 2400);
  assert.equal(timeline.statistics.positionFallbacks, 2);
});

test('camera counters with 160-unit steps or repeated zero positions never abort or alter 480-frame packets', () => {
  for (const positionOf of [index => index * 320, index => index * 160, () => 0, index => index % 3 === 0 ? 0 : index * 480]) {
    const timeline = new PacketTimeline(48000, 1e7);
    for (let index = 0; index < 100; index++) {
      assert.equal(timeline.place({ position: positionOf(index), timestamp: 1e7 + index * 100000, flags: index === 0 ? 1 : 0 }, 480), index * 480);
    }
    assert.ok(timeline.statistics.positionFallbacks > 0);
  }
});

test('native packet reads preserve bytes, replace silence, report flags and release every packet', () => {
  const allocation = koffi.alloc('uint8_t', 16);
  const packets = [{ frames: 2, flags: 0, bytes: Buffer.from([0, 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15]) },
    { frames: 2, flags: 7, bytes: Buffer.alloc(16, 255) }];
  const callbacks = [];
  let count = 0;
  function callback(signature, implementation) {
    const pointer = koffi.register(implementation, koffi.pointer(koffi.proto(signature)));
    callbacks.push(pointer); return pointer;
  }
  const table = koffi.alloc('void *', 6), object = koffi.alloc('void *', 1);
  const getBuffer = callback('int32_t TestPacketRead(void *self, void *data, void *frames, void *flags, void *position, void *timestamp)', (_self, data, frames, flags, position, timestamp) => {
    const packet = packets[0];
    koffi.encode(allocation, 'uint8_t', [...packet.bytes], packet.bytes.length);
    koffi.encode(data, 'void *', packet.flags & 2 ? null : allocation);
    koffi.encode(frames, 'uint32_t', packet.frames); koffi.encode(flags, 'uint32_t', packet.flags);
    koffi.encode(position, 'uint64_t', count * 2); koffi.encode(timestamp, 'uint64_t', 10000000 + count * 1000);
    return 0;
  });
  const releasePacket = callback('int32_t TestPacketRelease(void *self, uint32_t frames)', (_self, frames) => {
    assert.equal(frames, 2); packets.shift(); count++; return 0;
  });
  const getNext = callback('int32_t TestPacketNext(void *self, void *frames)', (_self, frames) => {
    koffi.encode(frames, 'uint32_t', packets[0]?.frames || 0); return 0;
  });
  koffi.encode(table, koffi.array('void *', 6), [null, null, null, getBuffer, releasePacket, getNext]);
  koffi.encode(object, 'void *', table);
  const source = new WasapiSource('playback', null, floatPlaybackFormat(), {});
  source.capture = object;
  const received = [];
  source.on('packet', (data, info) => received.push({ data, info }));
  try {
    source.poll();
    assert.equal(count, 2);
    assert.deepEqual(received[0].data, Buffer.from([0, 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15]));
    assert.deepEqual(received[1].data, Buffer.alloc(16));
    assert.equal(received[1].info.flags, 7);
    assert.equal(source.statistics.discontinuities, 1);
    assert.equal(source.statistics.timestampErrors, 1);
    assert.equal(source.statistics.silentPackets, 1);
  } finally {
    callbacks.forEach(value => koffi.unregister(value));
    [allocation, table, object].forEach(value => koffi.free(value));
  }
});
test('native device errors retain the exact Windows HRESULT', () => {
  const callback = koffi.register(() => -2004287484, koffi.pointer(koffi.proto('int32_t TestDeviceError(void *self, void *frames)')));
  const table = koffi.alloc('void *', 6), object = koffi.alloc('void *', 1);
  koffi.encode(table, koffi.array('void *', 6), [null, null, null, null, null, callback]);
  koffi.encode(object, 'void *', table);
  const source = new WasapiSource('microphone', null, floatPlaybackFormat(), {});
  source.capture = object;
  try { assert.throws(() => source.poll(), /0x88890004/); }
  finally { koffi.unregister(callback); koffi.free(table); koffi.free(object); }
});
