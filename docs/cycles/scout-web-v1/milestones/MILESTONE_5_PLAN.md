# Scout: Milestone 5 implementation plan — Recordings, consent and interview review

Status: **approved for implementation by the user on 16 October 2026; implemented.** See the [delivery summary](MILESTONE_5_DELIVERY.md) for actual evidence and unperformed checks.
Date: 16 October 2026.
Basis: [Product requirements](../../../products/scout-web/PRODUCT.md), sections 3–4; [technical design](../../../products/scout-web/TECHNICAL.md), sections 2–3; the [roadmap](../ROADMAP.md); approved M3 catalog decisions; and [M4 delivery](MILESTONE_4_DELIVERY.md).

This revision moves local transcription, audio conversion, local AI analysis and review/promotion of generated results into **Milestone 6**. The original M4 delivery handoff remains historical; this plan defines the approved revised M5 scope. The user approved the revision and authorized sequential implementation and testing without approval between packages.

## 1. Outcome and boundaries

After an interview, open its private details, attach an externally recorded audio file with a consent acknowledgment, and play the saved recording there. Review saved notes or an imported transcript with simple Markdown formatting. All manual workflows work without transcription tools, AI models or network access.

Included:

- Recording-consent acknowledgments, notes-only choices and retained decision history.
- Managed MP3/WAV/M4A uploads, explicit registration of existing round audio attachments, and checked links beneath the working recordings directory.
- Native audio controls on private interview-round details, including completed rounds.
- Plain-text transcript import, private reading and corrections that preserve the original.
- Simple Markdown rendering for long text, retaining ordinary text editing and exact stored source.
- Safe streaming, file preservation, upgrade/restore checks, operating guidance and an M5 delivery summary.

Deferred to M6: whisper.cpp transcription, FFmpeg conversion, Ollama analysis independently of transcription, processing status/cancellation/retry, generated transcript timestamps, generated summaries/suggestions, source snapshots for analysis, and explicit promotion of reviewed facts/actions.

Excluded from M5: processing workers/tables/settings, provider adapters, executable/model installation, native recording, live transcription/coaching, waveform editing, diarization, remote providers, rich-text editing, Mermaid/diagrams, embeddings/chat and automated data promotion. Native capture remains a later improvement, now M7.

Recording stays an external action. Notes and interview completion never require recording, consent prompts, transcript import or any optional tool.

## 2. Reviewed starting point and decisions

M4 reports **123 .NET tests + 6 JavaScript tests**, a clean build, fourteen migrations and no pending EF model changes. These are historical delivery results, not tests executed during this planning revision. Reproduce the baseline before implementation.

The application is one ASP.NET Core/Blazor Interactive Server project, one test project and one EF Core SQLite database. Keep these boundaries. Storage stays under the user-profile directory followed by Documents/Scout; managed originals remain in assets/documents/<generated-id>/original.<extension>. Existing generic document import and verified reads both enforce 50 MiB. Audio attachments currently have no recording-consent meaning or player.

M4 supplies canonical round notes/answers, revisions, reviewed Workspace projections and browser draft recovery. Keep the private management views separate from shared Workspace data. Formatting must not broaden a projection or change autosave/conflict behavior.

| Decision | Treatment |
| --- | --- |
| Consent versus capture | Record an acknowledgment of externally obtained consent. Say “Consent received — use external recorder”; never claim capture has started. |
| Existing attachments | Require explicit registration and a file-specific acknowledgment; reuse verified bytes without another copy. Presence of audio is not consent. |
| Historical recordings | Record acknowledgment time now, with an optional explanatory note. Do not infer/backdate when consent was originally obtained. |
| Playback | Native browser audio controls, user initiated, on private round details; no conversion tool required. |
| Audio compatibility | Accept MP3/WAV/M4A for storage. Playback depends on the actual browser/codec; show a useful failure state and download option when the verified file cannot play. |
| Markdown | Store source text, render a deliberately small safe subset, keep a plain-text view and ordinary text editors. No HTML input or external resource fetching. |
| Transcript reading | Formatted view with a plain-text toggle; importing or switching the view never changes original text or bytes. |
| Future processing | M6 begins from delivered M5 behavior. Do not add unused job entities, generated-result fields, model settings or provider interfaces now. |

No architectural contradiction blocks this revision. The defaults below are concrete proposals for review; no tool/hardware decision is needed to implement M5.

## 3. User workflows

### Recording choices and registration

