# Web v1 M6 — Package 1 design review

Subsequent decision, 16 October 2026: the user approved **Work centered** and authorized implementation across suitable pages. The original review below is retained as proposal history. Actual implementation and checks are recorded in [M6 delivery](../milestones/MILESTONE_6_DELIVERY.md).

Date: 16 October 2026. Status: inventory and interactive proposals prepared for review; composition is **not yet selected or approved for application rollout**. Application code and Scribe plans remain unchanged. The [M6 plan](../milestones/MILESTONE_6_PLAN.md), [ADR 0002](../../../engineering/decisions/0002-web-v1-desktop-ux.md) and [route/action inventory](M6_UX_INVENTORY.md) govern this work.

## Review material

The interactive source is [m6-layout-proposals.html](m6-layout-proposals.html), an HTML fragment displayed in the conversation. It contains three review screens: Application, Interview preparation and Position editor. The same fictional Nordlicht Systems application is used throughout. No personal data, backend operation, external message or actual download is involved.

Compare three named views of each screen:

- **Work centered — recommended:** context at left, the main task in the larger center column, resources at right. Predictable context placement supports repeated navigation across entity types.
- **Work first:** the main task occupies the larger left column, resources sit in the center and context moves right. This favors starting work at the reading edge but makes the relationship context less prominent.
- **Current structure:** a simplified reconstruction from source, using the existing 1,200px page and 720px card constraints. It illustrates vertical stacking and persistent small pickers. It is not a screenshot or a pixel-exact reproduction of the running application; the baseline groups are intentionally simplified for comparison.

The review-screen switcher and layout carousel belong to the design review, not the proposed Scout navigation. The actual main navigation remains recognizable. Optional review controls expose populated/sparse/long-name data, column spacing and simulated save failure. The prototype is presentation-only and deliberately does not implement every destination or field operation.

## Proposed page anatomy

Keep the existing top navigation and its destinations. Align a visible breadcrumb, title/status and compact entity actions below it. Use section actions beside headings, item actions beside their target and More for low-frequency actions. A generous desktop frame uses available width while paragraphs remain around a readable text measure. Column proportions begin at approximately **27% / 42% / 31%** of usable content width; these are proposals, not frozen pixel specifications.

| Review screen | Context column | Main work column | Supporting column |
|---|---|---|---|
| Application | Company/position, attempt/status, dates/notes and contacts. | Interview rounds, Prepare/Workspace entry and next actions. | Timeline, exact submitted-file evidence and documents. |
| Interview preparation | Schedule, participants, position/CV reference and private preparation context. | Selected question copies, intent/follow-ups and compact item actions. | Selected STAR content and private rehearsal notes. |
| Position editor | Identity, company/status, dates, URL, location and employment type. | Preserved original vacancy text, clearly read-only for an existing position. | Editable remote-work and personal notes with formatting preview. |

On new-position entry, the original-description region becomes editable until first save, matching current behavior. This prototype shows the existing-position case. Sparse application/preparation states keep useful Add/Manage actions without invented content. Long-name mode exercises title/breadcrumb wrapping; record names must not be truncated beyond recognition.

## Dialog and action walkthrough

1. On Application, use Manage contacts or Add contact. Inspect a focused central selection/form rather than a permanent form below the contacts list. Creating a company contact and selecting application contacts remain distinct operations.
2. Open the document unlink action. The confirmation explicitly says only the relationship is removed; original bytes and other associations remain. Cancel preserves the current view.
3. Inspect More → Archive. Archive remains restorable and distinct from permanent deletion. The inventory additionally covers existing permanent deletion of catalog questions; it is not modeled as generic file deletion.
4. Open Interview preparation. Questions and STAR material are visible simultaneously. Add selection and focused edits open centrally; ordering is a compact repeated action. Review & open Workspace leads to private approval in the real app; the prototype only explains this boundary.
5. Open Position editor, change a field, then Cancel or switch review screens. Inspect the central unsaved-change choice and return to editing. Saving shows a simulated confirmed outcome; failure mode retains the edited data rather than reporting success.

Icons represent familiar repeated actions and have accessible names. Important workflow actions keep text. The prototype includes the core dialog focus/Escape/dirty-edit behavior to assess interaction direction; this does not substitute for production accessibility or workflow testing. Full-page routes and existing input limits must remain available for substantial edits.

## Display assumptions and evidence

Primary target: the user's 3840 × 2160 physical display. **The actual OS scaling, browser zoom and browser CSS viewport have not been measured.** Do not label an emulated viewport as physical-device verification. Browser review should record these settings before finalizing production spacing.

The review is fluid and uses three columns at desktop widths. QA targets 3840 × 2160 CSS pixels, 2560 × 1440 CSS pixels and 1920 × 1080 CSS pixels as expansive, intermediate and scaled-4K approximations; they are not assertions about the user's setup. A narrow sanity check verifies content remains accessible rather than defining the main design. See [verification notes](M6_REVIEW_VERIFICATION.md) for actual runs and captured evidence.

## Decisions for design review

The recommendation is **Work centered** with shared entity chrome, compact local actions and focused central dialogs. Review whether the main work should instead start at the left edge, whether contact/history/resource placement feels natural, and whether the density and dialog size are comfortable on the actual display. Existing ADR 0002 fixes the 4K-first direction; it does not already approve one of these detailed compositions.

Package 2 should begin only after design feedback selects or refines the composition. The next work is shared presentation patterns in one representative production slice, not an immediate rewrite of every screen. Package 1 records design evidence; it does not complete M6 or release Web 1.0.
