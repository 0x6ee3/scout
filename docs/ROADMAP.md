# Scout — Portfolio roadmap

Date: 16 October 2026. Detailed milestone scope belongs to each product's development cycle. This overview tracks independent cycles; it does not impose a shared milestone sequence or release dependency.

| Cycle | Current state | Next work |
|---|---|---|
| [Scout.Web v1](cycles/scout-web-v1/ROADMAP.md) | M1–M6 implemented; [release baseline](cycles/scout-web-v1/RELEASE_SUMMARY.md) documented. | Final physical-display acceptance and real interview use; retain verification limits. |
| [Scout.Scribe v1](cycles/scout-scribe-v1/ROADMAP.md) | Planned independent Windows desktop companion; no implementation yet. | M1 recording, M2 local transcription, M3 local fact extraction and review. |
| Scout.Web v2 | No active cycle or committed scope. | Use real interview experience to prioritize ideas before creating a cycle. |

Web v1 does not wait for Scribe. Scribe runs without Web; Web accepts audio and transcripts from Scribe or other tools. Start with existing manual file import rather than shared storage internals or browser control.

Current execution priority: visually accept the implemented [Web M6](cycles/scout-web-v1/milestones/MILESTONE_6_DELIVERY.md) on the actual display before starting Scribe implementation or adding detail to Scribe plans. M6 retains the top navigation and delivers visible context, 4K-first three-column layouts, compact actions/icons and central dialogs. See [ADR 0002](engineering/decisions/0002-web-v1-desktop-ux.md).

After Web's UI overhaul, use Scout in several real interviews and collect feedback. Further Web features, including CV and LLM-assisted capabilities, remain in the [feature wishlist](FEATURE_WISHLIST.md) until selected. Scribe's own three-milestone sequence is tracked separately; its later milestones need detailed plans informed by earlier delivery evidence.

The [documentation overview](README.md) explains living documents and historical records. The [cycle decision](engineering/decisions/0001-product-development-cycles.md) supersedes the former M6A/M6B arrangement while preserving that proposal in the [Scribe archive](cycles/scout-scribe-v1/archive/README.md).