Private interview details offer a Recordings section with “Continue without recording” and “Consent received — use external recorder.” Each explicit choice appends a dated entry; neither is required for ordinary notes. Later notes-only choices preserve earlier acknowledgments and recordings.

Register by uploading audio, selecting an existing audio Document attached to this round, or linking a file already beneath <Scout root>/recordings/. Show the selected file and require confirmation that externally obtained consent covers that recording. Persist the acknowledgment and recording reference together. An optional private note is explanatory, not a claim of legal proof or participant verification.

Use a registration token so a repeated submission returns the same result; reject reuse of a token with a changed payload. Allow metadata archiving without deleting audio or old decisions. Archived rounds remain available for private historical reading/playback; do not allow new recording registrations on an archived round.

An optional recording-choice panel may be explicitly included in a reviewed Workspace, default off. Expose only the approved narrow choice/status label and commands. Never expose recording files, media controls, private consent notes/history or transcript content through the shared Workspace. Extend its approval fingerprint and freshness checks for this panel.

### Saved audio on interview details

After completing an interview and uploading/registering audio, its private details show the recording label and native play/pause, seek, elapsed/duration and volume controls where the browser supports them. Multiple recordings have clearly labeled players; none starts automatically. Playback can also be used privately before completion to verify an upload and never changes interview status.

Use an HTML audio element with controls and preload="none" pointed at the owned recording endpoint. Provide an explicit download action and accessible unavailable/unsupported-format feedback. Starting playback requires the running local Scout backend; the player does not imply offline browser caching.

A missing, changed or corrupt linked file produces a recoverable unavailable state. Do not substitute another file or play changed bytes under old metadata. Download is offered only when its own verified read succeeds. FFmpeg conversion and waveform/transcript synchronization are outside M5.

### Transcript import and correction

Private review accepts a UTF-8 TXT transcript independently of any recording or consent decision. Save its original file through the existing managed-document mechanism, attach it to the round, and create an immutable transcript text record. An optional source-recording association must belong to the same round.

List transcripts with their origin/date and optional recording label. Render the full saved text using the safe long-text component, with a plain-text toggle and original-file download. Do not create a transcript automatically when audio is uploaded or imply that Scout transcribed an imported file.

“Save corrected copy” creates a new transcript linked to its parent; retain the imported original and every earlier correction. Do not overwrite the original file or rewrite canonical interview notes. Edited copies remain plain source strings in SQLite. No processing-run or segment schema is introduced.

### Simple Markdown in long text

Support paragraphs/line breaks, headings, ordered/unordered lists, bold, italic, blockquotes and escaped inline/fenced code. Mermaid fences are ordinary code text. Keep typography readable in narrow panels, wrap long words and preserve a plain-text display option.

Apply the shared renderer to long-form read views: interview notes and answers, imported/corrected transcripts, preparation wording/notes, STAR fields and position descriptions. Inventory the actual read components during implementation and record covered surfaces in delivery. Short labels, names, tags, status text and operational error messages stay plain text. Generic TXT/PDF document previews keep their current behavior; this is not a general document-conversion feature.

Continue editing source Markdown in existing textareas; offer a formatted preview without replacing the editor or changing its save/draft logic. Existing text is interpreted only for display and is never bulk-converted. For transcripts, literal punctuation is always recoverable using the plain-text view and original file.

Workspace panels may render only their existing approved long-text projections using the same renderer. Formatting must not retrieve hidden source entities, add navigation to private records or reveal unrelated content. The transcript reader and player remain private.

## 4. Technical design and dependencies

Retain the current feature-oriented repository. Concrete file names may follow existing conventions:

~~~text
src/Scout.Web/
  Domain/
    InterviewRecordingDecision.cs
    InterviewRecording.cs
    InterviewTranscript.cs
  Features/
    InterviewRecordings/
      RecordingOperations.cs
      RecordingFileEndpoints.cs
    InterviewTranscripts/
      TranscriptOperations.cs
    InterviewWorkspace/            # narrow optional choice projection/commands
  Storage/
    LinkedRecordingStore.cs
  Components/
    Shared/MarkdownText.razor
    Pages/Interviews/              # private recording/review/transcript views
  Formatting/MarkdownRenderer.cs
  Data/InterviewReviewMappings.cs
  Data/Migrations/
tests/Scout.Web.Tests/
  InterviewRecordings/
  InterviewTranscripts/
  Formatting/
  Data/MilestoneFiveWorkflowTests.cs
~~~

