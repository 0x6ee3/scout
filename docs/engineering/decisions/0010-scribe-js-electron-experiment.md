# 0010 — Independent Scribe.js Electron experiment

Location update (29 October 2026): the project has moved to `src/Scribe.js`; root-level paths in the original decision below describe its initial location. See the [current usage guide](../../../src/Scribe.js/README.md). [ADR 0011](0011-scribe-js-pinned-microphone.md) refines microphone selection and [ADR 0012](0012-scribe-js-live-merged-recording.md) adds combined output.

Date: 2026-10-22. Status: Accepted. Scope: experimental Scribe.js product comparison.
Implementation: independent Electron project and portable Windows build; hardware
acceptance remains open. This does not supersede the .NET Scribe decisions.

## Context

The user requested the current Scout.Scribe product rebuilt as an Electron
application, using its documented features as the reference rather than translating
its classes. The .NET working tree contains ongoing Scribe UX changes which must
remain intact. A default Electron desktop-loopback implementation would not by
itself establish the pre-listening-volume playback behavior required by ADR 0008.

## Decision

Create a self-contained root-level `Scribe.js` project with Electron-native tray,
single-instance lifecycle, compact recording panel, separate Settings/Details,
restricted preload IPC and opt-in current-user sign-in startup. Preserve the
implemented recording product boundary described in ADRs 0007–0009; later planned
transcription and extraction are outside this experiment.

Use a utility process for capture/storage, direct Windows WASAPI calls through the
prebuilt Koffi N-API addon, and two independent FFmpeg MP3 child processes. Resolve
the default microphone anew for each session and retain native PCM/float packets.
Activate Windows application loopback with the Electron main-process tree excluded
and an explicit 48 kHz stereo float format; never query the virtual source's endpoint
mix format. Pace timestamped packets against a shared clock, preserving silence,
with bounded buffering. Produce two mono/48 kHz/256 kbps MP3s and a diagnostic journal,
flushing both file descriptors and journal at five-second checkpoints.

Use separate `Documents/Scribe.js/recordings`, `%APPDATA%/Scribe.js` preferences and
a separate login registration so the experiments can coexist. Sign-in and ordinary
launch are idle. Exit and suspend request finalization; a finalization deadline
reports failure while preserving partial files. Never modify the original Scribe's
settings, recordings or startup registration.

## Alternatives considered

- Porting the Forms/C# classes or launching the original executable would not
  exercise Electron product architecture or provide an independent experiment.
- Chromium desktop loopback is convenient, but does not establish the required
  endpoint-independent, excluded-process capture semantics.
- A general-purpose native audio package was evaluated; its process-loopback source
  queries `GetMixFormat` on a virtual source and assumes float microphone samples.
  Direct bindings avoid those compatibility assumptions and a native compiler.
- MediaRecorder/WebM would change the established MP3 export and crash-durability
  behavior. Streaming MP3 encoders preserve those user-visible requirements.

## Consequences

The Electron project is independent of the .NET build and does not need .NET at
runtime. It has a larger portable footprint and ships native Koffi/FFmpeg binaries.
Windows COM bindings, callback lifetimes and source timestamp handling require
focused testing. FFmpeg distribution licensing must be reviewed before publication.
Synthetic decode/crash tests and virtual-source initialization do not prove physical
microphone quality, listening-volume independence or long-session hardware behavior.
Those gates stay explicit in the experiment's verification report.

## References

- [Scribe.js usage and architecture](../../../src/Scribe.js/README.md).
- [Scribe.js verification](../../../src/Scribe.js/VERIFICATION.md).
- [ADR 0007: native/separate sources](0007-scribe-native-separate-capture.md).
- [ADR 0008: pre-volume playback](0008-scribe-playback-before-listening-volume.md).
- [ADR 0009: tray/settings UX](0009-scribe-tray-recording-panel.md).
- [Microsoft application-loopback sample](https://learn.microsoft.com/en-us/samples/microsoft/windows-classic-samples/applicationloopbackaudio-sample/).
- [Koffi native callbacks and lifetime](https://koffi.dev/callbacks).
