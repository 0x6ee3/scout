# 0007 — Scribe native-format capture and separate outputs

Date: 2026-10-22. Status: Accepted. Scope: Scout.Scribe v1, M1 capture experiment. Implementation: refactored; synthetic verification passed, hardware acceptance pending.

## Context

The mixed-output experiment made it difficult to distinguish microphone capture, playback capture and conversion problems. Hardware feedback reported audible microphone audio but missing PC playback. The user requested native-format capture followed by MP3 conversion, with separate files to test each source directly.

## Decision

Use fresh NAudio `WasapiRecorderBuilder` instances for the default microphone and default render endpoint with loopback enabled. Do not request a capture format or persist endpoint IDs. Resolve defaults at each Start; keep the endpoints for that session. Stop and Start after a Windows default change. Multi-endpoint aggregation and automatic mid-session migration remain outside this experiment.

Queue each source in its native format independently, then use a separate WMF resampler and MP3 encoder per source. Produce `microphone.mp3` and `playback.mp3` in the timestamped session directory. Each output is 48 kHz mono at 256 kbps. Conversion streams while recording; no whole-session PCM staging file is required. Do not mix sources or apply mixing headroom. Preserve silence, use bounded queues and retain five-second checkpoints for both outputs.

## Alternatives considered

Keeping capture-time format conversion and a mixed file preserves the earlier output contract but hides which capture/conversion stage failed. Saving complete PCM files before conversion increases storage and postpones the directly playable MP3 crash-preservation check. Independent streaming conversion allows both sources to be inspected immediately.

## Consequences

This partially supersedes the single mixed-file and capture-format approach in [0004](0004-scribe-recording-baseline.md), [0005](0005-scribe-audio-durability.md) and [0006](0006-scribe-default-devices-and-capture-stack.md). Other stack, device, durability and tray decisions remain accepted. A 90-minute session now uses approximately 346 MB across two MP3s. Any later combined export requires a new explicit change; this experiment does not merge files. Cross-source synchronization is not established by the separate-file synthetic tests.

Synthetic checks cover different native formats and source tones, fresh source resolution, exact output format, normal stop and forced termination preserving both files. Real microphone/playback, long-session behavior, device loss and Windows Media Player acceptance remain hardware gates.

## References

- [M1 plan](../../cycles/scout-scribe-v1/milestones/MILESTONE_1_PLAN.md)
- [Experiment guide and evidence](../../cycles/scout-scribe-v1/milestones/MILESTONE_1_CAPTURE_EXPERIMENT.md)
