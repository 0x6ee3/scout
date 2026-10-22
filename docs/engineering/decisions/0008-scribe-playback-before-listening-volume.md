# 0008 — Record application playback before listening volume

Date: 2026-10-22. Status: Accepted. Scope: Scout.Scribe v1 M1. Implementation: application-loopback capture implemented and measured on the LG/AMD output; broader hardware acceptance remains open.

## Context

The user requested playback recording independent of Windows listening volume, with no normalization or inverse-volume gain and with separate microphone/playback files. The earlier endpoint-loopback path captured application audio after endpoint attenuation on the tested Windows 11 build 10.0.26300.0 / LG HDR 4K / AMD endpoint. Documentation alone was insufficient to establish this hardware's behavior.

With a generated 1 kHz tone at amplitude 0.05, endpoint loopback measured 0.00302865 at Windows 20% (-24.3544 dB), 0.00091461 at 10% (-34.7547 dB), and 0.00302865 after restoring 20%. The change matches Windows attenuation. Native endpoint loopback without NAudio's conversion flags produced the same result, excluding capture-format conversion as the cause.

## Decision

Use NAudio's WASAPI application-loopback API: `WithProcessLoopback(Environment.ProcessId, ExcludeTargetProcessTree)` and `BuildAsync`. Capture all other applications before endpoint listening-volume processing. Exclude Scribe and its descendants. No inverse gain, volume monitoring, normalization or changes to Windows volume during normal recording.

The microphone still resolves the current Windows default input at each Start, captures its native format and remains fixed during the session. The virtual application-loopback device has no native endpoint mix format, so request 48 kHz stereo IEEE float explicitly. Convert both sources independently into `microphone.mp3` and `playback.mp3`, each mono/48 kHz/256 kbps with five-second checkpoints.

## Alternatives considered

Inverse endpoint-volume gain reproduces the level only if the tap actually includes that volume; it cannot recover muted/lost samples and would be wrong on pre-volume devices. Normalization alters the requested source-level behavior. Removing format-conversion flags from endpoint capture did not change the measured attenuation.

## Consequences

This partially supersedes endpoint-only playback scope and native-endpoint-format requirements in [0006](0006-scribe-default-devices-and-capture-stack.md) and [0007](0007-scribe-native-separate-capture.md). Playback is now endpoint-independent: application audio on non-default output devices can also be captured. This is an explicit scope change, not a claim that the API filters to the current default output. It suits the user's single-output setup and follows playback without caching an output device ID. Application/player volume and source content levels still affect recordings; listening-volume independence is not loudness normalization.

On the same hardware, application-loopback amplitude remained 0.0500000 at Windows 20%, 10% and restored 20%. An end-to-end session with a synthetic microphone and actual application playback produced both MP3s. Decoded playback amplitude was 0.0500001 in full one-second windows across the volume changes; the microphone tone remained approximately 0.199928. The microphone was not opened by these diagnostics. Windows volume was restored to 20%. Output mute, Bluetooth routing, long sessions, device changes and abrupt termination of this live path still need broader hardware acceptance; offline two-file crash regressions remain applicable.

## References

- [Microsoft application-loopback sample](https://learn.microsoft.com/en-us/samples/microsoft/windows-classic-samples/applicationloopbackaudio-sample/) — process exclusion and endpoint-independent scope.
- [Microsoft stream options](https://learn.microsoft.com/en-us/windows/win32/api/audioclient/ne-audioclient-audclnt_streamoptions) — pre/post-volume semantics; observed endpoint behavior must be verified on hardware.
- [Capture experiment and reproduction](../../cycles/scout-scribe-v1/milestones/MILESTONE_1_CAPTURE_EXPERIMENT.md).
