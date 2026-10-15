# Milestone 3 delivery summary

Implements the user-approved revised [M3 plan](MILESTONE_3_PLAN.md). M4 and later remain excluded. No commits were requested or created; existing unrelated working-tree changes were preserved.

## Package gates

Each package passed a build and the entire automated suite before continuing. Browser defects found at package gates were fixed and the build/full suite repeated before proceeding.

| Gate | Delivered | Passed | Failed / skipped |
| --- | --- | --- | --- |
| Baseline | Existing M1/M2 suite | 94 | 0 / 0 |
| 1 | Employer-question catalog, archive/restore/delete, tags and suggestions | 101 | 0 / 0 |
| 2 | Optional STAR experience catalog, shared tag suggestions and deep search | 103 | 0 / 0 |
| 3 | Round question copies, independent answers/notes, ordering, source deletion preservation | 106 | 0 / 0 |
| 4 | Round STAR selection, live source content, independent rehearsal notes | 108 | 0 / 0 |
| 5 | Guarded samples, upgrade/restore and rendering coverage, final integration | **113** | **0 / 0** |

Builds finished with **0 warnings and 0 errors**. EF reports **no pending model changes**. The final full suite completed in approximately 4.4 seconds. No runtime or test dependencies were added or upgraded.

### Commands and environment

The ordinary `dotnet test --solution Scout.slnx --no-build -m:1` orchestrator failed its runner handshake and ran zero tests in this session, including outside the sandbox. This is not recorded as a passing gate. Every gate above instead used the built xUnit executable directly, which discovers and runs the same complete suite:

```powershell
dotnet build Scout.slnx --no-restore -m:1 -p:UseSharedCompilation=false
dotnet tests/Scout.Web.Tests/bin/Debug/net10.0/Scout.Web.Tests.dll
dotnet ef migrations has-pending-model-changes --project src/Scout.Web --no-build
```

Tests needed execution outside the restricted sandbox: its denied temporary-file rename operations caused unrelated existing document tests to fail. The unrestricted baseline and all subsequent gates passed. The initially running Scout process also had to be stopped to release its executable before building. Rider tools were not exposed, so the repository's permitted CLI/file fallback was used.

Tests use real migrated SQLite databases and isolated temporary roots. Added coverage includes canonical/invalid/duplicate tags and limits; literal Unicode and long-field search; partial STAR; shared suggestions after question deletion; round ownership; retry/selection uniqueness; invalid multi-add rollback; source edits versus local copies; preserved answers/timestamps after source deletion; deletion rollback; source/round archival rules; saved order; live STAR updates; independent notes; seed guards; M2 upgrade; stopped-root restore; encoded component rendering; and rejection of foreign-round editor IDs. Component rendering uses the framework's `HtmlRenderer` with the real feature services; Scout intentionally disables ordinary page prerendering, so HTTP bootstrap responses alone cannot verify page content.

## Delivered behavior and architecture

Two catalogs only: **Employer questions** and **STAR experiences**. There is no expected-question catalog, category/language model, or question-to-experience association graph. Catalog membership is fixed by entity type. STAR fields are optional; only an experience title is required.

Tags are canonical lowercase words or hyphen-separated words, with up to 20 tags per entry and 50 characters each. Both editors suggest tags already used in either catalog, including archived entries. Badges use a deterministic six-color palette with text labels. Full catalog search includes longer notes/STAR fields; preparation-picker search covers question wording or experience title/cue and tags. Checked choices survive picker filter changes and participate in unsaved-navigation protection.

**Prepare interview** opens the round's private management page. Questions copy wording, intent, and follow-ups when first selected; private library notes are not copied. Answers and preparation notes belong to the round. Library edits do not overwrite prepared wording. Experiences reference current library content; rehearsal notes belong to the individual round. Selection retries do not duplicate records. Move up/down saves order; remove/restore retains content and restores at the end.

