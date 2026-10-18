# Scout.Scribe v1: Milestone 1 plan — Recording

Date: 16 October 2026. Status: planning only; no application implementation is part of this documentation change. Derived from the [superseded M6A proposal](../archive/MILESTONE_6A_PLAN.md), with product name, cycle ownership and successor scope revised by the [product-cycle decision](../../../engineering/decisions/0001-product-development-cycles.md). See the [Scribe roadmap](../ROADMAP.md). Read [Web M5 delivery](../../scout-web-v1/milestones/MILESTONE_5_DELIVERY.md) for delivered import behavior and verification limits.

## 1. Outcome and scope

Deliver a separate Windows 11 desktop executable named **Scout.Scribe**, primarily operated from the system tray. The user launches it manually, obtains participant consent outside the application, explicitly starts capture, records microphone and computer playback, stops capture and finds the saved audio. Scout.Web can be stopped throughout capture.

Scribe M1 covers recording only. Scribe M2 will add optional local transcription, including an opt-in post-stop processing step and a portable TXT result. Scribe M3 will add local fact extraction and review with preserved sources and separate reviewed output; automatic structured promotion into Web remains outside this baseline. Speech-to-text produces a transcript; audio conversion only prepares a compatible processing copy and is not itself transcription.

Scout.Web's M1–M5 are delivered; Web M6 is the UI overhaul that closes its intended v1 cycle. Scribe M1–M3 belong to an independent product cycle and are not a Web v1 release dependency. Several real interviews should guide later Web prioritization.

Excluded from Scribe M1: Web redesign, transcription/model downloads, AI analysis, browser start/stop, automatic uploads/imports, Web database access, interview selection/synchronization, macOS/Linux capture, installers, automatic startup, Windows services, waveform editing and a general plugin/provider framework. A runnable desktop executable and launch instructions are required; an installer is not.

## 2. Architectural assessment

This split puts device-dependent capture in a desktop process and interview management in the existing Web application. Files form the interoperability boundary. The modest cost is manual handoff and separate device/output settings. Do not solve that cost with a shared database or local control server before practical use shows a need.

Repository inspection found:

| Existing implementation | Consequence |
|---|---|
| `Scout.slnx` contains `src/Scout.Web` and `tests/Scout.Web.Tests`; Web targets `net10.0`. | Add a separate desktop project and focused test project during implementation. There is no native recorder to move today. |
| `Features/InterviewRecordings/RecordingOperations.cs` registers uploaded MP3/WAV/M4A, existing attachments or working-directory links with a per-file consent acknowledgment. | Reuse manual registration. Recording in Scribe does not register a Web record or replace Web's acknowledgment. |
| `Storage/DocumentFileStore.cs` accepts audio up to 1 GiB. | Design output for this ceiling; do not require a Web limit change. Browser codec support still needs actual playback verification. |
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
- Add these projects to `Scout.slnx`; update launch/publish and operations documentation when implemented. Keep Web runnable/buildable by itself.

Prefer a small Windows Forms shell for the tray and settings window in the existing C# ecosystem. Evaluate a suitable Windows microphone/loopback capture library, with NAudio as a candidate, in the first implementation package. Verify current primary documentation, supported runtime/package versions, licensing and actual device behavior then. No dependency or package version is selected or installed by this plan. Keep capture logic separate from tray event handlers without creating speculative shared infrastructure.

Scribe owns its process, device handles, local settings and output files. It does not load `Scout.Web`, call its internal operations, open its SQLite database, write managed assets, or require interview IDs. A second launch should activate the existing instance and must not start competing capture. No network service is needed for Scribe M1.

## 4. User flow and compact interface

