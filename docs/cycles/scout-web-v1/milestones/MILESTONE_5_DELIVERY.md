# Scout: Milestone 5 delivery summary

Date: 16 October 2026. Scope: the user-approved revised [M5 plan](MILESTONE_5_PLAN.md), following [M4 delivery](MILESTONE_4_DELIVERY.md). All five implementation packages are delivered. Browser upload automation and the additional limits below remain unverified; they are not reported as passing acceptance checks. No transcription, conversion, model/provider configuration, processing workers or generated-analysis infrastructure was introduced.

## Package gates

The M4 baseline was reproduced: **123 .NET tests and 6 JavaScript tests passed**, with zero build warnings/errors. The initial sandboxed .NET run failed 15 tests because temporary-file renames were denied. The complete suite passed with required filesystem access; the denied run is not a passing gate.

| Checkpoint | Delivered | Passing full-suite gate before continuing |
| --- | --- | --- |
| 1. Consent and managed registration | Append-only recording decisions; atomic managed uploads and attachment reuse; retry identity; archives; audio limits; optional reviewed Workspace choice panel. | **125 .NET + 6 JavaScript tests**. |
| 2. Working links and playback | Confined checked links; verified GET/HEAD/range/download routes; native controls, codec feedback and unavailable state on private details. | **135 .NET + 6 JavaScript tests**, clean build. |
| 3. Manual transcripts | Bounded strict UTF-8 import; retained TXT; immutable corrected copies and associations; private reader; ordinary HTTP correction form. | **138 .NET + 6 JavaScript tests**, clean build. |
| 4. Markdown | Restricted shared renderer, plain/formatted views and explicit previews at the surfaces below; preserved editing/draft behavior. | **147 .NET + 6 JavaScript tests**, clean build. |
| 5. Integrated delivery | Populated M4 upgrade, independent whole-root restore, failure injection, large-file/HTTP limits, browser checks, operating guidance and this summary. | **155 .NET + 6 JavaScript tests**, zero build warnings/errors; no pending EF model changes. |

Browser checks were performed during final integration rather than at each package checkpoint. Package 1 initially had test cancellation-token warnings, removed in package 2. The historical M4 upgrade test's hardcoded fourteen-migration assertion was changed to compare applied migrations with the current migration set; its preservation assertions remain.

Commands actually used from the repository root:

~~~powershell
dotnet restore Scout.slnx
dotnet build Scout.slnx --no-restore -m:1 -p:UseSharedCompilation=false
dotnet tests/Scout.Web.Tests/bin/Debug/net10.0/Scout.Web.Tests.dll
node --test tests/Scout.Web.Tests/BrowserDrafts/workspace-drafts.test.mjs
dotnet ef migrations has-pending-model-changes --project src/Scout.Web --no-build
dotnet list Scout.slnx package --vulnerable --include-transitive --no-restore
~~~

The direct xUnit runner executes the full suite. Test/formatter runs needing temporary-file or named-pipe access used the available permission mechanism. Initial restricted NuGet audit attempts failed; the final audit succeeded with network access and reported **no vulnerable packages** for either project using the current sources. A scratch browser harness retained an earlier NU1900 restore warning; that warning is not a successful audit. No framework or test dependency pins changed.

## Delivered behavior and preservation

The application remains one ASP.NET Core/Blazor Interactive Server project, one test project and one SQLite database. Concrete operations reuse the existing context factory, managed-document storage and local HTTP access guards. The fixed root gains only `recordings/` for working files; managed originals retain their existing generated paths outside static assets. No general SignalR limit was increased.

Consent choices are optional and dated when entered. Recording registration requires a separate acknowledgment covering the selected file and saves that decision together with the recording, Document and round association. Generic audio attachments never become recordings automatically. Existing attachments reuse verified bytes. Later notes-only choices and metadata archives preserve prior decisions and files. Archived rounds remain privately readable; new decisions/registrations/imports/corrections require an active round.

Registration and transcript commands use unique submission tokens and payload hashes. Unchanged retries resolve the existing result; changed payloads are rejected. Finalized files precede the SQLite commit and are preserved on uncertainty. Tests inject both a rejected metadata insert and a successful commit followed by a lost acknowledgment. The former leaves finalized bytes without partial database facts; the latter resolves the committed token without duplicate facts. There is no automatic orphan cleanup. Browser form state/tokens do not survive closing the form; check the saved list before starting a new attempt after uncertainty.

