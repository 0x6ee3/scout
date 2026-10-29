# Scout.Scribe v1 development cycle

Status: M1 produces separate microphone/playback MP3s and includes quiet tray startup, a compact recording panel, persistent settings and opt-in sign-in startup under [ADR 0009](../../engineering/decisions/0009-scribe-tray-recording-panel.md). Default-microphone capture and application playback before listening-volume attenuation are implemented; LG/AMD hardware measurements and saved MP3s verify playback volume independence at Windows 20% and 10%. See [ADR 0008](../../engineering/decisions/0008-scribe-playback-before-listening-volume.md). Broader hardware, sign-in and remaining reliability acceptance are pending; M1 is not yet delivered. No transcription or extraction is implemented. Scout.Scribe is the name of the independent companion previously proposed as Scout.Recorder.

| Milestone | Scope | Planning / delivery status |
|---|---|---|
| M1 — Recording | Independent Windows tray application capturing microphone and playback audio. | [Detailed plan](milestones/MILESTONE_1_PLAN.md); [package 1 capture evidence](milestones/MILESTONE_1_CAPTURE_EXPERIMENT.md); [current usage](../../../src/Scout.Scribe/README.md). Recording/tray/settings implemented; remaining acceptance pending. |
| M2 — Local transcription | Local speech-to-text, existing-file input and portable TXT export. | [Roadmap outline](ROADMAP.md#milestone-2--local-transcription); detailed plan follows M1 delivery. |
| M3 — Fact extraction and review | Local LLM processing of transcripts/manual text, with source-linked proposals and user review. | [Roadmap outline](ROADMAP.md#milestone-3--fact-extraction-and-review); detailed plan follows M2 delivery. |

The independent [Scribe.js Electron experiment](../../../src/Scribe.js/README.md) has separate settings, combined output and its own [verification evidence](../../../src/Scribe.js/VERIFICATION.md). It does not change the .NET cycle's acceptance status.

Read the [product definition](../../products/scout-scribe/PRODUCT.md), [technical boundary](../../products/scout-scribe/TECHNICAL.md) and [cycle roadmap](ROADMAP.md) together. Each milestone must preserve independent manual workflows and produce its own delivery record. Web v1 can ship before this cycle is complete.

The [archive](archive/README.md) preserves the earlier M6A/Scout.Recorder proposal and roadmap snapshot. They explain how this cycle arose and are superseded, not current scope. The [product-cycle decision](../../engineering/decisions/0001-product-development-cycles.md) records the change.

At cycle completion, add `RELEASE_SUMMARY.md` with the actual version, delivered artifacts, supported hardware/models, verification and known limits. Until then, plans describe intended behavior rather than existing functionality.
