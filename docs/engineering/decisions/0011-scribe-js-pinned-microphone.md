# 0011 — Allow a pinned microphone in Scribe.js

Date: 2026-10-22. Status: Accepted. Scope: Scribe.js experiment.
Implementation: microphone setting, persistence and exact endpoint capture added
in 0.1.1. Refines Scribe.js's default-only input decision in ADR 0010; the .NET
Scout.Scribe product and its historical ADRs are unchanged.

## Context

The user wants the C920 camera as microphone while retaining high-quality Bose
headset playback. Windows defaults can change independently of the app, and opening
a Bluetooth microphone can change the headset's audio profile. A recording input
should remain an explicit, predictable user choice.

## Decision

Add a Microphone choice to the existing separate Settings window. Enumerate active
input IDs/names without opening capture streams. Save the chosen endpoint ID with
the recording-folder preference; keep Windows default as a supported option and
as the migration behavior for older settings. Capture the selected endpoint exactly.
If it is unavailable, fail visibly; never fall back to another microphone. Disable
edits during recording/finalization and retain Save/Cancel semantics.

Request non-communications `AudioCategory_Other` on the physical capture client
before obtaining its format/initializing it. Record whether Windows accepted that
policy. Virtual application-loopback clients may lack IAudioClient2, so record that
limitation rather than claiming their category was explicitly changed. Do not alter
system defaults, headset services, registry ducking preferences or other apps.

## Consequences

A saved camera choice is stable even if Windows later changes its default input.
An unavailable camera requires an explicit settings change rather than silently
using a headset. This setting does not control other applications or guarantee a
particular Bluetooth profile under every Windows/device condition. Short isolated
and combined probes are recorded in the experiment verification report; broader
hardware acceptance remains separate.

## References

- [Scribe.js usage](../../../src/Scribe.js/README.md).
- [Verification and hardware observations](../../../src/Scribe.js/VERIFICATION.md).
- [ADR 0010](0010-scribe-js-electron-experiment.md).
- [Microsoft Bluetooth profile selection](https://learn.microsoft.com/en-us/windows-hardware/drivers/bluetooth/bluetooth-classic-audio).
