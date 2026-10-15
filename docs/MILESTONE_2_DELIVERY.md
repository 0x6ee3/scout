# Milestone 2 delivery record

15 October 2026. Implements the user-approved [M2 plan](MILESTONE_2_PLAN.md), within [SCOUT_SPEC.md](SCOUT_SPEC.md). M3 and later remain excluded. No commits were requested or created.

## Package gates

Each package passed a clean build and the full test suite before proceeding. Browser failures found at a package gate were fixed and retested before continuing.

| Package | Delivered | Tests passing at gate |
| --- | --- | --- |
| 1 | Application attempts, status history, fixed ownership, independent archive | 71 |
| 2 | Company contacts, explicit/participant links, manual timeline, date-only/timed input | 74 |
| 3 | Position/application actions, complete/reopen, interview rounds and lifecycle facts | 76 |
| 4 | Streamed document imports, five explicit associations, controlled preview/download | 87 |
| 5 | Prepared CV PDFs, reviewed externally sent submissions, immutable evidence, corrections/retry protection | 90 |
| 6 | Catalog/error/refresh polish, guarded samples, upgrade/restore tests, operating guide | 94 |

Final automated gate: **94 passed, 0 failed, 0 skipped**; build **0 warnings/errors**. EF reports **no pending model changes**. The extra text-preview assertion was changed from an unnecessarily expensive large-string search to checking the returned preview length; the complete suite finishes in about four seconds here.

Tests use actual migrated temporary SQLite databases and isolated files. Coverage includes status/event rollback; fixed ownership and contact company scope; archived selections and obligations; date-only and DST conversion; action owner checks; no-contact/notes-only interview completion; streamed byte limits/cancellation/failure retention; no-overwrite collision; traversal/alternate-path/junction rejection; size/hash corruption detection; safe HTTP headers/bytes/encoded previews/truncation/host-origin checks; frozen CV bytes/labels and corrected records; retry identity; one-CV database constraint; fresh samples; M1 upgrade preservation; and separate stopped-root restore including retained partials.

## Architecture and migrations

Retained one web project, one test project, concrete feature operations, detached edit models, and short-lived contexts. New storage helper is concrete; new HTTP endpoints only serve validated document IDs. Storage is a DI service with the same fixed current-user default so test hosts replace it before startup. No runtime dependency was added. Test-only Microsoft.AspNetCore.Mvc.Testing 10.0.13 is the sole package addition.

Additive migrations, following the three M1 migrations:

1. `20261015165501_ApplicationHistory`
2. `20261015170217_ContactsAndTimeline`
3. `20261015170657_ActionsAndInterviews`
4. `20261015171155_ManagedDocuments`
5. `20261015171814_CvsAndSubmissions`

Reviewed foreign keys/restricted deletion, composite link keys, action owner/context checks, immutable path/token/CV uniqueness, and the filtered one-CV-per-submission index. SQLite rebuilds TimelineEvents for added foreign keys; EF logs that those rebuild operations cannot all run in one transaction. They applied successfully, and upgrade/integrity tests pass. A failed/interrupted upgrade still requires investigation of a preserved whole-root copy; startup does not reset/fall back.

Core choices match the approved plan: repeat attempts; arbitrary valid status changes; manual history independent of current status; fixed relationship owners; machine-local timed input with explicit DST ambiguity; independently archived records; immutable document bytes; single-PDF CV versions; and explicit review/record of things sent outside Scout. Submission retry fingerprints include the reviewed CV label, while stored submission-file labels remain independent of later catalog edits.

Small reusable Razor controls handle load errors, draft/save/navigation behavior, explicit operation buttons, local time, and actual reused contact/document selections. Final browser checks exposed parent refresh issues in the review step, saved associations, correction marker, and asynchronously loaded catalog choices; these were fixed. Normal Blazor fields still participate in DataAnnotations validation and unsaved-input tracking.

## Browser checks

Using labeled fictional records, verified application creation/editing/status history and persistence through process restarts; contact creation and timeline association; a date-only event displaying “Time not recorded”; notes-only interview completion; an interview-owned next action and completion; document upload/download and encoded German text preview; attachment reuse and unlink preserving availability; two separate CV PDF imports with different hashes; explicit review then record; and a correction marker retaining original submission evidence.

The original submitted CV downloaded after the second CV import. Its downloaded SHA-256 matched the original fixture:

`D2A1B29D90D0A1F2912A97FFE2D1F811D975B11920D10F1AE31620C2D889E4C2`

The later CV has a different hash:

`A76515840D85DD5E87EAC39956BBACC20FC80D6CD27BBD00F015FEEE2884E595`

Both are 607-byte fictional PDFs. The second fictional submission is deliberately marked incorrect to demonstrate preserved correction history. These records are local examples, not claims that a real application was sent. Existing M1 companies/positions and original vacancy wording remain intact. Personal sample insertion was never enabled.

Final browser checks also verified populated company/application choices, selected status/archive filters, and return to the same action query. The application overview retained the deduplicated Application/Timeline contact origins after restart. Screenshots are in `.local/screenshots/m2-application.jpg` and `.local/screenshots/m2-submission.jpg`. Scout was left running in Production at localhost:5180 using no build, restore, or launch profile.

Browser automation is a smoke check, not a committed end-to-end test suite. Automated tests cover additional combinations/failure cases beyond the browser examples. No user's real six opportunities or private files were supplied/imported.

## Backup and restore verification

Before the first M2 migration, stopped-root backup:

`.local/backups/m2-before-20261015-185531`

After the M2 browser workflow, stopped-root backup:

`.local/backups/m2-final-20261015-193853`

All **four** source files matched the copied file hashes (clean shutdown left the database and three document files, with no sidecars). A separate read-only .NET checker opened the backup database and all document files through the storage verifier:

- `integrity_check`: **ok**; `foreign_key_check`: **no rows**.
- 2 companies, 2 positions, 1 application, 1 contact, 6 events, 1 action, 1 interview.
- 3 documents, 2 prepared CVs, 2 submissions, 2 submission files, 8 applied migrations.
- Every document hash/size verified; the exact M1 multiline vacancy snapshot matched.

The automated complete-workflow test separately copies a stopped temporary whole root, including a retained failed import, reopens it, verifies its relationships and bytes, and checks integrity/foreign keys. Nothing was restored over personal data. Workspace backups are ignored verification artifacts; use a separate durable location for regular backups.

## Deliberate limits

The backend must remain running. Drafts are manually saved and do not survive restart/reload. Imports are one file up to 50 MiB; text preview is limited to 256 KiB. Email/audio remain opaque download-only attachments. No cleanup/reconciliation, automated backups, parser, recorder, transcription, AI, Workspace, or preparation libraries were introduced.

The in-app browser native PDF preview showed a blank surface and could not be visually confirmed. Its cause in this browser session is unconfirmed. Controlled PDF response bytes/MIME/inline headers passed HTTP tests, and the separate download fallback returned the exact original bytes. Native viewer rendering should be checked in the user's regular browser; no PDF rendering dependency or relaxed preview policy was added to work around this limitation.

No operating-system network adapter was disabled or network-denial rule installed. Production launch with no build/restore and local assets was checked; source review found no outbound HTTP client. This is not a claim that the full workflow was exercised with external network access disabled. That environmental acceptance check remains unperformed.

See [README](../README.md) for operation, file-failure recovery, seeding, backup, upgrade, and restore procedures.

