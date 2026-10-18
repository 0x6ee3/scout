# Scout: Milestone 2 implementation plan

Status: approved and implemented. All six package gates passed; see [delivery record](MILESTONE_2_DELIVERY.md) for validation and environmental limits.
Date: 15 October 2026.
Scope authority: [Domain model](../../../products/scout-web/DOMAIN_MODEL.md), especially sections 1.3–1.9; [product features](../../../products/scout-web/PRODUCT.md), sections 2.3–2.4; [persistence and constraints](../../../products/scout-web/TECHNICAL.md), sections 3 and 5; and the [roadmap](../ROADMAP.md), section 1.
Existing foundation: [M1 architecture](MILESTONE_1_ARCHITECTURE.md), [M1 delivery record](MILESTONE_1_PLAN.md), and the [user](../../../products/scout-web/USER_GUIDE.md), [operations](../../../products/scout-web/OPERATIONS.md) and [development](../../../engineering/DEVELOPMENT.md) guides.

## 1. Outcome and boundary

After M2, the user can manually enter the six current opportunities and manage each application from planning to outcome: contacts, chronological developments, next actions, interview rounds, documents, and exact submitted CV files. The application detail page brings those records together without requiring a spreadsheet. All core behavior remains local and works without AI or external services.

This is **one milestone for approval and delivery**. The ordered work packages below keep implementation testable; they are not separate approval checkpoints. Build and run relevant tests after each package, fixing failures before continuing. Do not start application-code changes until this plan has been reviewed.

Included:

- Applications with editable status, dates, notes, and independent archive/restore.
- Company contacts linked to applications, timeline events, and interview participants.
- Flexible timeline events and lightweight next actions for an application or an unapplied position.
- Multiple interview rounds, scheduling, preparation notes, and manually saved interview notes.
- Safe local document imports, controlled file viewing/download, and reuse through explicit associations.
- Prepared CV PDF versions and a manual submitted-document history preserving exact files.
- Search, useful filters, navigation, validation, failure handling, migrations, tests, and operating documentation for these features.

Excluded: automatic communication or external form completion; importing the user's six opportunities without their supplied data; question/experience libraries; interview preparation selections; Interview Workspace; live note autosave/draft recovery; recording controls or consent state; transcription; AI; email/calendar integration; scraping; CV generation/editing; dashboard/analytics; installers; background processing; automated backups/cleanup.

An interview can be recorded as completed with notes alone. Generic audio attachments are opaque files in M2; the dedicated external-recording workflow, working-directory linking, playback, and larger audio handling belong to M4. There will be no inactive Workspace or recording buttons.

## 2. Repository review and decisions to approve

The repository currently has one `net10.0` Blazor Interactive Server application, one xUnit/Microsoft Testing Platform project, two business tables, three migrations, and 68 tests recorded as passing at the M1 gate. This planning task inspected code and documentation; it did not rerun those tests or change the database. No applicable `AGENTS.md` was found in the repository/ancestor paths inspected.

Reuse the interactive router, detached edit models, short-lived context factory, concrete feature operations, explicit archive predicates, query-backed catalogs, validation, and unsaved-navigation guard. Preserve the fixed `<user home>\Documents\Scout` root, loopback/Host/Origin protections, quiet offline message, and existing vacancy-snapshot rule.

There are **no specification contradictions that prevent M2**. These choices resolve areas intentionally left open by M1; they are proposals, not claims that the specification already mandates them.

