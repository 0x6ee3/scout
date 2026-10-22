# 0004 — Scribe recording baseline

Date: 2026-10-21. Status: Accepted requirements; framework and codec recommendations remain Proposed. Scope: Scout.Scribe v1 M1. Refines [ADR 0001](0001-product-development-cycles.md); preserves its independent product and file-handoff boundary. Implementation status: planning only.

## Context

The initial M1 plan proposed WAV segments, in-app consent acknowledgment and substantial recovery handling. The user wants a personal tray utility with compressed audio, one file per recording, simple device settings and manual restart after failure. Transcription and extraction remain separate milestones; Web integration is unplanned.

## Decision

- Target the latest generally available Windows 11 release at implementation/verification time. Older Windows versions are unsupported; a possible macOS application is a separate product effort. Record the exact tested Windows build in delivery evidence.
- Run primarily from the tray with small settings dialogs. Recording and non-recording icons must differ by shape as well as color; derive them from live session state. Every launch starts idle. Pause is optional and must not delay M1.
- Offer Windows default microphone and explicit connected-device selection, including the Logitech camera microphone and Bluetooth headset. Configure devices before recording and retain settings. Resolve a default-device choice anew at each start; do not silently switch devices during recording. The exact Windows default-device role remains an implementation detail to verify.
- Capture microphone and device-wide playback into one mixed compressed audio file. Include calls, notifications and other sounds routed through the captured playback endpoint. No per-application filtering, source tracks, meters, gain controls, automatic leveling or noise processing are required. Necessary resampling, synchronization and fixed mixing headroom remain engineering responsibilities.
- Consent is obtained outside Scribe before Record is clicked. Scribe has no consent prompt, consent recording or acknowledgment metadata. Web's existing import acknowledgment remains independent.
- Keep a configurable local root, defaulting to `<user home>/Documents/Scout.Scribe/`, with recordings beneath it and timestamp-based session names. No cloud storage adapter or provider abstraction is required now.
- Preserve available partial output after failure. Stop on required-device or writer failure, show non-recording/error state and allow a fresh explicit recording. Restart creates a new file; no automatic resume, merging, repair UI or Web-aware reminder is required.
- A build output directory that can be copied and launched locally is sufficient. No installer, publishing pipeline, single-file executable or self-contained deployment is required. Copy required companion files with the executable and document runtime prerequisites.

## Recommendations awaiting feedback and feasibility

Prefer C#/.NET Windows Forms with NotifyIcon for the shell and NAudio/WASAPI for capture. Evaluate the exact stable dependency/API versions in the capture experiment; no package is selected or installed by this record.

Recommend a single mono MP3 at 128 kbps constant bitrate, initially evaluating a 48 kHz mix. This is lossy voice-oriented storage, about 57.6 MB/hour excluding small overheads. Compare 64 kbps only if further size reduction is useful. Choose the streaming encoder after proving partial-file behavior; assess NAudio/Media Foundation first and FFmpeg if needed. Do not assume all encoder paths preserve useful data on interruption.

Prefer one file without routine rollover. Web's existing 1 GiB import ceiling remains: at 128 kbps it represents approximately 18.6 hours before overhead, far beyond a normal interview. An exceptional oversize file can remain local and be split manually for import. Warn before the handoff ceiling without truncating or discarding capture; no Web limit change is included. Transcription may decode and process chunks internally in M2 without splitting the preserved recording.

One playback endpoint is the proposed baseline. WASAPI loopback does not aggregate all physical output devices. Whether simultaneous capture of several output endpoints is actually needed remains open; do not claim every PC sound is captured regardless of routing.

## Alternatives considered

Windows Forms directly supplies the tray component and ordinary dialogs in the existing C# ecosystem. WPF offers richer XAML styling and binding but has little immediate benefit for M1; future review UI can revisit this. Electron supplies tray UI but adds its Chromium/Node process model without a cross-platform requirement.

PCM WAV is straightforward for processing but uses much more storage. AAC/M4A is compact, but ordinary MP4 finalization creates an interruption concern; fragmented MP4 mitigates this with compatibility tradeoffs. MP3 is the proposed simpler initial artifact and already accepted by Web. Actual encoder, seek, interrupted-file and browser behavior still require verification.

## Consequences

This changes the active M1 plan's WAV/mandatory-segmentation direction, in-app consent prompt, mandatory level preview, extensive recovery expectations and publish requirement. Preserve archived proposals and completed Web records unchanged.

An icon communicates state while Scribe is running; Windows may retain a stale icon after abrupt termination. Graceful shutdown removes it, restart registers a fresh idle icon, and shell restart/re-registration must be tested. No in-process mechanism can guarantee an instant icon update after its process crashes. Explain pinning the icon out of Windows' overflow area in launch guidance.

Lossy recording must be assessed on representative speech; compatibility with a decoder does not prove transcription accuracy. M2 retains the MP3 and creates the engine's required processing representation. Long-file processing limits, memory use and chunk/timestamp handling belong to M2.

## References

- [M1 plan](../../cycles/scout-scribe-v1/milestones/MILESTONE_1_PLAN.md), [technical design](../../products/scout-scribe/TECHNICAL.md).
- [Microsoft NotifyIcon](https://learn.microsoft.com/en-us/dotnet/api/system.windows.forms.notifyicon), [WPF overview](https://learn.microsoft.com/en-us/dotnet/desktop/wpf/overview/), [Electron process model](https://www.electronjs.org/docs/latest/tutorial/process-model).
- [Microsoft loopback recording](https://learn.microsoft.com/en-us/windows/win32/coreaudio/loopback-recording), [NAudio](https://github.com/naudio/NAudio).
- [whisper.cpp input/conversion documentation](https://github.com/ggml-org/whisper.cpp#quick-start), [FFmpeg container fragmentation](https://ffmpeg.org/ffmpeg-formats.html#Fragmentation).
