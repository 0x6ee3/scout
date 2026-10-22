# Scout.Scribe v1: Milestone 1 plan — Recording

Date: 16 October 2026; revised 21 October 2026. Status: package 1 experiment implemented with synthetic verification; real-device gates and packages 2–4 pending. Derived from the [superseded M6A proposal](../archive/MILESTONE_6A_PLAN.md), with ownership established by the [product-cycle decision](../../../engineering/decisions/0001-product-development-cycles.md). The [recording baseline](../../../engineering/decisions/0004-scribe-recording-baseline.md), [format/durability](../../../engineering/decisions/0005-scribe-audio-durability.md) and [capture stack](../../../engineering/decisions/0006-scribe-default-devices-and-capture-stack.md) decisions record accepted requirements. See the [Scribe roadmap](../ROADMAP.md) and [Web M5 delivery](../../scout-web-v1/milestones/MILESTONE_5_DELIVERY.md) for import behavior and verification limits.

## 1. Outcome and scope

Implementation update (22 October 2026): NAudio 3.3.0 captures the default microphone in its native format and application playback before listening-volume attenuation. Independent WMF conversion produces `microphone.mp3` and `playback.mp3` without mixing or gain compensation. [ADR 0008](../../../engineering/decisions/0008-scribe-playback-before-listening-volume.md) replaces endpoint-only playback with application loopback across output devices, excluding Scribe and its descendants. LG/AMD measurements and decoded MP3s verify volume independence at Windows 20% and 10%. [Evidence and test instructions](MILESTONE_1_CAPTURE_EXPERIMENT.md) are available; broader hardware gates, saved settings and remaining reliability/delivery work remain pending.

Deliver a separate executable named **Scout.Scribe** for the latest generally available Windows 11 release, primarily operated from the system tray with small settings dialogs. Older Windows versions are unsupported; any macOS application is separate. The user launches it manually, obtains participant consent outside Scribe, clicks Record, captures microphone and computer playback, stops capture and finds the saved audio. Scribe has no consent prompt or acknowledgment storage. Scout.Web can be stopped throughout capture.

Scribe M1 covers recording only. Scribe M2 will add optional local transcription, including an opt-in post-stop processing step and a portable TXT result. Scribe M3 will add local fact extraction and review with preserved sources and separate reviewed output; automatic structured promotion into Web remains outside this baseline. Speech-to-text produces a transcript; audio conversion only prepares a compatible processing copy and is not itself transcription.

Scout.Web's M1–M5 are delivered; Web M6 is the UI overhaul that closes its intended v1 cycle. Scribe M1–M3 belong to an independent product cycle and are not a Web v1 release dependency. Several real interviews should guide later Web prioritization.

Excluded from Scribe M1: Web redesign, transcription/model downloads, AI analysis, browser start/stop, automatic uploads/imports, Web database access, interview selection/synchronization or reminders, macOS/Linux capture, older Windows support, installers, distribution/publish pipelines, automatic startup, Windows services, waveform editing, automatic recovery/merging and a general plugin/provider framework. Pause is a nice-to-have only. A runnable Debug/Release output folder and launch/copy instructions are sufficient.

## 2. Architectural assessment

This split puts device-dependent capture in a desktop process and interview management in the existing Web application. Files form the interoperability boundary. The modest cost is manual handoff and separate device/output settings. Do not solve that cost with a shared database or local control server before practical use shows a need.

Baseline repository inspection before package 1 found:

| Existing implementation | Consequence |
|---|---|
| `Scout.slnx` contains `src/Scout.Web` and `tests/Scout.Web.Tests`; Web targets `net10.0`. | Add a separate desktop project and focused test project during implementation. There is no native recorder to move today. |
| `Features/InterviewRecordings/RecordingOperations.cs` registers uploaded MP3/WAV/M4A, existing attachments or working-directory links with a per-file consent acknowledgment. | Reuse manual registration. Recording in Scribe does not register a Web record or replace Web's acknowledgment. |
| `Storage/DocumentFileStore.cs` accepts MP3/WAV/M4A up to 1 GiB. | Normal compressed interviews should fit one file. Preserve exceptional oversized recordings locally and explain manual splitting for Web; no Web limit change. Verify browser playback. |
| `Features/InterviewTranscripts/TranscriptOperations.cs` imports strict UTF-8 `.txt`, at most 5 MiB and 1,000,000 .NET string characters; recording association is optional. | The desired transcript-only boundary already exists for Scribe M2 and external transcription tools. |
| `Domain/InterviewTranscript.cs` has imported/edited-copy origins; originals and corrections are preserved. | A Scribe-generated TXT is an external imported transcript. No generated-origin schema or migration is needed for file handoff. |
| `Configuration/StoragePaths.cs` owns Web's fixed root, assets and database; linked audio is beneath `<user home>/Documents/Scout/recordings/`. | Scribe should own a separate output directory. Upload is the default handoff; linking is optional only after an explicit user copy into Web's permitted directory. |
| `Features/InterviewTranscripts/TranscriptEndpoints.cs` exposes a guarded correction form; imports use concrete application operations. | Existing UI import is the supported boundary. There is no public recorder-ingestion API to reuse or imply. |
| M5 has no capture, conversion, processing worker or model configuration. | This is a new companion executable, not a refactor of an existing capture/transcription subsystem. |

**Required Scout.Web code changes for Scribe M1: none.** No migrations, shared entities, project references, endpoint changes or extraction of a common library are needed. Preserve current recording, transcript, consent, retry and Workspace behavior. The source inspection establishes compatibility intent, not proof that an unbuilt recorder works.

## 3. Project and process boundary

Proposed implementation structure:

- `src/Scout.Scribe/Scout.Scribe.csproj`: Windows-targeted .NET desktop executable, tray shell, compact settings/status window, capture/session operations and output handling.
- `tests/Scout.Scribe.Tests/`: focused session-state, output-preservation, limit and failure tests using isolated temporary storage and simulated devices where useful.
- Add these projects to `Scout.slnx`; document build-folder launch/copy and operation when implemented. Keep Web runnable/buildable by itself.

Use Windows Forms/NotifyIcon, NAudio/WASAPI capture and Windows Media Foundation (WMF) streaming MP3 encoding, accepted in the [capture stack decision](../../../engineering/decisions/0006-scribe-default-devices-and-capture-stack.md). Target Windows 11 x64 and .NET 10 (`net10.0-windows`, `win-x64`). A framework-dependent build folder requires .NET 10 Desktop Runtime; building requires the .NET 10 SDK. Copy all required output files with the executable. External tools/codecs outside NuGet are permitted if needed, with documented setup, but are not required merely because they are allowed. Select compatible stable NAudio versions in package 1 and prove the chosen WMF path's streaming/interruption behavior. If it cannot meet the durability gate, report the evidence and revise the decision explicitly. No dependency is installed by this plan. Keep capture/session logic separate from tray handlers.

Scribe owns its process, device handles, local settings and output files. It does not load `Scout.Web`, call its internal operations, open its SQLite database, write managed assets, or require interview IDs. A second launch should activate the existing instance and must not start competing capture. No network service is needed for Scribe M1.

## 4. User flow and compact interface