| Decision or tension | Proposed M2 treatment and reason |
| --- | --- |
| One or several applications per position? | Allow multiple attempts, including more than one active attempt. Offer an optional short label and show existing attempts before creation to avoid accidental duplicates. No unique Position FK and no forced one-attempt workflow. |
| “Statuses should be easy to modify.” | Use a simple status selector and a C# enum persisted as stable strings. Users may move directly between any valid values; no transition engine or user-configurable status-definition tables. Adding a new vocabulary item later is an ordinary code change. |
| Timeline is primary, while status is a current summary. | Record a status-change event in the same SQLite commit as the change. Keep user-entered historical events independent: adding an old rejection event does not silently change today's status. Status alone never proves a submission happened. |
| What counts as submitted? | The user explicitly records something already sent outside Scout. Selecting a CV only prepares a draft; confirming **Record submission** saves its date and exact selected files. Nothing is transmitted. |
| Exact submitted CV versus reusable attachments. | Imported bytes and their storage path are immutable. A submission references that immutable document, with a label snapshot; it does not follow a mutable “current CV” pointer. Many associations may reference the same stored bytes. |
| Multiple CV versions. | Each prepared CV row represents one imported PDF version, with optional language/focus metadata. Importing a changed source creates a new version. No CV family/version graph, editor, or replacement-file action. |
| Contacts and employer context. | Each contact belongs to one company. Applications use contacts of their position's company; events and rounds may choose from those contacts without requiring a participant or an application-level contact assignment first. External recruiters can be entered as contacts for the represented employer, with agency information in Role/Notes. |
| M1 company reassignment could invalidate M2 history. | Once a position has any application, block changing its CompanyId. Application.PositionId and Contact.CompanyId are fixed after creation. Explain the restriction; a mistaken owner is corrected by archiving the mistaken record and creating the right one, not by silently moving history. Ordinary names/details remain editable. |
| “Document import” overlaps later audio/transcript features. | Support ordinary file preservation/association now, including opaque audio/email attachments. Do not interpret them as recordings/transcripts, parse mail, or add processing-state tables. |
| General MVP dashboard versus M2 deliverables. | Deliver an application catalog, a next-action catalog, and the application overview. Defer a separate dashboard and calendar. |
| File preservation versus interrupted imports. | No database/filesystem transaction can be fully atomic. Finalize the file before committing document metadata; retain and report incomplete/unregistered files on failure. Never delete them automatically to simulate rollback. |

The plan can proceed after review without additional product answers if these defaults are accepted. The substantive review choices are repeat attempts, fixed status vocabulary, ownership restrictions, and the file/submission policies below. The exact fields, limits, and page arrangements are deliberately small defaults that can be adjusted during this review.

## 3. Architecture and repository additions

Keep the current solution and projects. Add feature folders only as their behavior is built:

```text
src/Scout.Web/
  Domain/                         # M2 entities and small enums
  Features/
    Applications/                 # operations, edit/list/detail models
    Contacts/
    Timeline/
    NextActions/
    Interviews/
    Documents/                    # import, associations, file read rules
    Cvs/                          # prepared PDF versions
    Submissions/                  # immutable submitted-file records
  Components/Pages/
    Applications/ Contacts/ NextActions/ Interviews/ Documents/ Cvs/
  Components/                     # share attachment/contact selectors only when reused
  Data/
    ScoutDbContext.cs
    Migrations/
  Storage/
    DocumentFileStore.cs           # concrete local stream/path operations
  Configuration/StoragePaths.cs
tests/Scout.Web.Tests/
  Applications/ Contacts/ Timeline/ NextActions/ Interviews/
  Documents/ Submissions/ Data/
```

Keep small projection records with their feature operations initially. Split EF mapping files by entity if the growing context mapping becomes difficult to read. `DocumentFileStore` receives resolved storage paths and streams; it does not depend on Blazor. Feature operations own validation and SQLite writes; components own draft/selection state. Register the concrete storage helper through DI, without a provider hierarchy or cloud-storage interface.

Use narrowly scoped HTTP GET endpoints for document bytes, not a new API project. Resolve files from document IDs and stored metadata. Keep all other business interaction in Blazor/concrete operations. Do not add generic entity-link tables, repositories, a workflow framework, MediatR, a unit-of-work wrapper, an outbox, or a background reconciliation service.

For isolated HTTP file tests, register `StoragePaths` through DI and resolve it consistently at startup, from the DbContext factory, and from file access. The default factory still hardcodes the approved current-user root. A test host replaces that service before startup with its unique temporary root; this is not a production storage override or settings screen. Design-time tooling continues using the same fixed normal root and never seeds.

### Dependency baseline

Keep the M1 pins: SDK **10.0.402**, `net10.0`/C# 14, installed .NET/ASP.NET Core **10.0.13**, EF SQLite/Design and local `dotnet-ef` **10.0.13**, and `xunit.v3` **4.0.2**. These are the repository's existing baseline, not a new upgrade proposal.

