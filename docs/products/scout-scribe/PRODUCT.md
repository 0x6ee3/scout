# Scout.Scribe — Product

Status: planned product; no desktop executable or processing features are implemented yet. This living document describes direction; consult the [Scribe v1 cycle](../../cycles/scout-scribe-v1/README.md) for milestone scope and evidence.

Scout.Scribe turns consented conversations into reviewable local material: recorded audio, transcripts and proposed facts. It is a small Windows desktop companion, primarily operated through the system tray with compact settings/status and processing review windows as needed. The name covers the complete workflow rather than capture alone.

## Capabilities by milestone

| Scribe v1 milestone | Intended outcome |
|---|---|
| M1 — Recording | Explicitly start/stop microphone and selected playback capture, see clear status, preserve playable audio and handle interruption. |
| M2 — Local transcription | Turn a completed or externally supplied recording into a reviewed, portable UTF-8 transcript using local speech-to-text. |
| M3 — Fact extraction and review | Process a transcript or manually supplied text with a local LLM; inspect, correct, reject or approve source-linked proposals and save a separate reviewed result. |

Capture works without transcription or LLM tools. Transcription accepts existing audio; extraction accepts existing text. Starting the application never starts recording or downloads models. Recording requires explicit initiation after participant consent obtained outside the application. Processing is local and explicitly selected or enabled through an opt-in post-stop transcription setting.

## Relationship with Scout.Web

Scribe runs independently of Web and never needs its database or interview IDs. Web remains usable with other audio/transcript tools or manual notes alone. Start with manual import of finalized audio and UTF-8 TXT through Web's existing workflows; the user selects the interview and any optional recording association there. A Scribe acknowledgment does not automatically supply Web's per-file recording acknowledgment.

LLM proposals are not verified facts. The initial result is a separate reviewed text artifact; the user can save it or copy approved information into Web. Automatic structured transfer, browser control and richer provenance ingestion require later integration design.

## Boundaries

Preserve original audio and text, expose failures clearly and retain available recovery data. Keep sources, model output and reviewed results distinct. No implicit remote processing, automatic promotion of claims, source overwrite or dependence on Web's release schedule. Additional operating systems, browser integration and advanced features remain in the [wishlist](../../FEATURE_WISHLIST.md).

See the [technical design](TECHNICAL.md), [cycle roadmap](../../cycles/scout-scribe-v1/ROADMAP.md) and [recording plan](../../cycles/scout-scribe-v1/milestones/MILESTONE_1_PLAN.md) for proposed implementation and acceptance details.
