const test = require('node:test');
const assert = require('node:assert/strict');
const fs = require('node:fs');
const os = require('node:os');
const path = require('node:path');
const { Preferences, validateFolder } = require('../app/preferences.cjs');

test('settings use defaults, save atomically, reload and preserve existing recordings', t => {
  const root = fs.mkdtempSync(path.join(os.tmpdir(), 'scribe-js-preferences-'));
  t.after(() => fs.rmSync(root, { recursive: true, force: true }));
  const file = path.join(root, 'config/settings.json');
  const folder = path.join(root, 'recordings');
  const preferences = new Preferences(file, folder);
  assert.equal(preferences.error, null);
  preferences.save(folder);
  fs.writeFileSync(path.join(folder, 'existing.mp3'), 'keep');
  preferences.save(path.join(root, 'next'));
  assert.equal(fs.readFileSync(path.join(folder, 'existing.mp3'), 'utf8'), 'keep');
  assert.equal(new Preferences(file, folder).value.recordingFolder, path.join(root, 'next'));
  assert.deepEqual(fs.readdirSync(path.dirname(file)), ['settings.json']);
});
test('corrupt settings are retained and block capture until explicitly corrected', t => {
  const root = fs.mkdtempSync(path.join(os.tmpdir(), 'scribe-js-invalid-'));
  t.after(() => fs.rmSync(root, { recursive: true, force: true }));
  const file = path.join(root, 'settings.json');
  fs.writeFileSync(file, '{broken');
  const preferences = new Preferences(file, root);
  assert.match(preferences.error, /Settings need attention/);
  assert.equal(fs.readFileSync(file, 'utf8'), '{broken');
  preferences.save(path.join(root, 'audio'));
  assert.equal(preferences.error, null);
});
test('rejects relative paths, empty folders, controls and invalid Windows syntax', () => {
  for (const value of ['', 'relative', null, 'C:\\bad\u0000folder', 'C:\\bad?folder', 'C:\\audio:stream']) assert.throws(() => validateFolder(value));
});
test('a selected microphone is persisted explicitly; legacy settings retain the Windows-default option', t => {
  const root = fs.mkdtempSync(path.join(os.tmpdir(), 'scribe-js-microphone-'));
  t.after(() => fs.rmSync(root, { recursive: true, force: true }));
  const file = path.join(root, 'settings.json');
  fs.writeFileSync(file, JSON.stringify({ recordingFolder: root }));
  const preferences = new Preferences(file, root);
  assert.equal(preferences.value.microphoneDeviceId, null);
  preferences.save(root, 'test:camera');
  assert.equal(new Preferences(file, root).value.microphoneDeviceId, 'test:camera');
  preferences.save(root);
  assert.equal(new Preferences(file, root).value.microphoneDeviceId, 'test:camera');
  preferences.save(root, null);
  assert.equal(new Preferences(file, root).value.microphoneDeviceId, null);
});
