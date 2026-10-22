# 0006 — Scribe default devices and capture stack

Date: 2026-10-21. Status: Accepted; not implemented. Scope: Scout.Scribe v1 M1. Partially supersedes [ADR 0004](0004-scribe-recording-baseline.md) on device selectors and dependency recommendations and [ADR 0005](0005-scribe-audio-durability.md) on encoder selection and checkpoint interval. Their other requirements remain applicable.

## Context

The user routes playback through one Windows output device and uses one Windows input device. Changing Windows defaults before an interview is sufficient for v1. The user accepts NAudio with Windows Media Foundation and five-second checkpoints; whole-system crashes, power outages and blue-screen recovery need no special design or verification.

## Decision

- Use NAudio/WASAPI for microphone and playback loopback capture and Windows Media Foundation for streaming mono MP3 at 256 kbps. Select compatible stable NuGet versions during implementation; 48 kHz mixing/resampling remains the starting technical direction. Do not silently substitute another encoder if the chosen path fails the crash-usability gate; report the evidence and revise the decision explicitly.
- Resolve the normal Windows system-default microphone and playback endpoint when recording starts (general default role, not a separately preferred communications device). Retain these actual endpoints for that recording. No device dropdown, stored device override, default-role selector or multi-endpoint aggregation in Scribe v1. Users change Windows defaults before starting. Keep the output-folder setting; source names may be displayed for clarity.
- Encode/write continuously with bounded buffers and perform file checkpoints every five seconds through a dedicated writer path, away from capture callbacks. Do not treat encoder finalization as a recurring flush. Measure encoder/output buffering and the surviving tail after application termination rather than promising that a five-second timer alone bounds all loss. Previously committed audio must play directly without repair. Validate cadence, write latency and audio continuity on the actual NVMe setup.
- Scope durability to application failures and device/writer errors, including abrupt process termination. Power loss, blue screens, OS crashes and storage-hardware recovery are outside M1; do not add special infrastructure or acceptance tests for them. Preserve the prior playable prefix on write failure where possible. Best-effort exception-path tray cleanup remains as in ADR 0005.
- Target Windows 11 x64, .NET 10 Windows Forms (`net10.0-windows`, `win-x64`), with a framework-dependent copyable output folder and .NET 10 Desktop Runtime prerequisite. Build requires the .NET 10 SDK. No installer, runtime bundling or publishing pipeline is needed. Copy all required dependencies with the executable.
- External tools/codecs outside NuGet are acceptable when needed and must have documented setup. They are not mandatory M1 dependencies merely because installation is permitted; FFmpeg/conversion requirements for transcription are decided in M2.

## Alternatives considered

An in-app device selector and simultaneous output-device capture add configuration the user does not need. One-second checkpoints were a proposed starting target, not a published audio-recording standard; five seconds is the accepted cadence to test. A self-contained deployment is unnecessary for the user's current build-folder workflow.

## Consequences

All user-facing M1 technical scope questions are settled. Package 1 validates implementation details: compatible NAudio APIs/versions, WMF streaming behavior, synchronization, checkpoint latency and playable interrupted files. A normal encoder finalization must not be required to save an entire interview. No claim of successful real-device verification exists yet.

Snipping Tool is a behavioral reference for simple combined microphone/system-audio capture. Microsoft's public feature announcement confirms both sources and their controls; the research did not establish its exact internal capture/encoder implementation. Do not claim that it uses NAudio or the identical WMF path selected for Scribe.

## References

- [M1 plan](../../cycles/scout-scribe-v1/milestones/MILESTONE_1_PLAN.md), [technical design](../../products/scout-scribe/TECHNICAL.md).
- [Microsoft Snipping Tool audio announcement](https://blogs.windows.com/windows-insider/2023/08/31/new-updates-for-snipping-tool-and-notepad-for-windows-insiders/).
- [NAudio WASAPI facilities](https://github.com/naudio/NAudio/blob/main/src/NAudio.Wasapi/README.md), [Windows MP3 encoder formats](https://learn.microsoft.com/en-us/windows/win32/medfound/mp3-audio-encoder).
