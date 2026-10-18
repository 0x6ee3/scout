# Scout: Milestone 3 implementation plan

Status: approved and implemented. All five package gates passed; see [M3 delivery summary](MILESTONE_3_DELIVERY.md) for actual results, limitations, and the M4 handoff.
Date: 15 October 2026.
Foundation: [Specification](../../../README.md#specification), [M1 architecture](MILESTONE_1_ARCHITECTURE.md), [M2 plan](MILESTONE_2_PLAN.md), [M2 delivery record](MILESTONE_2_DELIVERY.md), and the [user](../../../products/scout-web/USER_GUIDE.md) and [operations](../../../products/scout-web/OPERATIONS.md) guides.

## 1. Reviewed scope and outcome

After M3, the user can curate **two catalogs** and select entries for an interview:

1. **Employer questions**: reusable questions to ask, with a simple answer box on each interview-specific selection.
2. **STAR experiences**: reusable personal examples structured as Situation, Task, Action, and Result, with optional memory cues and reflection.

The user's clarification confirmed: **“Two catalogs: employer questions and STAR experiences.”** This replaces the earlier three-library design. There is no separate expected-interviewer-question catalog or question-to-experience association table. STAR experiences form the answer-preparation catalog; tags and optional likely-prompt notes help find an example for a question.

This review supersedes the specification's proposed language/category fields and its general archive-only rule **for reusable employer questions**. Employer questions may be permanently deleted. The rest of Scout retains its existing preservation rules. A manually saved interview-specific answer box is included in M3; the dedicated live Workspace remains M4.

Included:

- Employer-question create/read/edit/archive/restore and explicit permanent deletion.
- STAR experience create/read/edit/archive/restore; every STAR field optional.
- Tags only, with validation, existing-tag suggestions, and visually distinguishable badges.
- Interview-specific question and experience selections, private preparation notes, and saved ordering.
- Editable prepared question wording and a manually saved answer per selected question.
- Literal search, tag/archive filters, additive migrations, guarded samples, preservation tests, and documentation.

Excluded: language metadata/filters, translation, categories, a third catalog, question/experience association management, automatic question generation, document parsing/import into libraries, and new attachment owners. No dedicated Workspace, screen-sharing guarantees, asked-state controls, structured follow-up tracking, ad hoc live questions, autosave/draft recovery, recording/consent controls, transcription, AI, export, or dashboard.

Text remains ordinary Unicode text in the language the user chooses. There is no language selector or translation workflow. Existing M1/M2 fields, such as prepared-CV language, are outside this change. The preparation page remains a private management page with normal navigation, not a screen-share-safe Workspace.

This is **one milestone for review and delivery**. After approval, work through the packages below without intermediate permission requests, passing build, the full automated suite, and relevant browser checks after each package.

## 2. Decisions and remaining defaults

There are no blocking architectural uncertainties. User decisions take precedence where they differ from the original specification.

| Topic | Revised decision |
| --- | --- |
| Catalogs and entity types | Separate `EmployerQuestion` and `ProfessionalExperience` entities/screens. No mutable `QuestionKind` column: catalog membership is fixed by entity type. Entries cannot be converted between catalogs. |
| Deleting questions | Allow permanent deletion even when selected for interviews. Remove its library tags and detach source links atomically, while preserving interview-specific wording, answers, and notes. Confirm deletion and show how many selections retain copies. No recycle bin. |
| Other deletion behavior | Experiences and prepared items retain archive/remove-and-restore behavior. The exception does not extend to applications, interviews, documents, or other existing entities. |
| Prepared wording | Retain the earlier proposed default: copy wording, intent, and follow-ups on first selection. Library edits do not overwrite round-local copies. Source deletion leaves a self-contained item labeled “Source question deleted.” |
| Experience revisions | Retain the earlier proposed default: selections reference current library content. STAR edits appear wherever selected; round-specific rehearsal notes stay independent. Explain this in the editor. |
| Question-to-experience associations | Removed with the third catalog. Select experiences directly for a round. Optional “Useful for / likely prompts” text and tags describe where a story helps without a prompt catalog. |
| Answer box | Belongs to a selected question in a specific interview, never the global definition. Allow blank answers and explicit manual saving. No asked state, follow-up workflow, or inference that a filled answer proves when it was asked. |
| Language | No M3 language fields, enum, filters, pairing, translation provider, or localization work. The user curates wording manually. |
| Tags | No categories. A tag is one word or hyphen-separated words, for example `leadership`, `team-conflict`, or `technical-decisions`. Suggest existing tags while typing. |
| Suggestions wording | Interpret “when adding or editing tasks it should show suggestions of tasks” as tag suggestions in these catalog editors. No change to M2 next-action/task management is included. |
| STAR requiredness | Require only an experience title. Situation, Task, Action, Result, cue, reflection, likely prompts, and longer notes are optional. STAR remains the standard editor structure. |

The wording/revision questions were left unanswered, so their previous proposed defaults remain visible rather than being treated as newly confirmed decisions. Association choices are superseded by the confirmed two-catalog scope.

## 3. Repository review and architecture

Keep one `net10.0` Blazor Interactive Server application and one xUnit/Microsoft Testing Platform project. Reuse concrete feature operations, detached edit models, short-lived EF contexts, guarded forms, load/error controls, and actual-SQLite fixtures. M2's delivery record reports 94 passing tests and eight migrations; these are recorded results, not tests rerun for this documentation revision.

Keep the approved `<user home>\Documents\Scout` root. New content lives in the existing SQLite database; no files, directories, process, or external service are needed.

```text
src/Scout.Web/
  Domain/                         # EmployerQuestion, ProfessionalExperience, selections
  Features/
    Questions/                    # employer-question operations and models
    Experiences/                  # STAR experience operations and models
    InterviewPreparation/         # round selections, answers, ordering
  Components/Pages/
    Questions/ Experiences/ Interviews/
  Components/
    TagEditor.razor                # shared by both catalog editors
  Data/
    PreparationMappings.cs
    Migrations/
    DevelopmentSamples.cs
tests/Scout.Web.Tests/
  Questions/ Experiences/ InterviewPreparation/ Data/
docs/milestones/
  MILESTONE_3_PLAN.md
  MILESTONE_3_DELIVERY.md           # actual implementation evidence, created later
```

Use explicit foreign keys, concrete operations, and small projection records. Share a small tag-validation/suggestion helper because both editors need it. No generic catalog framework, generic entity links, repositories, mediator, search engine, rich-text editor, translation infrastructure, or speculative Workspace services.

Follow `AGENTS.md` during implementation: exposed Rider semantic/build/diagnostic tools where appropriate, and its CLI fallback when unavailable. Actual implementation and verification are recorded in the delivery summary.

### Dependency baseline

| Component | Existing repository pin / M3 action |
| --- | --- |
| SDK | `10.0.402`, existing `latestPatch` roll-forward policy |
| Target | `net10.0`, C# 14, existing ASP.NET Core shared framework |
| EF Core SQLite / Design | `10.0.13` |
| Local `dotnet-ef` | `10.0.13` |
| Tests | `xunit.v3` `4.0.2`, Microsoft Testing Platform |
| Test host | `Microsoft.AspNetCore.Mvc.Testing` `10.0.13` |
| New dependencies | **None** |

These are the inspected repository baseline, not a claim about newest available releases. Verify installed tools and restore existing pins when implementation starts; separately review any required patch upgrade.

## 4. Data model and invariants

Generated integer IDs for business records, UTC creation/update timestamps, and composite keys for tag rows. Restrict deletion by default; configure only the explicit question exception below.

### EmployerQuestion

- `Id`, required `Text` (4,000 characters).
- Optional `Intent`, `FollowUpQuestions`, and private library `Notes` (100,000 characters each).
- `IsArchived`, `CreatedAtUtc`, `UpdatedAtUtc`.
- `EmployerQuestionTag`: composite `(EmployerQuestionId, Tag)` key.

No language, category, kind, global answer, or suggested-interview-type field. Tags such as `hr` and `engineering` supply simple discovery without another classification mechanism. Follow-ups are multiline text, not recursive question links. Duplicate wording is permitted; no automatic deduplication.

### ProfessionalExperience

- `Id`, required `Title` (200 characters).
- Optional `MemoryCue` (1,000 characters).
- Optional `Situation`, `Task`, `Action`, `Result`, `Reflection`, `UsefulFor`, and `Notes` (100,000 characters each).
- `IsArchived`, `CreatedAtUtc`, `UpdatedAtUtc`.
- `ExperienceTag`: composite `(ProfessionalExperienceId, Tag)` key.

Label `UsefulFor` as “Useful for / likely prompts,” for example “Tell me about a disagreement” or “Describe a mentoring situation.” This is free text, not another catalog or association graph. A title-only entry is valid; the normal editor still presents STAR clearly.

No employer/application ownership is required. Personal content comes from the user; samples are explicitly fictional.

### InterviewQuestion

- `Id`, fixed `InterviewRoundId`, nullable source `EmployerQuestionId`.
- Prepared `Text`, `Intent`, `FollowUpQuestions`, initially copied from the definition, with the same limits.
- Optional private `PreparationNotes` and interview-specific `Answer` (100,000 characters each), initially empty.
- `DisplayOrder`, `IsArchived`, UTC creation/update timestamps.
- Filtered unique index on `(InterviewRoundId, EmployerQuestionId)` where the source ID is non-null, covering active and removed instances.

One source question per round, reusable in other rounds. Never copy private library notes. No implicit copy-back or refresh. Show a source link when present.

“Remove from preparation” archives the instance, retaining wording, answer, and notes. Re-adding a still-existing source restores that instance at the end without recopying the library. Removed instances remain available in a separate view.

### Permanent question deletion

1. Show **Delete question permanently** on question details, distinct from Archive.
2. Confirm disappearance from the catalog and the count of interview copies that will be retained. Opening/canceling the confirmation does not change data.
3. Atomically remove library tag rows, null referencing `InterviewQuestion.EmployerQuestionId` values, and delete the definition. Configure `ON DELETE SET NULL` for the source FK; keep other owner relationships restricted.
4. Retain prepared fields, answers, notes, archive state, order, and content-edit timestamps. Detaching a source is not a new answer edit.
5. Display “Source question deleted.” The retained item stays editable, removable, and restorable from its round, but cannot be selected from the library.

New selections require an existing source. A stale form opened before deletion fails usefully and retains its draft. A later catalog entry with identical wording has a new identity; do not merge it automatically with detached copies. This exception deletes question content only, never application/interview history or files.

### InterviewExperience

- `Id`, fixed `InterviewRoundId`, fixed `ProfessionalExperienceId`.
- Unique `(InterviewRoundId, ProfessionalExperienceId)` across active and removed items.
- Optional round-specific `PreparationNotes` (100,000 characters).
- `DisplayOrder`, `IsArchived`, UTC creation/update timestamps.

Selections show current cue/STAR content and retain independent rehearsal notes. Archiving a source preserves existing selections. Remove/restore preserves notes; restored items append to the visible list. No permanent deletion of experiences or these selections.

### Tags and suggestions

- At most 20 tags per catalog entry, 50 characters each.
- Canonical invariant-lowercase spelling: trim surrounding whitespace and collapse duplicates. Display the same spelling everywhere.
- Accept letters/numbers with ordinary hyphens between nonempty word segments. Unicode letters, including umlauts, are valid. Reject internal spaces, underscores, punctuation, leading/trailing hyphens, and repeated hyphens. Do not silently turn an invalid phrase into another tag.
- Valid: `leadership`, `team-conflict`, `c-sharp`, `führung`, `team-2`. Invalid: `team conflict`, `team_conflict`, `team--conflict`.
- Simple chip editor: type a tag, choose a suggestion or press Enter/Add; remove explicitly. Suggestions come from existing tags in either catalog, including archived entries, filtered by prefix. Limit displayed suggestions, not saved catalog results. No global Tag table or administration screen.
- Give badges a small deterministic color palette based on canonical tag text. The same tag looks consistent across catalogs, with readable contrast and a text label; color is not the only identifier. No color picker.
- Support keyboard use and notify the parent form's edit context. Typing/selecting a suggestion does not save the entry.
- Deleting a question removes its tag rows. A tag still used elsewhere remains suggested; unused tags naturally disappear from suggestions.

### Shared rules

- Validate limits/requiredness in operations and forms; SQLite mappings alone do not enforce string length. Reject blank required text, trim short fields, preserve nonblank multiline text, and render encoded with whitespace.
- Archive never cascades. Adding/restoring selections requires an active round/source. Existing items stay readable/editable under archived sources or ancestors. Restoring a detached question needs no source, but does require an active round.
- Active rounds remain usable under archived applications/companies, following M2's direct-parent rule. Completed/cancelled rounds can have preparation/answers corrected without changing lifecycle status.
- Every item operation validates ownership by the requested round. No moving selections between rounds or trusting submitted IDs alone.
- Move up/down persists separate question/experience orders. Append new/restored items; ID breaks ties. Reorder only the complete visible list, not a filtered subset. No drag-and-drop package.
- Validate multi-add/order requests before writing; commit once. Repeated adds return existing selections. Unique constraints guard simultaneous adds; report/reload conflicts without discarding drafts.
- Retain last-successful-save-wins editing. No concurrency/versioning framework or silent retry with a different payload.
- Index foreign keys, archive/tag lookups, and round/order retrieval. No full-text, language, category, or interview-type index.

## 5. Pages and workflows

### Catalogs

Navigation entries: **Employer questions** and **STAR experiences**. Routes: `/questions`, `/questions/{id}`, `/experiences`, `/experiences/{id}`, plus normal new/edit routes. No expected-question or association-management page.

Question rows show wording/excerpt, tags, and archive state. Experience rows show title, cue, tags, and archive state; details/editor expose STAR, reflection, likely prompts, and longer notes. Explain that experience edits appear in existing selections.

Combine literal search, a selected-tag filter, and archive filter (default active). No language/category/interview-type filters. Preserve query state through details/edit/back. Sort by question text or experience title, then ID. Distinguish an empty catalog, no matches, missing/deleted entry, and recoverable load error.

Search uses existing literal `OrdinalIgnoreCase` substring matching: `%` and `_` are ordinary characters; Unicode text remains supported without a language feature. Search question text/intent/follow-ups/notes/tags and experience title/cue/STAR/reflection/likely-prompts/notes/tags. Compare fields individually rather than across concatenated boundaries.

Apply SQL-translatable archive/tag filters first, then .NET text comparison over the small personal catalog. Fetch long search fields only for a nonempty search; normal list rows omit full STAR/notes. No result truncation, search service, or speculative pagination.

### Interview preparation and answers

Add **Prepare interview** on round details at `/interviews/{id}/preparation`, with company/position/application/round identity and a return link. Two sections:

1. **Questions to ask**: editable local wording/context and a round-specific answer box.
2. **STAR experiences**: explicitly selected examples, cue first and expandable STAR, with private rehearsal notes.

Search/filter the corresponding catalog, select entries, and explicitly save additions. Show source/detail links, ordering, remove/restore, and a removed-items view. Existing archived sources remain visible; deleted question sources are labeled.

Answers use a multiline input and explicit Save, dirty-navigation guard, and recoverable-failure handling. This is ordinary manually saved editing useful before/after a conversation, not M4's reliable live-note workflow. No “Start Workspace” action or screen-share claim.

Keep catalog edits separate from round wording/answers/notes. Filtering, expanding, or following links never implicitly saves a draft. Retain selections across filters and revalidate on save. Prevent duplicate in-flight submissions; retain input on validation/database failures.

Reuse `DraftForm`, `LoadPanel`, and operation controls. Tag/selection changes participate in dirty tracking. Verify parent refresh after asynchronous loads/saves, a failure mode found in M2. No new form framework, autosave, or durable browser draft.

## 6. Implementation packages and gates

At implementation start, preserve the working tree, verify tools, build, and run baseline tests. Back up the stopped whole root before applying migrations to personal storage. Never seed/reset an existing personal database.

Each package delivers schema, operations, UI, focused tests, then full regression and browser checks. Fix failures before continuing; no separate package approvals.

| Package | Deliverable | Verification before continuing |
| --- | --- | --- |
| 1. Employer questions | Question/tag migration; catalog/create/edit/details/archive/restore/delete; search/filter; shared tag editor/suggestions. | Persistence, limits, encoding, canonical tags, invalid tokens, deduplication, suggestions, badge consistency, unused-question deletion. Browser: keyboard suggestions, filter/edit, archive/restore, cancel/confirm delete, restart. |
| 2. STAR experiences | Experience/tag migration; optional STAR editor; cue-first catalog/details; search/filter/archive; shared tag suggestions across catalogs. | Title-only/full examples, STAR/prompt search, whitespace, tags retained elsewhere after question deletion. Browser: partial STAR, expandable details, suggestions, dirty-navigation/recoverable errors. |
| 3. Interview questions/answers | InterviewQuestion migration; preparation page; selection, snapshots, answer box, notes, order/remove/restore; source-deletion preservation. | Ownership, retried selection, independent wording/answers in two rounds, source-edit isolation, no private-note copying, atomic deletion/SET NULL, detached restore, stale-source rejection, order/rollback. Browser: save in two rounds, edit/delete source, retain copies/answers, restart. |
| 4. Interview STAR preparation | InterviewExperience migration; direct example selection, cue/STAR display, private notes, order/remove/restore. | Uniqueness, independent notes, live library edits, source/round archival rules, cross-round rejection. Browser: select/reorder, change library STAR, retain notes and selections after restart. |
| 5. Integration/delivery | Navigation/errors/empty states; guarded fictional samples; upgrade/backup/restore coverage; README/delivery record. | Acceptance flow, fresh/M2-upgrade migrations, seed guards, integrity/FKs, whole-root restore. Browser: keyboard/long text, deletion failure/confirmation, restart. Record unperformed environmental checks. |

Use existing commands, restoring first if needed:

```powershell
dotnet build Scout.slnx --no-restore
dotnet test --solution Scout.slnx --no-build
dotnet ef migrations has-pending-model-changes --project src/Scout.Web --startup-project src/Scout.Web --no-build
```

Build and the **entire automated suite** pass after every package. EF pending-model checks pass after schema changes and at delivery. Follow `AGENTS.md` for exposed Rider build/diagnostics and CLI fallback. Browser checks exercise actual interactive forms/persistence; HTTP GETs alone do not replace them. Resolve or record unavailable checks before claiming their gate passed.

## 7. Migrations, samples, and preservation

Add reviewed migrations as tables arrive; four schema-bearing packages are expected. Do not rewrite M1/M2 history, use `EnsureCreated`, or reset data. Existing interview columns/notes, documents, CVs, submissions, files, and storage layout need no changes. The nullable question-source FK is the explicit deletion exception.

Use migrated SQLite fixtures and isolated roots. Upgrade from M2's `20261015171814_CvsAndSubmissions` with populated relationships, notes, vacancy snapshots, and document/submission bytes; verify unchanged rows/hashes after all M3 migrations. Test filtered uniqueness and `SET NULL` against SQLite itself.

The final workflow test creates two rounds, questions, partial/full experiences, and independent selections/notes/answers. Edit then delete a source question, archive an experience, remove/restore selections, restart contexts, and copy a stopped temporary whole root. Verify restored wording, answers, notes, order, links, and original bytes. Failed multi-adds/deletions leave no partial changes. Check `integrity_check` and `foreign_key_check` on fresh/upgraded/restored fixtures.

Extend samples only in Development, explicitly enabled, with every business table empty in the existing transaction. A standalone question/experience prevents seeding. Use clearly fictional entries in one consistent language: partial/full STAR, question/experience selections, and a labeled fictional employer answer. Repeated seeding preserves edits; existing M2 personal data prevents insertion. Never represent samples as the user's achievements.

Question deletion is an explicit feature, not a cleanup task. No bulk delete, automatic retention policy, physical-file cleanup, reset, or import from personal documents. Existing M2 native-PDF/network-disabled verification limits remain separately documented.

## 8. Acceptance and done criteria

Use fictional data, preferably in an isolated test host/root:

1. Create employer questions and both title-only/full STAR experiences.
2. Add `leadership`/`team-conflict`; see suggestions and consistent badges in both editors. Reject `team conflict` without losing other edits.
3. Search inside questions and STAR; combine search/tag/archive filters and preserve them through navigation.
4. Prepare two rounds, saving independent wording, answers, rehearsal notes, and order.
5. Edit library sources: prepared question wording stays unchanged; STAR content updates while round notes remain independent.
6. Cancel then confirm question deletion. The definition disappears; round copies retain wording/answers/notes/order and a deleted-source label. Other records/tags remain intact.
7. Archive sources; remove/restore selections, including detached questions. Invalid/stale selections retain drafts and explain the problem.
8. Restart and verify saved content and preserved M2 notes/documents/submissions/status/history.
9. Confirm normal operation needs only local assets/services. If external-network-disabled testing is unavailable, record it as unperformed.

M3 is done when package gates are recorded, the full suite passes, build/EF checks are clean, migration/isolated-restore tests preserve old/new data, and operating/delivery documentation states actual evidence/limits. No fixed test count is promised.

## 9. Required delivery summary and M4 handoff

Produce `docs/milestones/MILESTONE_3_DELIVERY.md` as a required final output, following the structure and evidence standard of [MILESTONE_2_DELIVERY.md](MILESTONE_2_DELIVERY.md). Write it from actual implementation results; do not present this approved plan as completed work.

Include:

- Package gates: what each of the five packages delivered, build results, and actual passing/failing/skipped test counts after each gate.
- Final verification: automated suite, build warnings/errors, EF model check, browser workflows exercised, and any checks that remain unperformed.
- Final implementation and migrations: actual migration names, dependency changes if any, and material deviations from this approved plan with their reasons.
- Preservation evidence: M2 upgrade results, deletion/detachment behavior, independent round answers/notes, and isolated whole-root backup/restore checks. State which roots were used without exposing personal content.
- Deliberate limits and unresolved issues: particularly manual saving, lack of durable drafts, and anything M4 must address. Carry forward relevant M2 limitations without treating them as new failures or claiming they were fixed.
- An explicit **Starting point for M4**: actual entry points and reusable behavior for interview preparation, answers, STAR selections, documents/submitted CVs, and notes; relevant files; current test baseline; remaining work for Workspace privacy, asked state, reliable saving/recovery, consent, and audio handling.

The initial [M4 plan](MILESTONE_4_PLAN.md) is a functional draft prepared ahead of delivery. Before finalizing it, read the completed M3 delivery summary and inspect the resulting application rather than assuming this M3 proposal was implemented verbatim.

M3 implementation and its required delivery summary are complete. The delivery summary records actual verification and remaining limits; M4 implementation remains outside scope.
