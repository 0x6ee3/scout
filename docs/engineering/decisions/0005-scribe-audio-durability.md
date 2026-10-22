# 0005 — Scribe audio format and crash durability

Date: 2026-10-21. Status: Accepted requirements; checkpoint interval and encoder implementation require feasibility verification. Scope: Scout.Scribe v1 M1. Partially supersedes [ADR 0004](0004-scribe-recording-baseline.md): accepts Windows Forms, replaces its proposed 128 kbps format with 256 kbps, and strengthens partial preservation into a delivery gate. Implementation status: planning only.

## Context

The user accepts Windows Forms and mono MP3, chooses 256 kbps, and normally records 60–90 minute interviews. Preserving the recording before a crash is essential; waiting until Stop to make audio playable is unacceptable. A general crash handler should also attempt tray cleanup.

## Decision

Use C#/.NET Windows Forms with NotifyIcon. Record one mono MP3 at 256 kbps constant bitrate without routine segmentation. Audio payload is approximately 115.2 MB/hour or 172.8 MB/90 minutes, before small overheads. Web's 1 GiB ceiling corresponds to approximately 9.3 hours at this bitrate; no Web limit change is needed for the intended duration. Higher bitrate is a user-selected storage/quality tradeoff, not a claim of measured transcription improvement.

Stream encoded frames to the recording file continuously. Previously checkpointed audio must remain directly decodable after recorder termination without running Stop, a crash handler, a restart repair pass or a conversion of a whole-session temporary WAV. Crash usability is a release gate for the chosen encoder, not an optional best effort.

Checkpoint the complete pipeline: capture buffers, encoder buffering, encoded output and file buffers. Flushing only the FileStream cannot release audio still held by an encoder. Propose a one-second maximum end-to-end uncommitted tail under healthy storage, validated by forced-termination experiments; finalize the achievable bound during package 1 and document it explicitly. A one-minute flush interval would permit losing the last minute and is not the proposed default. Checkpoints include a disk flush where supported by the writer; an external encoder needs an equivalent verified strategy.

Measure actual surviving audio against known timed signals at arbitrary termination points, including around checkpoints and MP3 frame boundaries, and after a 90-minute run. The saved prefix must play without manual repair; metadata such as duration/seek indexes may be incomplete and must be tested separately. Tiny files terminated before the first checkpoint and the measured uncommitted tail are explicit limits. Disk failure/full conditions must report failure and preserve the prior committed prefix; bytes never captured or successfully written cannot be guaranteed. OS/power loss and storage hardware failure are separate from an application-process crash and must not be claimed tested without evidence.

Install top-level exception handling before tray/capture initialization. Handle expected capture/writer failures at their operation boundaries. Handle WinForms UI exceptions through Application.ThreadException (with explicit exception-mode policy), and use AppDomain.UnhandledException for last-chance notification. Route manageable failures through a bounded, idempotent shutdown path that marks non-recording, attempts audio flush/finalization, logs the error, hides/disposes the tray icon and exits. Last-chance handlers perform minimal best-effort cleanup and must not block waiting for a UI thread or locks that may be held by the faulting thread. Do not continue an interview in an unknown application state.

Crash handlers cannot run after forced process termination, FailFast or every catastrophic runtime/native failure. Audio durability must work without them. Guaranteed external tray cleanup would require an independently surviving process, adding lifecycle complexity; no watchdog is included in M1. Restart creates a new idle session and new file, as in ADR 0004.

## Alternatives considered

- Finalize-only encoding or retaining a whole interview in memory fails the crash-preservation requirement.
- Minute-scale flushing increases the potentially lost tail unnecessarily for these small local writes; the exact encoder/disk checkpoint strategy still needs measurement.
- Splitting every minute is not required: use one streaming MP3 if the selected encoder proves the committed prefix remains playable.
- A separate tray supervisor could improve hard-crash icon behavior, but is deferred to keep M1 a small application.

## Consequences

Select the capture/encoding dependency and stable versions through the experiment, with NAudio/WASAPI and Media Foundation or FFmpeg still candidates. Verify 256 kbps mono support, startup/streaming behavior and truncation tolerance explicitly. If a candidate cannot meet the durability gate, choose another encoder rather than weakening the requirement silently. Keep the local filesystem implementation; S3/object-storage semantics are future work.

## References

- [M1 plan](../../cycles/scout-scribe-v1/milestones/MILESTONE_1_PLAN.md), [technical design](../../products/scout-scribe/TECHNICAL.md), [ADR 0004](0004-scribe-recording-baseline.md).
- [WinForms ThreadException](https://learn.microsoft.com/en-us/dotnet/api/system.windows.forms.application.threadexception?view=windowsdesktop-10.0), [AppDomain.UnhandledException](https://learn.microsoft.com/en-us/dotnet/api/system.appdomain.unhandledexception), [FailFast](https://learn.microsoft.com/en-us/dotnet/api/system.environment.failfast).
- [FileStream.Flush](https://learn.microsoft.com/en-us/dotnet/api/system.io.filestream.flush?view=net-10.0), [Shell_NotifyIcon deletion](https://learn.microsoft.com/en-us/windows/win32/api/shellapi/nf-shellapi-shell_notifyiconw).