The only proposed new direct package is **Microsoft.AspNetCore.Mvc.Testing 10.0.13**, test project only. Its stable release is confirmed by the [official package listing](https://www.nuget.org/packages/Microsoft.AspNetCore.Mvc.Testing), checked 15 October 2026. Use a small `WebApplicationFactory` fixture for document-response/path-boundary tests, following [ASP.NET Core integration-test guidance](https://learn.microsoft.com/en-us/aspnet/core/test/integration-tests?view=aspnetcore-10.0). Keep the existing test runner.

Blazor `InputFile`, `FileStream`, SHA-256, controlled file responses, `TimeZoneInfo`, and browser PDF support require no added runtime packages. Do not add a PDF renderer, email parser, Markdown renderer, antivirus service, JavaScript build pipeline, audio library, or AI SDK. Recheck package availability and supported patches when implementation begins; do not silently take a major/prerelease upgrade.

## 4. Relational model and behavior

Use generated integer keys, explicit indexed foreign keys, restricted deletion, UTC creation/update timestamps, and stable enum strings as in M1. Requiredness below describes the minimum input, not an invitation to populate unknown dates/details. Long notes/descriptions stay plain text, preserve nonblank whitespace, and allow 100,000 characters. Short labels/titles/names allow 200, URLs 2,048, contact Role 200, email/phone 320/100, CV language/focus 20/200, and display filenames 255 characters. Validate all limits at the operation boundary as well as in forms.

### Applications and timeline

| Entity | Minimum proposed fields and constraints |
| --- | --- |
| `JobApplication` | Required PositionId; optional AttemptLabel, AppliedOn (`DateOnly`), OutcomeOn (`DateOnly`), OutcomeNotes, Notes; Status defaults Planned; IsArchived; audit timestamps. Derive company from Position; no second editable CompanyId. Name the CLR type `JobApplication` to avoid confusion with framework application types. |
| `ApplicationTimelineEvent` | Required ApplicationId, event date (`DateOnly`), kind, and description; optional exact occurrence UTC time; IsArchived and audit timestamps. Distinguish user-authored events from system events, with structured old/new application status for status changes. An optional unique SubmissionId links a system submission event to its authoritative record; an optional InterviewRoundId identifies round lifecycle events. Validate either link belongs to this application. |

Application statuses: **Planned, Applied, Screening, Interviewing, Offer, Rejected, Withdrawn, Accepted**. Do not synchronize them with PositionStatus or archive flags. Dates remain optional even for historical Applied/Accepted records; do not require invented dates to save. Outcome fields do not force a terminal status. Editing dates alone does not fabricate an event that something was sent.

Timeline kinds cover the specification's application submitted, manually sent email, received email, recruiter call, interview, coding challenge, documents received, offer, rejection, personal note, and Other, plus internal Application created and Status changed. Other + free text handles unusual processes. Choosing an email kind only records an event.

- Application creation records its initial state; a new Applied record is not automatically labeled a verified file submission.
- An actual status change appends one system event with before/after values, using the same context/commit. Setting the same value creates no duplicate event. General edits do not append generic audit noise.
- Manual historical events can be corrected or archived with ordinary audit timestamps. System event facts are not independently editable/archivable: correct the owning action or add a clarifying manual note. This is a small timeline, not event sourcing or a tamper-proof audit log.
- Permit a date without a time for historical events. When a time is supplied, derive/validate the local date consistently. Sort chronological views by event date, then known local occurrence time (untimed items have an explicit consistent placement), then ID. Show “time not recorded” instead of inventing midnight.
- The application overview shows recent events newest first with access to full chronological history. Archive filters refer to event state; archived events remain reachable.

### Contacts and interview rounds

| Entity / association | Fields and rules |
| --- | --- |
| `Contact` | Required CompanyId and Name; optional Role, Email, Phone, ProfileUrl, Notes; IsArchived; audit timestamps. Validate profile HTTP/HTTPS and email syntax; no lookup or sending. Duplicate names/emails are allowed. |
| `ApplicationContact` | Composite key ApplicationId + ContactId. Represents explicitly associated contacts. |
| `TimelineEventContact` | Composite key EventId + ContactId. Optional participants in a particular event. |
| `InterviewRound` | Required ApplicationId; Title defaults “Interview”; Type defaults Other; Status defaults Planned; optional ScheduledAtUtc, CompletedAtUtc, LocationOrMeetingUrl, PreparationNotes, InterviewNotes; IsArchived; audit timestamps. Types: HR, Engineering, Culture, Technical, Final, Other. Statuses: Planned, Scheduled, Completed, Cancelled. |
| `InterviewParticipant` | Composite key InterviewRoundId + ContactId. Zero participants is valid. |

Every contact link must resolve to the application's company. Enforce the rule for create/update/association operations, including IDs posted from a stale or altered form. Archived contacts remain shown on existing links but are not offered for new links; restore first. Creating an event/round with selected contacts is one database transaction; link validation failure changes nothing. Unlinking a contact association does not delete/archive the contact or its other links. The application overview lists the deduplicated union of explicitly linked contacts and event/round participants, identifying how each is related; it does not silently create extra ApplicationContact rows.

Interview scheduling is optional for a Planned/historical Completed round; Scheduled requires a valid date/time. Completion does not require contacts, files, audio, answers, or notes. CompletedAtUtc is optional for historical records and is not invented. Completing/cancelling a round changes only its round status and records a system timeline event in the same commit. That event says when the record was marked completed/cancelled, not that the historical interview necessarily happened then; show any known actual completion time separately. It does not silently change application status. Subsequent corrections remain possible and record the status correction; an unchanged state adds no duplicate event. Scheduling/preparation are ordinary private pages; live interview operation remains M4.

Use the Windows machine's local time zone, explicitly labeled in date/time forms, converting entered wall time to UTC. Reject nonexistent daylight-saving times; for an ambiguous time, ask the user to choose the displayed offset/standard-versus-daylight occurrence. Keep date-only fields independent of time-zone conversion. This follows the [standard .NET time-zone approach](https://learn.microsoft.com/en-us/dotnet/standard/datetime/let-users-resolve-ambiguous-times); no calendar/time library is needed.

### Next actions

`NextAction` has required Title, optional Description and DueOn (`DateOnly`), Status Open/Completed, optional CompletedAtUtc, IsArchived, and audit timestamps. It belongs to **exactly one** ApplicationId or PositionId, enforced by a database check constraint and foreign keys. A position-owned action supports work before an application exists; do not require creating an application first.

An application-owned action may optionally reference one interview round or one timeline event of that same application. A position-owned action has neither reference. Validate this scope; do not auto-promote/move tasks when an application is created. Owner is fixed after creation. Complete/reopen uses explicit operations, keeps the original due date, and sets/clears completion time without duplicating records. Archiving an erroneous task does not claim it was completed.

The global next-action catalog defaults to Open, sorts dated work by due date with ID tie-breakers and undated work last, and labels overdue work using the local date. Offer status, due/overdue, company/application, and Active/Archived/All filters. Existing open actions under archived parents remain visible with badges; archiving must not silently hide an obligation. Do not add reminders, notifications, recurring tasks, or scheduler infrastructure.

### Documents, prepared CVs, and submissions

| Entity / association | Fields and rules |
| --- | --- |
| `Document` | Original display filename, unique immutable StoredRelativePath, server-selected ContentType, actual byte Size, SHA-256, ImportedAtUtc; editable Description and Category; IsArchived; audit timestamps. Filename/path/bytes/hash/import facts cannot be changed by the general edit form. |
| `CompanyDocument`, `PositionDocument`, `ApplicationDocument`, `TimelineEventDocument`, `InterviewDocument` | Explicit composite owner + DocumentId keys; multiple attachments per owner and reuse of one Document across owners. No polymorphic EntityType/EntityId link. |
| `PreparedCv` | Unique required DocumentId for one validated PDF; DisplayName, optional Language and TechnologyFocus; IsArchived; audit timestamps. DocumentId never changes. New bytes mean a new row/document. |
| `ApplicationSubmission` | Required ApplicationId and SubmittedOn (`DateOnly`); optional Notes; RecordedAtUtc; unique RecordToken generated for a confirmation attempt; correction flag/note for an incorrectly recorded submission. Fixed owning application/date/files once confirmed. |
| `SubmissionFile` | Composite SubmissionId + DocumentId, Role (CV/CoverLetter/Other), LabelAtSubmission. Unique filtered constraint allows at most one CV per submission. Other files may be many; all links reference the same immutable Document store. |

Categories are a small enum (CV, CoverLetter, Vacancy, Email, Challenge, InterviewMaterial, Audio, Transcript, Other); they classify files only. No extraction/processing behavior follows from a category. CV eligibility comes from the explicit PreparedCv record and PDF validation, not a mutable category label.

**Manual submission flow:** choose the application, actual date, optional one prepared CV version, and any existing active documents; import first if needed. Show filenames/CV labels/date for review, then **Record submission**. The form states that it records material sent outside Scout. Permit no files when they are unavailable historically; do not require making up evidence. A manual timeline event may also describe a past submission without claiming known files.

One SQLite commit inserts the submission, selected-file links/labels, and its timeline event. An explicit optional “Update application to Applied” choice is offered only where sensible (default on for Planned, off otherwise); its status change is included in the same commit. If AppliedOn is empty, offer to set it to this date; never overwrite an existing date automatically. AppliedOn is editable summary information, while each submission preserves its own actual entered date. A later document resend is another submission, not replacement of the first.

Only active documents, and CVs whose PreparedCv and Document records are both active, may be newly selected. CV/document archive flags remain independent; show a badge and restore link if one is archived. Later archiving or metadata edits do not break history or rewrite its labels. On the application, show the most recent valid submitted CV with a link to complete history. A user can mark a mistaken submission **Recorded incorrectly**, with a reason, and enter a corrected record; keep the original dates, file associations, bytes, and correction visible. Do not detach, replace, or delete submitted files through generic attachment controls. Repeated confirmation with the same RecordToken returns the existing result without another event/status update; a different payload with that token is rejected.

General document associations can be removed explicitly with **Remove link**; the document and bytes remain in the catalog and on every other association. General attachment links and submission history are distinct. A document used by a submission need not also receive a redundant ApplicationDocument row to be visible in its submitted-files section.

### Archive and relationship policy

All editable main M2 records support independent archive/restore; link rows have explicit unlink operations. Submissions use the correction workflow above instead of hiding submitted history. Do not cascade archive state to descendants, rewrite statuses, or permanently delete business records/files.

Existing records under archived parents remain readable/editable with badges. New child records require an active direct owner; new applications additionally require both an active position and company, and new contacts require an active company. An existing active application remains usable when its company/position is archived. Archived direct owners must be restored to add a new child. Existing associations and historical downloads remain accessible regardless of archive flags. No hidden global EF query filters.

## 5. File storage, import, and controlled access

Retain the fixed root; add only what document imports actually need:

```text
<user home>\Documents\Scout\
  database\scout.db
  assets\
    documents\<generated-guid>\original.<allowed-extension>
```

Use the same immutable store for CVs and other attachments. Preserve the user-visible original filename in metadata, never as a physical path. Do not create audio/transcript/export subsystems or depend on source files remaining at their original locations.

### Import limits and validation

- A deliberate file picker and Import action; one file per operation, **50 MiB maximum**, visible before selection. Support `.pdf`, `.txt`, `.md`, `.eml`, `.msg`, `.mp3`, `.wav`, and `.m4a`. Other types, executables, HTML/SVG, archives, and directory imports are outside M2. Large recordings are a later decision, not a reason to add resumable uploads now.
- Stream Blazor `InputFile` data to disk with an explicit `OpenReadStream(maxAllowedSize)` bound; also count actual bytes. Never trust browser Size, ContentType, or filename as authority. Do not buffer whole imports in a byte array or raise SignalR message limits to the file-size limit. Use cancellation and a simple byte-count progress display. This follows [Microsoft's Blazor upload guidance](https://learn.microsoft.com/en-us/aspnet/core/blazor/file-uploads?view=aspnetcore-10.0).
- Enforce the allowlist, nonempty bounded bytes, safe filename metadata, and PDF header validation for PDFs/CVs. Opaque email/audio files are preserved/downloaded, not asserted to have been parsed or deeply validated. Browser `accept` is only a convenience. Choose response MIME types on the server; ignore claimed browser MIME. No antivirus guarantee or automatic execution is implied. See [ASP.NET Core upload guidance](https://learn.microsoft.com/en-us/aspnet/core/mvc/models/file-uploads?view=aspnetcore-10.0).
- Use generated directories/names and create-new writes; never overwrite an existing file. Reject traversal, rooted/UNC/device paths, alternate data streams, and path escape on reads as well as writes. Verify containment using normalized full paths and a separator-aware root comparison, not a naive prefix. Reject reparse points/junctions/symlinks in the managed asset root and its descendants used for access. Do not accept an arbitrary local source path through a form or URL.
- Compute SHA-256 during streaming. Reuse an existing imported document by selecting it; do not automatically merge new imports merely because hashes match. This preserves original-filename/import provenance without implementing duplicate-resolution UI.

### Commit and failure behavior

1. Validate draft metadata and target association before reading; generate one unique import path for the attempt.
2. Stream into a create-new `.partial` file in that generated directory, counting bytes/hashing. Finish validation, flush, close, and rename to the final generated filename without overwrite, within the same directory.
3. Only then use one SQLite transaction to save Document metadata plus any selected owner link and PreparedCv row. Revalidate the target at commit time. An unattached import is allowed; a requested association must not silently disappear.
4. Report success only after commit. If its outcome is uncertain, look up the attempt's unique stored path before offering a retry, so the same completed attempt is not registered twice.
5. On failure/cancel/crash, do not remove either source files or newly written files. An incomplete file or a complete file without metadata remains private and unregistered; explain the failure and retained relative location, preserve text inputs, and allow a deliberate retry. Startup does not guess owners, register partial files, reset data, or run cleanup. A restarted browser may require reselecting its source file.

SQLite can roll back metadata/links; it cannot roll back disk writes. Thus a crash may leave an orphaned retained file, but must not publish partial bytes as a successful import or a committed submitted document. Document this tradeoff and include such files in manual whole-root backups. No reconciliation table/job or automatic deletion is required.

### Read/view behavior

- Route document reads by ID, for example `/documents/{id:int}/download` and `/documents/{id:int}/view`. Apply the existing loopback/Host/Origin boundary. Do not mount `assets` as static content or put physical paths in public URLs.
- Resolve metadata and safely open only its managed file; an unknown ID, invalid path, missing file, or size/hash mismatch is a recoverable unavailable-file result. Keep metadata/history intact; never serve a different file as a substitute. Verify through one open file handle and keep it protected from writes while responding. Stream responses; do not load whole files into memory.
- **Open PDF** uses an explicit new-tab action and the browser's PDF support, with server-owned PDF MIME, deliberate inline disposition, `nosniff`, private/no-store caching, and a sandbox/no-external-content response policy. Provide Download if the browser cannot preview it. Verify headers and actual browser behavior; do not add a PDF library to work around a viewer preference.
- TXT/Markdown have a bounded UTF-8/BOM-aware encoded plain-text preview (first 256 KiB, marked if truncated). Do not execute HTML or render Markdown HTML; a decoding problem offers the unchanged file for download. All imports retain original bytes.
- Email/audio formats are download-only opaque attachments. Do not open a shell process, follow an email link automatically, fetch a remote image/font, start playback, or inspect arbitrary files. Original names in response headers must use framework-safe encoding; never concatenate untrusted header values.

## 6. Pages and navigation

Add Applications, Next actions, Documents, and CVs to normal navigation; keep Companies/Positions. Contacts are reachable from company and application pages through a searchable contact catalog. Timeline/interview creation is application-scoped. Use the existing styling, desktop spacing, explicit Save/Cancel, errors/retry, missing-record handling, and unsaved-navigation choices. General pages remain private, not screen-share safe.

| Page / route family | Useful behavior |
| --- | --- |
| `/applications`, `/applications/new?positionId=…`, details/edit | Search company/position/attempt label; filter company, status, Active/Archived/All. Default sort by last update descending, stable ID tie-breaker. Details show linked employer/position, status/dates/notes, contacts, open actions, interviews, timeline, attachments, and submitted history. |
| Company/position details | Add contacts/applications/documents where appropriate; show related records with independent archive badges. Position details also show position-owned actions. Existing attempts are visible before Add application. |
| `/contacts`, company-scoped new/details/edit | Name/company/role search and archive/company filters. Optional communication details; no send-email action or external automation. |
| Application-scoped timeline pages | Add/edit historical events and optional contacts/documents. Clear date-only versus timed events; deterministic chronology; system facts link back to their originating action. |
| `/next-actions` plus owner-scoped new/edit | Due/open/completed views and complete/reopen/archive controls. Keep open work under archived parents visible. |
| Interview detail/new/edit under an application | Type, planned schedule, participants, preparation notes, interview notes, attachments, and lifecycle controls. No global calendar, Workspace, or recording UI. |
| `/documents`, import/details/edit | Filename/description search; category/archive filters; size/import date; related-record links; attach existing/import new, Remove link, Open PDF/text preview, Download. No content indexing. |
| `/cvs`, version details/import | Prepared PDF versions searchable by label/language/focus; archive filter. No replacement bytes or generated CVs. |
| Application-scoped submission form/history | Explicit review and record of date/selected immutable files, history downloads, and correction markers. No “Send application” label. |

Persist filters in query parameters and preserve list context across details/edit/back as in M1. Apply structured filters in SQLite and use narrow summary projections plus .NET `OrdinalIgnoreCase` substring matching where appropriate. Never load attachment bytes or long vacancy/interview notes into catalog queries. No cross-application/global search in a future Workspace is implied by these catalogs.

## 7. Ordered implementation work packages

These are implementation steps within the one reviewed M2 plan. Add model changes/migrations alongside working behavior, not all possible tables up front. Each package ends with a clean build, passing relevant tests, and a focused browser check before moving on.

| Order | Working result | Verification gate |
| --- | --- | --- |
| 1. Application records and status history | Create/list/details/edit/archive applications from positions; multiple attempts; optional dates/notes; initial/status-change timeline; enforce ownership preservation when editing M1 positions. | Fresh/upgrade SQLite persistence, repeated attempts, enum validation, atomic status/event writes, no-op status handling, missing/stale parents and existing M1 regression tests; browser create/edit/back/restart. |
| 2. Contacts and complete timeline | Maintain company contacts; link applications/events; enter timed/date-only historical events, correct/archive manual entries. | Cross-company links rejected, archived selections preserved but unavailable for new links, multi-contact save atomicity, chronology/Unicode, no implicit status change or communication; browser multi-contact/history flow. |
| 3. Next actions and interview rounds | Position/application-owned work, due/completion states, scoped round scheduling/participants/private notes, timeline lifecycle records. | Database XOR owner rule, scope rejection, complete/reopen/idempotence, overdue sorting, archived-parent visibility, local/DST conversion, no-contact/no-audio completion, transaction failures; browser plan/complete/cancel round and retain notes. |
| 4. Document import, reuse, and reads | Real streamed file import to fixed assets root, document catalog, all five owner associations, safe preview/download and recoverable import/file errors. | Real temporary files/SQLite plus failure streams/DB failures, containment/collision/reparse checks, limits/signatures, retained partials/orphans, unlink preserves bytes, HTTP boundary/header/byte tests; browser PDF/text/email attach/reuse/download. |
| 5. Prepared CVs and submission history | Import prepared PDF versions, explicitly record externally sent document sets, retrieve exact historic bytes, preserve correction history. | Changed source/new version never alters earlier submission/hash/label, multiple submissions, one-CV constraint, archived-file history, atomic submission/event/status save, retained mistaken records, duplicate-submit protection; browser select/review/record/new version/history download. |
| 6. Complete local workflow and operating guide | Finish filters/empty/error states, consistent labels/focus, M2 development samples, upgrade/backup instructions, whole-application acceptance. | Full suite and pending-model check; fresh M2 and M1 upgrade; no reseeding personal data; restart plus isolated restored whole-root database/files check; repeat acceptance below. |

Use named additive migrations as each package needs them. Review SQLite table rebuilds/check constraints/indexes explicitly; preserve M1 rows, timestamps, snapshots, and IDs. Never rewrite old migrations already applied to personal data. Back up the stopped whole root before applying M2 to personal data. A failed migration stops startup; no reset/fallback.

Extend optional development samples only in the final package, with fictional records/files and all opt-in/Development guards retained. Emptiness checks must cover **all** business tables, not merely the original Companies/Positions. Do not populate M2 examples into an existing M1/personal database just because the new tables are empty. No seed marker, reset, or sample-cleanup mechanism. Any tiny sample file fixtures must be created explicitly through the same managed-file rules without overwriting retained files from an earlier failed seed.

## 8. Tests and final acceptance

Keep one test project, real temporary SQLite migrations, isolated assets, and minimal test infrastructure. Never test imports, permissions, or failure recovery against personal files. Preserve the M1 suite; extend it with behavior tests rather than tests of trivial Razor markup or property setters. Fault injection should use real SQLite constraints/read-only connections, deliberately failing input streams, and isolated filesystem conflicts where possible, not mocked EF queries.

Additional required checks across the packages:

- All required FK/index/check/unique constraints actually hold in SQLite, not just form validation; owners cannot drift through ordinary edits.
- Failed writes do not leave partial contact/document associations or false timeline/status/submission successes. Repeated completion/archive/status operations and double form submission are harmless. Submission confirmation carries an attempt token/identity through retry with a unique database constraint; a deliberate later submission gets a new identity.
- Path tests cover `..`, prefix siblings, drive/UNC/device paths, alternate streams, altered stored metadata, malicious display filenames, symlinks/junctions, same original filenames, and generated-name collision without overwrite. Verify limit+1 actual bytes, lying browser metadata, failed/cancelled streams, complete-file/DB-failure orphans, and missing/tampered history files. Retained failed imports are never downloadable by guessed file path.
- HTTP tests use the test host's temporary paths and test IDs, including unknown IDs, archived-history access, traversal attempts, exact bytes/hash, safe content disposition/MIME, no-store/nosniff/preview policy, and rejected hostile Host/Origin. These do not require an HTTP upload API solely for testing; import operations consume test streams directly, while the browser checks exercise `InputFile`.
- Dates round-trip without time-zone drift; historical untimed events remain untimed; test invalid/ambiguous DST values independently of the machine's current calendar date.
- No status/lifecycle action sends messages, opens remote URLs, or initiates recording. Code review and local browser/process checks confirm no outbound client/integration was introduced. Manually entered meeting/profile links open only on explicit activation.

Final user acceptance walkthrough, using fictional data until the user supplies real opportunity details:

1. Launch normally against an upgraded copy/test database, then verify the existing M1 companies/positions and preserved vacancy text.
2. Create an application for an existing position with minimal input, add another attempt, and verify independent status/history. Test invalid input and both unsaved-cancel choices.
3. Add a recruiter and an interviewer to the company; associate them appropriately. Enter historical events out of date order, including one with no known time. Confirm chronology and no inferred application status.
4. Create a next action on an unapplied position and one on an application; set a due date, complete/reopen, and find them via filters. Archive a parent and confirm its open work is still visible.
5. Schedule two rounds, select optional participants, add preparation/interview notes, and complete one without contacts/audio/files. Confirm its application-scoped timeline entry and notes after reopening.
6. Import a PDF, multiline German text/Markdown, and an exported email. Attach one document to more than one record; remove a general link and verify the original document and other link still work. Preview/download intentionally.
7. Import two prepared CV variants. Record one actual historical submission, change the external source PDF, import it as a new version, and record a later submission. Retrieve both exact earlier/later files. Mark a deliberately mistaken submission incorrect and verify its original evidence remains visible.
8. Search/filter applications, contacts, actions, documents, and CVs; open details/edit and return to the same query. Check empty/missing-file/not-found states and retained drafts after recoverable errors.
9. Stop/relaunch and verify database relationships and files. Back up the complete stopped root, including SQLite sidecars/assets/retained failed imports; validate a separate restored copy without overlaying/replacing personal data. Run the pending-model check and full suite.
10. Verify the normal workflow with external network access unavailable where the environment permits, using restored dependencies/local assets. Record the exact method and any unperformed checks; do not claim a disconnected-network test merely from launching with `--no-restore`.

Typical verification commands remain:

```powershell
dotnet tool restore
dotnet restore Scout.slnx
dotnet build Scout.slnx --no-restore
dotnet test --solution Scout.slnx --no-build
dotnet ef migrations has-pending-model-changes --project src/Scout.Web --startup-project src/Scout.Web --no-build
dotnet run --project src/Scout.Web --no-build --no-restore
```

Update the README and this plan with checks actually performed, migration/file layout, import limits/failure retention, manual submission terminology, and known limitations. Keep M1's stopped whole-root backup procedure; M2 makes backing up assets as important as the database.

## 9. Completion and review limits

M2 is complete when a complete manually managed application, its exact submitted files, contacts, events, tasks, and rounds survive restart and backup/restore; all package gates and final checks pass, with any environmental verification limits stated. The six current opportunities can then be entered through the UI from user-provided details, without automatically contacting anyone or inventing their records.

Known deliberate limits: fixed status/type vocabularies; same-company contact ownership; immutable owner relationships; last successful general edit wins; manually saved drafts rather than interview autosave; 50 MiB single-file imports; no deep email/audio parsing or malware scanning; possible retained unregistered files after failures; no full-text indexing; no screen-sharing privacy guarantee for ordinary pages. Submitted bytes are protected against changes through Scout and checked when read; external deletion/corruption of the data root is reported and recovered from backup, not silently repaired.

The user approved this design and all six work packages. M2 is implemented; M3 and later remain excluded. The original planning task made no application changes; subsequent implementation and checks are recorded in [MILESTONE_2_DELIVERY.md](MILESTONE_2_DELIVERY.md).


