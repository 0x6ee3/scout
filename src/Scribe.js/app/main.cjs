const { app, BrowserWindow, ipcMain, Tray, Menu, nativeImage, screen, shell, dialog, utilityProcess, powerMonitor, nativeTheme } = require('electron');
const path = require('node:path');
const fs = require('node:fs');
const { pathToFileURL } = require('node:url');
const { Preferences } = require('./preferences.cjs');
const { Coordinator } = require('./coordinator.cjs');
const { panelPosition } = require('./window-position.cjs');

app.setName('Scribe.js');
app.setAppUserModelId('Scout.ScribeJS');
const testMode = process.argv.includes('--test-mode');
const testRoot = process.env.SCRIBE_TEST_ROOT;
if (testMode) {
  if (!testRoot || !path.isAbsolute(testRoot)) throw new Error('Desktop tests require an absolute SCRIBE_TEST_ROOT.');
  app.setPath('userData', path.join(testRoot, 'user-data'));
}
const locked = app.requestSingleInstanceLock();
if (!locked) app.quit();
let tray, coordinator, preferences;
let quitting = false;
let exitPromise;
const windows = new Map();
const uiFile = path.join(__dirname, '../ui/index.html');
const uiURL = pathToFileURL(uiFile).href;
const asset = name => path.join(__dirname, '../assets', name);

