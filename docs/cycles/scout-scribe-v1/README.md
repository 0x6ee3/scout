# Scout.Scribe v1 development cycle

Status: planned; no Scribe executable, models or processing infrastructure have been implemented. Scout.Scribe is the name of the independent companion previously proposed as Scout.Recorder.

| Milestone | Scope | Planning / delivery status |
|---|---|---|
| M1 — Recording | Independent Windows tray application capturing microphone and playback audio. | [Detailed plan](milestones/MILESTONE_1_PLAN.md); not implemented. |
| M2 — Local transcription | Optional local speech-to-text, existing-file input and portable TXT export. | [Roadmap outline](ROADMAP.md#milestone-2--local-transcription); detailed plan follows M1 delivery. |
| M3 — Fact extraction and review | Optional local LLM processing of transcripts/manual text, with source-linked proposals and user review. | [Roadmap outline](ROADMAP.md#milestone-3--fact-extraction-and-review); detailed plan follows M2 delivery. |

Read the [product definition](../../products/scout-scribe/PRODUCT.md), [technical boundary](../../products/scout-scribe/TECHNICAL.md) and [cycle roadmap](ROADMAP.md) together. Each milestone must preserve independent manual workflows and produce its own delivery record. Web v1 can ship before this cycle is complete.

The [archive](archive/README.md) preserves the earlier M6A/Scout.Recorder proposal and roadmap snapshot. They explain how this cycle arose and are superseded, not current scope. The [product-cycle decision](../../engineering/decisions/0001-product-development-cycles.md) records the change.

At cycle completion, add `RELEASE_SUMMARY.md` with the actual version, delivered artifacts, supported hardware/models, verification and known limits. Until then, plans describe intended behavior rather than existing functionality.
