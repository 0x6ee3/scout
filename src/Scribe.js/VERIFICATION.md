# Scribe.js experiment evidence

Date: 22 October 2026. This is a working independent experiment; physical-device
acceptance remains open. Scout.Scribe's existing working-tree changes were preserved.
Automated checks did not record a physical microphone, play sound, alter Windows
volume or change startup registration.

## Feature reference and parity

The reference was the current [Scribe README](../Scout.Scribe/README.md),
[capture guide](../../docs/cycles/scout-scribe-v1/milestones/MILESTONE_1_CAPTURE_EXPERIMENT.md),
and ADRs [0007](../../docs/engineering/decisions/0007-scribe-native-separate-capture.md),
[0008](../../docs/engineering/decisions/0008-scribe-playback-before-listening-volume.md)
and [0009](../../docs/engineering/decisions/0009-scribe-tray-recording-panel.md).
The recently updated README/ADR 0009 describes the current tray UI; some older
capture-guide paragraphs still describe its previous combined window.

| Current feature | Electron implementation and evidence |
|---|---|
| Quiet idle tray startup | Real Electron Tray and hidden recording window; checked in development and portable builds. |
| Gray idle / red recording icon | Separate native PNG images; state-driven image/tooltip/menu updates. |
| Compact dark panel near pointer | Independent HTML/CSS UI in an Electron tool window, constrained to the monitor work area. Screenshots visually reviewed. |
| Explicit Start/Stop, elapsed time, folder/details | Real UI controls, independent state machine and native folder opening. Start/Stop exercised with generated tones. |
| Hide/Escape while recording | Desktop test verifies capture continues with the panel hidden. |
| Second launch restores existing panel | Actual second Electron process tested while recording; no competing recording. |
| Separate Settings Save/Cancel | Actual filesystem persistence; Cancel discards drafts; invalid folders rejected; saves blocked during capture. |
| Opt-in sign-in launch | Electron current-user login registration with separate identity. No automated registry writes; sign-in remains manual acceptance. |
| Default microphone per Start | Direct default eCapture/eConsole lookup, native format and device ID/name. No selector or default-device caching; physical capture untested. |
| Pre-volume application playback | Direct process-loopback activation with the Electron main-process tree excluded. Virtual activation/fixed-format initialization pass in Node and packaged utility processes. Real volume/mute behavior remains unmeasured here. |
| Two separate mono/48 kHz/256 kbps MP3s | Actual encode/decode verifies format, own-tone amplitude and rejection of the other tone. |
| Native PCM and silence | Packet tests retain bytes, substitute silence and preserve flags/HRESULTs; timestamped queues preserve source gaps. |
| Five-second checkpoints/partial output | Actual fsync checkpoints; forced termination leaves both files directly decodable without repair. Longer synthetic session checkpoints repeatedly. |
| Details/diagnostics/failures | Bounded event history and durable JSONL; Windows, worker and encoder errors retain partial directories. Worker-death transitions tested. |
| Stop/finalize before Exit | Actual portable app exits during capture and writes a completed journal event. |

## Checks performed

Environment: Windows 11 build 10.0.26300; Node.js 24.22.1; npm 11.19.1;
Electron 45.0.0; Koffi 3.3.3; ffmpeg-static 5.3.0 supplying FFmpeg 6.1.1.

- `npm run check`: all application/UI/script/test JavaScript syntax checks passed.
- `npm test`: **16 passed, 0 failed**, using actual MP3 processes and native FFI
  calls/fake COM packet sources where appropriate. The Windows virtual playback
  interface initializes twice without calling Start. No physical microphone opens.
- `npm run test:desktop`: development app passed, including native addon loading in
  the audio utility process, hidden startup, settings, recording/hiding/second-instance,
  Details and finalization on Exit.
- `npm run package`: portable Windows x64 executable built successfully.
- Packaged `npm run test:desktop` with `SCRIBE_EXECUTABLE`: passed against the actual
  `dist/Scribe.js-win32-x64/Scribe.js.exe`, including native dependency loading.
- `npm run synthetic -- --seconds 30`: completed normally; both files contain
  approximately 31 seconds including readiness, repeated positive checkpoint pairs,
  approximately 1 MB per track, final-track records and a completed event.

Screenshots/synthetic recordings are under ignored `test-results/`; the portable
build is under ignored `dist/`. The initial sandbox passed audio checks but denied
settings-file renames and desktop launch. Those checks subsequently passed with
the necessary access. The first npm install also lacked the newly installed Node
directory in its child's PATH; prepending `C:\Program Files\nodejs` fixed it.
These environment failures were not counted as passing runs or worked around by
changing application behavior.

## Microphone timing correction after the first user recording

The user's C920 microphone recording in session
`2026-10-22T14-28-34-715Z_62815132` was reported as crunchy while application playback
sounded fine. The journal showed 48 kHz stereo float for both sources, no encoder
backlog, no timestamp-error flags and one microphone discontinuity. Analysis of the
decoded microphone found sharp jumps near 10 ms packet boundaries and no samples
above 0.98 full scale. This is consistent with a packet-placement problem; the MP3
and journal do not retain the original per-packet timestamps, so they cannot alone
prove how much timestamp jitter that specific capture had.

