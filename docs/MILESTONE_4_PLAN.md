# Scout: Milestone 4 implementation plan — Interview Workspace

Status: **approved and implemented; see [M4 delivery summary](MILESTONE_4_DELIVERY.md) for verification and limits.**
Date: 16 October 2026.
Basis: [SCOUT_SPEC.md](SCOUT_SPEC.md), sections 5, 8 and the M4 roadmap; the approved [M3 revisions](MILESTONE_3_PLAN.md); and the actual [M3 delivery summary](MILESTONE_3_DELIVERY.md).

This plan replaced the advance functional draft after review of M3 delivery and the relevant code. The user approved the revised scope and implementation of all four packages, with tests required after each package. All audio and consent work previously proposed for M4 is deferred to M5; no M5 planning document is created here.

## 1. Outcome and scope

Conduct a mock interview with Scout in a dedicated tab/window on one display and the video call on another: consult approved position/CV material, ask questions, recall selected STAR examples, and capture answers/notes with visible saving and recovery. Exclude unrelated Scout data and private fields not approved for inclusion.

One milestone, with ordered packages as test gates rather than separate approval checkpoints. Core use remains local and independent of AI/external services. The .NET process must run for server operations; browser drafts protect interruptions without turning Blazor Server into an offline application.

Included:

- Private content review followed by an interview-scoped Workspace with a neutral title and separate layout.
- Approved round/position information, an explicitly selected submitted CV, questions, STAR examples, and notes.
- Asked/unasked state, answers/quick notes, follow-up flags, ad hoc round questions, and explicitly referenced earlier-round answers.
- Autosave, Save now, browser draft recovery, and concurrent-edit conflicts.
- A complete manual-notes interview workflow without recording setup or consent prompts.
- Privacy, persistence/recovery, upgrade/restore, wide-screen/keyboard checks, operating documentation, and a delivery summary.

Excluded: all audio and recording-consent functionality (deferred to M5), native recording or external-recorder control/detection, transcription/AI, translation, a third catalog, categories/language metadata, library administration inside the Workspace, dashboards, collaborative editing, a service worker/offline app, audio playback/editing, and draggable layouts. M3 private preparation remains available.

## 2. Reviewed starting point and decisions

M3 delivered two catalogs, round-local question copies/answers, live library references for STAR selections, independent rehearsal notes, ordering/remove/restore, and preserved copies after permanent library-question deletion. Its recorded baseline is **113 tests passed, zero build warnings/errors, twelve migrations, no pending model changes**. These are delivery results, not tests rerun during this planning task.

Reviewed the domain entities, preparation/interview operations and pages, routing/layout/reconnect behavior, submitted-file mappings, document operations/endpoints/storage, and dependency pins. Rider tools are not exposed in this session; file inspection and the repository's permitted fallback were used. Follow `AGENTS.md` during implementation.

Existing limits: manual saving; last-successful-save-wins edits; private preparation retrieves global catalog choices/full source entities; both document import and verified read enforce 50 MiB; native PDF rendering was unconfirmed in the in-app browser. M4 must extend these actual conditions for Workspace privacy and reliable notes. Existing generic document/audio attachment behavior and the 50 MiB limit remain unchanged; recording registration, audio-specific limits and working-file links belong to M5.

| Decision | Proposed treatment |
| --- | --- |
| Sharing versus private preparation | Separate private setup and Workspace routes/layouts. Only included fields reach the Workspace; collapsing an approved panel is presentation, not privacy enforcement. |
| Library edits after approval | Snapshot approved read-only material. Later source edits cannot silently reveal new text in an approved Workspace. M3 experience views remain live. |
| Existing answers/interview notes | Keep canonical fields. Explicitly enable their display/editing; these then remain live while included. Label that visibility in private editors. Preparation/rehearsal notes default excluded and can be included as reviewed read-only copies. |
| Deleted-source versus ad hoc questions | Add an origin distinction: both can have null library source IDs, but only the former should say “source deleted.” |
| Concurrent edits | Narrow content revisions for questions and interview notes, including existing writers. Reject stale saves and preserve drafts; no database-wide concurrency retrofit. |
| Earlier answers | Explicit, read-only attributed snapshots from other rounds of the same application. No automatic history loading or source-answer edits. |
| Audio and consent | Deferred to M5 in full: no M4 recording choices, consent records, recording metadata, audio pages, working-file links or audio-specific file-limit changes. |
| Privacy limit | Covers Scout's scoped Workspace, not the desktop, arbitrary management pages, or instantaneous erasure of previously displayed content in old/offline tabs. |