Audio imports and verified reads share a **1 GiB** limit; other documents retain **50 MiB**. Buffers are bounded. Working paths reject rooted/UNC paths, traversal, alternate streams, invalid path components and reparse points/junctions. Windows verification checks the final opened handle against the checked path. Reads verify full size/SHA-256, rewind and serve that same handle; changed bytes are never substituted. MP3/WAV/M4A acceptance is a storage policy, not codec validation.

Private media routes validate the round, recording, consent and managed-source association. Native controls use `preload="none"`, no autoplay and response-owned seekable streams with framework range handling. Responses carry trusted MIME, `nosniff` and private/no-store headers. HTTP tests cover 200/full, 206/range, 416, HEAD, safe download, wrong-round rejection and changed/missing sources. Cancellation verification releases its handle; completed responses release handles before file replacement. Missing sources show an unavailable state without offering a download; corrupt but hash-matching codec data can still be downloaded safely.

Transcripts import **strict UTF-8 TXT**, capped at **5 MiB and 1,000,000 decoded characters**, without recording or consent prerequisites. Original bytes are preserved as a managed Document. Corrections create children retaining the parent/original-file/optional recording associations; they do not rewrite notes or source files. Composite same-round FKs protect recording, decision and transcript-parent ownership. A bounded multipart HTTP form validates antiforgery and accepts the million-character correction without sending it through the interactive circuit. HTTP tests verify valid maximum-size correction, duplicate identity, altered-payload rejection and missing antiforgery rejection. Archive filtering is explicit, with private history retained.

## Markdown and Workspace boundaries

