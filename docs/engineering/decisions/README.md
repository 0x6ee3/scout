# Architecture Decision Records

Use Architecture Decision Records (ADRs) to explain significant choices and why they were made. Include substantial architecture, product-boundary and durable UX/engineering decisions. Routine implementation details and small visual tweaks do not need separate records.

| Record | Status | Scope |
|---|---|---|
| [0001 — Product development cycles and Scout.Scribe](0001-product-development-cycles.md) | Accepted | Independent product cycles, historical documentation and companion naming. |
| [0002 — Web v1 desktop UX](0002-web-v1-desktop-ux.md) | Accepted; implemented in M6 | Finish Web first; 4K composition, page actions and central dialogs. |
| [0003 — Shared Web presentation patterns](0003-web-presentation-patterns.md) | Accepted and implemented | Work centered composition, shared forms, native dialogs and the private/shared layout boundary. |
| [0004 — Scribe recording baseline](0004-scribe-recording-baseline.md) | Accepted; partially superseded by [0005](0005-scribe-audio-durability.md) and [0006](0006-scribe-default-devices-and-capture-stack.md) | Initial recording baseline; successor records refine format, durability, default devices and stack. |
| [0005 — Scribe audio format and crash durability](0005-scribe-audio-durability.md) | Accepted; encoder/checkpoint details superseded by [0006](0006-scribe-default-devices-and-capture-stack.md) | Windows Forms, mono MP3 256 kbps, playable checkpointed audio after crashes and best-effort tray cleanup. |
| [0006 — Scribe default devices and capture stack](0006-scribe-default-devices-and-capture-stack.md) | Accepted; implementation pending | NAudio/WMF, Windows defaults without device selectors, five-second checkpoints and Windows x64/.NET 10. |
| [0007 — Scribe native-format capture and separate outputs](0007-scribe-native-separate-capture.md) | Accepted; refactored, hardware gates pending | Partially supersedes 0004–0006: native-format capture and two independently converted MP3s for testing. |
| [0008 — Scribe playback before listening volume](0008-scribe-playback-before-listening-volume.md) | Accepted; LG/AMD volume independence verified | Partially supersedes 0006–0007: endpoint-independent application playback, default microphone and two files; no volume compensation. |

## Convention

Create `NNNN-short-descriptive-name.md` using the [template](TEMPLATE.md), taking the next unused number. Record the date, status and affected product/cycle. Explain context, decision, meaningful alternatives and consequences; link the relevant plan and earlier decisions. Keep the record concise enough to understand without replaying the chat.

Use Proposed until a direction is accepted; Accepted is a decision status, not a claim that implementation exists. Rejected proposals may be retained when they explain an important choice. If an accepted decision changes materially, add a new ADR identifying what it supersedes rather than rewriting the old rationale. Update the index to show the successor; incidental link repairs are allowed. Partial supersession should name the affected part rather than imply the whole earlier decision is obsolete.

Milestone plans describe the work and acceptance checks. Delivery/release records describe what actually happened. ADRs explain why the direction was chosen. Keep those responsibilities distinct and link between them. Significant later changes should continue this collection across products and development cycles.
