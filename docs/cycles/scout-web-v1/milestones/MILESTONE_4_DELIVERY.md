# Scout: Milestone 4 delivery summary

Date: 16 October 2026. Scope: the approved revised [M4 plan](MILESTONE_4_PLAN.md), following [M3 delivery](MILESTONE_3_DELIVERY.md). All four packages are implemented. Audio and consent remain deferred to M5; no M5 implementation or planning document was added.

## Package gates

The recorded M3 baseline was reproduced before implementation: **113 tests passed**, zero build warnings/errors. Initial sandbox-denied file moves failed thirteen existing document tests; the same suite passed with the required temporary-file access. Denied runs were not counted as passing gates.

| Package | Delivered | Passing gate before continuing |
| --- | --- | --- |
| 1. Reviewed scoped Workspace | Private field-by-field approval; reviewed copies; separate layout/title; correct submitted-CV selection and guarded file routes; isolated errors. | **117 .NET tests**; clean build; browser approval/scoped-content/no-CV checks. |
| 2. Live round behavior | Asked/follow-up flags, canonical answers/notes, ad hoc origin and retry token, attributed prior-answer copies, protected private writers and invalidated reviews after selection removal. | **119 .NET tests**; clean build; browser asked/follow-up save and ad hoc creation. |
| 3. Reliable capture | Native input listeners and IndexedDB; per-target save queues; honest status, autosave/Save now/Ctrl+S; conflict choices; reload/restart recovery; private recovery of older-review drafts. | **119 .NET tests + 4 JavaScript queue tests**; clean build; real browser autosave, conflict, explicit recovery and backend restart. |
| 4. Integrated delivery | Scoped search/state filters, panel/preset persistence, review freshness check, reconnect integration, privacy/query/HTTP regression, populated M3 upgrade and stopped-root restore, operating documentation. | **123 .NET tests + 6 JavaScript tests**; zero build warnings/errors; no pending EF model changes; final browser and backup checks below. |

Commands actually used:

~~~powershell
dotnet build Scout.slnx --no-restore -m:1 -p:UseSharedCompilation=false
dotnet tests/Scout.Web.Tests/bin/Debug/net10.0/Scout.Web.Tests.dll
node --test tests/Scout.Web.Tests/BrowserDrafts/workspace-drafts.test.mjs
dotnet ef migrations has-pending-model-changes --project src/Scout.Web --no-build
~~~

Rider MCP was unavailable; the repository's CLI fallback was used. Build, test, server and formatter runs needing local process/file access used the available permission mechanism. The direct xUnit runner executes the actual suite, rather than treating an unavailable runner handshake or zero tests as success. JavaScript tests use Node 24.19.0 and native node:test, without npm dependencies or a frontend build.

## Delivered behavior and architecture

One Interactive Server application, one test project, concrete Workspace operations and short-lived EF contexts. Existing dependency pins remain unchanged: SDK 10.0.402, net10.0, EF/ASP.NET test tooling 10.0.13 and xunit.v3 4.0.2. No application dependency was added.

Private setup previews actual content and starts with nothing selected. Approval atomically replaces the profile; it checks both the expected configuration revision and a fingerprint of the previewed sources. A source edit between preview and approval cannot silently introduce unreviewed text. The launch link is user-activated, uses a separate tab target and noopener/noreferrer.

The Workspace receives explicit projections of approved snapshots and enabled live fields. Source-library entities/private notes are not loaded and hidden afterward. SQL-command tests verify source tables and excluded interview notes are not selected; canonical answer reads filter to enabled inclusions in SQL. Ownership/permission checks use narrow metadata. Malformed Workspace routes and CV failures stay neutral, without management navigation or filesystem names. A dedicated layout/error boundary preserves the neutral title.

Enabled live answers/notes keep the existing canonical fields. Revision checks cover Workspace and existing private editors. Conflicting writes retain the draft and require review; identical retries acknowledge the existing save without incrementing the revision again. Ad hoc creation is atomic and uses a unique payload-checked token, including after recovery. Asked state is independent of text. Source-deleted questions remain distinct from ad hoc questions. Earlier answers are attributed read-only copies from another round of the same application.

Native listeners persist edits before relying on SignalR. Queues serialize each question's flags/answer and each notes target; old acknowledgments cannot clear newer input. Initial editor values remain stable across parent rerenders, preserving native dirty DOM values/caret. Browser/tab identities prevent unsent drafts from overwriting one another. Current-review recovery queries only permitted targets. Older-review drafts are shown only in private recovery, where copying/saving and explicit reapproval are required for shared visibility.

Configuration changes are checked on focus and every fifteen seconds, as well as at reads/saves. Changed profiles stop editing and clear shared content; archived rounds reopen read-only. Previously approved offline tabs cannot be remotely erased. Local draft storage failures retain visible text and provide copy/download controls, while revoked editing permission stops further input without disabling copying. The reconnect reload waits for local draft transactions; if recovery is unavailable it keeps copy controls usable instead.

## Database changes and preservation

Two additive migrations, bringing the total from twelve to fourteen:

1. `20261015225800_InterviewWorkspace`: configuration, question/experience inclusions, attributed prior answers; same-round composite ownership constraints and submitted-file reference.
2. `20261015230317_WorkspaceCapture`: asked/follow-up/origin/revision/creation-token fields and note revision; checked origins and filtered unique creation identity.

Four new tables; no recording/consent tables, audio storage path, larger import limit, service worker, provider abstraction or background process. Existing document import/read limits remain 50 MiB. Applied migrations were not rewritten. Existing question origins default Library, flags false and revisions zero. SQLite table-rebuild migration warnings were observed; upgrade/integrity/foreign-key tests passed. Whole-root backup remains necessary before migration.

