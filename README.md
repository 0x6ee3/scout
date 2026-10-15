# Scout

A local personal job application manager for Windows 11, using ASP.NET Core, Blazor Interactive Server, EF Core, and SQLite. M1–M3 provide companies, positions, applications, contacts, timelines, next actions, interviews, documents, prepared CV versions, submission history, employer questions, and STAR interview preparation.

## Run

Install .NET SDK **10.0.402** or a later patch in the same feature band:

```powershell
dotnet tool restore
dotnet restore Scout.slnx
dotnet run --project src/Scout.Web --no-restore
```

Open **http://localhost:5180**. Keep one Scout process running; stop with **Ctrl+C**. Startup creates storage directories and applies committed migrations without resetting data. After building, use `dotnet run --project src/Scout.Web --no-build --no-restore`.

Restore needs NuGet access. Normal operation uses the local process and local scripts/styles, without accounts, AI, or integrations. Website/profile/meeting links open only on explicit activation. Scout does not send applications/messages, fetch vacancies, or record interviews.

When the backend stops, an open page shows **Scout is offline**, without a countdown. Start Scout and reconnect/reload; the previous server circuit may no longer exist. Unsaved drafts do not survive restart or reload.

Append `-- --Scout:Port=5181` to use another loopback port. Scout enforces loopback listeners and rejects unexpected Host/cross-origin HTTP/WebSocket requests. There is no login or at-rest encryption; software running under your account can access the root.

## Manage an application

1. Add a company (name required), then a position (company/title required). The first nonempty pasted vacancy description becomes read-only with a capture timestamp. Keep later observations in personal notes.
2. Add an application from a position. Multiple attempts are allowed, with optional labels. Application status is separate from opportunity status and archive state. Creation/status changes append timeline facts in the same commit; status alone does not prove files were sent.
3. Add company contacts and associate them with applications, events, or interviews. The overview deduplicates contacts and labels their origins. Archived contacts remain on existing links; restore them before adding new links.
4. Enter historical events with a date and optional time. Manual events can be corrected/archived without changing application status. System facts link to their owning interview/submission where applicable; add a clarifying note instead of rewriting them.
5. Create next actions for an application or a position before applying. Due dates are optional. Complete/reopen explicitly; archiving does not claim completion. The global catalog keeps open work visible under archived parents and sorts dated work first.
6. Add interview rounds, participants, scheduling, preparation, and manually saved notes. Scheduled requires a time. Historical completion can omit its time, participants, notes, files, and audio. Lifecycle history records when the record changed, without inventing when an interview happened or changing application status.
7. Import documents/prepared CVs. Use **Record externally sent submission**, select the exact files/date, review, then **Record submission**. Status/date-applied changes are explicit options. Historical submissions may have no files. Mark mistakes incorrect with a reason while retaining evidence; record corrections separately.

Application statuses: Planned, Applied, Screening, Interviewing, Offer, Rejected, Withdrawn, Accepted. Any valid transition is allowed. Opportunity statuses remain Discovered, Interested, Rejected by me, Expired. Dates and notes do not force status.

Timed forms label the machine's local zone. Invalid daylight-saving times are rejected; repeated times require an offset choice. Date-only values have no zone conversion.

Application.PositionId, Contact.CompanyId, interview ownership, and action ownership stay fixed. A position's company cannot change after any application exists, including archived attempts. Archive a mistaken record and create one under the correct owner. Other general edits use last successful save wins.

Archive/restore is independent and never cascades. New children require active direct parents; new applications also require active company/position records. Existing active applications remain usable under archived ancestors. Existing files/history remain readable after archival. The only permanent-delete action is for reusable employer questions; their interview-specific copies and answers are preserved.

## Prepare an interview