1. Launch Scribe manually. It appears idle in the tray; launch never starts recording. First use opens a small setup window to select microphone, playback endpoint and output directory.
2. Open **Settings / devices** to inspect selected sources and levels. Level preview is explicitly activated and never persists audio. The playback endpoint must be the endpoint carrying the call; do not claim universal capture of every computer sound or application.
3. Choose **Start recording** in the tray menu. A compact prompt requires acknowledgment that participant consent was obtained for this session. Store the acknowledgment time as a user statement, not evidence of when participants actually consented. Only explicit confirmation starts capture.
4. While recording, show a distinct tray icon, textual status/elapsed time and a small optional status window with source activity and **Stop**. Disable duplicate start and device/output changes for the active session. Do not depend on transient notifications or color alone to communicate recording.
5. Choose **Stop recording**. Show finalization progress, then the saved file location and **Open output folder**. A silent source must be visible as a warning; a disconnected or failed required source must stop capture and report partial output rather than silently continuing as a successful two-source recording.
6. Start Scout.Web when convenient, choose the interview and manually register the finalized audio with its existing consent acknowledgment. Completion and manual notes never depend on this step.

Tray actions: Start, Stop, Show status, Settings / devices, Open output folder and Exit, enabled for the current state. Closing the small window leaves the tray application running. Exiting during capture requires an explicit stop-and-exit choice; no silent discard. Document how to locate a hidden tray icon. Offer meaningful text labels and keyboard navigation in the settings/status window.

Do not put Scribe status or consent history into the screen-shared Workspace. Tray icons, notifications and Scribe windows are desktop surfaces outside Web's projection privacy boundary; explain this when arranging screen sharing.

## 5. Capture and preservation requirements

Capture the selected microphone and selected playback endpoint concurrently into a normal, finalized audio file containing both sources. Start with one mixed PCM WAV output for straightforward handoff; separate source tracks are optional and must not delay the core result. The first package must prove mixing, resampling and synchronization across differing device sample formats and silence periods. Avoid clipping, unbounded memory buffering and accumulated timing drift. Test the actual call/headset route; audible output in a different endpoint is not covered automatically.

Use unique session directories beneath a Scribe-owned default such as `<user home>/Documents/Scout.Scribe/recordings/`, with an explicit output-directory setting. Create new files without overwriting. Show the destination before starting, check writability and report disk-space problems. Keep finalized audio, partial files and recovery evidence; no automatic deletion or cleanup. Output outside Web's root needs its own backup guidance.

Bound audio segments below Web's 1 GiB per-file limit, allowing header overhead. PCM WAV grows quickly, so the implementation must select and document a size/duration policy and roll into clearly numbered, independently playable segments before the ceiling. Do not truncate a long interview or promise that all sessions fit one file. Web already permits registering multiple recordings. Verify continuity at segment boundaries and show all output paths after stop.

Use an explicit lifecycle: Idle → Starting → Recording → Finalizing → Completed, with a recoverable Failed/interrupted state. Completion means capture handles are released and output headers/files are finalized and validated. Do not offer in-progress files as completed exports. Persist minimal session/recovery metadata separately from audio where needed: session ID, chosen sources/formats, acknowledgment/start/stop times, segment order and completion/error state. This is Scribe-local metadata, not a Web import requirement.

Handle device access denial, unplugging, endpoint changes, capture initialization failure, disk-full/write failure and finalization failure visibly. Sleep/resume and process termination must never silently restart recording. On next launch, identify interrupted sessions, retain partial bytes and explain whether recovery succeeded or the file needs external repair. Do not promise recovery of bytes never flushed to disk. Stop and restart after a fresh explicit action; do not silently switch devices mid-interview.

## 6. Artifact contract and later API

The initial API is a documented file contract, usable by any compatible tool:

| Artifact | Producer responsibility | Scout.Web handoff |
|---|---|---|
| Finalized audio (Scribe M1) | Mixed microphone/playback WAV, unique filename up to 255 characters, each nonempty file below 1 GiB; ordered segments when needed. | Manually upload each file into the selected round using existing registration. MP3/M4A from other tools remain supported. Playback depends on actual codec/browser. |
| Transcript (Scribe M2) | Strict UTF-8 TXT, nonempty, at most 5 MiB and 1,000,000 .NET string characters per import; show oversize failures and offer explicitly ordered parts rather than truncate. Preserve audio and exported text. | Import via the existing transcript UI without audio or consent-choice prerequisites. Optional recording association is selected by the user within that round. |
| Optional Scribe metadata | Local session/provenance/recovery information; Scribe M2 should retain engine/model/settings and source identity locally. | No automatic metadata ingestion or structured provenance mapping is promised. TXT may carry readable source/timestamp annotations; richer ingestion needs a later contract. |

