# Scout.Scribe — Technical design

Status: proposed architecture. No project, dependency, local engine or model is introduced by this documentation reorganization. The [recording plan](../../cycles/scout-scribe-v1/milestones/MILESTONE_1_PLAN.md) contains the current detailed scope; later milestones need their own plans.

## Process and project boundary

Propose `src/Scout.Scribe/Scout.Scribe.csproj` as a Windows-targeted .NET desktop executable and `tests/Scout.Scribe.Tests/` for focused lifecycle/file/failure checks. Prefer a small Windows Forms tray/settings shell; assess a microphone/loopback library such as NAudio during M1's feasibility package. Verify current supported versions, licenses and device behavior at implementation time.

Scribe owns its devices, settings, capture sessions and output directory, proposed as `<user home>/Documents/Scout.Scribe/recordings/`. It runs without Web, does not reference `Scout.Web`, open its SQLite database or write its managed assets, and requires no local control server. No common library or provider framework is required in advance. Add projects to the solution only when implementing them; Web remains separately buildable.

## Artifact handoff

| Artifact | Initial compatibility contract |
|---|---|
| Audio — M1 | Finalized mixed PCM WAV with microphone/playback, unique names up to 255 characters and nonempty segments below Web's 1 GiB cap. Verify actual browser playback and rollover continuity. Web also continues accepting MP3/M4A from other producers. |
| Transcript — M2 | Nonempty strict UTF-8 TXT, at most 5 MiB and 1,000,000 .NET string characters per Web import. Explicit ordered parts or clear oversize handling; no silent truncation. Optional source annotations do not create structured Web provenance. |
| Reviewed extraction — M3 | Separate user-reviewed text result, preserving source and proposed/approved distinctions. Exact export format is decided in the M3 plan. If TXT is intended for Web transcript import, it must meet that import's limits and is treated as an external imported text artifact. |

Default handoff is manual upload, which copies preserved output into Web's managed storage. Web's optional audio links only accept relative paths beneath its own `recordings/` directory; Scribe's arbitrary output path is not directly linkable. User-controlled copying is required for that route. No automatic watcher, API or structured promotion is implied. Independent Scribe output requires independent backup guidance.

## Local processing

M2 chooses a local speech-to-text engine and necessary conversion tools after M1 evidence; whisper.cpp and FFmpeg are candidates. Preserve originals and create processing copies. Include explicit input choice, opt-in post-stop transcription, progress, cancellation/retry, interruption handling, transcript limits and engine/model/settings provenance.

M3 chooses a local LLM/runtime, with Ollama as a candidate only. Define supported fields, source snapshots/references, long-input strategy, model/hardware requirements, result review and local export. Transcription and LLM interpretation are separate stages. Extraction accepts manually supplied text and cannot require a recording run. No silent promotion to verified facts or overwrite of Web records.

No remote provider is enabled by default or invoked implicitly. Required tools/models must be configured explicitly; their installation and operational requirements belong to implementation plans. The [shared constraints](../scout-web/TECHNICAL.md#5-important-constraints) remain applicable.

## Verification and evolution

M1 must prove real microphone/playback mixing, sustained synchronization, device loss, segment rollover, recovery and Web import/playback. Processing milestones add focused lifecycle, source-preservation, limit and review checks. Record actual environments and unperformed checks in delivery records. The [cycle roadmap](../../cycles/scout-scribe-v1/ROADMAP.md) and [decision record](../../engineering/decisions/0001-product-development-cycles.md) define ownership and history.