1. In **Employer questions**, curate reusable wording, intent, suggested follow-ups, private library notes, and tags. Answers belong to interviews, not this catalog.
2. In **STAR experiences**, save a title and any useful Situation, Task, Action, Result, memory cue, reflection, likely prompts, and notes. Only the title is required. These are your own experiences; sample stories are fictional.
3. Tags are lowercase single words or hyphen-separated words, up to 20 per entry and 50 characters per tag. Type a tag and press Enter or Add tag; existing tags from both catalogs appear as suggestions. Spaces/underscores/repeated hyphens are rejected. Badges use consistent colors. Search includes longer notes/STAR fields; tag and archive filters combine with search and persist through back links.
4. From a round, choose **Prepare interview**. Select employer questions and STAR examples independently; checked additions survive picker filtering. Save additions explicitly. Picker search covers question wording or experience title/cue and tags; use the full catalogs to search inside longer content.
5. Use **Edit wording / answer** for a round-specific question copy, answer box, and private preparation notes. Initial selection copies wording/intent/follow-ups, never private library notes. Later library edits do not rewrite prepared copies. The same source can be used in another round with an independent answer.
6. Selected experiences show current library content. Editing STAR text updates all selections; **Edit rehearsal notes** saves private notes for just this round. Library editors explain this distinction.
7. Move up/down saves order. Remove from preparation archives the selection; show removed items to restore it without losing wording/answers/notes. New/restored selections require an active round and source. Existing selections remain editable under archived sources/ancestors; a deleted-source question can still be restored from its round.
8. **Delete question permanently** removes only its reusable definition and tags, after an explicit confirmation showing the number of retained interview copies. Those copies keep their wording, answers, notes, and order with a **Source question deleted** label. There is no recycle bin or implicit merge with a later similar question. Experiences and other records still use archive/restore.

These are private management pages with manual saving and navigation guards. Unsaved drafts do not survive reload/backend shutdown. There is no screen-shareable Workspace, asked state, autosave/recovery, recording, transcription, language setting, category system, or separate expected-question catalog in M3. See [M3 delivery](docs/MILESTONE_3_DELIVERY.md) for evidence and the M4 starting point.

Catalog search is literal `OrdinalIgnoreCase` substring matching: `MÜNCHEN` matches `münchen`, and `%`/`_` are ordinary characters. Structured filters combine and persist through catalog details/edit/back query links. Full timeline is chronological, untimed entries before timed entries on the same day, then ID; the overview shows newest first. Accent folding, pagination, and full-text indexing are deferred.

Forms validate in UI and operations, block duplicate in-flight saves, retain drafts on recoverable failures, and warn before leaving modified input. Short labels are normally 200 characters, long notes 100,000, URLs 2,048, contact email/phone 320/100, CV language/focus 20/200. Nonblank long text preserves whitespace and displays encoded. There is no autosave or durable draft recovery.

## Files and submission evidence

The fixed root is `<user home>\Documents\Scout`, normally `C:\Users\Max\Documents\Scout`:

```text
Documents\Scout\
  database\scout.db
  assets\documents\<generated GUID>\original.<allowed extension>
```

It is resolved independently of the working directory, shared by Development/ordinary runs, and outside static assets. Only tests replace the storage service with temporary roots; no storage settings screen is provided.

Imports accept **one file up to 50 MiB**: PDF, TXT, MD, EML, MSG, MP3, WAV, M4A. Streaming counts actual bytes and calculates SHA-256. PDFs require a PDF header. Original names are display metadata, never physical paths. Managed paths reject traversal, alternate streams, unexpected names, and reparse points/junctions.

One document can attach to multiple companies, positions, applications, events, and interviews. **Remove link** removes only that association. Bytes, path, hash, size, name, and import facts stay fixed; description/category/archive are editable. Changing the source does not change Scout's copy. Changed bytes require a new import/CV version.

Each prepared CV represents one imported PDF. Submission files reference immutable documents and preserve the confirmed labels. Later renaming, new versions, or archival do not change earlier evidence. The same confirmation identity/details returns its existing submission on retry; different details require a new form. Nothing is transmitted.

Controlled document-ID routes verify path, size, and hash before serving files. PDF preview uses the native browser viewer with download fallback. Native rendering was not confirmed in the in-app browser (blank surface); use Download original if needed. Text/Markdown preview is encoded plain text, capped at **256 KiB** with a truncation notice. Email/audio are download-only. Responses use server MIME, safe disposition, nosniff, private/no-store caching, and a restrictive preview policy. Missing/changed files produce recoverable errors; Scout never substitutes files. M2 has no mail/Markdown/audio processing, deep PDF inspection, or malware scanner.