Use the existing DbContext factory, operations, form/error conventions, Document storage and endpoint guards. No repository layer, mediator, separate API, new database, job framework or background worker is needed. Safe Markdown formatting warrants one shared component and one small rendering helper, not a general content platform.

| Dependency | Proposal |
| --- | --- |
| SDK/framework | Keep repository .NET SDK 10.0.402 / net10.0 and current roll-forward settings. |
| EF SQLite/Design, dotnet-ef, ASP.NET integration tests | Keep repository 10.0.13 pins. |
| xunit.v3 / JavaScript tests | Keep 4.0.2 and native node:test conventions. |
| Markdown | Add Markdig 1.3.2, the stable version listed on the [official package page](https://www.nuget.org/packages/Markdig/), checked 16 October 2026. Recheck availability/security at implementation start and pin the reviewed stable version. |
| Media player | Built-in browser HTML audio and ASP.NET Core file responses; no player, codec or JavaScript framework package. |

No whisper.cpp, FFmpeg, Ollama or models are installed/configured in M5. Their stable versions and Windows behavior must be assessed when planning M6, rather than carrying forward stale runtime pins.

### Data model

Add only three purpose-specific tables plus the Workspace inclusion field:

- **InterviewRecordingDecision:** round FK, choice (NotesOnly or ExternalConsentAcknowledged), acknowledgment UTC, optional private note capped at 1,000 characters, creation UTC. Append-only; absence is valid. Consent state must never be inferred from interview completion or generic attachment presence.
- **InterviewRecording:** round FK; file-specific consent-decision FK; storage kind ManagedDocument or LinkedWorkingFile; exactly one DocumentId or working-root-relative path; original display name, trusted MIME/extension, expected length and SHA-256; creation/archive UTC; unique registration token and confirmed-payload hash. Constraints/operations ensure a consent acknowledgment and reused Document association belong to this round. Referenced source metadata is immutable.
- **InterviewTranscript:** round FK; origin Imported or EditedCopy; immutable text and SHA-256; optional original TXT Document FK, optional same-round recording FK and parent-transcript FK; creation UTC and archive metadata. Imported rows require the original Document; corrections require the parent. Use a retry token for import/correction commands. No Transcribed origin, run FK or generated segment placeholders until actually implemented in M6.
- **Workspace configuration:** recording-choice visibility defaults false. Extend inclusion/fingerprint rules without exposing full new entities in shared projections.

Use restrictive FKs and explicit same-round validation, including concurrent changes and cross-application attempts. Reuse existing archive policies; no source file deletion through recording/transcript commands. Corrections preserve the original source association. Lists filter archives explicitly, with read-only historical access.

### Files, limits and transaction boundaries

Keep the fixed root and existing managed document layout. Add only recordings/ for checked working-file links. Do not create processing/artifact directories.

Audio imports and verified audio reads share a **1 GiB** maximum; non-audio documents keep 50 MiB. Transcript import is additionally limited to **5 MiB UTF-8 and 1,000,000 decoded characters**. Reject invalid encoding, oversized text and unsupported types before committing metadata. Generic text preview keeps its current 256 KiB cap. Audio extension/MIME matching is a storage policy, not proof that a browser can decode a file.

Stream uploads and hashing in bounded buffers; do not load recordings into memory, browser drafts or the SignalR message payload. Apply the same trusted file-kind limits in existing import/read operations so a larger accepted recording can subsequently be opened. Do not increase general SignalR message limits. Use the current streaming upload mechanism; set only required request/upload limits if applicable. Submit large transcript corrections through a bounded ordinary HTTP form/endpoint with antiforgery validation, rather than sending a million-character textarea value through the interactive circuit. Preserve raw text; enforce the same character limit server-side.

Linked files must resolve beneath recordings/: reject traversal, rooted/UNC paths, alternate data streams and reparse/symlink escapes. Verify the opened file's length/hash and retain that handle for reading rather than reopening an unchecked path. Mismatches require explicit new registration with acknowledgment; never silently update the expected hash. Registration/linking must not move or delete working files.

Filesystem finalization and SQLite are not atomic. Finalize managed bytes first, then commit Document, round association, decision and recording (or imported transcript) in one transaction. On an ambiguous commit, resolve the submission token before retrying. Preserve uncertain/orphaned finalized files for explicit recovery; do not clean up potentially committed originals automatically. Cancel/failure states explain whether a file was registered. Limit temporary cleanup to positively identified uncommitted task-owned bytes.

### Private streaming and native playback

Add a round-owned recording GET/HEAD media endpoint using IDs, never client-supplied filesystem paths. Validate round/recording/source associations before resolving bytes; enforce the existing private/local access model. Keep existing Workspace document endpoints and scoping intact. Neither endpoint discovery nor a recording ID substitutes for access validation.

Return the already verified seekable stream with a trusted audio MIME type and **enableRangeProcessing=true** using [ASP.NET Core Results.File](https://learn.microsoft.com/en-us/dotnet/api/microsoft.aspnetcore.http.results.file?view=aspnetcore-10.0). Its range handling and response-owned stream disposal support native seeking without implementing custom range parsing. Add private/no-store and nosniff headers, safe filename handling and recoverable failures. Do not serve recordings from wwwroot/static-file middleware or weaken existing document-preview protections.

Keep the current full hash verification guarantee on every newly opened source, then rewind and serve the requested range from that handle. This can delay opening/seeking a large file, especially linked audio; measure and report it rather than introducing an unvalidated integrity cache. Range responses must not buffer the entire recording. Test cancellation and handle disposal, 200/full GET, 206/valid range, 416/unsatisfiable range and HEAD semantics with a real HTTP test host. In-browser checks verify seeking actually works.

### Safe Markdown rendering

Use a shared Markdig pipeline with raw HTML disabled, no advanced extensions, and a restricted HTML renderer. Emit only the supported structural/emphasis/code tags, HTML-encode literal content and omit arbitrary attributes. Render Markdown links/images as readable labels/alt text, with no active destinations or resource requests in this first version. This keeps the requested formatting small; existing dedicated document/download navigation remains unchanged.

Do not treat DisableHtml alone as a complete content policy: standard Markdown also permits links and images. The restricted renderer must handle those explicitly. Markdig documents [renderer customization](https://xoofx.github.io/markdig/docs/advanced/renderers/); keep that customization confined to the shared helper. Only that helper's safe output may be passed to Blazor's HTML rendering facility; never pass stored/imported text directly as trusted markup.

Test script tags, event attributes, SVG/iframes, javascript/data/file URLs, images, raw HTML and malformed/nested input. No scripts, embedded media, external styles, remote images, autolink extensions or diagram execution. Retain source strings/hashes unchanged; later M6 source references must use original text offsets, never rendered HTML offsets.

Reuse current field limits for existing notes. Parse only on explicit preview/read or changed saved content, not on every draft keystroke. Use a finite parser nesting limit and a bounded render budget, with a plain-text fallback for excessive/pathological input; never silently truncate stored text. Verify behavior at the 1,000,000-character transcript limit. Scope CSS to the rendered text container, especially in Workspace.

## 5. Ordered implementation packages and test gates

These packages are sequential implementation/test checkpoints within M5, not separate approval requests. Run the full existing .NET and JavaScript suites after each package and resolve failures before continuing. Add meaningful behavior/security tests alongside the changed workflow; use fictional recordings/text and isolated storage.

### Package 1 — Consent and managed recording registration

Deliver private recording choices/history, managed upload and explicit existing-attachment registration, retry-safe atomic metadata, consistent audio limits, archive/read behavior and optional reviewed Workspace choice panel. Keep manual completion independent of consent.

Gate: baseline/build/suites pass; test absent choice, notes-only, later changes/history, historical acknowledgment, attachment reuse, duplicate submissions versus altered-payload tokens, cross-round rejection, archive behavior, oversize/canceled/failed uploads, uncertain-commit recovery and unchanged originals. Workspace tests prove default-off/fresh approval/scoped status with no private metadata leakage.

### Package 2 — Working links and saved-audio playback

Deliver checked working-root links and verified media streaming, plus native controls on private details for managed and linked recordings. Add codec/unavailable feedback and verified download actions.

Gate: build/suites pass; test traversal/UNC/reparse/changed/missing sources, same-round route guards, range/HEAD responses and disposal. Browser checks cover completed-round upload followed by play/pause/seek, refresh/reopen, multiple recordings, no autoplay, and an unsupported or corrupt media file. Verify recording bytes/hashes remain unchanged and controls never enter the shared Workspace.

### Package 3 — Manual transcript review

Deliver bounded UTF-8 TXT import, original-file preservation, transcript list/reader, optional same-round recording association, corrected copies and plain-text reading without tools. Formatting follows in package 4.

Gate: build/suites pass; test notes-only import, invalid encoding, byte/character limits, repeat import/correction commands, immutable parents/original files, association validation and archive/history reading. Browser checks cover import/read/correct/reopen without any recording or provider configured.

### Package 4 — Markdown long-text display

Deliver the safe shared renderer, formatted/plain views and preview at the specified long-text surfaces. Preserve existing editors, stored source, browser drafts and approved Workspace data boundaries.

Gate: build/suites pass; renderer tests cover requested headings/lists/bold/italic and literal code, hostile markup/URLs/images, nesting/size fallback and exact source preservation. Browser checks cover realistic long notes/transcripts, preview/edit round-trips, plain-text mode, narrow Workspace panels, autosave/draft recovery and no external resource requests. Re-run scoped Workspace projection/HTTP tests; formatting must not make hidden data available.

### Package 5 — Integrated upgrade, preservation and delivery

Deliver migrations, populated M4 upgrade/restore checks, operational guidance and M5 delivery summary. Fix integration issues before considering M5 complete.

Gate: full build/.NET/JavaScript suites pass, no pending EF model changes; reproduce acceptance below using an independent stopped-root restore and real browser. Record actual counts and unperformed checks honestly.

Commands follow M4's established runner:

~~~powershell
dotnet build Scout.slnx --no-restore -m:1 -p:UseSharedCompilation=false
dotnet tests/Scout.Web.Tests/bin/Debug/net10.0/Scout.Web.Tests.dll
node --test tests/Scout.Web.Tests/BrowserDrafts/workspace-drafts.test.mjs
dotnet ef migrations has-pending-model-changes --project src/Scout.Web --no-build
~~~

Restore dependencies when adding Markdig; subsequent --no-restore checks assume restore succeeded. Reuse temporary SQLite/files, existing HtmlRenderer and HTTP integration conventions. No new general browser framework or processing binary is required for these gates.

## 6. Upgrade, acceptance and delivery output

Add migrations after 20261015230317_WorkspaceCapture; do not rewrite fourteen applied migrations. Existing rows remain unchanged except purposeful defaults. No generic attachment becomes a registered recording, no consent is inferred/seeded, no transcript is generated and no reviewed profile automatically includes new data. Check every mapped business table's seed guard.

Upgrade a populated M4 fixture containing both M3 catalogs, detached/ad hoc questions, revisions, archived selections, Workspace snapshots, multiple applications/rounds and exact submitted files. Verify SQLite integrity/FKs/model consistency and unchanged existing source bytes. Stop Scout before copying SQLite and the entire root, including linked recordings; test the copied root independently without depending on the original file paths. A backup that omits recordings/ is incomplete.

Acceptance:

1. Complete a notes-only interview with no recording choice, tools or providers.
2. Acknowledge consent and upload/reuse/link fictional recordings. Reopen a completed round and play/pause/seek its saved audio using native controls.
3. Demonstrate unsupported-codec feedback, unavailable linked audio, safe downloads and retained original bytes.
4. Import a transcript without recording; create a corrected copy and reopen both original/correction separately.
5. Render headings, lists and bold/italic in long notes/transcripts; preview/edit source and switch to plain text without content loss. Hostile Markdown cannot execute or fetch resources.
6. Check two-application privacy, disabled Workspace fields, stale reviews, M4 draft/conflict recovery and exact submitted-CV evidence. No player, transcript or private history leaks into a shared projection.
7. Restore a stopped-root copy and verify recordings, consent history, transcripts, corrected-copy relationships and approved Workspace behavior. Check operation without external services; report any actual network-blocking exercise separately.

Produce **docs/milestones/MILESTONE_5_DELIVERY.md** following M4's format: baseline/package/final test counts and commands; migrations/dependency versions; browser/negative/security cases; source/consent/formatting/privacy guarantees; backup/restore results; actual covered Markdown surfaces; codec and large-file verification observations; deviations/unperformed checks; and a concrete M6 starting point.

## 7. Decisions and handoff for review

Approved defaults: 1 GiB audio cap; working links confined to the fixed root's recordings directory; native private playback with no autoplay; three new tables; safe basic Markdown with inert link/image labels and a plain-text view; immutable imported transcripts and corrected copies.

M6 owns local transcription with whisper.cpp/FFmpeg and independent local text analysis with Ollama, including generated-result review and explicit structured promotion. Its detailed implementation plan must first read the actual M5 delivery summary and inspect delivered files/models. Reassess tool versions, Windows binaries/models/hardware, source snapshot/provenance design, long-input/model limits, processing lifecycle and cancellation/retry there. M5 introduces none of that infrastructure in advance. The roadmap records M6's scope; a detailed M6 plan is intentionally not created here.
