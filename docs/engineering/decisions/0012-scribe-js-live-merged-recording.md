# 0012 — Live merged recording in Scribe.js

Date: 2026-10-22. Status: Accepted. Scope: Scribe.js 0.1.2.
Implementation: live merged MP3 and retained source tracks. Adds combined output
to the separate-output Electron experiment in ADR 0010; does not change the .NET
Scout.Scribe implementation or rewrite the earlier ADR 0007 capture experiment.

## Context

After confirming the corrected microphone capture sounds good, the user requested
keeping both outputs at 48 kHz and adding the merge back to produce one audio file.
The separate outputs remain useful for diagnostics and recovery. The existing
streaming/checkpointed capture should retain its interruption behavior.

## Decision

Create `recording.mp3` as the primary combined file in every new session, alongside
`microphone.mp3`, `playback.mp3` and `events.jsonl`. Encode all three live as mono,
48 kHz, 256 kbps MP3. Retain the source files; no existing recordings are rewritten.

Feed the same paced, aligned native PCM to the independent source encoders and a
third FFmpeg process with two anonymous input pipes. Resample sources independently
inside that process. Average each source's channels to mono, then mix with fixed
weights of 0.5 microphone and 0.5 playback. Disable dynamic normalization and dropout
gain changes. Preserve leading silence, gaps and the longer tail; do not concatenate
or time-stretch sources. This establishes fixed mixing headroom without a stereo
channel summation boost. The source diagnostic MP3 conversion path remains unchanged.

Checkpoint the combined file together with both source files every five seconds.
Only report recording ready when every encoder produces output. Finalize all three
on Stop/Exit; encoder failure remains a session failure with partial output retained.
The UI identifies the combined file as the saved recording and Details describes
the retained source tracks.

## Alternatives considered

Post-stop mixing of the two MP3s is simpler but introduces another lossy encoding
generation, a potentially long merge on Exit and no combined output after a crash.
Replacing source capture with a shared native mixed stream would disturb the now
verified microphone/headset capture path. Parallel PCM-fed encoding preserves it.

## Consequences

The combined file contains both sources without an extra MP3 decoding generation
and is independently playable after checkpoints. Three MP3s use approximately
346 MB per hour total (about 518 MB over 90 minutes), plus journal/format overhead.
The extra encoder costs CPU and bounded pipe buffering. A lone source in the mix
retains half gain; it is not automatically amplified when the other source is quiet
or ends. Stereo information is deliberately reduced to mono for this voice workflow.

## References

- [Scribe.js usage](../../../src/Scribe.js/README.md).
- [Verification evidence](../../../src/Scribe.js/VERIFICATION.md).
- [ADR 0010](0010-scribe-js-electron-experiment.md), [ADR 0011](0011-scribe-js-pinned-microphone.md).
- [Historical separate-output experiment](0007-scribe-native-separate-capture.md).
- [FFmpeg amix documentation](https://ffmpeg.org/ffmpeg-filters.html#amix).
