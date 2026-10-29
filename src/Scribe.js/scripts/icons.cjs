// Small code-native icons: no remote assets or image-generation dependency.
const fs = require('node:fs');
const path = require('node:path');
const zlib = require('node:zlib');
const root = path.join(__dirname, '../assets');
fs.mkdirSync(root, { recursive: true });
function crc32(data) {
  let crc = 0xffffffff;
  for (const byte of data) {
    crc ^= byte;
    for (let bit = 0; bit < 8; bit++) crc = (crc >>> 1) ^ (0xedb88320 & -(crc & 1));
  }
  return (crc ^ 0xffffffff) >>> 0;
}
function chunk(kind, data) {
  const type = Buffer.from(kind);
  const size = Buffer.alloc(4); size.writeUInt32BE(data.length);
  const crc = Buffer.alloc(4); crc.writeUInt32BE(crc32(Buffer.concat([type, data])));
  return Buffer.concat([size, type, data, crc]);
}
function png(size, pixel) {
  const rows = Buffer.alloc(size * (size * 4 + 1));
  for (let y = 0; y < size; y++) for (let x = 0; x < size; x++) {
    const offset = y * (size * 4 + 1) + 1 + x * 4;
    const color = pixel(x / size, y / size);
    color.forEach((value, index) => { rows[offset + index] = value; });
  }
  const ihdr = Buffer.alloc(13); ihdr.writeUInt32BE(size); ihdr.writeUInt32BE(size, 4); ihdr[8] = 8; ihdr[9] = 6;
  return Buffer.concat([Buffer.from('89504e470d0a1a0a', 'hex'), chunk('IHDR', ihdr), chunk('IDAT', zlib.deflateSync(rows)), chunk('IEND', Buffer.alloc(0))]);
}
for (const recording of [false, true]) {
  const image = png(32, (x, y) => {
    const visible = recording ? (x - .5) ** 2 + (y - .5) ** 2 < .15 : x > .2 && x < .8 && y > .2 && y < .8;
    return visible ? recording ? [255, 80, 97, 255] : [160, 171, 190, 255] : [0, 0, 0, 0];
  });
  fs.writeFileSync(path.join(root, recording ? 'tray-recording.png' : 'tray-idle.png'), image);
}
const appIcon = png(256, (x, y) => {
  const outside = x < .05 || x > .95 || y < .05 || y > .95;
  const bar = [[.26, .35, .65], [.42, .23, .77], [.58, .32, .68], [.74, .42, .58]].some(([center, top, bottom]) => Math.abs(x - center) < .035 && y > top && y < bottom);
  return outside ? [0, 0, 0, 0] : bar ? [239, 245, 255, 255] : [63, 111, 231, 255];
});
fs.writeFileSync(path.join(root, 'app.png'), appIcon);
const header = Buffer.alloc(22); header.writeUInt16LE(1, 2); header.writeUInt16LE(1, 4);
header.writeUInt16LE(1, 10); header.writeUInt16LE(32, 12); header.writeUInt32LE(appIcon.length, 14); header.writeUInt32LE(22, 18);
fs.writeFileSync(path.join(root, 'app.ico'), Buffer.concat([header, appIcon]));
