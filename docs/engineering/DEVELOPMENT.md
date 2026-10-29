# Scout development guide

Run commands from the repository root. See the [quick start](../../README.md#quick-start) for launching Scout, [operations](../products/scout-web/OPERATIONS.md) for data protection and restore, and the [documentation overview](../README.md) for specifications and milestone plans.

## Stack and repository

Scout.Web uses ASP.NET Core, Blazor Interactive Server, EF Core and SQLite, with tests in `tests/Scout.Web.Tests`. The independent Windows Forms capture experiment lives in `src/Scout.Scribe`, with focused tests in `tests/Scout.Scribe.Tests`. Scribe uses NAudio 3.3.0/Windows Media Foundation, .NET 10 and Windows x64. Prefer concrete Web feature operations and short-lived EF contexts. There is no separate JavaScript frontend build.

The application targets .NET 10. [global.json](../../global.json) selects SDK **10.0.402**, allows later patches in that feature band and selects Microsoft Testing Platform. EF Core and the local `dotnet-ef` tool are pinned to **10.0.13**; test dependencies are **xunit.v3 4.0.2** and **Microsoft.AspNetCore.Mvc.Testing 10.0.13**. ASP.NET Core runtime selection follows the installed compatible framework; the application does not separately pin a runtime patch.

The independent [Scribe.js Electron experiment](../../src/Scribe.js/README.md) lives under `src/Scribe.js`, outside the .NET solution. It has its own npm lockfile, tray/windows, direct Windows audio bindings and portable packaging. Its [verification report](../../src/Scribe.js/VERIFICATION.md) records the JavaScript/native/desktop checks and remaining hardware acceptance. Run its development commands from `src/Scribe.js`.

## Build and verification

First restore dependencies with `dotnet tool restore` and `dotnet restore Scout.slnx`. Restore requires NuGet access.

```powershell
dotnet build Scout.slnx --no-restore
dotnet test --solution Scout.slnx --no-build
dotnet ef migrations has-pending-model-changes --project src/Scout.Web --startup-project src/Scout.Web --no-build
```

If `dotnet test` reports a runner handshake failure/zero discovered tests in the Codex execution environment, run the built xUnit runner directly: `dotnet tests/Scout.Web.Tests/bin/Debug/net10.0/Scout.Web.Tests.dll`. This executes the full same suite. Temporary file-rename checks require filesystem access; sandbox-denied runs are not passing gates. M3's delivery record states the command actually used and its results.

Scribe's direct runner is `dotnet tests/Scout.Scribe.Tests/bin/Debug/net10.0-windows/win-x64/Scout.Scribe.Tests.dll`. Its codec/crash tests use synthetic audio only and never open capture devices. Run the suite with the necessary filesystem/IPC permissions if the sandbox blocks temporary-file renames or test-runner pipes. See the [capture experiment guide](../cycles/scout-scribe-v1/milestones/MILESTONE_1_CAPTURE_EXPERIMENT.md) for launching the UI, synthetic crash commands and manual microphone/headset checks. The UI starts idle; recording is always explicit. Web can still build/run independently with its project path.

Run the full .NET suite after each package. Native JavaScript reliability tests use installed Node without npm packages or a frontend build:

~~~powershell
node --test tests/Scout.Web.Tests/BrowserDrafts/workspace-drafts.test.mjs
node --test tests/Scout.Web.Tests/BrowserUi/ui-dialog.test.mjs
~~~

Tests use real SQLite migrations and temporary files/streams; test hosts replace storage before startup. They cover ownership, archives, atomic history, DST, imports/failures/limits, path/junction rejection, HTTP safety, immutable submissions/retries, seed guards, M1 upgrade, and whole-root restore. Personal files are never imported/reset by tests.

Workspace checks also cover review scope and privacy, live saving, stale revisions, browser drafts and recovery. Test counts and commands actually used belong in delivery records rather than this guide.

### Verification limits

See the [M2 delivery record](../cycles/scout-web-v1/milestones/MILESTONE_2_DELIVERY.md) for gates, browser checks, backup evidence, and limits. Browser smoke checks are not a committed end-to-end suite. No network adapter was disabled; --no-restore does not establish disconnected-network operation. Runtime code introduces no outbound HTTP client or remote assets.

See the [M4 delivery summary](../cycles/scout-web-v1/milestones/MILESTONE_4_DELIVERY.md) for Workspace browser, privacy, recovery, upgrade and restore evidence. Native JavaScript tests use test doubles for some storage failures; they do not establish behavior under arbitrary power loss or every real browser quota condition.

## Shared presentation

Web M6 uses `wwwroot/ux.css` after the existing styles, local SVG icons and shared ActionLink/PageActions/UiDialog/DraftForm components. UiDialog wraps the native dialog element; `wwwroot/js/ui-dialog.js` manages dirty Escape handling, busy dismissal and focus return. Reuse focused form components for dialogs and direct routes. Keep intermediate review distinct from a confirmed save. Do not add private navigation to WorkspaceLayout; only its approved projection belongs there. See [ADR 0003](decisions/0003-web-presentation-patterns.md) and [M6 delivery](../cycles/scout-web-v1/milestones/MILESTONE_6_DELIVERY.md).

## Database migrations

Generate a migration with `dotnet ef migrations add DescribeTheChange --project src/Scout.Web --startup-project src/Scout.Web --output-dir Data/Migrations`; review migration/designer/snapshot and run checks before applying it to personal data.

Back up the [entire data root](../products/scout-web/OPERATIONS.md#backup-upgrade-and-restore) before upgrading a personal installation. Startup applies committed migrations; failure stops startup without resetting data. Never rewrite applied migrations or substitute `EnsureCreated`. Keep migration counts and per-milestone upgrade evidence in delivery records.

## File storage and import mechanics

Storage resolves to `<user home>\Documents\Scout`, independent of the working directory and shared by Development and ordinary runs. It is outside static assets. Tests replace storage with temporary roots; there is no storage settings screen.

Imports accept **one file up to 1 GiB for MP3/WAV/M4A audio**, or **50 MiB for PDF, TXT, MD, EML and MSG**. Imports and verified reads use the same trusted file-kind limit. Transcript imports additionally enforce 5 MiB UTF-8 and 1,000,000 decoded characters; corrections use a bounded, antiforgery-protected multipart HTTP form. Streaming counts actual bytes and calculates SHA-256. PDFs require a PDF header. Original names are display metadata, never physical paths. Managed paths reject traversal, alternate streams, unexpected names, and reparse points/junctions.

Controlled document-ID routes verify path, size and hash before serving files. Responses use server MIME, safe disposition, `nosniff`, private/no-store caching and a restrictive preview policy. Guessed paths cannot download unregistered files. See the [user guide](../products/scout-web/USER_GUIDE.md#files-and-submission-evidence) for preview behavior and current limits.

Imports create a new `.partial`, stream/flush it, rename without overwrite, then register metadata/links/CV in one SQLite commit. Database/filesystem writes cannot share a transaction. Failure/interruption retains partial or unregistered files and reports their relative location where possible. Uncertain commits are checked by that exact generated path before offering retry. No automatic cleanup/reconciliation exists; guessed paths cannot download unregistered files. Back them up too. Restore original bytes from backup after external loss/corruption.

## Optional samples

Samples are **disabled by default**. They insert only when explicitly enabled in **Development** and **every business table is empty**, including standalone documents. Existing business data prevents reseeding. Repeating seeding never changes edited rows; design-time EF commands never seed.

```powershell
dotnet run --project src/Scout.Web --no-restore -- --Development:SeedSampleData=true
```

The launch profile sets Development. Fictional samples include .example companies/positions, an application, contacts, events, actions, interviews (including notes-only completion), and an embedded CV PDF/submission. Files use managed import rules. Failed seeding rolls back rows and retains any created file. No sample reset/cleanup command exists; isolated tests avoid touching personal data.

Samples also include employer questions, tags, partial/full fictional STAR experiences, selected round preparation, and an explicitly fictional answer. Standalone library content prevents seeding just as existing companies/documents do.

## Design and delivery references

Read the [technical guidance and constraints](../products/scout-web/TECHNICAL.md), [domain model](../products/scout-web/DOMAIN_MODEL.md), relevant [product requirements](../products/scout-web/PRODUCT.md), and [roadmap](../ROADMAP.md) before planning a slice. Specifications include future capabilities; inspect the actual implementation and preceding delivery summary before using them as evidence of current behavior.

Record significant architecture, product-boundary or durable UX choices in the [Architecture Decision Records collection](decisions/README.md) using its numbered template. Link the relevant milestone; preserve accepted historical rationale and record material changes in a successor ADR. Routine implementation details do not need an ADR. [ADR 0002](decisions/0002-web-v1-desktop-ux.md) records the earlier Web-first priority and 4K-first UX direction; Web M6 is delivered and Scribe M1 implementation is now underway.

The [cycle index](../README.md#milestone-plans-and-delivery-records) links to plans and implementation evidence. Web M5 delivers recording/consent, private playback, immutable manual transcripts and safe basic Markdown using Markdig 1.3.2. Its [delivery summary](../cycles/scout-web-v1/milestones/MILESTONE_5_DELIVERY.md) records actual gates and verification limits. [Web v1 M6](../cycles/scout-web-v1/milestones/MILESTONE_6_DELIVERY.md) delivers the desktop UI overhaul. The independent [Scribe v1 cycle](../cycles/scout-scribe-v1/README.md) has its own M1 recording, M2 local transcription and M3 fact extraction/review. Read the [product-cycle decision](decisions/0001-product-development-cycles.md) for numbering and historical preservation rules.
