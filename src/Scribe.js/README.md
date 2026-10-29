# Scribe.js

An experimental Electron implementation of Scout.Scribe's current recording product.
The application, tray, windows, lifecycle, storage and audio pipeline are implemented
in JavaScript. It does not launch, reference or copy the .NET Scribe application.
Requires **Windows 11 x64**. The portable build includes its runtime; no Node.js,
.NET, FFmpeg installation or compiler is needed to run it.

## Launch

Double-click **[Start Scribe.cmd](Start%20Scribe.cmd)** in this folder, or run
`dist\Scribe.js-win32-x64\Scribe.js.exe`. Keep that entire build folder together.
Launch is quiet: look for the gray square in the Windows tray/overflow area.
Left-click it to show the recording panel. Right-click it for recording, folder,
Settings, Details and Exit. A second interactive launch reopens the existing panel.

**Start recording** is the only operation that starts audio capture. Closing the
panel or pressing Escape hides it while recording continues. Stop finalizes both
MP3 files, including the combined recording. Exit during capture stops and finalizes before quitting; the menu labels
that action explicitly. The tray changes to a red circle during recording. Sleep
requests a graceful stop and never automatically resumes recording.

## Settings and output

Settings is a separate window with Save/Cancel. The default output root is
`<Documents>\Scribe.js\recordings`, deliberately separate from the original Scribe
experiment. Preferences live in `%APPDATA%\Scribe.js\settings.json`; Electron's
profile is also isolated under Scribe.js. Changing the folder affects future
sessions and never moves files. Settings cannot be saved during capture/finalization.
Corrupt saved settings block Start until corrected through Save.

The **Microphone** setting can pin a specific input, such as the C920 camera.
Scribe then opens that exact endpoint and reports an error if it is unavailable;
it never silently substitutes a headset microphone. The Windows-default option
remains available. Mic streams explicitly request the non-communications `Other`
category. This app cannot prevent another application from using the headset's mic
or triggering Bluetooth hands-free mode.

Launch at Windows sign-in is opt-in and is only changed by Save. It uses Electron's
current-user login-item API with its own `Scribe.js` registration. Sign-in startup
is idle and quiet. Refresh that preference after moving the portable folder. If
Windows prevents updating startup registration, the folder is still saved and
Settings reports the startup failure. Automated checks never change this registry
preference.

Every session creates a unique timestamped directory containing:

- **`recording.mp3`**: the combined microphone and PC audio, ready to listen to or
  import as a single recording.
- `microphone.mp3`: the selected microphone, or the Windows default resolved again
  on each Start when that option is selected.
- `playback.mp3`: all application playback across output devices, excluding the
  Electron application's complete process tree, before listening-volume attenuation.
- `events.jsonl`: source IDs/native formats, environment, recording/finalization
  events, five-second checkpoint byte counts, flush times and capture statistics.

All three MP3s are **mono, 48 kHz, 256 kbps**. The combined file is encoded live from
the same aligned PCM as the source files, without decoding/re-encoding those MP3s.
For the mix, each source's channels are averaged to mono, then microphone and PC
audio each contribute 50%. This fixed headroom avoids a stereo summation boost or
doubling the level when both sources are loud. There is no AGC, dynamic normalization
or inverse-volume gain. The separate files retain their existing conversion path
for diagnostics/recovery. The microphone captures its native
PCM/float format and retains that device until Stop. Playback explicitly requests
48 kHz stereo float from Windows' virtual application-loopback source. Stop and
Start after changing the default microphone. Protected audio may not be capturable.

Details contains the current/last session path and bounded diagnostics. Capture or
encoding failures show the panel, stop both tracks, and retain partial output.
An unexpected worker exit also reports failure. New sessions never overwrite old
files, and there is no automatic cleanup or crash recovery rewriting existing data.

## Develop and verify

Use Node.js 24+ and npm, with network access for dependency/runtime downloads:

```powershell
cd src/Scribe.js
npm ci
npm start
```

Electron 45 downloads its binary on first use when needed. npm install-script
permissions are pinned in `package.json` for FFmpeg and Koffi. A fresh terminal may
be needed after installing Node.js; in an older PowerShell session this is enough:

```powershell
$env:Path = 'C:\Program Files\nodejs;' + $env:Path
```

```powershell
npm run check
npm test
npm run test:desktop
npm run package
$env:SCRIBE_EXECUTABLE = (Resolve-Path '.\dist\Scribe.js-win32-x64\Scribe.js.exe').Path
npm run test:desktop
Remove-Item Env:SCRIBE_EXECUTABLE
```

`npm test` uses real streaming MP3 encoders, decodes and checks separate tones,
tests forced termination, settings and lifecycle, and initializes the Windows
virtual playback interface **without starting capture**. Desktop checks use
Playwright to drive the real Electron windows/tray lifecycle with generated audio;
they write screenshots and sessions only under ignored `test-results`. They never
open a physical microphone, play sound or update sign-in registration. Desktop/IPC
and file-rename checks need an ordinary desktop shell; the Codex restricted sandbox
may deny them. Such failures are not passing checks.

Generate a longer device-free session with:

```powershell
npm run synthetic -- --seconds 60
```

The portable build is unsigned, is intended for local experimentation, and includes
native Koffi and FFmpeg binaries outside an ASAR archive. No installer, updater,
server, telemetry, transcription, extraction or Web integration is included.
The original Scribe's later planned transcription/extraction milestones are not
implemented features and were not part of this rebuild.

## Architecture and acceptance

- `app/main.cjs`: tray, isolated windows, restricted IPC, login integration and exit.
- `app/coordinator.cjs`: session state machine and bounded worker deadlines.
- `app/audio-worker.cjs`: Electron utility-process crash boundary.
- `app/wasapi.cjs`: direct Windows COM/WASAPI bindings through Koffi, preserving native
  microphone packets, silence flags and source timestamps. No C++ build or .NET bridge.
- `app/pcm-clock.cjs`: bounded queues aligned once with the system clock, then
  appended by actual packet frame counts. Timestamp jitter, hardware counters in
  different units and virtual sources reporting zero cannot cut samples or stop
  recording. Flagged discontinuities with corroborating timing preserve real gaps.
- `app/audio-session.cjs` and `encoder.cjs`: session journal, two independent source
  encoders plus a two-input streaming FFmpeg mixer, and five-second `fsync`
  checkpoints on all three outputs. Streaming MP3 frames do not require
  a final seek-table/header rewrite to be decoded after a crash.
- `ui/`: local HTML/CSS/JavaScript, sandboxed renderers, context isolation, restrictive
  CSP, no Node access, remote navigation, downloads or renderer media permissions.

See the [verification and parity report](VERIFICATION.md), [dependency notices](THIRD_PARTY.md)
and [architecture decision](../../docs/engineering/decisions/0010-scribe-js-electron-experiment.md).
The combined recording decision is recorded in [ADR 0012](../../docs/engineering/decisions/0012-scribe-js-live-merged-recording.md).
Live microphone/call capture, listening-volume/mute changes, Bluetooth routing,
long physical-device sessions and Windows sign-in still require hardware acceptance.
Successful initialization and synthetic checks do not establish those results.