Permanent deletion applies only to reusable employer questions. Its confirmation displays the retained-copy count. One database commit removes library tags and the definition; the source foreign key sets itself to null on existing copies. Prepared wording, answers, notes, order, archive state, and last content-edit timestamps survive. Detached copies remain editable/removable/restorable and display **Source question deleted**. Experiences and other records keep archive/restore behavior.

Kept one web project, one test project, concrete feature operations, detached form models, short-lived contexts, and existing form/load/error controls. Shared additions are the tag editor/badges, actual reused preparation selector, and STAR display. No new process, API layer, file layout, infrastructure, or speculative Workspace abstraction.

### Migrations

Four additive migrations after M2's eight, bringing the total to twelve:

1. `20261015220329_EmployerQuestionCatalog`
2. `20261015220847_StarExperienceCatalog`
3. `20261015221221_InterviewQuestionPreparation`
4. `20261015221434_InterviewExperiencePreparation`

Reviewed creation of the six new tables, tag/owner indexes, restricted owner deletion, question-source `SET NULL`, and filtered unique round/source identity. Existing M2 tables/columns are unchanged. SQLite tests verify the constraints and rollback behavior; applied migrations were not rewritten.

Opt-in Development samples now include fictional employer questions, partial/full STAR examples, tags, selected preparation, and a fictional answer. Seeding still requires every business table to be empty, including standalone M3 entries, and preserves edited data on repeat runs. Samples were not enabled against the normal personal root.

## Browser checks

Used clearly labeled fictional records in the normal local root, following the stopped pre-migration backup. Existing M1/M2 records were retained. Verified:

- Question creation, keyboard tag entry, invalid-tag feedback retaining the draft, correcting/saving a tag, tag filtering, and question archive/restore.
- Dirty-navigation warning after edits and successful **Keep editing**, then saving without losing the question text.
- Partial STAR creation, existing-tag reuse, cue-first display, expanding optional fields, and searching inside Situation text with return-query links.
- Interview question selection, explicit answer/private-note saving, selection retained across no-match picker filters, saved ordering, and remove/restore.
- The same question selected in two rounds, with the second round starting without the first round's answer or private notes.
- Library wording edits leaving existing round copies/answers unchanged.
- STAR selection, saved rehearsal notes, and library STAR edits appearing in preparation while local rehearsal notes remain intact.
- Saved content reopening after process restarts during package checks.
- Permanent-delete confirmation/cancellation and the correct count of two retained round copies. Actual deletion/detachment and failure rollback were exercised in isolated automated tests; the permanent-delete confirmation was not submitted against the normal root during browser checks.

The browser gate found two issues that were fixed: the question editor needed explicit parent refresh after loading/replacing its form model, and question/experience cards needed distinct type-prefixed rendering keys when both had ID 1. Final browser checks rendered both sections successfully. No remaining observed circuit/rendering failure is being carried forward from these cases.

Screenshots: `.local/screenshots/m3-preparation.png` and the compact saved-answer view `.local/screenshots/m3-answer.png`. Browser checks are smoke tests, not a committed browser automation suite. The new second round and question/example records are labeled fictional verification data; no personal career history or real employer answer was invented.

After the final stopped-root backup, restarted the Production build at `http://localhost:5180` and verified the saved answer and STAR rehearsal notes again. Scout is left running with the preparation page available in the browser.

## Backup, upgrade, and restore evidence

Stopped pre-M3 whole-root backup:

`.local/backups/m3-before-20261016-000024/Scout`

Stopped final whole-root backup:

`.local/backups/m3-final-20261016-002530/Scout`

All **six** files in the stopped final source root matched copied hashes, including SQLite sidecars. A separate checker opened the workspace copies, compared the pre-M3 data with the final database, and verified documents through the storage verifier:

- All **26 existing M2 rows across 19 tables** remained present and unchanged. New fictional M3 work adds records; it does not rewrite existing M2 rows.
- `integrity_check`: **ok**; `foreign_key_check`: **no rows**.
- **3 documents** verified against stored hash and size.
- **12 applied migrations**, **2 reusable questions**, **1 experience**, **3 prepared question copies**, and **1 selected experience** in the final browser-check root.