The original code independently positioned every packet using QPC time. A generated
continuous 440 Hz tone with 10 ms packets and alternating 0.8 ms timestamp jitter
reproduced a concrete defect: 47,431 of 48,000 frames changed, including 3,652
inserted silent frames. Windows provides a separate device-frame position precisely
for sample placement. The correction aligns the source's start using QPC, then
positions subsequent packets from the device-frame counter. It preserves actual
missing frames, handles flagged counter resets and records timing diagnostics in
the final capture-statistics event. No gain, filtering or normalization was added.

The same jittering-tone regression now preserves the original PCM byte for byte.
`npm run check` and **19 tests** pass, including real MP3 encode/decode/crash tests,
the new jitter regression, device gaps, invalid timestamps and counter resets.
The portable app's capture modules were updated without interrupting a running
instance; every new recording creates a new audio worker and loads the correction.
The existing recording was left untouched. A new physical microphone sample is
needed to confirm the user's observed crunch is resolved.

## 0.1.1: hardware counter regression, input selection and failure-panel correction

The user's later sessions failed at startup with `The capture device frame counter
moved backwards without a discontinuity flag`. This was a regression in the first
timing correction above. User-authorized, three-second probes (all PCM discarded,
no recording files) established the actual driver behavior:

- The C920 provides 480 frames per packet in the engine's 48 kHz format while its
  reported device position advances by 320 units. Those positions cannot be treated
  as offsets in the delivered 48 kHz sample stream.
- Application loopback reports position zero for every packet despite delivering
  continuous 480-frame packets. Treating this as a backward counter was invalid.
- Microphone QPC timestamps vary by up to 41 sample periods in the short combined
  probe, reinforcing why independently repositioning each packet can create clicks.

The corrected timeline aligns the first packet with QPC and appends complete
subsequent packets by their actual delivered frame counts. It only creates gaps
for flagged discontinuities with corroborating evidence; counter mismatches are
diagnostics. The jittering-tone regression still preserves every original PCM byte.

The probes explicitly opened the C920, not the Bose microphone. `AudioCategory_Other`
was successfully applied to the camera. The virtual playback client does not support
IAudioClient2 (HRESULT 80004002), so its policy is reported as not explicitly applied.
The user confirmed Bose playback stayed clear during camera-only, playback-only
and combined corrected-pipeline probes. The three-second combined probe completed
without faults, missing-device frames or timestamp-error flags. These observations
do not prove every profile transition or long-session behavior.

An independent Electron failure-panel bug was also fixed: odd window widths caused
fractional coordinates passed to `setPosition`, producing an unhandled rejection.
Coordinates are now integers, asynchronous window-opening failures are caught and
logged, and desktop QA exercises automatic failure-panel opening at an odd width.

Settings now saves an explicit microphone endpoint, including the C920 camera. A
missing selected input never falls back to a headset. The user's requested C920
choice was saved while preserving the recording folder and startup registration.

`npm run check` and **24 focused tests** pass. Development and portable desktop
checks pass, including failed-start UI recovery, microphone persistence, normal
Start/Stop/restart and finalize-on-exit. Restarting the existing app is required to
load the updated main-window code and saved input preference. Later physical-device
recording/listening acceptance still applies.

## 0.1.2: combined recording

The user confirmed the latest microphone recording sounds fine and requested a
single combined file while keeping the 48 kHz output format. New sessions now
produce primary `recording.mp3` plus the two existing source MP3s. A third live
encoder mixes the aligned PCM before any MP3 encoding, with explicit mono channel
averaging and fixed 0.5/0.5 source weights. Source capture/device settings and the
existing source MP3 conversion are preserved. There is no dynamic normalization
or volume change when one source goes quiet/ends.

`npm run check` and **26 focused tests** pass. Updated real encode/decode checks
verify the combined file's mono/48 kHz/256 kbps format and both isolated tones at
the intended mixing levels. Additional tests verify leading silence/tone timing,
unequal source lengths, the longer tail, no gain jump after one source ends, and
coincident .95-level stereo inputs producing .95 rather than doubled/clipped output.
The forced-process-termination test now verifies all three MP3s decode without repair,
each had positive checkpoint bytes, and both tones survive in the combined output.
Desktop checks cover the primary combined-file save message, all three files and
all three final-track journal records on Stop/Exit. These tests use generated audio;
the combined output still needs listening acceptance with the user's real setup.

## Remaining interactive acceptance

1. Launch the portable app, find/click its tray icon and record numbered phrases
   into the default microphone while another app plays known audio. Stop and listen
   to both files independently; check Details for the expected device and format.
2. Repeat at several Windows listening volumes and mute. Compare decoded source
   amplitudes: the intended pre-volume API is implemented, but the .NET Scribe's
   earlier LG/AMD measurement is not evidence for this app.
3. Switch microphone defaults between sessions, unplug the active microphone while
   recording, change output routes, and try the actual headset/Bluetooth/call setup.
4. Run a longer physical-device session and check timelines, silence, checkpoints,
   final audio and memory use. Repeat abrupt termination around checkpoints using
   fictional audio. Short synthetic crash tests do not establish a universal
   five-second tail guarantee or power-loss safety.
5. Enable sign-in launch only if desired, test a sign-in, then disable it if no
   longer needed. Check taskbar overflow and mixed-DPI positioning manually.

No .NET source, project reference, Web database or migration changed for this
experiment. Its independent JavaScript project was verified with the checks above;
the existing .NET build/test suite was not rerun.