1. Launch manually into the idle tray state, including after a crash. First use opens output-directory setup; recording uses the default microphone and application playback before listening volume.
2. Resolve the normal Windows default microphone each Start and retain it for the session. No device dropdown, saved override or role selector. Change the microphone in Windows before recording, including between camera and Bluetooth input. Playback captures applications across output devices without pinning a render endpoint. Settings retain the output folder and may display capture-source names. No level preview, gain controls or meters are required.
3. Choose **Start recording** after obtaining consent outside Scribe. Start immediately after device/output validation, without a consent dialog or acknowledgment metadata.
4. Use unmistakably different recording and non-recording tray icons, distinguished by shape and color, plus textual menu/tooltip status. Enter recording state only when capture and writing are active. Settings changes and duplicate start are disabled during a session. No status window is required for M1.
5. Choose **Stop recording**, show finalizing/non-recording state, then the saved path and **Open output folder**. A failed required source or writer stops capture and reports retained partial output. Silence alone is not a device failure.
6. Start Scout.Web when convenient, choose the interview and manually register the finalized audio with its existing consent acknowledgment. Completion and manual notes never depend on this step.

Tray actions: Start, Stop, Settings, Open output folder and Exit, enabled for the current state; status is available in the menu/tooltip. Closing settings leaves the tray running. Exit during capture follows a stop-and-exit flow. Document moving the icon out of Windows' hidden overflow. Remove the icon on graceful exit and re-register it after Explorer restart. Abrupt termination can leave a stale Windows tray icon; an instant crash indication cannot be guaranteed. Restart always registers idle state, never a persisted recording state. Offer meaningful labels and keyboard navigation in dialogs.

Register top-level exception hooks before UI/capture initialization: Application.ThreadException with an explicit exception-mode policy for UI faults, plus AppDomain.UnhandledException for last-chance notification. Handle capture/writer errors at operation boundaries. For manageable failures use a bounded, idempotent shutdown path: mark non-recording, attempt audio flush/finalization, log, hide/dispose the tray icon and exit. Last-chance cleanup is minimal best effort, without waiting on possibly blocked UI threads or held locks. Forced termination, FailFast and catastrophic runtime/native failures may bypass all cleanup. Do not rely on crash handlers for audio durability. A separate tray watchdog is outside M1.

Do not put Scribe status into the screen-shared Workspace. Tray icons, notifications and Scribe windows are desktop surfaces outside Web's projection privacy boundary; explain this when arranging screen sharing.

## 5. Capture and preservation requirements

Capture the Windows default microphone in its native format and application playback using WASAPI process-loopback exclusion of Scribe and its descendants. Playback is captured before listening-volume processing across output devices; its virtual source requires an explicit 48 kHz stereo float format because no native endpoint format exists. Convert independently into `microphone.mp3` and `playback.mp3`, each mono/48 kHz/256 kbps. No mixing, gain compensation, normalization or noise suppression. Validate independent conversion, bounded buffering, silence, relative timing and listening-volume independence. Resolve the microphone afresh each Start; playback has no pinned endpoint. The earlier endpoint-only scope is superseded by ADR 0008.

Use a configurable Scribe-owned local root, default `<user home>/Documents/Scout.Scribe/`, with timestamp-named sessions beneath `recordings/`. Keep settings and minimal session metadata within this hierarchy. No OneDrive dependency or cloud abstraction. Create files without overwriting, check writability and expose disk-space problems. Retain finalized and partial output without automatic cleanup. Document independent backup; copyable executable location is separate from the data root.

Use two files per recording, one per source, with no routine segmentation. Typical interviews last 60–90 minutes: each file is approximately 115.2–172.8 MB, totaling 230.4–345.6 MB plus small overheads. Web's 1 GiB ceiling applies per file (about 9.3 hours before overhead). Warn before that handoff ceiling, keep capturing while storage permits, and explain manual splitting of exceptional oversized files for Web. No silent truncation or Web limit change. M2 must evaluate processing of separate sources, engine-specific long-input and memory limits; no automatic merging is implemented here.

Crash-usable audio is a mandatory delivery gate. Stream MP3 frames continuously with bounded buffers; checkpointed audio must decode directly after application termination without Stop, a crash handler, repair or full-session conversion. Perform file checkpoints every five seconds on a dedicated writer path, away from capture callbacks. Account for encoder/output buffering and use the supported file-flush mechanism; do not finalize/reset the encoder at every checkpoint. Measure the surviving audio tail and checkpoint latency on the actual NVMe setup: a five-second timer alone is not proof of a five-second end-to-end loss bound. Power outages, blue screens, OS crashes and hardware-failure recovery are outside M1; add no special design or acceptance tests for them. Bytes never captured/written cannot be guaranteed.

