# Scout — Portfolio roadmap

Updated: 29 October 2026. Detailed milestone scope belongs to each product's development cycle. This overview tracks independent cycles; it does not impose a shared milestone sequence or release dependency.

| Cycle | Current state | Next work |
|---|---|---|
| [Scout.Web v1](cycles/scout-web-v1/ROADMAP.md) | M1–M6 implemented; [release baseline](cycles/scout-web-v1/RELEASE_SUMMARY.md) documented. | Final physical-display acceptance and real interview use; retain verification limits. |
| [Scout.Scribe v1](cycles/scout-scribe-v1/ROADMAP.md) | M1 capture experiment and tray/settings implemented; M1 is not yet delivered. | Remaining recording reliability and hardware acceptance; then M2 local transcription and M3 fact extraction/review. |
| Scout.Web v2 | No active cycle or committed scope. | Use real interview experience to prioritize ideas before creating a cycle. |

Web v1 does not wait for Scribe. Scribe runs without Web; Web accepts audio and transcripts from Scribe or other tools. Start with existing manual file import rather than shared storage internals or browser control.

Web M6 is implemented; physical-display acceptance remains open in its [delivery record](cycles/scout-web-v1/milestones/MILESTONE_6_DELIVERY.md). Scribe M1 implementation is underway independently. The earlier Web-first sequencing in [ADR 0002](engineering/decisions/0002-web-v1-desktop-ux.md) records the priority at that time.

The independent [Scribe.js Electron experiment](../src/Scribe.js/README.md) lives under `src/Scribe.js`, outside the .NET solution. It compares recording implementations and does not establish delivery of .NET Scribe M1 or add transcription/extraction scope. Its [verification report](../src/Scribe.js/VERIFICATION.md) records its own acceptance limits.

After Web's UI overhaul, use Scout in several real interviews and collect feedback. Further Web features, including CV and LLM-assisted capabilities, remain in the [feature wishlist](FEATURE_WISHLIST.md) until selected. Scribe's own three-milestone sequence is tracked separately; its later milestones need detailed plans informed by earlier delivery evidence.

The [documentation overview](README.md) explains living documents and historical records. The [cycle decision](engineering/decisions/0001-product-development-cycles.md) supersedes the former M6A/M6B arrangement while preserving that proposal in the [Scribe archive](cycles/scout-scribe-v1/archive/README.md).
