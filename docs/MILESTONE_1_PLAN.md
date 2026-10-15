# Scout: Milestone 1 implementation plan

Status: approved by the user, with a fixed Documents/Scout storage root; implementation has not started.  
Date: 15 October 2026.  
Architecture and dependency pins: [MILESTONE_1_ARCHITECTURE.md](MILESTONE_1_ARCHITECTURE.md).  
Source of scope: [SCOUT_SPEC.md, section 10](SCOUT_SPEC.md#10-roadmap).

## Outcome and scope

After Milestone 1, the user can launch Scout on Windows using `dotnet run --project src/Scout.Web`, manage companies and their positions, retain pasted vacancy text, find records, archive/restore them, and reopen the app without losing saved data. No external service is needed at runtime.

Included: one Blazor application, basic navigation/local styling, one SQLite database, EF migrations, a fixed `<user home>\Documents\Scout` storage root, minimal company/position fields, search/filtering, and optional fictional development samples.

Excluded: applications, contacts, timelines, next actions, interviews, dashboards, document/file import, CV management, question/experience libraries, Workspace, recording, transcription, AI, scraping, integrations, installers, and automated backup. Do not add their tables, screens, dependencies, or abstraction scaffolding.

## Delivery approach

Implement the following slices in order. Each ends with visible behavior, persistence where relevant, and focused verification. Commit migrations with the slice that changes the model. Build and run affected tests before reporting a slice complete. Keep the approved architecture/README aligned with changes.

M1.1 necessarily includes the initial project setup, but it also delivers a real company creation flow; there is no separate week of infrastructure work. Subsequent slices extend working behavior. Browser checks can be manual or performed with available browser tooling; do not build an end-to-end testing platform for trivial forms.

### M1.1 — Launch locally and save the first company

**User outcome:** Start Scout, enter a company name, save it, and see the same record after restart.

Work:

- Create `Scout.slnx`, `Scout.Web`, and `Scout.Web.Tests`; target `net10.0` and the documented stable dependencies. Add the SDK pin, local EF tool manifest, editor settings, and ignore rules. Do not install globally scoped tools.
- Create the standard Blazor shell with Companies/Positions navigation, local CSS, and companies as the landing page. Add only the company list/create/detail routes that work in this slice; add the Positions link when its page exists.
- Resolve the fixed `<user home>\Documents\Scout` root, with `database\scout.db` and `assets` beneath it; validate paths and create required directories outside static assets. Resolve the user profile through .NET rather than assuming a `%USERHOME%` variable exists. Keep the loopback port configurable and add loopback/host/origin protections to normal host setup.
- Add the initial Company entity with name, archive flag, and timestamps. Add `ScoutDbContext`, its factory, feature operations, the initial migration, and startup migration application. Create the initial company form/list/detail using real SQLite.
- Add a short README with prerequisites, tool restore/package restore, launch command, data locations, and stop instructions. Mark sample data as not yet available.

Acceptance and checks:

- Automated: apply migrations to a new temporary SQLite file, create a company through its operation, dispose all contexts, reopen the file, and read the same name/ID/timestamps.
- Automated: blank/whitespace names are rejected and create no row; database/asset paths resolve beneath the supplied root independently of the working directory. Tests supply a temporary root and never write to the real Documents directory; initialization failures never trigger a fallback database.
- Browser/process smoke: launch on localhost, save a company, restart, and find it again. Inspect actual listeners to confirm no wildcard/LAN binding. A hostile Host or browser Origin is rejected while the legitimate UI works.
- Failure smoke: an inaccessible database directory and an occupied port produce actionable startup failures without resetting data.

### M1.2 — Maintain useful company details

**User outcome:** Add a website, reference information, location, and personal notes; edit them without losing saved data.

Work:

- Add the remaining proposed Company fields and their migration. Implement company edit and complete detail views, sharing a company form where useful.
- Keep reference description separate from personal notes. Validate lengths and HTTP/HTTPS URLs; retain German/English text and line breaks.
- Add Save/Cancel, visible field errors, save-in-progress controls, unsaved-navigation protection, and not-found handling. Render all supplied text encoded.
- Preserve original creation time and update the modification time only on successful writes. Do not bind entity keys, timestamps, or archive state to the general edit form.

Acceptance and checks:

- Automated SQLite tests: optional values round-trip; updates keep the ID/creation time; invalid updates do not alter the stored record; editing a nonexistent ID returns not-found rather than inserting.
- Browser smoke: keyboard-create and edit a company, cancel an edit, and follow a deliberately opened website link. HTML-like notes display as text. Validation retains entered form values.
- Recoverable save failures leave the edit state available and show no false success.

### M1.3 — Track positions and preserve vacancy text

**User outcome:** Create several positions for a company, paste a vacancy description, edit position details, and navigate between the company and its opportunities.

Work:

- Add Position, its opportunity-status enum, the required indexed Company FK with restricted deletion, and a migration.
- Implement the proposed position fields, list/create/detail/edit pages, and concrete position operations. Add the working Positions navigation link.
- Company details show linked positions and offer Add position with the company preselected. The general creation form selects an existing company and offers a normal link to create one if none exist.
- Require only company/title input; default status/discovery date. Do not enforce an application workflow or fetch vacancy URLs.
- Save the first nonempty pasted job description and capture timestamp. Make it read-only afterward; notes hold subsequent corrections/observations. Enforce the preservation rule below the UI as well.
- Allow explicit company reassignment to an active company. Keep snapshot/timestamps safe during other edits and show company links on position pages.

Acceptance and checks:

- Automated against SQLite: one company can have multiple positions; a position cannot have a missing company; the database FK also rejects orphans. Saving a position does not accidentally insert or change its company.
- Automated: creation defaults, date-only values, optional fields, and stable status strings round-trip; invalid status values and invalid URLs are rejected.
- Automated: an empty snapshot can be filled once; subsequent general edits cannot replace it or its capture time; notes and ordinary position fields can still change.
- Browser smoke: create a position from both entry points, paste a multiline German vacancy, edit notes/status, restart, and verify the original snapshot. Confirm no URL is fetched or scraped.

### M1.4 — Archive and restore without losing relationships

**User outcome:** Remove unwanted companies/positions from their active catalogs and restore them later with all information intact.

Work:

- Add explicit archive/restore operations and controls for both entities. Use flags already present in their models; provide no permanent delete.
- Implement Active / Archived / All catalog filters and clear badges on detail pages.
- Company archiving changes no positions. Active positions under an archived company remain visible with a company-archive badge. Explain this behavior at the company archive action.
- Prevent creating positions under archived companies; keep existing positions editable. Validate active targets when reassigning a company.
- Preserve links, text, creation time, position status, and row counts when archiving/restoring. Avoid hidden global query filters.

Acceptance and checks:

- Automated: archive/restore round-trips preserve record counts, snapshots, relationships, and opportunity status. Repeated archive/restore calls are harmless.
- Automated: company archiving leaves child flags/statuses unchanged; list predicates and company-selection rules follow the agreed policy.
- Browser smoke: archive a company with an active position; find the position with its badge; restore the company. Archive/restore the position and verify all values. Direct links to archived records still work.

### M1.5 — Find companies and positions quickly

**User outcome:** Find an employer or position by text, company, status, and archive state without navigating a growing list manually.

Work:

- Add explicit search submission, clear/reset controls, informative empty states, and predictable sorting.
- Search company name/location and position title/company/location. Apply company/status/archive filters together. Treat search text literally.
- Use narrow read projections, database-side structured filtering, and .NET case-insensitive substring matching as described in the architecture. Avoid loading full vacancy descriptions into list queries.
- Keep filter/search values in query parameters so detail/back navigation can return to the same list. Default companies to name order; positions to discovery-date descending, with stable tie-breakers.

Acceptance and checks:

- Automated: combined filters, archive modes, no-match results, sorting, and literal `%`/`_` search text behave correctly. `MÜNCHEN` matches `münchen`; no accent/transliteration promise.
- Browser smoke: search, open details, go back, change company/status, and reset. Empty database and no-result states both provide a useful next action.

### M1.6 — Try sample opportunities and verify the complete local workflow

**User outcome:** Explore fictional data safely, then use an empty personal database with documented startup and backup steps.

Work:

- Add a tiny deterministic fictional development dataset with multiple companies/positions, varied statuses, multiline descriptions, and archived examples. Insert in one transaction only when explicitly enabled in Development and both business tables are empty.
- Make sample insertion independent from migrations/design-time commands. Do not add seed markers or sample-management tables. Do not implement a reset/delete-data action.
- Finish README instructions for the fixed storage root shared by Development and ordinary runs, migration authoring/application, explicit optional seeding, manual stop-and-copy backup/restore, troubleshooting, and known limits. No user-facing storage overrides are required.
- Polish the existing desktop pages for readable spacing, focus order, labels, errors, and save feedback. Remove unused template demonstration pages and links. Do not add a dashboard or new feature area.

Acceptance and checks:

- Automated: opt-in empty-development seeding inserts expected rows; running again changes nothing. Disabled seeding, a nonempty database, and non-Development execution insert nothing, even if the flag is set.
- Automated: a fresh file receives all committed migrations and can run the full company/position operations. Reapplying migration startup is harmless and preserves data. Check the model has no uncommitted schema changes.
- Browser/process acceptance: complete the workflow below using the fixed storage root, with external network access unavailable after restore. Keep automated persistence/seeding checks in isolated temporary roots.
- Document the checks actually performed and any remaining limitations; do not report browser steps as automated unless they were.

## Verification strategy

Use one xUnit project and real temporary SQLite database files. Each test receives an isolated temporary location and applies the committed migrations; use independently opened contexts to verify persistence and database constraints. Never point tests at the user's data. Test cleanup is restricted to files created within that isolated test directory and must never touch source documents or recordings.

Prioritize feature rules and real persistence over rendering internals: required fields, relationships, preservation, archive semantics, search, storage path resolution, and seed isolation. Avoid mocks of EF queries and avoid assertions that merely repeat property setters. No automated coverage percentage is a milestone goal.

Build and run relevant tests after each slice; run the complete small suite at the final gate. Typical final checks once the projects exist:

```powershell
dotnet tool restore
dotnet restore Scout.slnx
dotnet build Scout.slnx --no-restore
dotnet test Scout.slnx --no-build
dotnet ef migrations has-pending-model-changes --project src/Scout.Web --startup-project src/Scout.Web
dotnet run --project src/Scout.Web
```

These are future verification commands, not commands executed by this planning task. First restore needs access to the package feed; ordinary Scout usage must not depend on an external service. EF design-time commands must resolve the same configured storage settings and must not seed.

## Final user acceptance walkthrough

1. Launch with `dotnet run --project src/Scout.Web`; reach the local company list with no login or external-service setup.
2. Create two companies with name only. Add reference information, a URL, a location, and private notes to one; cancel a separate edit.
3. Create multiple positions under one company. Paste a vacancy description, set status/closing date, and retain optional information in notes.
4. Navigate company → position → company; edit a position without changing its preserved snapshot.
5. Search company/position lists, combine filters, and verify German text. Empty/no-result states remain usable.
6. Archive/restore a position and its company independently. Check the opportunity status, description, notes, and relationship remain intact.
7. Stop and relaunch Scout. Verify saved records. Confirm the listener remains loopback-only and data is not reachable as static files.
8. Verify sample insertion/idempotence through isolated temporary databases. Only enable samples in the real root deliberately while it is empty; verify samples never appear in a nonempty database or outside Development.
9. Stop Scout, copy the complete `Documents\Scout` root as a documented manual backup, and verify the copied database using an isolated persistence check. Preserve the originals.

Milestone 1 is complete when this walkthrough and relevant tests pass. The next planning/implementation decision can then be based on actual company/position use; nothing in this plan implements Milestone 2.
