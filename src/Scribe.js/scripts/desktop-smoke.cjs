const { _electron: electron } = require('playwright');
const fs = require('node:fs');
const path = require('node:path');
const assert = require('node:assert/strict');
const { spawn } = require('node:child_process');
const root = path.resolve(__dirname, '..');
const testRoot = path.join(root, 'test-results', `desktop-${Date.now()}`);
fs.mkdirSync(testRoot, { recursive: true });
const executablePath = process.env.SCRIBE_EXECUTABLE || require('electron');
const packaged = !!process.env.SCRIBE_EXECUTABLE;
const env = { ...process.env, SCRIBE_TEST_ROOT: testRoot };
delete env.ELECTRON_RUN_AS_NODE;
async function main() {
  const application = await electron.launch({ executablePath, args: [...(packaged ? [] : [root]), '--test-mode'], env, timeout: 20000 });
  const logs = [];
  application.process().stderr.on('data', data => logs.push(data.toString()));
  try {
    const panel = await application.firstWindow();
    await panel.waitForFunction(() => !!window.scribe);
    assert.equal(await application.evaluate(() => globalThis.scribeTest.windows.get('panel').isVisible()), false);
    assert.equal(await application.evaluate(() => globalThis.scribeTest.snapshot().phase), 'idle');
    assert.equal(await panel.evaluate(() => typeof window.require), 'undefined');
    // Load the packaged native addon and initialize only the virtual playback
    // interface. Never call Start or open a microphone in automated desktop QA.
    const nativeFormat = await application.evaluate(() => globalThis.scribeTest.probeNative());
    assert.equal(nativeFormat.sampleRate, 48000);
    await application.evaluate(() => globalThis.scribeTest.showWindow('panel'));
    // Regression: failure reporting on an odd-width/high-DPI window must not
    // reject Electron setPosition or leave the failure panel hidden.
    await application.evaluate(() => {
      const window = globalThis.scribeTest.windows.get('panel');
      const [, height] = window.getSize();
      window.setSize(397, height);
      window.hide();
      globalThis.scribeTest.coordinator.update({ phase: 'failed', error: 'Synthetic startup failure' });
    });
    await panel.getByText('Synthetic startup failure', { exact: true }).waitFor();
    assert.equal(await application.evaluate(() => globalThis.scribeTest.windows.get('panel').isVisible()), true);
    await panel.screenshot({ path: path.join(testRoot, 'panel-idle.png') });
    const settingsOpened = application.waitForEvent('window');
    await panel.getByRole('button', { name: 'Settings', exact: true }).click();
    const settings = await settingsOpened;
    await settings.getByLabel('Recording folder', { exact: true }).waitFor();
    await settings.getByLabel('Recording folder', { exact: true }).fill(path.join(testRoot, 'discarded'));
    await settings.getByRole('button', { name: 'Cancel', exact: true }).click();
    assert.equal(fs.existsSync(path.join(testRoot, 'settings.json')), false);
    const settingsReopened = application.waitForEvent('window');
    await panel.getByRole('button', { name: 'Settings', exact: true }).click();
    const settings2 = await settingsReopened;
    await settings2.getByLabel('Recording folder', { exact: true }).waitFor();
    await settings2.getByLabel('Recording folder', { exact: true }).fill('relative');
    await settings2.getByRole('button', { name: 'Save settings', exact: true }).click();
    await settings2.getByText('Choose a valid absolute recording folder.', { exact: true }).waitFor();
    assert.equal(fs.existsSync(path.join(testRoot, 'settings.json')), false);
    await settings2.getByLabel('Recording folder', { exact: true }).fill(path.join(testRoot, 'sessions'));
    await settings2.evaluate(() => document.getElementById('microphone-device').add(new Option('Test camera', 'synthetic:camera')));
    await settings2.getByLabel('Microphone', { exact: true }).selectOption('synthetic:camera');
    await settings2.screenshot({ path: path.join(testRoot, 'settings.png') });
    await settings2.getByRole('button', { name: 'Save settings', exact: true }).click();
    assert.equal(JSON.parse(fs.readFileSync(path.join(testRoot, 'settings.json'), 'utf8')).microphoneDeviceId, 'synthetic:camera');
    await panel.getByRole('button', { name: 'Start recording', exact: true }).click();
    await panel.getByRole('button', { name: 'Stop recording', exact: true }).waitFor({ timeout: 18000 });
    await panel.screenshot({ path: path.join(testRoot, 'panel-recording.png') });
    const lockedSettingsOpened = application.waitForEvent('window');
    await panel.getByRole('button', { name: 'Settings', exact: true }).click();
    const lockedSettings = await lockedSettingsOpened;
    await lockedSettings.getByText('Stop the recording before changing settings.', { exact: true }).waitFor();
    assert.equal(await lockedSettings.getByRole('button', { name: 'Save settings', exact: true }).isDisabled(), true);
    const blockedSave = await lockedSettings.evaluate(() => window.scribe.save({ recordingFolder: 'C:\\blocked', launchAtLogin: false }));
    assert.equal(blockedSave.ok, false);
    await lockedSettings.getByRole('button', { name: 'Cancel', exact: true }).click();
    await panel.keyboard.press('Escape');
    assert.equal(await application.evaluate(() => globalThis.scribeTest.windows.get('panel').isVisible()), false);
    assert.equal(await application.evaluate(() => globalThis.scribeTest.snapshot().phase), 'recording');
    const second = spawn(executablePath, [...(packaged ? [] : [root]), '--test-mode'], { env, windowsHide: true, stdio: 'ignore' });
    await new Promise((resolve, reject) => {
      const timeout = setTimeout(() => { second.kill(); reject(new Error('Second-instance launch timed out.')); }, 10000);
      second.on('exit', () => { clearTimeout(timeout); resolve(); });
      second.on('error', error => { clearTimeout(timeout); reject(error); });
    });
    await application.evaluate(async () => {
      const deadline = Date.now() + 5000;
      while (!globalThis.scribeTest.windows.get('panel').isVisible()) {
        if (Date.now() > deadline) throw new Error('Second launch did not reopen the recording panel.');
        await new Promise(resolve => setTimeout(resolve, 50));
      }
    });
    const detailsOpened = application.waitForEvent('window');
    await panel.getByRole('button', { name: 'Details', exact: true }).click();
    const details = await detailsOpened;
    await details.getByRole('heading', { name: 'Recording details' }).waitFor();
    await details.screenshot({ path: path.join(testRoot, 'details.png') });
    await panel.getByRole('button', { name: 'Stop recording', exact: true }).click();
    await panel.getByText('Recording saved', { exact: true }).waitFor({ timeout: 18000 });
    const state = await application.evaluate(() => globalThis.scribeTest.snapshot());
    assert.equal(state.lastResult.completed, true, JSON.stringify(state));
    assert.equal(fs.existsSync(path.join(state.directory, 'microphone.mp3')), true);
    assert.equal(fs.existsSync(path.join(state.directory, 'playback.mp3')), true);
    assert.equal(fs.existsSync(path.join(state.directory, 'recording.mp3')), true);
    await panel.getByText('recording.mp3 is ready in your folder.', { exact: true }).waitFor();
    // Exit while actively capturing exercises finalization from the tray lifecycle.
    await panel.getByRole('button', { name: 'Start recording', exact: true }).click();
    await panel.getByRole('button', { name: 'Stop recording', exact: true }).waitFor({ timeout: 18000 });
    const finalDirectory = await application.evaluate(() => globalThis.scribeTest.snapshot().directory);
    await application.evaluate(() => { void globalThis.scribeTest.exit(); });
    await application.waitForEvent('close', { timeout: 20000 });
    const journal = fs.readFileSync(path.join(finalDirectory, 'events.jsonl'), 'utf8');
    assert.ok(journal.includes('"kind":"completed"'));
    assert.equal(journal.trim().split('\n').map(JSON.parse).filter(event => event.kind === 'final-track').length, 3);
    assert.ok(!logs.join('').includes('UnhandledPromiseRejection'), logs.join(''));
    console.log(`Desktop checks passed (${packaged ? 'portable build' : 'development'}). Screenshots: ${testRoot}`);
  } catch (error) {
    fs.writeFileSync(path.join(testRoot, 'stderr.txt'), logs.join(''));
    console.error(`Desktop evidence: ${testRoot}`);
    throw error;
  } finally { await application.close().catch(() => {}); }
}
main().catch(error => { console.error(error); process.exitCode = 1; });
