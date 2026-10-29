// Windows 11 x64 WASAPI binding. Native packets stay in their source format;
// conversion/MP3 encoding is downstream. Loaded only by the audio utility process.
const koffi = require('koffi');
const { EventEmitter } = require('node:events');
const ole = koffi.load('ole32.dll');
const mm = koffi.load('Mmdevapi.dll');
const coInitialize = ole.func('int32_t CoInitializeEx(void *reserved, uint32_t flags)');
const coCreate = ole.func('int32_t CoCreateInstance(const void *clsid, void *outer, uint32_t context, const void *iid, _Out_ void **result)');
const coFree = ole.func('void CoTaskMemFree(void *memory)');
const clearVariant = ole.func('int32_t PropVariantClear(void *value)');
const activate = mm.func('int32_t ActivateAudioInterfaceAsync(const char16_t *device, const void *iid, void *params, void *handler, _Out_ void **operation)');
const kernel = koffi.load('kernel32.dll');
const queryCounter = kernel.func('int QueryPerformanceCounter(_Out_ int64_t *value)');
const queryFrequency = kernel.func('int QueryPerformanceFrequency(_Out_ int64_t *value)');
const frequency = [0]; queryFrequency(frequency);
function clock100ns() { const value = [0]; queryCounter(value); return Number(value[0]) * 1e7 / Number(frequency[0]); }
const prototypes = new Map();
const callbackTypes = new Map();
const ids = {
  enumeratorClass: 'BCDE0395-E52F-467C-8E3D-C4579291692E', enumerator: 'A95664D2-9614-4F35-A746-DE8DB63617E6',
  client: '1CB9AD4C-DBFA-4c32-B178-C2F568A703B2', capture: 'C8ADBD64-E71E-48a0-A4DE-185C395CD317',
  client2: '726778CD-F60A-4EDA-82DE-E47610CD78AA',
  unknown: '00000000-0000-0000-C000-000000000046', agile: '94EA2B94-E9CC-49E0-C0FF-EE64CA8F5B90',
  completion: '41D949AB-9862-444A-80F6-C261334DA5EB'
};
function guid(value) {
  const parts = value.split('-');
  const result = Buffer.alloc(16);
  result.writeUInt32LE(parseInt(parts[0], 16)); result.writeUInt16LE(parseInt(parts[1], 16), 4); result.writeUInt16LE(parseInt(parts[2], 16), 6);
  Buffer.from(parts[3] + parts[4], 'hex').copy(result, 8);
  return result;
}
function check(hr, action) {
  if (hr < 0) throw new Error(`${action} failed (Windows HRESULT 0x${(hr >>> 0).toString(16).padStart(8, '0')}).`);
  return hr;
}
function method(object, index, signature, ...args) {
  if (!object) throw new Error('A Windows audio interface was unavailable.');
  let proto = prototypes.get(signature);
  if (!proto) { proto = koffi.proto(`int32_t Method${prototypes.size}(void *self${signature ? ', ' + signature : ''})`); prototypes.set(signature, proto); }
  const table = koffi.decode(object, 'void *');
  const target = koffi.decode(table, index * 8, 'void *');
  return koffi.call(target, proto, object, ...args);
}
function release(object) { if (object) method(object, 2, ''); }
function bytes(pointer, length) { return Buffer.from(koffi.decode(pointer, 'uint8_t', length)); }
let comReady = false;
function initializeCom() {
  if (comReady) return;
  const hr = coInitialize(null, 0);
  // Electron can already have initialized COM in a different apartment.
  if (hr !== -2147417850) check(hr, 'Initialize COM');
  comReady = true;
}