No uncertainty blocks this proposal. The main review choices are snapshots, visibility of enabled live fields, and conflict/recovery behavior. These are proposed defaults rather than claims that the specification mandates them.

## 3. Review, launch, and live workflows

### Private setup

**Start Interview Workspace** on round details/preparation first opens `/interviews/{id}/workspace/setup` inside normal management navigation. Preview actual text/files and explicitly approve:

- Round/company/position labels, scheduled information, and job-description snapshot.
- One submitted CV or none. Candidates are role-CV `SubmissionFile` rows from this application's non-incorrect submissions. Never automatically choose the newest prepared CV. Later correction of selected evidence requires review, not substitution.
- Individual active prepared questions; optional intent/follow-ups; whether each answer editor is enabled, showing any existing answer for review.
- Individual selected experiences and the exact cue/STAR fields displayed. Reflection, library notes, likely prompts, and rehearsal notes default off. M3 selection is not blanket disclosure permission.
- Whether to show/edit existing `InterviewRound.InterviewNotes`. Preparation notes default off; optional inclusion is a read-only snapshot.
- Explicit prior-answer references from this application's other rounds.

No contacts, email/phone, meeting URLs, or arbitrary attachments in the initial Workspace. They stay private. A configuration for manual notes remains possible without CV/preparation, but still previews existing notes before approval.

After saving approval, offer a user-activated link with `target="_blank"` and `rel="noopener noreferrer"` to `/interviews/{id}/workspace`. Avoid an asynchronous popup. Use title **Scout · Interview Workspace**. Without approval, show a neutral setup-required screen and load no interview content.

Reopening uses the approved configuration and approval time. Read-only material remains frozen until private review refreshes it. Enabling a live answer/note field explicitly permits its subsequent contents to be visible while included; private editors explain this. Disabled fields and their drafts are not loaded.

### Layout and interaction

Use responsive CSS grid with balanced/notes-wide presets and collapsible panels: interview/position, submitted CV, questions, STAR cues, and notes. Remember collapse/preset preferences locally; they cannot enable excluded content. No splitter/drag library.

Support Tab/Shift+Tab, labeled controls, expandable approved fields, and Ctrl+S for Save now. Preserve typing/caret through saves. Search/tag filters cover included entries only. Separate asked/unasked from unanswered: existing answer text never implies asked state.

No global navigation/search, library links, unrelated notifications, contact details, or filesystem paths. Exiting does not automatically navigate to private management pages. Missing CV/preparation has useful empty states and does not block notes.

Ad hoc questions stay in this round and never create library definitions. Use the existing round answer field for answers/quick notes; keep private preparation notes distinct. Follow-up flags do not automatically create next actions. Saving answers/notes and marking questions never change interview/application status or create timeline events for each save.

Archived rounds can reopen approved content read-only; restore privately before editing or adding questions. Active rounds under archived ancestors follow M3's direct-parent rule.

## 4. Technical architecture and dependencies

Retain one Interactive Server web project and one test project, short-lived EF contexts, concrete operations, explicit relationships, fixed root, and loopback/Host/Origin protections.

```text
src/Scout.Web/
  Domain/                          # configuration/inclusions
  Features/
    InterviewWorkspace/
      WorkspaceOperations.cs       # approval, scoped DTOs, permitted writes
      WorkspaceModels.cs
      WorkspaceFileEndpoints.cs    # selected CV only
    InterviewPreparation/          # protected existing question writer
    Interviews/                    # protected existing notes writer
  Components/
    Layout/WorkspaceLayout.razor
    Pages/Interviews/
      WorkspaceSetup.razor         # private
      Workspace.razor              # isolated layout
    Workspace/                     # concrete panels/editors/save feedback
  wwwroot/js/workspace-drafts.js
  Data/WorkspaceMappings.cs
  Data/Migrations/
tests/Scout.Web.Tests/
  InterviewWorkspace/ Data/
```

Override the router's default management layout with `@layout WorkspaceLayout`. Keep prerendering disabled. Reuse styling/tag badges, context-factory conventions, and verified file access, but not `Preparation.razor`, `ExperienceStory`, or their full-entity retrieval: these intentionally load private content.