Imports create a new `.partial`, stream/flush it, rename without overwrite, then register metadata/links/CV in one SQLite commit. Database/filesystem writes cannot share a transaction. Failure/interruption retains partial or unregistered files and reports their relative location where possible. Uncertain commits are checked by that exact generated path before offering retry. No automatic cleanup/reconciliation exists; guessed paths cannot download unregistered files. Back them up too. Restore original bytes from backup after external loss/corruption.

## Optional samples

Samples are **disabled by default**. They insert only when explicitly enabled in **Development** and **every business table is empty**, including standalone documents. Existing M1 data prevents M2 reseeding. Repeating seeding never changes edited rows; design-time EF commands never seed.

```powershell
dotnet run --project src/Scout.Web --no-restore -- --Development:SeedSampleData=true
```

The launch profile sets Development. Fictional samples include .example companies/positions, an application, contacts, events, actions, interviews (including notes-only completion), and an embedded CV PDF/submission. Files use managed import rules. Failed seeding rolls back rows and retains any created file. No sample reset/cleanup command exists; isolated tests avoid touching personal data.

M3 samples add employer questions, tags, partial/full fictional STAR experiences, selected round preparation, and an explicitly fictional answer. Standalone library content prevents seeding just as existing companies/documents do.

## Backup, upgrade, and restore

1. Stop every Scout process and wait for shutdown.
2. Copy the **entire root** into a new separate backup: database, assets, any SQLite WAL/SHM sidecars, and retained failed imports.
3. Verify file counts/hashes, `PRAGMA integrity_check` (ok), and `PRAGMA foreign_key_check` (no rows). Stronger restore checks also open files and relationships from a separate copied root.
4. Restart Scout. Keep durable backups separately; there is no automatic backup/retention.

Back up before upgrading. M2 adds five migrations after M1's three. SQLite may rebuild TimelineEvents when adding foreign keys. Failed migrations stop startup without reset/fallback. Preserve the whole root before investigating failures/locks. Never rewrite applied migrations or substitute EnsureCreated.

M3 adds four further migrations for the two catalogs/tags and their round selections, bringing the total to twelve. Existing M2 tables and file bytes are preserved. Whole-root backups also preserve new preparation and answers; do not restore only a subset of tables.

To restore: stop Scout, retain the current whole root separately, then put the complete backup at the fixed root. Do not overlay only the database onto existing sidecars/assets. Start the matching version; newer committed migrations apply on startup. Tests/acceptance restore separate copies and never overlay personal data.

## Build and verification

```powershell
dotnet build Scout.slnx --no-restore
dotnet test --solution Scout.slnx --no-build
dotnet ef migrations has-pending-model-changes --project src/Scout.Web --startup-project src/Scout.Web --no-build
```

If `dotnet test` reports a runner handshake failure/zero discovered tests in the Codex execution environment, run the built xUnit runner directly: `dotnet tests/Scout.Web.Tests/bin/Debug/net10.0/Scout.Web.Tests.dll`. This executes the full same suite. Temporary file-rename checks require filesystem access; sandbox-denied runs are not passing gates. M3's delivery record states the command actually used and its results.

Runtime/EF pins remain **10.0.13**, with **xunit.v3 4.0.2**. M2 adds only **Microsoft.AspNetCore.Mvc.Testing 10.0.13**, test-only. Microsoft Testing Platform is selected in global.json.

Tests use real SQLite migrations and temporary files/streams; test hosts replace storage before startup. They cover ownership, archives, atomic history, DST, imports/failures/limits, path/junction rejection, HTTP safety, immutable submissions/retries, seed guards, M1 upgrade, and whole-root restore. Personal files are never imported/reset by tests.

Generate a migration with `dotnet ef migrations add DescribeTheChange --project src/Scout.Web --startup-project src/Scout.Web --output-dir Data/Migrations`; review migration/designer/snapshot and run checks before applying it to personal data.

