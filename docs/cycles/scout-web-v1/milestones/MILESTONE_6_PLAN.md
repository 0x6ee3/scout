# Scout.Web v1: Milestone 6 — UI and UX overhaul

Date: 16 October 2026. Revision: approved Work centered composition implemented across suitable pages; 4K and three-column layouts remain the primary design target. Status: implemented, with physical-display user acceptance open. See [delivery evidence and remaining limits](MILESTONE_6_DELIVERY.md), [M5 delivery](MILESTONE_5_DELIVERY.md) and the [UX decision](../../../engineering/decisions/0002-web-v1-desktop-ux.md). This is the final milestone of the [Web v1 cycle](../README.md).

Package 1 started on 16 October 2026. The [route/action inventory](../design/M6_UX_INVENTORY.md), [interactive composition review](../design/M6_DESIGN_REVIEW.md) and [verification notes](../design/M6_REVIEW_VERIFICATION.md) retain the original proposal evidence. The user subsequently approved Work centered and authorized rollout. Packages 2–5 now have implementation and verification evidence in the delivery record; the actual monitor/scaling walkthrough remains for the user.

## 1. Outcome and priority

Finish Scout.Web 1.0 before starting Scribe implementation or adding detail to its plans. The existing functionality is useful and has been exercised by the user. This milestone improves presentation and interaction while preserving business behavior, data and workflows.

Design for the user's **4K display first**. Use available width for three complementary columns on information-rich views rather than widening a single vertical stack. A 900p or Full HD viewport is not the baseline that constrains the main design. Reduced-column layouts are secondary adaptations; fine-tuning smaller screens can follow. Long notes and transcripts still need readable line lengths, not text stretched across the entire display.

Keep the existing top navigation and its destinations. Under it, make the current location, current entity and available actions immediately apparent. Replace scattered large buttons, persistent editing forms and below-form confirmations with consistent action bars and central dialogs.

## 2. Starting-point observations

These are source-review findings, not a completed visual acceptance exercise:

| Current pattern | Planned improvement |
|---|---|
| `wwwroot/app.css` caps the main area at 1,200px and many detail/form cards at 720px. | A substantially wider desktop content area with three-column composition where useful, appropriate gutters and readable text within each section. |
| Detail/edit pages commonly offer only a back link; nested context varies between views. | A visible, consistent breadcrumb trail above the page title, with real entity names and preserved return/filter context. |
| Application details stack submissions, next actions/interviews, contacts, timeline, attachments and archive controls. | Related information arranged side by side; frequent actions available in a predictable action strip. |
| `ApplicationContacts` and `Attachments` render selection forms directly in detail pages. | Read-first sections with compact local actions; open focused selection/edit dialogs only when requested. |
| Interview detail/preparation actions are scattered, including buttons before the heading. | Put preparation, Workspace launch and edit actions together beneath the entity heading. |
| `DraftForm` and individual editors show internal unsaved-navigation prompts below forms; operation outcomes appear in local paragraphs. | Central dialogs for decisions and explicit save outcomes, with field-level guidance retained where corrections are needed. |
| Preparation cards repeat large wording/edit/move/remove buttons. | Compact item action groups and consistent icons, while keeping important actions discoverable. |
| The Interview Workspace already has a wider grid and its own privacy/saving behavior. | Extend the desktop composition deliberately without weakening its approved-data boundary or interrupting live note capture. |

## 3. Common page structure

Use the same hierarchy across list, detail and editor views, adapted to their tasks:

```text
Existing top navigation
Breadcrumb: collection → named entity → nested entity → current view
Page title + entity status / short context
Entity action bar: primary action | related actions | More
┌────────────────────┬─────────────────────────┬────────────────────┐
│ Context / relations│ Main work / content     │ Activity / resources│
│ and compact facts  │                         │ and supporting info │
└────────────────────┴─────────────────────────┴────────────────────┘
```

Column assignments depend on the view; this is a layout pattern, not three mandatory empty boxes. Give the main task more width when useful. Keep text size comfortable, align section headers/actions, reduce excessive gaps and avoid deeply nested cards. Prefer normal page scrolling over several competing tall scrolling panels. A compact action bar may remain visible during scrolling if it does not obscure content, focus targets or dialogs.

**Breadcrumbs:** show a readable trail in a stable location with sufficient contrast, clickable ancestors and a distinct current item. For a nested interview, a trail may follow company → position → application attempt → interview → preparation. Use actual relationships, distinguish repeated application attempts, and avoid meaningless IDs as the only context. Direct links, missing/archived entities and long names need deliberate states. Catalog search/filter return context must survive detail/edit/cancel navigation. Avoid an excessively long list of every ancestor collection.