Give the Workspace its own generic error boundary/error UI and scoped not-found handling. Unknown Workspace paths, invalid IDs, component failures and file errors must not fall back to the management Error/NotFound page and expose its navigation. Preserve the neutral title and quiet offline message; the shared reconnect component must not redirect into private management. Leave ordinary management error handling intact.

Business actions remain in concrete Blazor operations; new HTTP routes only serve narrowly selected bytes. Use a small native JS module without npm/build tooling. No generic repositories, provider hierarchy, mediator, background service, messaging bus, or speculative recording/AI integration.

| Component | Existing baseline / M4 action |
| --- | --- |
| SDK | `10.0.402`, latest-patch roll-forward |
| Target/framework | `net10.0`, C# 14, existing ASP.NET Core |
| EF SQLite/Design and dotnet-ef | `10.0.13` |
| Tests | `xunit.v3` `4.0.2`, MTP, `Microsoft.AspNetCore.Mvc.Testing` `10.0.13` |
| Browser drafts | Native IndexedDB and JS interop |
| New dependencies | **None** |

These are inspected repository pins, not a latest-release claim. Restore/verify tools at implementation start; no package upgrade is required by this design.

## 5. Data model and scoped operations

### Approved configuration

One `InterviewWorkspaceConfiguration` per round, keyed by round ID: stable generated `WorkspaceId` GUID, integer `ConfigurationRevision`, UTC approval/update times, and `RequiresReview`. Defaults include nothing. Store approved round/position/schedule/description snapshots, optional preparation-note snapshot, `InterviewNotesEnabled`, and optional `(SubmissionId, DocumentId)` composite FK to `SubmissionFile`. Validate CV role and same-application ownership.

Three explicit inclusion tables:

| Record | Content |
| --- | --- |
| `WorkspaceQuestion` | Composite round/question key, FK to configuration and same-round `InterviewQuestion`; snapshots of wording and only approved intent/follow-ups/preparation notes/tags; `AnswerEnabled` permits canonical live answer access. |
| `WorkspaceExperience` | Composite round/selection key, FK to configuration and same-round `InterviewExperience`; snapshot title/cue and only approved STAR/other fields. No global entity serialization. |
| `WorkspacePriorAnswer` | Generated ID, owner round FK, source question-instance FK, approved question/answer/source-round-label snapshots and known dates. Validate different round, same application. Read-only context, not another editable answer. |

Add unique alternate `(InterviewRoundId, Id)` keys on question/experience selections for composite ownership FKs. Restrict deletion. Validate membership, active selections, lengths and file ownership before one atomic approval/configuration replacement. No partial inclusion updates.

Limits match M3: question 4,000, labels 200, cue 1,000, long text 100,000. Store excluded fields as absent/null, not all text plus flags. Tags are a bounded serialized snapshot array validated with current tag rules; there is no need to query them in SQL or add taxonomy tables. These are purposeful approved display copies, not a general version graph. Setup may query candidate previews; Workspace retrieval cannot.

Removing an included round selection privately atomically advances its configuration revision and sets `RequiresReview`. Additional library/preparation entries never broaden an existing profile. Library-question deletion does not invalidate its retained prepared copy; archiving a global source does not erase already approved content. Selected submissions later marked incorrect block CV access pending review. Private setup is required to refresh source snapshots.

### Live fields and revisions

Extend `InterviewQuestion` with `Origin` (`Library`, `AdHoc`), `IsAsked`, `FollowUpRequired`, integer `ContentRevision`, and nullable GUID `CreationToken`. Existing rows default to Library, false/false, revision zero. Do not infer asked state from old answers. AdHoc requires null `EmployerQuestionId`; Library permits null after source deletion. Filtered unique `(InterviewRoundId, CreationToken)` supports retry-safe ad hoc creation.

Keep `Answer` as the canonical answer/quick-note text. Add integer `InterviewRound.NotesRevision` for its existing `InterviewNotes`. No second note document, duplicate answer store, or automatic timestamp transcript. Existing UTC timestamps, length limits, ordering/archive and deletion behavior remain.

Configure these application-managed revisions as EF concurrency tokens. All writers participate, including `PreparationOperations.SaveQuestionAsync`, prepared-question editors, and `InterviewOperations.SaveAsync`/the existing interview editor. Carry the version loaded with the form to a conditional write. Reading the newest row inside Save and treating that as the expected version does not protect an old form. Existing forms containing notes reject stale saves and retain input. Do not retrofit unrelated entities.