See the [M2 delivery record](docs/MILESTONE_2_DELIVERY.md) for gates, browser checks, backup evidence, and limits. Browser smoke checks are not a committed end-to-end suite. No network adapter was disabled; --no-restore does not establish disconnected-network operation. Runtime code introduces no outbound HTTP client or remote assets.

## Design and scope

- [Specification](docs/SCOUT_SPEC.md)
- [M1 architecture](docs/MILESTONE_1_ARCHITECTURE.md)
- [M1 delivery record](docs/MILESTONE_1_PLAN.md)
- [Approved M2 plan](docs/MILESTONE_2_PLAN.md)

One application project, one test project, concrete feature operations, short-lived EF contexts. M3 preparation libraries/selections and the M4 Interview Workspace are implemented. Audio/consent, transcription and structured analysis belong to M5; integrations, scraping, dashboards and automated cleanup remain deferred. No real opportunities were invented/imported; enter the user's six opportunities from supplied details.

## Interview Workspace (M4)

From an interview's details or preparation, choose **Start Interview Workspace**. The private review starts with nothing selected and replaces the previous approval. Review exact text and the submitted CV before approving; source changes during review require reloading and reviewing again. Close old Workspace tabs before changing visibility. After approval, use **Open Interview Workspace** in a separate tab/window.

Only approved content appears in the Workspace. Position/description, prepared wording, selected STAR fields and attributed earlier answers are reviewed copies; refresh them through private review. Enabled live answers/interview notes remain visible as their contents change. Preparation/rehearsal/library notes are excluded unless explicitly included. Selected submitted CV bytes are verified and never replaced by a newer CV. Use **Download submitted CV** if native PDF preview is blank.

Mark asked/follow-up state independently of answer text. Add ad hoc questions directly to this round. Search/tag matching and asked/unanswered/follow-up filters cover included entries only. Collapsing panels and choosing Balanced/Notes wide affect layout, not permissions. There is no recording setup or consent prompt in M4.

Live edits store browser drafts on input, autosave after 750 ms idle (with a 3-second attempt interval during continuous typing), and save on blur or **Save now**. Ctrl+S saves live answer/note changes; ad hoc creation still requires **Add question**. **Draft stored in this browser** is distinct from **Saved to Scout**. A stale save shows both versions and requires an explicit decision. Private question/interview editors also reject stale answer/note revisions; copy retained form text before reloading.

After refresh or backend restart, recover current-review drafts with **Restore draft**, **Keep saved version** or **Copy draft**. Copy controls also offer a text download. Recovery of excluded/older-review drafts is available only on the private review page: copy wanted text into the private interview/preparation editor, save, then explicitly approve its inclusion. Drafts are separated by Workspace, round, review, target and browser tab. They are not automatically expired.

Scout must run for database operations. Drafts depend on completed IndexedDB writes in the same browser profile/origin/port and can be lost if browser data is cleared/evicted. Browser storage failures show recovery-unavailable feedback; save or copy immediately. The quiet reconnect path waits for pending local writes before reloading a rejected circuit, or leaves copy controls available when safe recovery cannot be established. Old/offline tabs may retain previously approved content; screen-share privacy covers this view, not other desktop windows. Archived rounds reopen read-only.

M4 adds two migrations (four Workspace tables plus focused state/revision fields), bringing the total to fourteen. Stop Scout and back up the whole root before upgrade. SQLite may rebuild preparation tables; migration failure stops startup without resetting data. Whole-root backups contain canonical SQLite saves, not unsent browser drafts.

Run the full .NET suite after each package. Native JavaScript reliability tests use installed Node without npm packages or a frontend build:

~~~powershell
node --test tests/Scout.Web.Tests/BrowserDrafts/workspace-drafts.test.mjs
~~~

See the [M4 plan](docs/MILESTONE_4_PLAN.md) and [M4 delivery summary](docs/MILESTONE_4_DELIVERY.md) for architecture, actual gates, browser evidence, restore checks and the M5 handoff.
