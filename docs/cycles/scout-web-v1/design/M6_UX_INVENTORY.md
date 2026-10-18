# Web v1 M6 — UX inventory

Date: 16 October 2026. Package 1 working record. Scope: source inspection of **43 routed Razor components and 55 route templates**, plus shared controls. No live application screen or user data was changed. Read the [review brief](M6_DESIGN_REVIEW.md) and [M6 plan](../milestones/MILESTONE_6_PLAN.md) alongside this inventory.

## Findings and priorities

| Priority | Finding | Proposed treatment |
|---|---|---|
| High | The ordinary main container is 1,200px wide; most detail/form cards cap at 720px. | A wide desktop frame with three meaningful columns, comfortable type and readable long-text blocks. |
| High | Context varies between back links, titles, numeric IDs and company links. | Named breadcrumbs, consistent entity title/status and preserved list-return context. |
| High | Application work, contacts, timeline, submissions and attachments form a long stack. | Context left, active work center, activity/evidence right; compare work-first ordering before selecting a convention. |
| High | Contact/attachment/preparation selection forms remain open in read views. | Read-first sections with explicit Add, Manage or Attach actions opening focused dialogs. |
| High | Unsaved-navigation prompts and many outcomes appear below the form. | Central discard/confirmation/save-result dialogs, preserving the current draft on failure. |
| High | Private preparation and restricted Workspace have different content boundaries. | Preserve private setup and explicit approval; never reuse full private breadcrumbs/dialog state in the shared Workspace. |
| Medium | Buttons repeat across preparation cards; open/download/unlink actions occupy full lines. | Compact section/item action groups with consistent icons and named targets. |
| Medium | Large editors have useful source/reference material but mostly vertical fields. | Position editor proposal: facts, preserved source, editable notes side by side. Keep large editor routes. |
| Medium | List filters already wrap horizontally; long entries and supporting details still need consistency. | Keep list order and search semantics, improve row/action alignment rather than forcing every list into three columns. |

Evidence: [shared CSS](../../../../src/Scout.Web/wwwroot/app.css), [main layout](../../../../src/Scout.Web/Components/Layout/MainLayout.razor), [application details](../../../../src/Scout.Web/Components/Pages/Applications/Details.razor), [preparation](../../../../src/Scout.Web/Components/Pages/Interviews/Preparation.razor), [position editor](../../../../src/Scout.Web/Components/Pages/Positions/Editor.razor).

## Route coverage

The routes below are copied from current `@page` directives, including alternate editor routes and the Workspace catch-all. Source paths are relative to `src/Scout.Web/Components/Pages/`. Patterns refer to the action matrix in the next section. **Every current routed component is represented; this is not a claim of browser verification of every route.**