Use an explicit lifecycle: Idle → Starting → Recording → Finalizing → Completed, plus Failed/interrupted. Completion means released handles and finalized output; partial files are not completed exports. Keep minimal local metadata as needed: session identity, actual sources/formats, start/stop times and completion/error state. Do not store consent acknowledgments. Drive the tray icon from live lifecycle state, not persisted session status.

Handle device denial/loss, initialization, disk-write and finalization failure visibly. A default-microphone change must not silently reroute its active capture; loss of that endpoint stops the session. Playback is endpoint-independent and continues with application audio after output routing changes. Sleep/resume and termination never restart recording automatically. Preserve the playable prefix and allow manual restart into a new session. No merging, automatic repair or interview-aware reminders. Reject encoders requiring normal finalization for playback; document incomplete duration/seek metadata separately from audio loss.

## 6. Artifact contract and later API

The initial API is a documented file contract, usable by any compatible tool:

| Artifact | Producer responsibility | Scout.Web handoff |
|---|---|---|
| Finalized audio (Scribe M1 experiment) | Separate `microphone.mp3` and `playback.mp3`, each mono/48 kHz/256 kbps in a unique timestamped session directory. Both committed outputs must remain playable if interrupted before finalization. | Manually upload files up to 1 GiB each. Exceptional larger recordings stay local and need manual splitting for import. Verify actual browser playback. |
| Transcript (Scribe M2) | Strict UTF-8 TXT, nonempty, at most 5 MiB and 1,000,000 .NET string characters per import; show oversize failures and offer explicitly ordered parts rather than truncate. Preserve audio and exported text. | Import via the existing transcript UI without audio or consent-choice prerequisites. Optional recording association is selected by the user within that round. |
| Optional Scribe metadata | Local session/provenance/recovery information; Scribe M2 should retain engine/model/settings and source identity locally. | No automatic metadata ingestion or structured provenance mapping is promised. TXT may carry readable source/timestamp annotations; richer ingestion needs a later contract. |

Upload is the simplest default: Scribe output is copied into Web's preserved managed storage. Optional Web linking requires a user-controlled copy beneath Web's `recordings/` directory and a relative path; it cannot link an arbitrary Scribe path. Never move or remove the only source copy automatically. Keep the existing Web verification, consent and ownership checks.

Browser-triggered recording and automatic transfer are deferred. If later justified, design an explicitly authorized local protocol with versioned contracts, caller/origin checks, recording initiation safeguards, ownership selection and idempotent transfer. A localhost URL alone is not an authorization design. Do not create endpoints, custom URL handlers, watchers or shared database access now.

## 7. Implementation packages and gates

| Package | Reviewable result | Gate |
|---|---|---|
| 1. Capture feasibility and dependency verification | NAudio/WASAPI plus WMF experiment using Windows defaults, actual camera/Bluetooth devices, mono MP3 256 kbps and five-second file checkpoints. Select stable versions and validate sample conversion/writer behavior. | Verify both sources, 90-minute synchronization, quiet speech/silence, checkpoint latency, directly playable committed audio after application termination with measured lost tail, and browser import/playback. |
| 2. Independent desktop shell | Single-instance tray lifecycle, Windows-default sources without selectors, output setting, immediate explicit start/stop and distinct icons. | Runs with Web stopped; correct idle state on restart, no consent prompt, duplicate capture or database dependency. |
| 3. Reliable output and failures | Continuous independent writing/checkpoints for both files, finalization, bounded buffering, crash shutdown hooks and manual restart into a new session. | State/file tests plus device-loss, write failure, UI/background exceptions, sleep/resume, Explorer restart and forced-exit exercises; checkpoint preservation cannot depend on exception hooks. |
| 4. Handoff and delivery | Web import/playback, build-folder launch/copy instructions, backup/partial-file guidance and M1 delivery record. | Complete checklist; record exact Windows build, hardware/browser and unverified cases. No distribution pipeline required. |