Opt-in Development seeding already checks every mapped business table, so new standalone Workspace records prevent reseeding. No additional seeded visibility approval was introduced: existing fictional preparation can be reviewed through the same user flow. Samples were not enabled against the normal root.

The prior M2-to-M3 upgrade fixture now inserts historical interview rows using the historical schema, before current model columns exist. Its preservation assertions remain. New tests upgrade a populated M3 fixture with two rounds, answers, STAR preparation and exact submitted bytes, exercise M4, then reopen an independently copied stopped root and verify snapshots, canonical values, IDs, hashes and SQLite integrity.

## Browser verification

Used clearly labeled fictional records already present from M1–M3 plus two new M4 ad hoc questions, without inventing personal interview history. Verified in the in-app browser:

- Private approval defaults, current text previews and the explicit approved-content link; approved text and neutral title in a separate browser tab.
- No global navigation, unrelated content, excluded library/rehearsal notes or unapproved STAR fields in the Workspace. Automated rendering also checks canaries in a separate application and rejects its prior-answer selection.
- Asked/follow-up persistence; normal answer autosave; distinct local-draft and database-saved feedback; Ctrl+S; ad hoc creation without a library definition.
- Two simultaneous tabs: the stale tab keeps its text, shows the current saved version and resolves only through an explicit choice.
- An unsent ad hoc question survives a stopped/restarted backend and is offered for explicit restoration in a fresh circuit.
- The final reconnect path displays the quiet offline message with **no countdown**, automatically reloads after circuit rejection, offers the retained draft and registers the restored question once.
- Private recovery shows an older-review draft; the new shared review does not offer that draft. Older-review recovery is deliberately a private copy/save/reapprove workflow.
- Tag search, unasked/all filters, expanding only approved STAR fields, remembered panel collapse and Notes wide preset. A 1920×1080 CSS viewport check showed no horizontal overflow and a double-width notes panel; the temporary viewport override was reset. This is browser layout verification, not a physical multi-monitor video-call session.
- Exact submitted-CV route and successful browser download to `submitted-cv.pdf`; missing-CV empty state was checked earlier. Native PDF preview remains blank/unconfirmed in this browser. HTTP tests verify exact bytes, disposition/cache/security headers, stale/wrong/missing revision rejection and corrupted bytes.
- A malformed round/unknown Workspace route displays the isolated unavailable message and no private navigation.

Final screenshot: [Workspace verification](../../../../.local/screenshots/m4-workspace.jpg). Smoke checks are not a committed browser automation framework. Storage denial/quota failure and in-flight acknowledgment timing are exercised with native-module test doubles; browser quota exhaustion, arbitrary power loss, and forced component failure were not induced in the live browser. No external-network adapter/rule was disabled, so network-disabled acceptance is not claimed.

## Backup and restore evidence

The first process inventory did not expose a pre-existing dotnet-hosted Scout process. The DLL lock identified it; after verifying its Scout command line it was stopped. The earlier live copy was superseded by this actual stopped pre-M4 backup:

`.local/backups/m4-stopped-before-20261016-005756/Scout`

Stopped final whole-root backup:

`.local/backups/m4-final-20261016-012246/Scout`

All **six files**, including SQLite sidecars, matched the final stopped source by hash. A separate read-only checker opened workspace copies and established:

- **38 pre-M4 rows across 25 tables** preserved using their original columns. Only question 1's UpdatedAtUtc was excluded, because the fictional asked/follow-up save intentionally changes that timestamp; its original wording/answer/private notes remain unchanged.
- Expected question 1 state: asked/follow-up true, content revision 1. Two new fictional M4 questions account for the increase from three to five prepared questions.
- **Three document hashes/sizes verified**, fourteen migrations, one approved Workspace.
- `integrity_check=ok`; `foreign_key_check` returned no rows.

The scratch checker used existing project dependencies. Its restore emitted NU1900 because vulnerability-feed metadata was unreachable; the checker completed successfully. The application solution's final build is clean. No copy was restored over personal storage. These local workspace copies are verification artifacts, not a durable backup policy.

## Operating limits and M5 starting point

Read the [user guide](../../../products/scout-web/USER_GUIDE.md#interview-workspace) for review/launch, saving, conflicts and private draft recovery, and [operations](../../../products/scout-web/OPERATIONS.md#backup-upgrade-and-restore) for backup instructions. Scout must run for server operations. Browser drafts require completed IndexedDB writes in the same browser profile/origin; whole-root backups preserve canonical SQLite saves, not uncommitted browser drafts. Clearing/evicting browser data can remove drafts. Storage failures require immediate saving/copying. Private management forms remain manual-save forms; their text does not gain browser recovery through this milestone.

Close old Workspace tabs before changing visibility. Approved copies do not automatically refresh after source edits; enabled live answer/note fields intentionally do. Native PDF rendering and externally disconnected-network operation remain unconfirmed. The normal root contains labeled fictional verification records and one older-review browser draft available for private inspection.

M5 should begin with this delivery summary and the updated specification/roadmap. All initial audio/consent work is still outstanding: decision history and optional notes-only choice, recording metadata, external-file registration/import/linking, reviewed size/path/preservation/retry behavior and related verification. Transcription/analysis must remain optional and usable with manual notes independently of audio. No audio provider, consent entity, recorder integration, M5 migration or M5 plan was prebuilt. Native capture remains a separate later improvement under the existing roadmap.