function parseWaveFormat(buffer) {
  if (buffer.length < 18) throw new Error('Incomplete Windows audio format.');
  const tag = buffer.readUInt16LE(0);
  const sampleRate = buffer.readUInt32LE(4);
  const channelsPerFrame = buffer.readUInt16LE(2);
  const bitsPerChannel = buffer.readUInt16LE(14);
  let encoding = tag;
  if (tag === 0xfffe) {
    if (buffer.length < 40 || buffer.readUInt16LE(16) < 22) throw new Error('Incomplete extensible audio format.');
    const subtype = buffer.subarray(24, 40);
    if (subtype.equals(guid('00000001-0000-0010-8000-00aa00389b71'))) encoding = 1;
    else if (subtype.equals(guid('00000003-0000-0010-8000-00aa00389b71'))) encoding = 3;
    else throw new Error('The microphone uses an unsupported Windows audio subtype.');
  }
  if (![1, 3].includes(encoding)) throw new Error('The microphone does not expose PCM or IEEE float.');
  const frameBytes = buffer.readUInt16LE(12);
  if (!sampleRate || !channelsPerFrame || frameBytes !== channelsPerFrame * bitsPerChannel / 8) throw new Error('Invalid Windows audio frame alignment.');
  return { sampleRate, channelsPerFrame, bitsPerChannel, isFloat: encoding === 3, frameBytes };
}
function floatPlaybackFormat() {
  const buffer = Buffer.alloc(18);
  buffer.writeUInt16LE(3); buffer.writeUInt16LE(2, 2); buffer.writeUInt32LE(48000, 4);
  buffer.writeUInt32LE(384000, 8); buffer.writeUInt16LE(8, 12); buffer.writeUInt16LE(32, 14);
  return buffer;
}

function deviceName(device, fallback) {
  const store = [null], value = Buffer.alloc(24);
  try {
    check(method(device, 4, 'uint32_t mode, _Out_ void **store', 0, store), 'Read microphone properties');
    const key = Buffer.alloc(20);
    guid('A45C254E-DF1C-4EFD-8020-67D146A850E0').copy(key);
    key.writeUInt32LE(14, 16); // PKEY_Device_FriendlyName
    check(method(store[0], 5, 'const void *key, void *value', key, value), 'Read microphone name');
    return value.readUInt16LE(0) === 31 ? koffi.decode(koffi.decode(value, 8, 'void *'), 'char16_t', -1) : fallback;
  } catch { return fallback; }
  finally { clearVariant(value); release(store[0]); }
}

function configureNonCommunications(client) {
  const newer = [null];
  const queried = method(client, 0, 'const void *iid, _Out_ void **result', guid(ids.client2), newer);
  if (queried < 0) return { category: 'Other', applied: false, hresult: (queried >>> 0).toString(16) };
  try {
    const properties = Buffer.alloc(16);
    properties.writeUInt32LE(16); // cbSize; bIsOffload=false; AudioCategory_Other=0; options=0.
    const hr = method(newer[0], 16, 'const void *properties', properties);
    return { category: 'Other', applied: hr >= 0, hresult: (hr >>> 0).toString(16) };
  } finally { release(newer[0]); }
}

// Enumerating IDs/properties does not activate microphone capture clients.
function listMicrophones() {
  initializeCom();
  const enumerator = [null], collection = [null], defaultDevice = [null], defaultId = [null];
  try {
    check(coCreate(guid(ids.enumeratorClass), null, 23, guid(ids.enumerator), enumerator), 'Enumerate microphones');
    let currentDefault = null;
    if (method(enumerator[0], 4, 'int flow, int role, _Out_ void **device', 1, 0, defaultDevice) >= 0 &&
        method(defaultDevice[0], 5, '_Out_ void **id', defaultId) >= 0) currentDefault = koffi.decode(defaultId[0], 'char16_t', -1);
    check(method(enumerator[0], 3, 'int flow, uint32_t state, _Out_ void **collection', 1, 1, collection), 'List active microphones');
    const count = [0]; check(method(collection[0], 3, '_Out_ uint32_t *count', count), 'Count microphones');
    const result = [];
    for (let index = 0; index < count[0]; index++) {
      const device = [null], id = [null];
      try {
        check(method(collection[0], 4, 'uint32_t index, _Out_ void **device', index, device), 'Read microphone');
        check(method(device[0], 5, '_Out_ void **id', id), 'Read microphone ID');
        const value = koffi.decode(id[0], 'char16_t', -1);
        result.push({ id: value, name: deviceName(device[0], value), isDefault: value === currentDefault });
      } finally { coFree(id[0]); release(device[0]); }
    }
    return result;
  } finally { coFree(defaultId[0]); release(defaultDevice[0]); release(collection[0]); release(enumerator[0]); }
}

