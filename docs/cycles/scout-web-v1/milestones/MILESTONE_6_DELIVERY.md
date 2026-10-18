# Scout.Web v1 — Milestone 6 delivery

Date: 16 October 2026. Status: implemented; user acceptance on the physical display remains open. The user approved the Work centered design and authorized its application across suitable pages. This record follows the [plan](MILESTONE_6_PLAN.md), [initial design review](../design/M6_DESIGN_REVIEW.md) and [implementation decision](../../../engineering/decisions/0003-web-presentation-patterns.md).

## Delivered behavior

The existing top navigation remains. Management pages now use the desktop width, visible named breadcrumbs and compact entity actions. Rich company, position, application, interview, preparation and event views put related content into three columns. Application context sits left, interview rounds and next actions occupy the larger center, and timeline, submissions and documents sit right. Contact, CV and next-action details use two useful groups. Ordered catalogs and timelines remain ordered lists rather than being split into unrelated columns.

Large editors retain their routes and use grouped fields across the available width. The position editor explicitly separates identity, preserved vacancy text and editable notes. Application/contact edits, contact creation, attachment selection, preparation selection, prepared-question edits and rehearsal-note edits can open centrally. Direct editor routes remain available. Recording registration and transcript import are also on demand.

Generic actions use local SVG icons with accessible names and focus/hover labels. Important workflow actions retain visible text. Archive/restore lives in More; unlink and preparation removal explain preservation. Existing permanent employer-question deletion keeps its separate irreversible warning and retained-copy count.

Native dialogs provide central confirmations, explicit save acknowledgment and dirty-close review. Save failures retain input and show actionable feedback. Intermediate submission review does not claim that a record was saved; final recording and correction are acknowledged after persistence. Discarded file selections are cleared so detached file-input handles cannot be reused. Busy operations block duplicate saves/dismissal. Workspace autosave and repetitive ordering remain quiet.

Private Workspace setup and the shared Workspace use the wider layout while retaining the existing projection and saving/recovery code. Shared content stays limited to explicit approval. Private breadcrumbs and management navigation are absent there. Transcript correction retains its existing bounded ordinary HTTP form.

## Verification performed

All browser exercises used fictional sample data in a separate `.local/m6-browser-data` root, never personal storage. Headless Microsoft Edge ran at a **3840 × 2160 CSS-pixel viewport**, device scale factor 1 and default browser zoom. Additional application checks used 2560, 1920 and 900 CSS-pixel widths. These are browser viewport checks, not measurements of the user's monitor, OS scaling or window size.

| Check | Result |
|---|---|
| Solution build, `dotnet build Scout.slnx --no-restore -m:1 -p:UseSharedCompilation=false` | Passed, zero warnings/errors. |
| Built xUnit runner, `dotnet tests/Scout.Web.Tests/bin/Debug/net10.0/Scout.Web.Tests.dll` | 155 passed; none failed/skipped. |
| Native Node tests for Workspace drafts and dialogs | 11 passed: six existing reliability tests and five dialog lifecycle/focus tests. |
| EF pending-model check | No changes since the last migration. M6 adds no migration. |
| Route walkthrough | 52 concrete URLs covering all normal page families, create/edit alternatives, transcript review and not-found; no visible Blazor error or document overflow. Root redirect, error endpoint and Workspace catch-all were not separate walkthrough cases. |
| Additional widths | Application used three columns at 2560/1920 and two at 900, without document overflow. |
| Contact and position edits | Actual fictional saves, save acknowledgments, dirty Escape/cancel, Keep editing and retained input passed. |
| Application and preparation dialogs | Actual application and prepared-question saves passed; attempted navigation from dirty modal prompted before leaving. |
| Submission workflow | Review, return to selection, final record, acknowledgment and mark-incorrect reason passed. Original evidence remains retained. |
| Transcript import | Actual UTF-8 TXT upload through dialog and acknowledgment passed. |
| Keyboard and failure handling | Required-field failure retains the form; Tab/Shift+Tab boundaries and return to the invoker passed after adding explicit wrapping. Discarded file selection is cleared and saved contact selections are restored on reopen. |
| Shared Workspace | Explicit approval, excluded private-note sentinel, absence of private navigation, live note save and reload passed. |

The flow run reported no page exceptions or HTTP failures. The broad route walk recorded one expected 404 for its deliberate `/not-found` navigation. The isolated helper build reported NU1900 because NuGet vulnerability data was unavailable; that audit is not a passed check. Screenshots and machine-readable local walkthrough results are in `.local/screenshots/m6/`; selected fictional captures are retained in [implementation evidence](../design/evidence/implementation/README.md).

## Scope and remaining limits

Packages 1–5 have implementation and verification evidence; physical-display acceptance remains for the user. The [package 1 comparison](../design/M6_DESIGN_REVIEW.md) preserves the simplified before/proposal reference, not a pixel-exact screenshot of the former application. Production screenshots show the actual delivered layouts.

No recorder, transcription engine, local LLM, schema change, new frontend framework or remote icon dependency is introduced by M6. Existing M5 code/data changes in the working tree precede this UI work. Historical M1–M5 records and their limits still apply.

Manual screen-reader testing, every browser/codec combination, physical display/zoom acceptance and exhaustive failure injection for every form have not been performed. General editor drafts still do not survive reload/backend shutdown. Workspace recovery and privacy regressions are covered by the existing tests, but this walkthrough does not replace all M4/M5 hardware, restore and real-interview checks. Scribe remains a separate, unimplemented product cycle.