The automated M2-to-M3 workflow additionally upgrades a populated temporary M2 database, keeps exact document/submission bytes and labels, exercises independent round preparation and deleted-source copies, copies a stopped whole root, reopens the copy, and verifies order, answers, notes, live STAR content, integrity, and foreign keys. Nothing was restored over personal storage. Workspace backups are verification artifacts, not a durable backup strategy.

## Deliberate limits

- Save is manual; drafts are guarded against navigation but do not survive reload/backend shutdown. Concurrent general edits remain last successful save wins. M4 must add reliable live saving/recovery.
- These are private management pages, not screen-share-safe views. They intentionally have global navigation, library choices, and private notes. Collapsed STAR content is still loaded.
- The two catalogs have full literal searches; picker search is intentionally limited to summary fields/tags. No pagination, full-text index, translation, or taxonomy management.
- No asked state, ad hoc live-question flow, structured follow-up-required flag, consent record, audio workflow, Workspace, transcription, or AI was added.
- M2's unconfirmed native PDF rendering in the in-app browser remains a separate limitation. Byte/download verification passed previously; M3 did not add a PDF viewer or claim to fix native preview.
- No external network adapter was disabled or network-denial rule applied. Production runs used local assets with no build/restore, but external-network-disabled acceptance remains unperformed.

## Starting point for M4

Read this summary before finalizing or implementing [MILESTONE_4_PLAN.md](MILESTONE_4_PLAN.md). At M3 delivery it was an incomplete functional draft; its current review status is recorded in that document.

| Existing entry point | What M4 can build on |
| --- | --- |
| `Components/Pages/Interviews/Details.razor` | **Prepare interview** entry; existing round notes, participants, and attachments. Add a distinct Workspace entry after its visibility choices are designed. |
| `Components/Pages/Interviews/Preparation.razor` | `/interviews/{id}/preparation`; current question/experience selections, order, and private management context. Preserve it as the private preparation flow. |
| `Features/InterviewPreparation/PreparationOperations.cs` | Question selection snapshots, manual answer/context edits, remove/restore, ordering, and round ownership checks. `InterviewQuestion.Id` remains stable when its library source is deleted. |
| `Features/InterviewPreparation/ExperiencePreparationOperations.cs` | Selected live STAR content and independent rehearsal notes; archived-source selections remain readable. |
| `Domain/InterviewQuestion.cs`, `Domain/InterviewExperience.cs` | Existing persisted answers, preparation notes, selection identities/order, and nullable deleted-question source. Extend the actual model as needed; avoid a second answer store. |
| `Components/Pages/Interviews/PreparedQuestionEditor.razor`, `PreparedExperienceEditor.razor` | Manual editing behavior to retain alongside future reliable live editing. |
| `Features/Documents`, `Features/Cvs`, `Features/Submissions` | Managed immutable files, controlled byte access, exact submitted-file evidence. The correct submitted CV must be chosen explicitly when multiple submissions exist. |
| `InterviewRound.PreparationNotes` / `InterviewNotes` | Existing general preparation and interview notes. Reconcile their role with selection-specific notes when designing Workspace saving. |
| `Components/DraftForm.razor`, `LoadPanel.razor`, tag/selector controls | Existing validation, failure retention, navigation guards, and private catalog controls. They do not provide autosave, recovery, or a privacy boundary. |

**Do not reuse the private preparation page's retrieval unchanged for the shareable Workspace.** It loads catalog choices and selected library entities, including private source notes/STAR fields. M4 needs explicit visibility choices and retrieval limited to approved interview content; hiding those fields with CSS/collapsed panels is insufficient.

Start from **113 passing tests**, twelve migrations, and the unchanged dependency pins. Remaining M4 work: scoped/share-aware layout and visibility review; correct position/submitted-CV panels; asked state and ad hoc round questions; attributed prior-round answer context; follow-up indicators; reliable saving/draft recovery and conflict handling; notes-only/consent workflow; external-audio import/linking with reviewed size/path/failure behavior. No external-recorder control or transcription currently exists.