// Stable allocations and registered callbacks remain alive until Windows releases
// the completion handler. A timed-out activation lives only until this worker exits.
const pendingHandlers = new Set();
async function activatePlayback(excludedPid) {
  initializeCom();
  let refs = 1;
  let finished = false;
  let resolve, reject;
  const ready = new Promise((yes, no) => { resolve = yes; reject = no; });
  const holder = { callbacks: [], allocations: [] };
  pendingHandlers.add(holder);
  const table = koffi.alloc('void *', 4);
  const object = koffi.alloc('void *', 1);
  holder.allocations.push(table, object);
  koffi.encode(object, 'void *', table);
  function register(signature, callback) {
    let proto = callbackTypes.get(signature);
    if (!proto) { proto = koffi.proto(signature); callbackTypes.set(signature, proto); }
    const pointer = koffi.register(callback, koffi.pointer(proto));
    holder.callbacks.push(pointer);
    return pointer;
  }
  function cleanup() {
    if (refs !== 0 || !finished || holder.cleaned) return;
    holder.cleaned = true;
    pendingHandlers.delete(holder);
    holder.callbacks.forEach(callback => koffi.unregister(callback));
    holder.allocations.forEach(memory => koffi.free(memory));
  }
  const query = register('int32_t ScribeQuery(void *self, const void *iid, void *output)', (_self, iid, output) => {
    const requested = bytes(iid, 16);
    if ([ids.unknown, ids.agile, ids.completion].some(id => requested.equals(guid(id)))) {
      refs++;
      koffi.encode(output, 'void *', object);
      return 0;
    }
    koffi.encode(output, 'void *', null);
    return -2147467262;
  });
  const addRef = register('uint32_t ScribeAddRef(void *self)', () => ++refs);
  const removeRef = register('uint32_t ScribeRelease(void *self)', () => {
    refs--;
    setImmediate(cleanup);
    return refs;
  });
  const complete = register('int32_t ScribeComplete(void *self, void *operation)', (_self, operation) => {
    try {
      const status = [0], client = [null];
      check(method(operation, 3, '_Out_ int32_t *status, _Out_ void **client', status, client), 'Read playback activation');
      check(status[0], 'Activate application playback');
      if (finished) release(client[0]);
      else { finished = true; resolve(client[0]); }
    } catch (error) { if (!finished) { finished = true; reject(error); } }
    return 0;
  });
  koffi.encode(table, koffi.array('void *', 4), [query, addRef, removeRef, complete]);
  const params = koffi.alloc('uint8_t', 12);
  const blob = Buffer.alloc(12); blob.writeUInt32LE(1); blob.writeUInt32LE(excludedPid, 4); blob.writeUInt32LE(1, 8);
  koffi.encode(params, 'uint8_t', [...blob], 12);
  // PROPVARIANT on Windows x64: vt at 0, BLOB at 8 (length, padding, pointer).
  const variant = koffi.alloc('uint8_t', 24);
  const variantHeader = Buffer.alloc(24); variantHeader.writeUInt16LE(65); variantHeader.writeUInt32LE(12, 8);
  koffi.encode(variant, 'uint8_t', [...variantHeader], 24);
  koffi.encode(variant, 16, 'void *', params);
  holder.allocations.push(params, variant);
  const operation = [null];
  let timeout;
  try {
    check(activate('VAD\\Process_Loopback', guid(ids.client), variant, object, operation), 'Request application playback');
    const expired = new Promise((_, no) => { timeout = setTimeout(() => { finished = true; no(new Error('Windows playback activation timed out.')); }, 7000); });
    return await Promise.race([ready, expired]);
  } finally {
    clearTimeout(timeout);
    release(operation[0]);
    refs--;
    setImmediate(cleanup);
  }
}