**Actions:** group entity-level actions together beneath the title, rather than scattering them among introductory paragraphs. Use section-level actions beside the relevant heading and item-level actions beside the affected item. Keep primary actions such as Prepare interview or Start Workspace visible; use a labeled More menu for less frequent actions such as archive/restore and record metadata. Avoid duplicating the same large button in several sections.

**Icons:** establish one consistent visual vocabulary for add, edit, open, download, attach/link, unlink, move, archive and restore. Repeated generic item actions can be icon-only with meaningful accessible names and tooltips available on hover and keyboard focus. Important or unfamiliar actions retain a visible label. Name the target where ambiguous, such as “Unlink interview-notes.md.” Do not imply that unlink/archive deletes an original file. Use normal links for destinations and buttons for operations; keyboard users must have access without hover.

## 4. Screen coverage and proposed composition

Inventory every current route and shared action component before implementation. The examples below guide composition; they do not restrict the work to a few named screens.

| View family | 4K composition and interactions |
|---|---|
| Companies and positions | Context/contact/relationship summary; main description and notes; related opportunities/applications and documents. Three columns on rich records, with shorter facts grouped efficiently. |
| Applications | Company/position, application status and contacts; interview rounds and next actions; activity/timeline, submissions and documents. Put edit and common create/link actions in the appropriate action bars. |
| Interviews and private review | Schedule, participants and context; preparation/interview notes; recordings, transcripts, attachments and follow-up resources. Keep private playback and review clearly identified. |
| Preparation | A deliberate three-column arrangement for selected questions, STAR/rehearsal material and interview context/resources. Open add/select/edit tasks on demand instead of leaving pickers and all action buttons permanently expanded. |
| Interview Workspace and setup | Use available 4K space for approved reference material, questions/answers and notes/STAR material. Preserve existing layout choices where useful, note visibility, keyboard flow and the separate private approval/setup process. |
| Documents, CVs and submission history | Main preview/content with metadata/relationships and versions/submission evidence alongside it where useful. Group open/download/edit/link actions; preserve exact submitted-file identity. |
| Contacts, next actions, question and STAR catalogs, timelines and remaining routes | Apply the same context/header/action conventions. Use compact list rows or tables and horizontal search/filter controls; use multiple columns for meaningful supporting content rather than artificially splitting one ordered list. |
| Editors | Group related short fields horizontally and distribute substantial field groups across available columns. Use the space for source/reference context where relevant. Keep labels and long-text editing readable; preserve ordinary page routes for large editors and direct navigation. |

Maintain clear empty, loading, unavailable and archived/read-only states. A sparse record need not fill every column. Layout must follow the task rather than introduce new relationships, summaries or dashboard features.

## 5. Dialogs, editing and feedback

Use centered dialogs with a clear title, subject and action buttons for focused interactions. They must remain visible in the current browser window, not appear at the bottom of a long page.

| Situation | Intended interaction |
|---|---|
| Archive/restore, unlink, removing a selection or existing permanent catalog-question deletion | A central confirmation names the affected item and explains what changes and what is preserved. Keep existing question deletion explicitly distinct from archive/unlink, including irreversible-warning and affected interview-copy information. Do not introduce additional deletion capabilities. |
| Internal navigation with unsaved edits | Central “Keep editing / Leave without saving” dialog replacing the below-form prompt. Browser tab-close/reload protection remains the browser's native prompt where required. |
| Add/edit contact information; choose contacts or existing attachments; other bounded edits | A focused central form or picker, with save/cancel and the relevant context. Cancel preserves saved data; closing a dirty form offers the same discard protection. Preserve inputs and selections after validation or save failure. |
| Explicit save/create/edit/link outcome | On confirmed success, show a brief central acknowledgment naming what was saved, followed by return to the relevant updated context. Do not claim success while saving, on failure or after an uncertain result. No automatic dismissal. |
| Validation or operation failure | Keep field guidance beside the field; show a visible summary in the active dialog or a central recoverable outcome when appropriate. Preserve the draft and provide a clear retry/review route without stacking dialogs. |
| Workspace autosave, asked-state toggles and repetitive move/reorder actions | Keep unobtrusive visible status rather than opening a dialog on every keystroke or routine interaction. Explicit confirmation is still required for decisions that discard data or alter sensitive scope. |
| Download/open/preview | Start the requested existing action directly; never claim a download finished just because a link was clicked. Surface an actual failure with a clear recovery path. |

Use one active dialog at a time. Manage initial focus, contain keyboard focus while open, make background content inactive and return focus to the invoking control or a logical successor. Provide clear Cancel/Close labels and safe Escape behavior; backdrop clicks must not discard edits. Allow long forms/content to scroll within the available height. During an operation, disable duplicate submission and explain pending state; dismissal must not leave the user unsure whether it completed.

