const fs = require('node:fs');
const path = require('node:path');
const { randomUUID } = require('node:crypto');

function validateFolder(value) {
  if (typeof value !== 'string' || !value.trim() || !path.isAbsolute(value.trim()) ||
      /[\x00-\x1f<>"|?*]/.test(value) || (process.platform === 'win32' && value.slice(2).includes(':'))) {
    throw new Error('Choose a valid absolute recording folder.');
  }
  return path.resolve(value.trim());
}

function validateMicrophone(value) {
  if (value === null || value === undefined || value === '') return null;
  if (typeof value !== 'string' || value.length > 1024 || /[\x00-\x1f]/.test(value)) throw new Error('Choose an available microphone.');
  return value;
}
class Preferences {
  constructor(file, defaultFolder) {
    this.file = file;
    this.value = { recordingFolder: defaultFolder, microphoneDeviceId: null };
    this.error = null;
    try {
      const saved = JSON.parse(fs.readFileSync(file, 'utf8'));
      this.value.recordingFolder = validateFolder(saved.recordingFolder);
      this.value.microphoneDeviceId = validateMicrophone(saved.microphoneDeviceId);
    } catch (error) {
      if (error.code !== 'ENOENT') this.error = `Settings need attention: ${error.message}`;
    }
  }

  save(folder, microphone = this.value.microphoneDeviceId) {
    const recordingFolder = validateFolder(folder);
    const microphoneDeviceId = validateMicrophone(microphone);
    // Test actual write access before accepting a preference. Never move old recordings.
    fs.mkdirSync(recordingFolder, { recursive: true });
    const probe = path.join(recordingFolder, `.scribe-write-${randomUUID()}`);
    const fd = fs.openSync(probe, 'wx');
    fs.closeSync(fd);
    fs.unlinkSync(probe);
    fs.mkdirSync(path.dirname(this.file), { recursive: true });
    const temp = `${this.file}.${randomUUID()}.tmp`;
    try {
      const output = fs.openSync(temp, 'wx');
      try {
        fs.writeFileSync(output, `${JSON.stringify({ recordingFolder, microphoneDeviceId }, null, 2)}\n`);
        fs.fsyncSync(output);
      } finally { fs.closeSync(output); }
      fs.renameSync(temp, this.file);
    } finally { if (fs.existsSync(temp)) fs.unlinkSync(temp); }
    this.value = { recordingFolder, microphoneDeviceId };
    this.error = null;
    return this.value;
  }
}
module.exports = { Preferences, validateFolder };