class WasapiSource extends EventEmitter {
  constructor(name, client, format, device) {
    super();
    this.name = name; this.client = client; this.formatBuffer = format;
    this.metadata = parseWaveFormat(format); this.device = device;
    this.statistics = { packets: 0, bytes: 0, silentPackets: 0, discontinuities: 0, timestampErrors: 0 };
  }
  initialize() {
    this.audioPolicy ??= configureNonCommunications(this.client);
    // Shared native capture. Application loopback requires LOOPBACK and explicit
    // format; the virtual source has no endpoint mix format to query.
    check(method(this.client, 3, 'int shareMode, uint32_t flags, int64_t duration, int64_t periodicity, const void *format, const void *session',
      0, this.name === 'playback' ? 0x20000 : 0, 10000000, 0, this.formatBuffer, null), `Initialize ${this.name}`);
    const capture = [null];
    check(method(this.client, 14, 'const void *iid, _Out_ void **capture', guid(ids.capture), capture), `Get ${this.name} capture service`);
    this.capture = capture[0];
    return this;
  }
  start() {
    check(method(this.client, 10, ''), `Start ${this.name}`);
    this.running = true;
    this.timer = setInterval(() => {
      try { this.poll(); }
      catch (error) { clearInterval(this.timer); this.emit('fault', error); }
    }, 10);
  }
  poll() {
    let packets = 0;
    while (true) {
      const next = [0];
      check(method(this.capture, 5, '_Out_ uint32_t *frames', next), `Read ${this.name} packet size`);
      if (!next[0]) break;
      if (++packets > 2000) throw new Error(`${this.name} capture did not drain within its bounded poll.`);
      const data = [null], frames = [0], flags = [0], position = [0], timestamp = [0];
      check(method(this.capture, 3, '_Out_ void **data, _Out_ uint32_t *frames, _Out_ uint32_t *flags, _Out_ uint64_t *position, _Out_ uint64_t *timestamp',
        data, frames, flags, position, timestamp), `Read ${this.name} packet`);
      try {
        const length = frames[0] * this.metadata.frameBytes;
        if (length > this.metadata.sampleRate * this.metadata.frameBytes * 4) throw new Error('Windows returned an oversized audio packet.');
        const silent = !!(flags[0] & 2);
        const packet = silent ? Buffer.alloc(length, !this.metadata.isFloat && this.metadata.bitsPerChannel === 8 ? 128 : 0) : bytes(data[0], length);
        this.statistics.packets++; this.statistics.bytes += length;
        if (silent) this.statistics.silentPackets++;
        if (flags[0] & 1) this.statistics.discontinuities++;
        if (flags[0] & 4) this.statistics.timestampErrors++;
        this.statistics.firstPackets ??= [];
        if (this.statistics.firstPackets.length < 8) this.statistics.firstPackets.push({ frames: frames[0], position: Number(position[0]), timestamp: Number(timestamp[0]), flags: flags[0] });
        this.emit('packet', packet, { timestamp: Number(timestamp[0]), flags: flags[0], position: Number(position[0]) });
      } finally { check(method(this.capture, 4, 'uint32_t frames', frames[0]), `Release ${this.name} packet`); }
    }
  }
  stop() {
    clearInterval(this.timer);
    if (this.running) {
      this.running = false;
      check(method(this.client, 11, ''), `Stop ${this.name}`);
      this.poll();
    }
  }
  close() {
    try { this.stop(); }
    finally { release(this.capture); release(this.client); this.capture = null; this.client = null; }
  }
}

function openMicrophone(pinnedDeviceId = null) {
  initializeCom();
  const enumerator = [null], device = [null], client = [null], format = [null], deviceId = [null];
  try {
    check(coCreate(guid(ids.enumeratorClass), null, 23, guid(ids.enumerator), enumerator), 'Create Windows device enumerator');
    if (pinnedDeviceId) check(method(enumerator[0], 5, 'const char16_t *id, _Out_ void **device', pinnedDeviceId, device), 'Resolve selected microphone (no default fallback)');
    else check(method(enumerator[0], 4, 'int flow, int role, _Out_ void **device', 1, 0, device), 'Resolve default microphone');
    check(method(device[0], 5, '_Out_ void **id', deviceId), 'Read microphone ID');
    const id = koffi.decode(deviceId[0], 'char16_t', -1);
    check(method(device[0], 3, 'const void *iid, uint32_t context, void *params, _Out_ void **client', guid(ids.client), 23, null, client), 'Activate default microphone');
    const audioPolicy = configureNonCommunications(client[0]);
    check(method(client[0], 8, '_Out_ void **format', format), 'Read native microphone format');
    const header = bytes(format[0], 18);
    const nativeFormat = bytes(format[0], 18 + header.readUInt16LE(16));
    const source = new WasapiSource('microphone', client[0], nativeFormat, { id, name: deviceName(device[0], 'Current Windows default microphone') });
    source.audioPolicy = audioPolicy;
    source.initialize();
    client[0] = null; // Ownership transferred to the source.
    return source;
  } finally {
    coFree(format[0]); coFree(deviceId[0]); release(client[0]); release(device[0]); release(enumerator[0]);
  }
}
async function openPlayback(excludedPid) {
  const client = await activatePlayback(excludedPid);
  const source = new WasapiSource('playback', client, floatPlaybackFormat(), { name: 'Application audio before listening volume', excludedPid });
  try { return source.initialize(); }
  catch (error) { source.close(); throw error; }
}
module.exports = { openMicrophone, openPlayback, activatePlayback, parseWaveFormat, floatPlaybackFormat, guid, release, clock100ns, WasapiSource, listMicrophones, configureNonCommunications };