Do not turn every full-page editor into a modal: substantial position, STAR or long-transcript editing must retain enough room and recoverable navigation. The ordinary HTTP transcript-correction form must keep its existing large-input and failure-recovery behavior. Adapt its presentation without routing large text through Workspace autosave.

No private company/application trail, tooltip, menu item, confirmation, global success message or cached dialog content may enter the screen-shared Workspace unless already authorized by its projection. Its compact context and dialogs use approved data only; private setup remains separate.

## 6. Delivery packages

| Package | Reviewable outcome | Completion evidence |
|---|---|---|
| 1. UX inventory and composition | Route/action inventory; before/after 4K proposals for application details, interview preparation and a representative editor; breadcrumb/action/icon/dialog conventions. | Include three-column examples with realistic fictional content, long names and empty states. Record current browser viewport and scaling; settle composition before propagating it. |
| 2. Shared presentation patterns | Wider page frame, breadcrumbs, entity/section/item actions, icon treatment and central dialog/feedback patterns. Keep the top navigation. | Verify context, keyboard/focus behavior, wrapped actions and confirmed save/failure/discard flows on a representative vertical slice. |
| 3. Apply across management and review | Companies, positions, applications, contacts, next actions, catalogs, timeline, documents/CVs/submissions and interview details/review. | Verify each route/action is accounted for; no obsolete duplicate action blocks or permanently open small pickers remain unintentionally. Existing workflows and data semantics still work. |
| 4. Preparation and live Workspace | Three-column preparation and appropriate wide Workspace composition, respecting approved content and live capture. | Exercise question/STAR selection, edits, ordering and notes; verify saves, conflicts/recovery and private versus shared surfaces. |
| 5. Consistency and Web v1 closeout | Final 4K walkthrough, focused regressions, updated user guidance, M6 delivery record and cycle release summary. | Report actual checks, known limits and a usable Web v1 baseline. Continue with real interviews before prioritizing new Web work; Scribe stays separate. |

This is primarily frontend work. Reuse existing operations, validation, routes and persistence contracts; no schema migration, API expansion, frontend framework replacement or new business capability is planned. If a real implementation constraint requires a backend change, document it and its behavioral impact explicitly rather than widening scope silently. Keep technical component/dependency choices for implementation, with an ADR only where the decision has lasting significance.

## 7. Acceptance and verification

- On the user's 4K setup, show meaningful simultaneous columns on rich application/interview/preparation views. Important related information and actions should no longer require traversing a single narrow stack. Compare before/after screenshots with the same representative data and browser settings.
- Record physical display resolution, browser viewport in CSS pixels, OS scaling, browser zoom and window size separately. A 3840 × 2160 monitor at 200% scaling does not provide 3840 CSS pixels of working width. Exercise both an expansive full-screen viewport and the user's actual scaled setup; make no claim of testing physical hardware from viewport emulation alone. Capture actual user scaling during the first visual exercise rather than assuming it.
- Confirm visible entity context and actionable breadcrumbs when entering directly, from filtered lists and through company/position/application/interview relationships. Back/cancel returns to the expected view without losing search/filter state.
- Complete application tracking, contact/attachment linking, preparation, live notes, exact submitted-CV access, saved-audio playback and transcript/corrected-copy review. Verify central confirmations/save outcomes, truthful errors, dirty-form cancellation and retained drafts. Original files and earlier transcript copies remain unchanged.
- Check keyboard navigation, focus order/return, dialog Escape/cancel, icon names/tooltips, readable contrast and long-content wrapping. Avoid hover-only controls. Verify the UI remains usable when zoomed or narrowed, with orderly column reduction and no obscured actions; smaller-screen visual optimization is secondary to the 4K target, not a separate mobile redesign.
- Verify Workspace approval, excluded fields, private history, stale reviews, autosave/conflicts and draft recovery after affected UI changes. No private breadcrumb, menu or dialog leaks into a shared projection. Routine note capture must not cause repeated modal interruptions.
- Run behavior-appropriate checks from the [development guide](../../../engineering/DEVELOPMENT.md#build-and-verification) during implementation, plus focused browser exercises. Do not add tests that merely mirror CSS or screenshot counts. Report actual upload/codec/browser/hardware limits; M5's unverified checks are not retroactively passed by a redesign.
- Produce `MILESTONE_6_DELIVERY.md` with completed packages, before/after evidence, regressions, deviations and unperformed checks, then the cycle's `RELEASE_SUMMARY.md`. These records now describe the implemented baseline and distinguish automated/browser checks from outstanding physical-display acceptance.