function snapshot() {
  return { ...coordinator.snapshot(), version: app.getVersion(), synthetic: testMode };
}
function createWindow(view) {
  const sizes = { panel: [396, 438], settings: [580, 600], details: [760, 560] };
  const [width, height] = sizes[view];
  const window = new BrowserWindow({ width, height, minWidth: view === 'details' ? 560 : width,
    minHeight: height, show: false, resizable: view === 'details', maximizable: view === 'details',
    minimizable: view !== 'panel', skipTaskbar: view === 'panel', alwaysOnTop: view === 'panel',
    title: view === 'panel' ? 'Scribe.js' : `${view === 'settings' ? 'Settings' : 'Recording details'} — Scribe.js`,
    backgroundColor: '#15181e', icon: asset('app.png'), autoHideMenuBar: true,
    webPreferences: { preload: path.join(__dirname, 'preload.cjs'), sandbox: true, contextIsolation: true,
      nodeIntegration: false, backgroundThrottling: false, spellcheck: false } });
  window.setMenu(null);
  window.webContents.setWindowOpenHandler(() => ({ action: 'deny' }));
  window.webContents.on('will-navigate', event => event.preventDefault());
  window.webContents.on('will-attach-webview', event => event.preventDefault());
  window.on('close', event => {
    if (quitting) return;
    if (view === 'panel' || view === 'details') { event.preventDefault(); window.hide(); }
  });
  window.on('closed', () => windows.delete(view));
  window.loadFile(uiFile, { query: { view } });
  windows.set(view, window);
  return window;
}
function showWindow(view) {
  return openWindow(view).catch(error => {
    console.error(`Could not open ${view}: ${error.message}`);
    coordinator?.log({ utc: new Date().toISOString(), kind: 'window-error', detail: { view, error: error.message } });
  });
}
async function openWindow(view) {
  let window = windows.get(view);
  if (!window || window.isDestroyed()) window = createWindow(view);
  if (window.webContents.isLoading()) await new Promise(resolve => window.webContents.once('did-finish-load', resolve));
  if (view === 'panel') {
    const cursor = screen.getCursorScreenPoint();
    const area = screen.getDisplayNearestPoint(cursor).workArea;
    const [width, height] = window.getSize();
    window.setPosition(...panelPosition(cursor, area, width, height));
  }
  if (window.isMinimized()) window.restore();
  if (view === 'settings') window.webContents.send('scribe:settings-open');
  window.show();
  window.focus();
}
function loginOptions() {
  return { name: 'Scribe.js', path: process.execPath,
    args: app.isPackaged ? ['--login'] : [path.resolve(__dirname, '..'), '--login'] };
}
function readLogin() {
  if (testMode) return { enabled: false, error: null };
  try { return { enabled: app.getLoginItemSettings(loginOptions()).openAtLogin, error: null }; }
  catch (error) { return { enabled: false, error: error.message }; }
}
function refresh() {
  const state = snapshot();
  for (const window of windows.values()) {
    if (!window.isDestroyed()) window.webContents.send('scribe:state', state);
  }
  tray.setImage(nativeImage.createFromPath(asset(state.phase === 'recording' ? 'tray-recording.png' : 'tray-idle.png')));
  tray.setToolTip(`Scribe.js — ${state.phase === 'recording' ? 'Recording microphone and playback' : state.phase}`);
  tray.setContextMenu(Menu.buildFromTemplate([
    { label: 'Show recording', click: () => void showWindow('panel') },
    { label: 'Start recording', enabled: !state.busy && !preferences.error, click: () => runAction('start') },
    { label: state.phase === 'finalizing' ? 'Finalizing…' : 'Stop recording', enabled: ['starting', 'recording'].includes(state.phase), click: () => runAction('stop') },
    { type: 'separator' },
    { label: 'Open output folder', click: () => runAction('folder') },
    { label: 'Settings…', click: () => void showWindow('settings') },
    { label: 'Details…', click: () => void showWindow('details') },
    { type: 'separator' },
    { label: state.busy ? 'Stop recording and exit' : 'Exit', click: () => void exit() }
  ]));
}
async function runAction(action) {
  try {
    if (action === 'start') {
      if (windows.get('settings')?.isVisible()) throw new Error('Close Settings before starting a recording.');
      coordinator.start();
    } else if (action === 'stop') await coordinator.stop();
    else if (action === 'folder') {
      const target = coordinator.state.directory || preferences.value.recordingFolder;
      fs.mkdirSync(target, { recursive: true });
      const error = await shell.openPath(target);
      if (error) throw new Error(error);
    } else if (['panel', 'settings', 'details'].includes(action)) await showWindow(action);
    else throw new Error('Unknown action.');
    return { ok: true };
  } catch (error) {
    coordinator.log({ utc: new Date().toISOString(), kind: 'notice', detail: { error: error.message } });
    void showWindow('panel');
    return { ok: false, error: error.message };
  }
}
async function exit() {
  if (exitPromise) return exitPromise;
  exitPromise = (async () => {
    await coordinator?.stop();
    quitting = true;
    tray?.destroy();
    for (const window of windows.values()) window.destroy();
    app.quit();
  })();
  return exitPromise;
}
function trusted(event) {
  return [...windows.values()].some(window => !window.isDestroyed() && event.sender === window.webContents &&
    event.senderFrame === window.webContents.mainFrame && event.senderFrame.url.startsWith(`${uiURL}?`));
}
function handle(channel, handler, requiredView) {
  ipcMain.handle(`scribe:${channel}`, async (event, value) => {
    if (!trusted(event) || (requiredView && windows.get(requiredView)?.webContents !== event.sender)) throw new Error('Untrusted application frame.');
    return handler(event, value);
  });
}