| Source | Route templates | UX treatment |
|---|---|---|
| `Home.razor` | `/` | Preserve redirect to companies; no dashboard introduced. |
| `Companies/Index.razor` | `/companies` | Catalog pattern. |
| `Companies/Details.razor` | `/companies/{Id:int}` | Entity context, edit/add contacts/positions, notes/documents, archive/restore. |
| `Companies/Editor.razor` | `/companies/new`; `/companies/{Id:int}/edit` | Named context, grouped fields, save/discard pattern. |
| `Positions/Index.razor` | `/positions` | Catalog pattern; company/status filters. |
| `Positions/Details.razor` | `/positions/{Id:int}` | Company context, original description/notes, applications/documents, entity actions. |
| `Positions/Editor.razor` | `/positions/new`; `/positions/{Id:int}/edit` | Three-column proposal; preserve create versus immutable-source edit distinction. |
| `Applications/Index.razor` | `/applications` | Catalog pattern; position/company/status context. |
| `Applications/Details.razor` | `/applications/{Id:int}` | Three-column proposal with contacts/work/activity/evidence. |
| `Applications/Editor.razor` | `/applications/new`; `/applications/{Id:int}/edit` | Bounded edit dialog where practical; keep position identity, attempt checks and full route. |
| `Applications/Timeline.razor` | `/applications/{Id:int}/timeline` | Ordered timeline with compact event actions and archive filter; no edits for system facts. |
| `Applications/EventDetails.razor` | `/events/{Id:int}` | Application context, event/contacts/documents; respect system versus manual event rules. |
| `Applications/EventEditor.razor` | `/applications/{ApplicationId:int}/events/new`; `/events/{Id:int}/edit` | Focused edit, dates/time-zone ambiguity and contacts preserved. |
| `Applications/SubmissionDetails.razor` | `/submissions/{Id:int}` | Exact-file evidence, download, mark incorrect with reason; no editing historical facts. |
| `Applications/SubmissionEditor.razor` | `/applications/{ApplicationId:int}/submissions/new` | Keep explicit review then record; compact file selection and central confirmation. Never send files externally. |
| `Contacts/Index.razor` | `/contacts` | Catalog/relationship context; retain company filtering. |
| `Contacts/Details.razor` | `/contacts/{Id:int}` | Company, contact facts/notes, open/edit/archive actions. |
| `Contacts/Editor.razor` | `/contacts/new`; `/contacts/{Id:int}/edit` | Focused dialog from parent; direct page route retained. |
| `NextActions/Index.razor` | `/next-actions` | Catalog pattern; preserve due/status/owner filtering. |
| `NextActions/Details.razor` | `/next-actions/{Id:int}` | Named owner/interview/event context; complete/reopen distinct from archive. |
| `NextActions/Editor.razor` | `/next-actions/new`; `/next-actions/{Id:int}/edit` | Focused task entry with existing ownership choices. |
| `Documents/Index.razor` | `/documents` | Catalog pattern; import separate from attach existing. |
| `Documents/Details.razor` | `/documents/{Id:int}` | Content/metadata/relations with compact preview/download/edit/archive actions. |
| `Documents/Editor.razor` | `/documents/import`; `/documents/{Id:int}/edit` | Preserve upload progress/limits, imported bytes and manual saving; do not conflate metadata edit with file replacement. |
| `Cvs/Index.razor` | `/cvs` | Catalog pattern; import/find prepared versions. |
| `Cvs/Details.razor` | `/cvs/{Id:int}` | Preserved PDF and version facts; open/download/edit metadata/archive. |
| `Cvs/Editor.razor` | `/cvs/import`; `/cvs/{Id:int}/edit` | Preserve imported PDF, metadata and association semantics. |
| `Questions/Index.razor` | `/questions` | Search/filter/tags plus compact catalog actions. |
| `Questions/Details.razor` | `/questions/{Id:int}` | Edit/archive plus **existing permanent catalog deletion**, with explicit distinct confirmation and interview-copy preservation. |
| `Questions/Editor.razor` | `/questions/new`; `/questions/{Id:int}/edit` | Long-text source/edit/preview and tags; preserve limits. |
| `Experiences/Index.razor` | `/experiences` | Search/filter/tags plus compact catalog actions. |
| `Experiences/Details.razor` | `/experiences/{Id:int}` | STAR content, edit/archive; preserve linked interview selections. |
| `Experiences/Editor.razor` | `/experiences/new`; `/experiences/{Id:int}/edit` | Substantial editor stays roomy; clearly distinguish shared library changes from rehearsal notes. |
| `Interviews/Details.razor` | `/interviews/{Id:int}` | Named application/round context, grouped prepare/Workspace/edit actions, private notes/media/resources. |
| `Interviews/Editor.razor` | `/applications/{ApplicationId:int}/interviews/new`; `/interviews/{Id:int}/edit` | Schedule/status/participants and explicit save, no implicit recording. |
| `Interviews/Preparation.razor` | `/interviews/{Id:int}/preparation` | Three-column proposal; selected question copies, STAR content and private context. |
| `Interviews/PreparedQuestionEditor.razor` | `/interviews/{RoundId:int}/questions/{Id:int}/edit` | Bounded dialog or full route for long input; preserve revisions and source-copy semantics. |
| `Interviews/PreparedExperienceEditor.razor` | `/interviews/{RoundId:int}/experiences/{Id:int}/edit` | Rehearsal notes only; keep library content ownership clear. |
| `Interviews/Transcript.razor` | `/interviews/{RoundId:int}/transcripts/{Id:int}` | Private reader, parent/original links and immutable corrected copies; keep ordinary HTTP correction submission. |
| `Interviews/WorkspaceSetup.razor` | `/interviews/{Id:int}/workspace/setup` | Private review/approval, prior answers, exact CV selection and draft recovery; no automatic inclusion. |
| `Interviews/Workspace.razor` | `/interviews/{RoundKey}/workspace`; `/interviews/{RoundKey}/workspace/{*UnknownPath}` | Separate restricted layout, approved context only, existing search/filter/layout and quiet live-save status. |
| `NotFound.razor` | `/not-found` | Consistent recovery context without inventing an entity. |
| `Error.razor` | `/error` | Visible recoverable failure; no false save-success claim. |