Upload is the simplest default: Scribe output is copied into Web's preserved managed storage. Optional Web linking requires a user-controlled copy beneath Web's `recordings/` directory and a relative path; it cannot link an arbitrary Scribe path. Never move or remove the only source copy automatically. Keep the existing Web verification, consent and ownership checks.

Browser-triggered recording and automatic transfer are deferred. If later justified, design an explicitly authorized local protocol with versioned contracts, caller/origin checks, recording initiation safeguards, ownership selection and idempotent transfer. A localhost URL alone is not an authorization design. Do not create endpoints, custom URL handlers, watchers or shared database access now.

## 7. Implementation packages and gates

| Package | Reviewable result | Gate |
|---|---|---|
| 1. Capture feasibility and dependency decision | Minimal two-source capture experiment on the intended Windows hardware; chosen desktop/capture dependencies and output format/segment policy documented. | Listen to both sources, measure synchronization in a sustained call, verify silence/device formats and a WAV import/playback in the actual Web browser. Record limitations before expanding UI. |
| 2. Independent desktop shell | Executable, single-instance tray lifecycle, settings and explicit per-session consent/start/stop. | Runs with Web stopped; no automatic start, duplicate capture, hidden active state or database dependency. |
| 3. Reliable output and failures | Finalization, segment rollover, bounded buffering, failure states and retained interrupted sessions. | Focused state/file tests plus actual device-loss, disk-write failure, sleep/resume and forced-exit exercises on fictional recordings. |
| 4. Handoff and delivery | Manual Web import/playback of Scribe output, standalone publish/launch instructions, output/backup/recovery guidance and Scribe M1 delivery record. | Complete acceptance checklist below; report hardware/browser and any unverified cases explicitly. |

Build/test commands and dependency versions are recorded when the projects exist. Test Scribe behavior proportionally; rerun Web checks only if Web code changes or a demonstrated compatibility issue requires a separate fix. Never use personal interview recordings or the production data root for automated tests.

## 8. Acceptance checklist

- With Web stopped, manually launch Scribe, select the actual call devices, acknowledge consent and explicitly start. Capture distinct fictional speech from the microphone and playback source and verify both in the exported audio.
- Stop from the tray; confirm accurate visible state, finalization, device-handle release and discoverable playable output. Closing the window preserves tray operation; exit while recording follows the explicit stop flow. A second launch does not duplicate capture.
- Perform a sustained mock interview on the intended headset/call setup; record duration, synchronization/level observations, CPU/memory and segment behavior. Check a segment rollover with a reduced test threshold and continuity in playback. Do not infer long-session success from a short sample.
- Exercise absent/denied devices, unplugging, endpoint changes, invalid/unwritable output, write/finalization failure, sleep/resume and process interruption. Retain available bytes, show errors and require explicit restart. Record any physical-hardware case not exercised.
- Start Web after capture; manually import all finalized segments into the correct fictional round, acknowledge consent and verify play/pause/seek and download. Files and metadata in one application must not be silently altered by the other.
- Confirm Web still works without Scribe, and Scribe without Web. Existing external-audio and transcript-only imports remain available. No Web migration, capture control or private data is added to the shared Workspace.
- Deliver launch/publish, devices, output limits, independent backup and recovery instructions, plus `MILESTONE_1_DELIVERY.md` with actual evidence and limits. Do not claim unperformed acceptance checks passed.

## 9. Open implementation decisions and next milestone

Resolve in package 1: dependency versions/licensing, Windows packaging/runtime choice, capture/mixing formats, segment threshold, supported endpoint behavior and sustained synchronization. These do not require changing the chosen application boundary or adding Web features.

Write the detailed Scribe M2 plan from M1's actual output and delivery evidence. Decide local engine/model/hardware requirements, explicit opt-in post-stop transcription, existing-file input, conversion copies, progress/cancellation/retry, interrupted processing, transcript limits and provenance. Model failure must never lose a recording. Keep model processing outside Web; import the result through the existing TXT boundary. Plan Scribe M3 fact extraction and review from M2 evidence; tighter integration and automatic structured promotion remain separately prioritized future work.
