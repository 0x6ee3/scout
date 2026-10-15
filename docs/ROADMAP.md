# Roadmap

This is a snippet taken from SCOUT_SPEC.md and only for readability. The SCOUT_SPEC.md is still the valid source of truth.

## 10. Roadmap

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

### Milestone 5 — Audio, consent, transcription and structured analysis

Goal: Add optional consent-aware external-audio workflows and turn recorded or manually written interviews into reusable knowledge.

Deliverables:

- Recording-consent acknowledgment and decision history.
- Explicit choice to continue without recording; notes and interview completion remain independent of audio.
- External-audio import/registration and links from the configured working directory.
- Recording metadata, file preservation, audio size-limit policy, safe file access and import/link failure handling.
- Optional local audio transcription.
- Text transcript import.
- Processing status and errors.
- Basic transcript reader.
- Optional AI analysis.
- Review of extracted questions, facts, and actions.
- Controlled transfer of approved facts into structured data.

Acceptance: The user can acknowledge externally obtained consent and import/link a recording, or continue with manual notes alone; obtain a summary when an analysis provider is configured; and review extracted information before saving it.

### Milestone 6 — Future improvements

Potential features:

- Native Windows microphone and loopback recording.
- Germany-wide company discovery.
- Domain-based company categorization.
- Vacancy change detection and periodic checks.
- AI-assisted extraction of vacancy requirements.
- Import and deduplication of existing question documents.
- AI-assisted interview question suggestions.
- AI-based retrieval over previous interviews.
- Personal interview knowledge chatbot.
- CV customization.
- Arbeitszeugnis analysis.
- Cover-letter drafting.
- More sophisticated dashboard and reporting.

Only implement future features when they provide clear value.