Build/test commands and dependency versions are recorded when the projects exist. Test Scribe behavior proportionally; rerun Web checks only if Web code changes or a demonstrated compatibility issue requires a separate fix. Never use personal interview recordings or the production data root for automated tests.

## 8. Acceptance checklist

- With Web stopped, launch idle, switch Windows microphone defaults before separate recordings, and explicitly start without a consent prompt or selector. Verify microphone in its own file and application playback/notifications in the other file. Verify playback level across listening-volume changes and output routing; application-loopback scope includes all output devices.
- Stop from the tray; confirm accurate visible state, finalization, device-handle release and discoverable playable output. Closing the window preserves tray operation; exit while recording follows the explicit stop flow. A second launch does not duplicate capture.
- Perform a 90-minute mock interview on the intended devices; record relative timing, quiet-speech quality, CPU/memory and file growth at 256 kbps per source. Confirm two playable compressed files and exercise an oversize warning at a reduced test threshold. Do not infer long-session success from a short sample.
- Exercise absent/denied devices, unplugging, endpoint changes, invalid/unwritable output, write/finalization failure, sleep/resume and process interruption. Retain available bytes, show errors and require explicit restart. Record any physical-hardware case not exercised.
- Force termination at arbitrary points around checkpoints/frame boundaries and after a 90-minute run. Play the saved prefix directly without repair, measure the missing tail against known timed signals and record any duration/seek limitations. Exercise disk-full/write failure separately; retain the prior committed prefix.
- Verify tray state through start/stop/failure, Explorer restart and application restart. Exercise UI/background exceptions to verify best-effort shutdown/icon cleanup, separately from forced kill which bypasses it. Confirm restart is idle and a manual new recording preserves the previous partial file. Document stale-icon behavior after forced exit.
- Start Web after capture; import finalized audio into the correct fictional round, acknowledge consent in Web and verify play/pause/seek and download. Neither application silently alters the other's files/metadata.
- Confirm Web still works without Scribe, and Scribe without Web. Existing external-audio and transcript-only imports remain available. No Web migration, capture control or private data is added to the shared Workspace.
- Deliver build/launch/copy, device, output-limit, backup and partial-file instructions plus `MILESTONE_1_DELIVERY.md` with evidence and limits. No installer/publish pipeline required. Do not claim unperformed checks passed.

## 9. Open implementation decisions and next milestone

The current experiment uses Windows 11 x64, .NET 10 Windows Forms, NAudio default-microphone and application-loopback capture, independent WMF conversion, two mono MP3s at 256 kbps, five-second checkpoints, external consent and manual restart. Playback volume independence supersedes the earlier endpoint-only scope under ADR 0008. Use a framework-dependent copyable build folder; additional tools/codecs are acceptable when necessary. Whole-system crash/power-loss recovery and future S3 storage are outside M1. Package 1 still needs broader playback compatibility, routing, relative timing, checkpoint-latency and lost-tail evidence. Combined export or M2 handling of separate sources requires follow-up planning. Retain dependency notices and exact hardware verification.

Write the detailed Scribe M2 plan from M1's actual output and delivery evidence. Decide local engine/model/hardware requirements, explicit opt-in post-stop transcription, existing-file input, conversion copies, progress/cancellation/retry, interrupted processing, transcript limits and provenance. Model failure must never lose a recording. Keep model processing outside Web; import the result through the existing TXT boundary. Plan Scribe M3 fact extraction and review from M2 evidence; tighter integration and automatic structured promotion remain separately prioritized future work.