Added **Markdig 1.3.2**, rechecked against its [official package listing](https://www.nuget.org/packages/Markdig/). A basic pipeline disables raw HTML and limits nesting. A dedicated AST-to-HTML helper emits only supported structural/emphasis/code tags, encodes literal content and emits no input-derived attributes. Links/images render inert child labels; destinations are discarded. Mermaid is ordinary escaped fenced code. No renderer outputs active links, images, scripts, SVG, iframes, external styles or diagrams. Only the shared helper's output is rendered as trusted markup.

Input length/complexity, nesting, node count, output size and elapsed render work are bounded, with a complete plain-text fallback. The elapsed check occurs after parsing and during traversal; it is not a hard preemptive parser timeout. Tests exercise the million-character transcript limit and hostile/malformed/nested input. Stored source and hashes remain unchanged. CSS stays within the text container and wraps long words/code.

Actual covered surfaces:

- Private interview preparation/interview notes in details; prepared question wording, intent, follow-ups, preparation notes and answers in preparation; selected STAR stories/rehearsal notes through the shared story component.
- Imported and corrected transcript readers, with literal plain-text toggle and original TXT download.
- Question-library intent/follow-ups/private notes; STAR memory cues and full STAR/notes read fields.
- Preserved position descriptions in details and the editor's immutable-description view.
- Private Workspace review of long-text values and existing approved Workspace descriptions, question wording, intent/follow-ups/preparation, STAR fields and prior-answer wording/answers.
- Explicit source previews in interview, prepared-question, rehearsal-note, question-library, STAR and position textarea editors. Workspace answers/notes use **Preview current source**, reading the existing native textarea on request and preserving its save queue and recovery behavior.

Short names, checkbox labels, tags, statuses and errors stay plain text. Generic document TXT/MD/PDF previews retain their existing behavior. Transcript correction text stays a native HTTP textarea; preview it by saving the child and using its formatted/plain reader. No rich editor or transcript autosave/draft system was added.

The optional Workspace choice panel defaults off and requires private approval. Its source-decision revision participates in review freshness; commands validate the approved configuration revision and enabled scope. The projection exposes only a narrow status label. No recording file/player, transcript, private consent note or decision history enters Workspace data. Existing two-application privacy, query, HTTP, exact submitted-CV and optimistic-conflict/draft tests all pass. Formatting operates on already approved projections, without fetching hidden entities.

## Browser verification

Used an isolated fictional root and a scratch WebApplicationFactory/Kestrel harness on loopback port 5195. The normal Documents/Scout root was neither upgraded nor populated. Verified in the in-app browser:

- Completed-round linked and managed WAV playback, native pause and keyboard seek, multiple labeled players, no autoplay, and refresh/reopen behavior. Playback did not change completion status.
- Corrupt MP3 bytes display native failure and useful codec/unavailable guidance while retaining a verified download option.
- Existing-attachment registration first rejects absent file-specific acknowledgment, then succeeds with it and reuses the attachment. The form resets its selected source afterward.
- Temporarily unavailable working WAV shows recoverable feedback and omits its player/download, while other managed players remain available. The fictional file was restored before backup.
- Imported transcript reading, formatted/plain switching, ordinary HTTP corrected-copy submission, reopening the correction and following its parent back to the unchanged imported source. The hostile sample contains zero active links/images/scripts/iframes in its rendered text container.
- Default-off Workspace choice inclusion, explicit approval, narrow notes-only command/status, absence of private media/transcript links, source preview, autosave confirmation and exact source after reload.
- A 650-pixel viewport test showed no horizontal overflow while displaying the Workspace source editor and Markdown preview. The viewport override was reset.

Screenshots: [private recording controls](../../../../.local/screenshots/m5-recordings.png), [transcript reader](../../../../.local/screenshots/m5-transcript.png), [scoped Workspace](../../../../.local/screenshots/m5-workspace.png). These are local fictional verification artifacts, not a committed browser automation framework.

The browser file chooser failed while supplying the fictional WAV, eventually reporting that its tab no longer belonged to the browser session. Therefore **end-to-end browser audio upload and transcript file selection/import are unverified**. Managed streaming/imports are tested through operations; playback of managed uploaded bytes and browser registration of an existing attachment were verified separately. M4's conflict/draft recovery cases were rerun as automated regressions; its complete simultaneous-tab/offline manual exercise was not repeated. No browser resource-request trace, exhaustive codec matrix, mid-transfer HTTP cancellation, physical multi-monitor call, real power-loss test or network-blocking exercise was performed. No disconnected-network claim is made.

## Upgrade, backup and independent restore

Two migrations follow the fourteen existing migrations, without rewriting them:

- `20261016125634_InterviewRecordings`: decision and recording tables, restrictive associations, source-kind constraint, unique registration token and default-off Workspace inclusion field.
- `20261016130217_InterviewTranscripts`: transcript table, immutable-origin/parent shape, retry identity and composite same-round source/parent keys. SQLite rebuilds Recordings to add its composite principal key; back up the whole root before upgrading.

Final schema: **sixteen migrations**, no pending EF model changes. The seed guard enumerates every mapped business table and therefore includes all three new tables. Upgrade does not infer consent, register generic attachments, generate transcripts or enable new Workspace fields.

The automated populated-M4 fixture includes both catalogs, detached/ad hoc questions, revisions, archived selections, approved question/STAR snapshots, multiple applications/rounds and exact submission/audio files. It upgrades from `20261015230317_WorkspaceCapture`, verifies defaults and original text, then copies the stopped root. Original asset/recording directories are made unavailable before independently reopening and verifying every restored file, transcript relationship, snapshot, submitted evidence and SQLite integrity/FKs.

The separate browser root was stopped and all **nine files** were copied and compared by SHA-256. The original root was moved away from its old path before launching the independent copy:

- Stopped source: `.local/m5-browser-root-stopped/`.
- Independent copy: `.local/m5-restored-root/`.

Browser verification on the copy reopened linked playback, corrected transcripts and parent links, retained consent history, approved Workspace choice/status and saved Markdown notes. The original path remained unavailable. A stopped-copy checker then verified **integrity_check=ok**, clean FKs, **16 migrations, 4 verified managed Documents, 4 recordings, 5 decisions, 2 transcripts including 1 correction, and 1 Workspace configuration**. All stored transcript hashes and linked-file hashes matched. These are workspace verification copies, not a durable user backup policy.

## Large-file observations and M6 starting point

The committed suite imports and verifies audio **50 MiB + 64 KiB**, above the old limit, through bounded streams. An oversized working file is rejected before hashing. In a separate local **64 MiB** sample, linked inspection took **76 ms**, linked verified open **43 ms**, managed import **131 ms** and managed verified open **46 ms**. These single local observations include warm filesystem effects; they do not establish 1 GiB performance. Every new media open fully hashes its source; seeking can therefore repeat verification and delay playback. Full 1 GiB upload/browser behavior was not exercised.

M6 should start by inspecting the delivered recording/transcript operations, composite ownership keys, preserved original bytes/source strings, retry contracts, guarded streaming and Markdown helper. Retain the optional/manual workflows and private Workspace boundary. Plan current whisper.cpp/FFmpeg Windows binaries/models and independent Ollama text analysis only then. Settle source snapshots/provenance, long-input limits, generated-result review/promotion, processing lifecycle and cancellation/retry from this actual schema. No speculative provider, job, generated-transcript segment or analysis-result schema exists in M5.