Advance content revisions for relevant edits, not source detachment alone. Metadata operations cannot bypass protected-field checks. Catch concurrency exceptions before generic database errors. SQLite needs application-managed tokens rather than a SQL Server-style automatic rowversion; see [EF Core concurrency guidance](https://learn.microsoft.com/en-us/ef/core/saving/concurrency).

Ad hoc creation checks active round and current configuration, then atomically creates the round item and approved inclusion. The creation token returns the existing item on identical retry; different payload reuse is rejected. Disable its creation form in flight so its confirmed payload stays fixed. Append that explicitly approved new question without changing the private approval revision or other visibility choices; existing save queues/drafts remain valid. Later private review still advances the configuration revision.

### Retrieval and write boundary

Use explicit `AsNoTracking().Where(round...).Select(...)` projections returning sealed DTOs, never EF entities/navigation graphs. Return approved snapshots, enabled live fields, revisions and relevant state only. Do not fetch private source notes in order to hide them. Scoped search uses these DTOs, including snapshot tags for source-deleted questions.

Permission/ownership guards themselves select only necessary IDs, archive/configuration state and revisions: they must not load a full `InterviewRound` containing excluded notes before denying access. Keep note/answer payloads out of logs and browser console diagnostics.

Every read/save verifies round, current configuration revision, no review requirement, and exact target/field inclusion. Ownership alone does not authorize a hidden field. Accept typed commands for answer, asked/follow-up flags, enabled interview notes and ad hoc creation, not arbitrary property paths or global IDs. Private review owns configuration changes. Recheck on reconnect/focus and with a small periodic revision check; stale configurations pause editing and clear displayed content before a neutral review-required message.

Server checks block future reads/writes after a visibility change; old/offline tabs may still hold previously approved content. Tell users to close old Workspace tabs before changing visibility. No promise of instantaneous remote erasure. Pending drafts cannot bypass new inclusion rules and remain available privately.

Narrow `/interviews/{id}/workspace/cv/view` and `/download` routes resolve only selected, still-valid submitted evidence. Apply configuration/ownership checks, hash/size verification, neutral filename/title (`submitted-cv.pdf`), private/no-store headers, nosniff and the existing restrictive preview policy. Errors stay neutral and Workspace-local, without a global document-page link. Native preview plus download fallback remains; do not add a PDF renderer to work around the earlier unconfirmed viewer. Users review PDF contents before inclusion.

Embed the current configuration revision in CV URLs and require it server-side; missing/stale revisions cannot retrieve a newly selected file from an old tab. Archived document metadata alone does not invalidate immutable submitted evidence. Incorrect submissions, missing bytes or mismatched hashes do; never substitute another CV.

## 6. Autosave, browser drafts, and conflict recovery

This reliability change applies to Workspace capture, not every form in Scout.

1. Attach native input listeners to concrete text editors and change listeners to asked/follow-up controls. Capture edits locally in IndexedDB before depending on a SignalR round trip. The editor owns its dirty DOM value/caret; an unrelated rerender cannot replace it with older server text.
2. Local records contain schema version, WorkspaceId, round/target, configuration revision, base content revision/value, tab ID, monotonic draft version, payload and update time. Tabs have separate records so they cannot overwrite each other's unsent drafts. Unsaved ad hoc questions use their creation token before receiving a database ID.
3. Write during input, not first on unload. Serialize/coalesce local writes so an older write cannot replace newer input. Show **Draft stored in this browser** only after transaction completion. Quota/denial/write failure leaves current text intact with **Draft recovery unavailable — save or copy now** feedback.
4. Send server saves after 750 ms idle, no more than 3 seconds between attempts during continuous typing, and on blur/Save now. One request per question/notes target is in flight. Fields on one question share a serialized queue because they share its revision. Coalesce later input and advance expected versions only from acknowledgments.
5. Distinguish **Saving**, **Saved to Scout**, **Unsaved/offline**, and **Conflict**. Browser draft storage is not a SQLite save. A response acknowledges its captured draft version; it never clears newer input/drafts. New input typed in flight is preserved and saved afterward. Autosave creates no timeline spam.
6. On reload/reconnect, obtain allowed-target metadata first, then retrieve drafts only for this Workspace and permitted fields. No global get-all-drafts scan or popup revealing another interview. Old-configuration/excluded drafts are recovered only in private setup with explicit reapproval.
7. If a draft already equals saved content, clear only that acknowledged record. Otherwise offer **Restore draft**, **Keep saved version**, and **Copy draft**. Changed base revision/value means conflict, not automatic replay. Restore requires current field permission and conditional save against the version reviewed by the user.
8. Conflict responses return only approved current fields/revisions. Retain the draft and pause that target; user chooses saved content or explicitly resolves/copies their draft and saves against the current version. No automatic concatenation, blind retry over newer values, or silent last-writer-wins. Other targets may continue saving.
9. Retain drafts across backend failure. The quiet existing offline/reconnect flow has no countdown and may reload a rejected circuit; recovery must work after that reload. Full editing features need a running backend. Flush during ordinary navigation where possible and warn while unsaved; asynchronous unload work is not the primary protection.
10. Clear only exact committed/discarded draft versions after acknowledgment or an explicit choice. No age-based cleanup of unsaved drafts. Provide copy/download of unsaved text when storage/save fails. Dispose listeners/timers/JS references on leaving.

For an uncertain server commit, re-read only permitted current fields: if all fields in the captured patch already equal its desired payload, acknowledge that version without another write/revision increment. Otherwise retain the draft and use the normal conditional-save/conflict flow. This avoids a generic request journal while making text/state retries safe; ad hoc creation uses its separate unique creation token.

IndexedDB is asynchronous; transaction completion and unload behavior matter ([MDN guidance](https://developer.mozilla.org/en-US/docs/Web/API/IndexedDB_API/Using_IndexedDB)). Browser storage can be denied, evicted or cleared and is scoped to profile/origin ([storage limits](https://developer.mozilla.org/en-US/docs/Web/API/Storage_API/Storage_quotas_and_eviction_criteria)). Do not promise recovery before a local write completes, after arbitrary power loss, browser-data clearing, or at another browser/port. Whole-root backups contain canonical SQLite saves, not uncommitted browser drafts. No cloud sync or OS-wide encryption feature.

## 7. Implementation packages and gates

At implementation start: preserve working changes, reread M3 delivery, verify tools, stop Scout before rebuilding its locked executable, back up the stopped whole root and establish the actual baseline. Never reset/reseed personal storage.

| Package | Vertical deliverable | Gate before continuing |
| --- | --- | --- |
| 1. Reviewed scoped Workspace | Configuration/inclusion snapshots, private review/launch, isolated layout, position/CV/question/STAR projections, narrow CV routes. | Fresh/upgrade SQLite; ownership and role checks; excluded-field/query/serialization tests; invalid CV/config IDs; review-required behavior. Browser: distinct applications, selected fields only, no global navigation/search, neutral errors, CV/no-CV/fallback. |
| 2. Live round behavior | Asked/follow-up state, ad hoc origin/token, prior-answer references, manual answers/notes, revisions in every relevant writer, selection-removal invalidation. | Deleted-source/ad hoc distinction; atomic retry-safe creation; independent prior/current answers; owner checks; races between Workspace and private editors. Browser: manual capture, reopen, stale form, preserved prepared wording. |
| 3. Reliable capture | Native input capture/IndexedDB, save queues/timing, honest feedback, Save now, conflict handling, recovery/navigation protection. | Edits during in-flight save; out-of-order acknowledgments; unknown commits; multiple tabs; denied/quota storage; refresh/disconnect/backend restart; excluded recovery targets and changed profiles. Actual browser interruption checks required. |
| 4. Integrated delivery | Layout/keyboard polish, privacy/recovery regression, samples, M3 upgrade/restore, operating guide/delivery summary. | Full suite/build/model checks; two-application mock; restarts/recovery; old/new data and hashes preserved; isolated restore; environmental limits recorded. |

After **every package**, run the entire suite and fix failures before proceeding. Use exposed Rider build/diagnostics per `AGENTS.md`, otherwise CLI fallback. M3 required direct xUnit execution because `dotnet test` failed its handshake; verify the current environment and record the real command. Zero tests or sandbox-denied file runs do not pass a gate.

```powershell
dotnet build Scout.slnx --no-restore -m:1 -p:UseSharedCompilation=false
dotnet tests/Scout.Web.Tests/bin/Debug/net10.0/Scout.Web.Tests.dll
dotnet ef migrations has-pending-model-changes --project src/Scout.Web --no-build
```

HTTP GETs alone verify only the interactive bootstrap. Reuse M3's `HtmlRenderer` pattern for component content, migrated temporary SQLite for data, and isolated storage/test hosts. Browser tests exercise actual circuits and native draft storage. Record steps/screenshots and meaningful committed operation/rendering/failure tests; no new browser-framework dependency solely for smoke checks. Resolve required unavailable gates before claiming their behavior verified.

## 8. Migrations, preservation, and test detail

Add reviewed migrations as schema-bearing packages arrive; never rewrite twelve applied migrations. Four new tables are expected: configuration and three inclusions/references. No consent or recording tables are added in M4. Existing-row additions are the focused origin/state/revision/token fields and ownership indexes. Existing origins default Library; asked/follow-up flags default false and revisions zero. New configurations include nothing. Do not infer conversation facts or rewrite M3 answers/notes/order/source-deleted copies.

Upgrade a populated fixture from `20261015221434_InterviewExperiencePreparation`: both catalogs, two rounds, detached copies, archived selections, exact submitted files/hashes. Verify old rows except explicit defaults, EF model consistency, `integrity_check` and `foreign_key_check` before/after migration and in a separate stopped whole-root restore. Include missing/corrupt selected-CV cases. Never overlay personal data. Source deletion must still preserve content/revisions.

Privacy fixtures put distinctive canaries in another application and excluded fields of the current one. Assert absence from projections, serialized DTOs, rendered Workspace, title/links/errors, JS initialization and fetched drafts—not merely invisibility. Capture generated query projections/commands to show excluded fields were not selected. Private setup may retrieve previews. Wrong round/item/CV/config IDs fail without cross-scope content. Reviewed, attributed prior answers are the deliberate earlier-round exception.

Include malformed Workspace URLs, missing rounds, thrown component errors, stale CV URLs and reconnect/recovery failures in that privacy coverage. Error paths must be as isolated as the successful view.

Concurrency tests use independent contexts and all relevant writers. Browser tests interrupt after local draft completion and before/after server acknowledgment, type while saving, restore/discard/copy, and resolve competing tabs. Verify no stale acknowledgment clears new input, no other interview's draft appears, and local draft completion is distinct from database save. Test the actual quiet reconnect/rejected-circuit reload path.

Keep opt-in Development/every-business-table-empty seeding, including new tables. Fictional samples may demonstrate an approved profile, prior-answer context and manual-note capture. No consent or recording sample records are introduced in M4. Existing M3 or standalone rows prevent reseeding. No automatic deletion of originals, failed imports, unsaved drafts or backups.

## 9. Acceptance and required delivery output

Use fictional material and isolated roots where practical:

1. Prepare two applications with distinct private notes/STAR text. Approve one round's exact fields, CV and optional prior answer. Open a separate Workspace; excluded/unrelated data is absent from retrieval, expansion, search and recovery.
2. Check balanced/notes-wide layouts, panel preferences, keyboard use and Save now on the wide display. Remembering a panel cannot grant inclusion permission.
3. Mark questions, capture answers/notes, flag follow-up, add an ad hoc question, and consult approved STAR fields. Preserve M3 wording/history, including deleted-source copies.
4. Attribute a chosen earlier answer and keep it unchanged while editing the current one. Exercise no/missing CV and native-viewer download fallback.
5. Refresh, disconnect, restart the backend, fail a save, deny/exhaust draft storage and edit concurrently. Recover only permitted same-interview drafts, with no false saved status or silent overwrite; document the unavoidable limits.
6. Change inclusion privately/remove a prepared item. Old profiles stop reads/writes and require review; excluded drafts cannot appear. Close stale/offline tabs when changing shared content.
7. Finish a mock interview using manual answers/notes without recording setup, recording choices or consent prompts. No M4 operation requires audio.
8. Verify reopen and isolated stopped-root restore preserve M3 content and submitted hashes. Exercise external-network-disabled runtime if permitted; otherwise report it unperformed.

Produce `docs/MILESTONE_4_DELIVERY.md` using [M3's evidence format](MILESTONE_3_DELIVERY.md): actual package build/test gates and final counts; migrations/dependency changes/deviations; browser/mock/privacy/recovery results; backup/restore evidence; limits; and the M5 starting point, explicitly identifying the deferred audio/consent workflow, recording metadata, import/linking, file-limit policy and related verification. These need M5 design review rather than placeholder M4 implementation. Do not claim native PDF rendering or network-disabled behavior passed from byte tests/local-asset inspection alone.

M4 is complete only when gates pass and delivery documentation distinguishes verification from environmental limits. The completed implementation and actual verification results are recorded in [MILESTONE_4_DELIVERY.md](MILESTONE_4_DELIVERY.md).
