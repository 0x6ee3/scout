# 0002 — Web v1 desktop UX

Date: 16 October 2026. Status: accepted product/UX direction from the user; implementation planned. Scope: Scout.Web v1 M6. This refines sequencing under [ADR 0001](0001-product-development-cycles.md), whose independent-cycle and historical-preservation decisions remain in force.

## Context

The user has exercised the delivered functionality and wants to finish Web 1.0 before starting Scribe or expanding its plans. Current detail/form widths and vertically stacked related sections underuse a 4K display. Entity context, scattered actions and below-form confirmations make navigation and operations harder to follow. The existing top navigation is acceptable.

## Decision

Prioritize Web M6 and close its v1 cycle first. Keep the current top navigation; give ordinary views a consistent visible breadcrumb, title/status, entity action strip and responsive content composition. Design for the user's 4K display first, using three complementary columns on information-rich views. Do not use a 900p/Full HD single-column baseline to limit the main design. Preserve readable text and record actual browser viewport, scaling and zoom in visual verification.

Group generic actions with consistent icons and accessible labels/tooltips, while keeping primary actions discoverable. Use central dialogs for focused add/edit/select tasks, archive/unlink/discard confirmations and confirmed explicit-save outcomes. Keep field validation near fields and live Workspace autosave/repetitive interaction status unobtrusive. No new permanent-delete capability is implied.

Preserve business behavior, file/history retention, routes and Workspace privacy/saving contracts. This is a frontend milestone; no migration or backend expansion is planned. Implementation details and later significant deviations receive their own decisions as needed.

## Alternatives considered

- Retain narrow cards and only reduce padding: insufficient use of available width and insufficient improvement to related-work context.
- Establish two columns or a small-screen-first design as the primary constraint: does not meet the user's explicit 4K/three-column priority. Reduced-column behavior remains a secondary adaptation.
- Convert every interaction to an icon-only action or modal: weakens discoverability and interrupts live interview work. Use icons/dialogs by task, with visible labels and inline status where appropriate.
- Start Scribe while polishing Web: increases concurrent unfinished work; finish the existing product cycle first.

## Consequences

The older living product guideline favoring “minimal modal dialogs” is replaced with deliberate central dialogs for decisions and focused tasks. Information-rich pages need actual composition work; widening every paragraph is insufficient. Accessible dialog behavior, truthful save feedback and draft/privacy preservation are acceptance requirements, not visual polish alone.

Ordinary page routes remain useful for large editors, direct entry and recovery. Browser-managed reload/tab-close prompts cannot be restyled as application dialogs. Display resolution alone does not establish available CSS width, so user setup must be recorded during visual validation. Smaller-display refinements may follow the 4K target, but narrowing/zooming must not make existing actions inaccessible.

No Scribe code, new Scribe plan detail or Web application code is added by this planning change. M6 completion and the v1 release require later delivery evidence.

## References

- [Web M6 plan](../../cycles/scout-web-v1/milestones/MILESTONE_6_PLAN.md).
- [Web UX guidelines](../../products/scout-web/PRODUCT.md#6-ux-guidelines).
- [M5 delivery and verification limits](../../cycles/scout-web-v1/milestones/MILESTONE_5_DELIVERY.md).
- [ADR convention](README.md).
