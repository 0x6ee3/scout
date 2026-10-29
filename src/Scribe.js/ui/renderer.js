/* The preload exposes only bounded application operations; there is no Node here. */
const $ = id => document.getElementById(id);
const view = new URLSearchParams(location.search).get('view') || 'panel';
$(view === 'panel' ? 'panel' : `${view}-view`).hidden = false;
let state;
let actionPending = false;
let settingsSaving = false;
let settingsLoading = false;
let actionError = null;
const labels = { idle: 'Ready to record', starting: 'Opening audio sources…', recording: 'Recording', finalizing: 'Finalizing your files…', saved: 'Recording saved', failed: 'Recording needs attention' };

function duration(ms) {
  const total = Math.floor(Math.max(0, ms) / 1000);
  return [Math.floor(total / 3600), Math.floor(total / 60) % 60, total % 60].map(n => String(n).padStart(2, '0')).join(':');
}
let finalDuration = 0;
function render(value) {
  const wasBusy = state?.busy;
  state = value;
  document.body.dataset.phase = state.phase;
  if (wasBusy && !state.busy) finalDuration = state.startedAt ? Date.now() - state.startedAt : 0;
  if (state.phase === 'starting') finalDuration = 0;
  if (view === 'panel') {
    $('phase').textContent = labels[state.phase];
    $('description').textContent = state.phase === 'saved' ? 'recording.mp3 is ready in your folder.' : state.phase === 'recording' ? 'Recording microphone and PC audio together.' : state.phase === 'failed' ? 'Open Details for diagnostics and retained files.' : 'Microphone and PC audio, together.';
    $('record-label').textContent = state.phase === 'recording' ? 'Stop recording' : state.phase === 'starting' ? 'Starting…' : state.phase === 'finalizing' ? 'Finalizing…' : 'Start recording';
    $('record').disabled = actionPending || ['starting', 'finalizing'].includes(state.phase) || !!state.settingsError;
    const error = actionError || state.settingsError || state.error;
    $('panel-error').hidden = !error;
    $('panel-error').textContent = error || '';
    $('test-banner').hidden = !state.synthetic;
    updateTimer();
  } else if (view === 'details') {
    $('details-summary').textContent = labels[state.phase];
    $('session-directory').textContent = state.directory || state.recordingFolder;
    $('version').textContent = `Scribe.js ${state.version}`;
    const log = $('events');
    const atBottom = log.scrollTop + log.clientHeight >= log.scrollHeight - 30;
    log.textContent = state.events.length ? state.events.map(event => `${event.utc.slice(11, 19)}  ${event.kind}\n${JSON.stringify(event.detail, null, 2)}`).join('\n\n') : 'Start a recording to see capture and checkpoint details.';
    if (atBottom) log.scrollTop = log.scrollHeight;
  } else {
    ['recording-folder', 'microphone-device', 'browse', 'save-settings'].forEach(id => { $(id).disabled = state.busy || settingsSaving || settingsLoading; });
    $('launch-login').disabled = state.busy || settingsSaving || settingsLoading || $('launch-login').dataset.unavailable === 'true';
    $('settings-locked').hidden = !state.busy;
  }
}
function updateTimer() {
  if (view !== 'panel' || !state) return;
  $('elapsed').textContent = duration(state.busy && state.startedAt ? Date.now() - state.startedAt : finalDuration);
}
function showMessage(id, text) { $(id).textContent = text || ''; $(id).hidden = !text; }
async function action(name) {
  try {
    const result = await window.scribe.action(name);
    actionError = result?.error || null;
  } catch (error) { actionError = error.message; }
  if (state) render(await window.scribe.state());
}
if (view === 'panel') {
  $('record').addEventListener('click', async () => {
    if (actionPending) return;
    actionPending = true;
    render(state);
    await action(state.phase === 'recording' ? 'stop' : 'start');
    actionPending = false;
    render(await window.scribe.state());
  });
  for (const name of ['folder', 'details', 'settings']) $(name).addEventListener('click', () => action(name));
  setInterval(updateTimer, 250);
} else if (view === 'details') {
  $('details-folder').addEventListener('click', () => action('folder'));
} else {
  async function loadSettings() {
    settingsLoading = true;
    if (state) render(state);
    try {
      const preferences = await window.scribe.preferences();
      $('recording-folder').value = preferences.recordingFolder;
      const microphone = $('microphone-device');
      microphone.replaceChildren(new Option('Windows default microphone', ''));
      for (const device of preferences.microphones || []) microphone.add(new Option(device.name + (device.isDefault ? ' (Windows default)' : ''), device.id));
      if (preferences.microphoneDeviceId && ![...microphone.options].some(option => option.value === preferences.microphoneDeviceId)) microphone.add(new Option('Saved microphone (currently unavailable)', preferences.microphoneDeviceId));
      microphone.value = preferences.microphoneDeviceId || '';
      $('launch-login').checked = preferences.enabled;
      $('launch-login').dataset.unavailable = String(!!preferences.error);
      showMessage('settings-message', preferences.microphoneError || (preferences.error ? `Sign-in preference is unavailable: ${preferences.error}` : null));
    } catch (error) { showMessage('settings-message', error.message); }
    settingsLoading = false;
    if (state) render(state);
  }
  window.scribe.onSettingsOpen(loadSettings);
  $('browse').addEventListener('click', async () => {
    try { const folder = await window.scribe.browse(); if (folder) $('recording-folder').value = folder; }
    catch (error) { showMessage('settings-message', error.message); }
  });
  $('cancel-settings').addEventListener('click', () => window.scribe.hide());
  $('settings-form').addEventListener('submit', async event => {
    event.preventDefault();
    if (settingsSaving || state.busy) return;
    settingsSaving = true;
    render(state);
    try {
      const result = await window.scribe.save({ recordingFolder: $('recording-folder').value, microphoneDeviceId: $('microphone-device').value || null, launchAtLogin: $('launch-login').checked });
      if (result.ok && !result.warning) await window.scribe.hide();
      else showMessage('settings-message', result.error || result.warning);
    } catch (error) { showMessage('settings-message', error.message); }
    settingsSaving = false;
    render(await window.scribe.state());
  });
  void loadSettings();
}
document.addEventListener('keydown', event => {
  if (event.key === 'Escape' && !settingsSaving) { event.preventDefault(); void window.scribe.hide(); }
});
window.scribe.onState(render);
window.scribe.state().then(render);