Private byte-serving and correction HTTP endpoints are not new view routes and are not redesigned as public navigation. Preserve guarded document/recording access and the correction form contract.

## Action and shared-component matrix

| Pattern / shared control | Placement and interaction | Behavior to preserve |
|---|---|---|
| `MainLayout`, page headings, back links and `CatalogLinks` | Keep main navigation; visible named breadcrumbs and entity action strip beneath it. | Existing routes, direct entry, list query return/cancel context and not-found states. |
| Catalog index filters and rows | Horizontal filters, readable rows, adjacent item actions where appropriate. | Filtering, ordering, archive visibility, empty/no-results distinction. |
| `ApplicationWork` | Interview rounds and next actions in primary work area; Add beside headings. | Explicit parent association, complete/reopen separate from archive. |
| `ApplicationContacts`, `ContactPicker` | Read contacts; Manage selection or Add contact opens a dialog. | Company-related versus explicitly selected contacts. Creating a contact does not silently select it. |
| `Attachments`, `DocumentPicker`, `DocumentFilePicker` | Attach/import at section header; open/download/unlink by each file. | Existing-file selection versus upload; unlink preserves original and other associations, limits/hash verification unchanged. |
| `SubmissionHistory`, submission review/correction | Evidence alongside activity/resources, with Record externally sent submission action. | Two-step review, immutable exact file versions, incorrect-marker explanation and no external sending. |
| `PreparationPicker`, `PreparationOptions` | Add/restore selection in central picker, selected content remains the main read surface. | Copies versus library links, archived sources, no accidental duplicate inclusions. |
| Preparation item operations | Adjacent edit/move/remove icon groups. | Existing ordering and local-copy fields; remove selection differs from deleting a catalog item. |
| `ArchiveActions`, `OperationButton` | Less frequent archive/restore in More; confirmation and recoverable outcome centrally. | Record-specific archival, history retention, failure feedback and duplicate-click protection. |
| Existing question permanent deletion | Explicitly labeled destructive entry in question More menu, separate from archive. | Current affected-copy count and irreversible catalog deletion warning; interview copies retain text/answers/notes. This already exists; no new deletion is added. |
| `DraftForm`, bespoke editors, `LocalTimeField` | Save/cancel in stable action area; central discard/result dialogs, field messages stay near inputs. | Validation, retained drafts, native external-navigation protection, date/time ambiguity handling and manual saving. |
| `RecordingReview`, `SavedRecording`, `TranscriptReview` | Compact private lists with on-demand registration/import and item actions. | Explicit file-specific consent, notes-only path, no capture initiation, playback/download, originals and metadata archives. |
| `MarkdownText`, `MarkdownPreview`, `ExperienceStory`, tags | Readable text, explicit preview and compact tags; keep long fields wide enough. | Exact strings, plain-text fallback, inert unsafe markup, private/shared distinction. |
| Workspace `CaptureEditor`, `PrivateDraftRecovery`, `WorkspaceLayout` | Approved panels in available width; quiet status during typing, recoverable conflict/draft dialogs. | Projection privacy, approval freshness, revision checks and current IndexedDB recovery behavior. |
| `LoadPanel`, error/reconnect surfaces | Consistent loading/unavailable/error context; do not cover failures with stale success dialogs. | Retry/reconnect guidance, unresolved-save uncertainty and no silent discard. |

## Interaction checks to carry into implementation

Explicit saves produce a central acknowledgment only after confirmed success. Archive/unlink/delete confirmations must name the entity and state exact retention consequences. Routine asked-state/reordering/autosave updates stay unobtrusive. Native close/reload protection remains native where the browser requires it.

Long editors retain page routes; a dialog is not a reason to reduce existing fields or limits. Long filenames, names, translated content, sparse records, read-only/archive states, direct navigation, errors, dirty cancellation and keyboard return focus all need representation. See the [design brief](M6_DESIGN_REVIEW.md) for the proposed interactions and what the prototype actually demonstrates.
