# Scout.Web v1 — Roadmap

Reading note (29 October 2026): the milestone descriptions below retain the original release goals and sequencing. Actual delivery narrowed M3 to employer questions and STAR experiences, without an expected-question catalog or question-to-experience graph; see [M3 delivery](milestones/MILESTONE_3_DELIVERY.md#delivered-behavior-and-architecture). Web M6 is implemented and Scribe M1 implementation is underway independently. Use the [portfolio roadmap](../../ROADMAP.md) for current priorities and the [user guide](../../products/scout-web/USER_GUIDE.md) for implemented behavior.

Milestone goals, deliverables and acceptance criteria for Web 1.0. M1–M6 are implemented; [M6 delivery](milestones/MILESTONE_6_DELIVERY.md) and the [release baseline](RELEASE_SUMMARY.md) record actual checks and remaining physical-display acceptance. Detailed records are indexed in the [cycle overview](README.md). Scribe's milestones are independent and do not block Web 1.0.

[Documentation overview](../../README.md). Section numbers are local to this document.

## 1. Roadmap

### Milestone 1 — Application foundation

Goal: Have a functioning local Scout application.

Deliverables:

- ASP.NET Core project.
- Blazor UI.
- SQLite database with EF Core migrations.
- Configuration for storage directories.
- Basic navigation and styling.
- Company and position CRUD.
- Sample/seed data for development.

Acceptance: The user can launch Scout locally and manage companies and positions without external services.

### Milestone 2 — Application management

Goal: Enter the six current job opportunities and manage their application processes.

Deliverables:

- Applications and statuses.
- Contacts.
- Timeline events.
- Next actions.
- Interview rounds.
- Document import and association.
- Prepared CV management and submitted-file history.

Acceptance: A complete application can be tracked without needing a spreadsheet, including its documents, next actions, and interview history.

### Milestone 3 — Interview preparation

Goal: Make Scout useful for preparing the next interview.

Deliverables:

- Question catalog for employer questions.
- Catalog of expected interviewer questions.
- Professional experience library.
- Question-to-experience associations.
- Interview-specific preparation lists.
- Search and filtering.
- Support for German and English content.

Acceptance: The user can prepare an interview using Scout instead of maintaining multiple text documents.

### Milestone 4 — Interview Workspace

Goal: Make Scout usable during a live video interview.

Deliverables:

- Dedicated browser window/tab.
- Interview-specific data only.
- Screen-share-aware UI.
- Position details and submitted CV.
- Prepared employer questions.
- Prepared professional experiences.
- Mark-as-asked and notes functionality.
- Reliable note saving.
- Manual-note interview workflow without audio setup or consent prompts.

All audio and recording-consent work is deferred to Milestone 5; M4 introduces no recording UI, metadata, storage or audio-specific limit changes.

Acceptance: The user can conduct a mock interview with Scout on one display, record answers and notes, and safely share the Workspace without revealing unrelated Scout records.

### Milestone 5 — Recordings, consent and interview review

Implemented: see the [M5 delivery summary](milestones/MILESTONE_5_DELIVERY.md) for package gates, restore evidence and remaining browser verification limits.

Goal: Manage optional consent-aware external recordings, play saved audio after an interview, and review notes/transcripts with simple formatting without processing tools.

Deliverables:

- Recording-consent acknowledgment and decision history.
- Explicit choice to continue without recording; notes and interview completion remain independent of audio.
- External-audio upload/registration and links from the configured working directory.
- Recording metadata, file preservation, audio size-limit policy, safe file access and import/link failure handling.
- Native media controls on private interview-round details for saved recordings, including completed interviews.
- Safe range-capable streaming, unavailable-file and unsupported-codec feedback.
- Manual text transcript import, private reading and corrected copies that preserve the original.
- Safe simple Markdown rendering for long-text read views, plain-text access and source-text editing/preview.
- Upgrade/restore and Workspace privacy checks, operating guidance and M5 delivery summary.

Acceptance: The user can acknowledge externally obtained consent and upload/link a recording, play/pause/seek it on the completed round's private details, or continue with manual notes alone; import/review a transcript and read formatted notes without requiring transcription tools or AI.

### Milestone 6 — UI overhaul

Goal: Make the existing Web workflows more compact, consistent and easier to use, completing the intended Web v1 development cycle.

Deliverables: retain the top navigation; add visible breadcrumbs and entity action strips; use 4K-first three-column composition on rich views; introduce consistent generic-action icons, compact local actions and central dialogs for focused edits, confirmations and explicit-save outcomes. Preserve discoverability, readable text, reliable saving/draft recovery and Workspace privacy; routine live autosave stays unobtrusive.

Acceptance: Complete application tracking, preparation, live notes and private review on the intended displays with the revised UI; verify affected keyboard/dialog behavior, saving/conflicts and screen-share boundaries. Record actual checks and remaining limitations in M6 delivery evidence and then a cycle release summary.

See the [M6 UX plan](milestones/MILESTONE_6_PLAN.md) and [ADR 0002](../../engineering/decisions/0002-web-v1-desktop-ux.md). Finish this Web 1.0 cycle before starting Scribe implementation or expanding its plans. No recorder, transcription or LLM engine is part of M6. After delivery, use the application in real interviews before prioritizing a possible Web v2 cycle from the [wishlist](../../FEATURE_WISHLIST.md).
