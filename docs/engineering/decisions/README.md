# Architecture Decision Records

Use Architecture Decision Records (ADRs) to explain significant choices and why they were made. Include substantial architecture, product-boundary and durable UX/engineering decisions. Routine implementation details and small visual tweaks do not need separate records.

| Record | Status | Scope |
|---|---|---|
| [0001 — Product development cycles and Scout.Scribe](0001-product-development-cycles.md) | Accepted | Independent product cycles, historical documentation and companion naming. |
| [0002 — Web v1 desktop UX](0002-web-v1-desktop-ux.md) | Accepted; implemented in M6 | Finish Web first; 4K composition, page actions and central dialogs. |
| [0003 — Shared Web presentation patterns](0003-web-presentation-patterns.md) | Accepted and implemented | Work centered composition, shared forms, native dialogs and the private/shared layout boundary. |

## Convention

Create `NNNN-short-descriptive-name.md` using the [template](TEMPLATE.md), taking the next unused number. Record the date, status and affected product/cycle. Explain context, decision, meaningful alternatives and consequences; link the relevant plan and earlier decisions. Keep the record concise enough to understand without replaying the chat.

Use Proposed until a direction is accepted; Accepted is a decision status, not a claim that implementation exists. Rejected proposals may be retained when they explain an important choice. If an accepted decision changes materially, add a new ADR identifying what it supersedes rather than rewriting the old rationale. Update the index to show the successor; incidental link repairs are allowed. Partial supersession should name the affected part rather than imply the whole earlier decision is obsolete.

Milestone plans describe the work and acceptance checks. Delivery/release records describe what actually happened. ADRs explain why the direction was chosen. Keep those responsibilities distinct and link between them. Significant later changes should continue this collection across products and development cycles.
