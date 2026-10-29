# Scout.Scribe v1 — Roadmap

Current-status update (29 October 2026): the capture experiment and tray/recording panel/persistent settings are implemented; [ADR 0009](../../engineering/decisions/0009-scribe-tray-recording-panel.md) adds opt-in sign-in startup. Hardware, interactive sign-in and remaining reliability acceptance are pending; M1 is not yet delivered. The dated milestone direction below is retained; follow the [current usage guide](../../../src/Scout.Scribe/README.md) for operation. The independent [Scribe.js experiment](../../../src/Scribe.js/README.md) has its own implementation and verification.

Date: 16 October 2026; updated 21 October 2026. A separate product development cycle for recording, transcription and local fact extraction. M1 package 1 has an [implemented capture experiment](milestones/MILESTONE_1_CAPTURE_EXPERIMENT.md) with synthetic verification; hardware acceptance and remaining packages are pending. No milestone is delivered yet. Detailed later plans must use earlier delivery evidence rather than assume proposals worked unchanged.

## Milestone 1 — Recording

Deliver an independently launched tray executable for the latest Windows 11 x64 only, capturing the Windows-default microphone and application playback after explicit start. Playback uses endpoint-independent application loopback before listening-volume attenuation, excluding Scribe and its descendants, under [ADR 0008](../../engineering/decisions/0008-scribe-playback-before-listening-volume.md). Users change the default microphone in Windows before Start; no Scribe selector is needed. Consent is handled outside Scribe. Include output-folder settings, distinct tray icons, clear stop/finalization, separate microphone/playback MP3s and retained partial output. Failure requires manual restart into a new session; pause is optional.

Acceptance: with Scout.Web stopped, capture both sides of a fictional call, stop and locate playable output, then manually import/play it in Web. No Web code changes or database access are required for the file handoff.

Accepted implementation direction: .NET 10 Windows Forms, NAudio/WASAPI native-format capture and independent Windows Media Foundation conversion, producing separate microphone/playback mono MP3s at 256 kbps each for 60–90 minute interviews. See the [native/separate-output decision](../../engineering/decisions/0007-scribe-native-separate-capture.md), which supersedes the earlier mixed-file experiment. Crash-usable committed audio is mandatory; write continuously with five-second checkpoints for both files and measure remaining encoder/output tail and latency. Add best-effort exception-path tray cleanup without relying on it for audio survival. Whole-system crash/power-outage recovery is outside M1. A copyable framework-dependent build folder is sufficient; external tools/codecs are permitted when needed. See the [format/durability decision](../../engineering/decisions/0005-scribe-audio-durability.md) and its [successor](../../engineering/decisions/0006-scribe-default-devices-and-capture-stack.md).

See the [M1 plan](milestones/MILESTONE_1_PLAN.md) for packages, artifact limits and verification.

## Milestone 2 — Local transcription

Produce a transcript from completed capture or a user-selected existing audio file using a local speech-to-text engine. Offer an opt-in automatic post-stop step once configured, plus explicit processing, progress/errors, cancellation, retry and interruption handling. Preserve original audio and use separate conversion copies where necessary.

Export strict UTF-8 TXT compatible with Web's current import: at most 5 MiB and 1,000,000 .NET string characters per import, with explicit ordered parts if needed rather than truncation. Recording and Web remain usable without models. Select and verify engines, model/hardware requirements and packaging in the later M2 plan; whisper.cpp/FFmpeg are candidates only.

Acceptance: locally transcribe recorded and existing-file audio, review/export text and import it into Web without requiring audio registration. Failure/cancellation preserves source audio and supports retry. Record engine/model/settings and source identity locally; do not promise automatic ingestion of that provenance by Web.

## Milestone 3 — Fact extraction and review

Use an explicitly configured local LLM to propose facts and other useful information from a selected transcript or manually supplied interview text. Input must not require a Scribe recording/transcription run. Preserve source snapshots and distinguish reported statements, uncertain interpretations and user-approved information. Include source references, review/edit/reject, status, cancellation/retry and clear model failure handling.

Decide exact extraction fields, context/chunking limits, model/hardware requirements, local output format and packaging in the later M3 plan. A local runtime such as Ollama is a candidate, not a selected dependency. The minimum handoff is a user-reviewed text report that can be saved or copied into Web manually. Automatic structured promotion into Web requires a separately designed integration and is outside this baseline.

Acceptance: select local text, run extraction, inspect proposals alongside source passages, reject/edit/approve them and save a separate reviewed result without changing the original. Unsupported conclusions remain visible as uncertain; no suggestion becomes a verified Web fact automatically. Manual transcription/review remains usable without the LLM.

After actual delivery, publish the cycle release summary. Additional integration, operating systems and advanced Web/CV features stay in the [wishlist](../../FEATURE_WISHLIST.md) until prioritized.