if (locked) app.whenReady().then(() => {
  nativeTheme.themeSource = 'dark';
  const storage = testMode ? testRoot : path.join(app.getPath('appData'), 'Scribe.js');
  preferences = new Preferences(path.join(storage, 'settings.json'), testMode ? path.join(testRoot, 'recordings') : path.join(app.getPath('documents'), 'Scribe.js', 'recordings'));
  coordinator = new Coordinator({ preferences, synthetic: testMode, createWorker: () => {
    const worker = utilityProcess.fork(path.join(__dirname, 'audio-worker.cjs'), [], { serviceName: 'Scribe.js audio', stdio: 'pipe' });
    worker.stderr?.on('data', data => coordinator.log({ utc: new Date().toISOString(), kind: 'worker-diagnostic', detail: data.toString().slice(-2000) }));
    return worker;
  } });
  const panel = createWindow('panel');
  const session = panel.webContents.session;
  session.setPermissionCheckHandler(() => false);
  session.setPermissionRequestHandler((_contents, _permission, callback) => callback(false));
  session.webRequest.onBeforeRequest((details, callback) => callback({ cancel: !details.url.startsWith('file:') && !details.url.startsWith('devtools:') }));
  tray = new Tray(nativeImage.createFromPath(asset('tray-idle.png')));
  tray.on('click', () => void showWindow('panel'));
  coordinator.on('change', state => {
    refresh();
    if (state.phase === 'failed' && !windows.get('settings')?.isVisible()) void showWindow('panel');
  });
  refresh();
  handle('state', () => snapshot());
  handle('action', (_event, value) => runAction(value));
  handle('hide', event => BrowserWindow.fromWebContents(event.sender).close());
  handle('preferences', () => {
    let microphones = [], microphoneError = null;
    try { microphones = require('./wasapi.cjs').listMicrophones(); }
    catch (error) { microphoneError = error.message; }
    return { ...preferences.value, ...readLogin(), microphones, microphoneError };
  }, 'settings');
  handle('browse', async event => {
    if (coordinator.busy) throw new Error('Stop recording before changing the folder.');
    const result = await dialog.showOpenDialog(BrowserWindow.fromWebContents(event.sender), {
      title: 'Choose recording folder', defaultPath: preferences.value.recordingFolder,
      properties: ['openDirectory', 'createDirectory'] });
    return result.canceled ? null : result.filePaths[0];
  }, 'settings');
  handle('save', (_event, value) => {
    if (coordinator.busy) return { ok: false, error: 'Stop recording before changing settings.' };
    try {
      if (!value || typeof value.launchAtLogin !== 'boolean') throw new Error('Invalid settings.');
      preferences.save(value.recordingFolder, value.microphoneDeviceId);
      let warning = null;
      if (!testMode) {
        try {
          const previous = readLogin();
          if (previous.error) throw new Error(previous.error);
          if (previous.enabled !== value.launchAtLogin) {
            app.setLoginItemSettings({ ...loginOptions(), openAtLogin: value.launchAtLogin });
            if (readLogin().enabled !== value.launchAtLogin) throw new Error('Windows did not apply the launch-at-sign-in preference.');
          }
        } catch (error) { warning = `Recording folder saved. Sign-in preference could not be applied: ${error.message}`; }
      }
      coordinator.update({ error: null });
      return { ok: true, warning };
    } catch (error) { return { ok: false, error: error.message }; }
  }, 'settings');
  powerMonitor.on('suspend', () => { if (coordinator.busy) void coordinator.stop(); });
  app.on('second-instance', (_event, commandLine) => { if (!commandLine.includes('--login')) void showWindow('panel'); });
  app.on('activate', () => void showWindow('panel'));
  if (testMode) globalThis.scribeTest = { snapshot, showWindow, coordinator, preferences, windows, exit,
    probeNative: () => new Promise((resolve, reject) => {
      const worker = utilityProcess.fork(path.join(__dirname, 'audio-worker.cjs'), [], { serviceName: 'Scribe.js native verification' });
      const timeout = setTimeout(() => { worker.kill(); reject(new Error('Native verification timed out.')); }, 10000);
      worker.on('message', message => {
        if (message.type !== 'probe-result') return;
        clearTimeout(timeout);
        if (message.error) reject(new Error(message.error));
        else resolve(message.metadata);
      });
      worker.on('exit', code => { clearTimeout(timeout); if (code !== 0) reject(new Error(`Native verification exited (${code}).`)); });
      worker.postMessage({ type: 'probe-native', excludedPid: process.pid });
    }) };
  if (preferences.error) {
    coordinator.log({ utc: new Date().toISOString(), kind: 'settings-error', detail: preferences.error });
  }
}).catch(error => { console.error(error); app.exit(1); });
app.on('window-all-closed', () => {});
app.on('before-quit', event => { if (!quitting) { event.preventDefault(); void exit(); } });
