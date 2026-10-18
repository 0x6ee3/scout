# Scout.Scribe v1 — Roadmap

Date: 16 October 2026. A separate product development cycle for recording, transcription and local fact extraction. All milestones below are planned, not delivered. Detailed later plans must use earlier delivery evidence rather than assume proposals worked unchanged.

## Milestone 1 — Recording

Deliver an independently launched Windows tray executable capturing the selected microphone and call playback endpoint after explicit consent acknowledgment and start. Include compact device/status controls, clear stop/finalization, preserved audio, bounded segments and visible failures/recovery.

Acceptance: with Scout.Web stopped, capture both sides of a fictional call, stop and locate playable output, then manually import/play it in Web. No Web code changes or database access are required for the file handoff.

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
