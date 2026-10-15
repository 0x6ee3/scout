# Scout — Product & Technical Specification

**Status:** Initial specification, ready for implementation planning  
**Target environment:** Windows 11, local-only, single user  
**Primary goal:** Build a practical personal job-search and interview-management application.  
**Development approach:** AI-assisted implementation with Codex, delivered incrementally.

---

## 1. Product vision

Scout is a personal application for managing companies, vacancies, job applications, documents, CV versions, interview preparation, interview records, and professional experiences.

It replaces scattered spreadsheets, text documents, and folders with a structured application.

Scout serves two complementary purposes:

1. **Job application management:** Track employers, positions, contacts, applications, documents, communications, and next actions.
2. **Interview preparation and knowledge management:** Prepare questions, reference professional experiences, conduct interviews with a dedicated workspace, and preserve what was learned.

Eventually, Scout may also assist with discovering German employers in selected industries, tracking vacancies, and extracting insights from interview history.

### Product principles

- **Single-user and local-first.** No hosting, accounts, cloud deployment, or multi-user features.
- **Useful before intelligent.** All essential workflows must function without AI.
- **Simple implementation.** Prefer standard framework features and mature libraries.
- **Human control.** AI-generated facts and recommendations require review before changing authoritative data.
- **No automatic communication.** Scout must never submit job applications, complete external web forms, send emails, or contact employers.
- **Preserve information.** Do not delete source documents, recordings, transcripts, or files from the working directory.
- **Interview-friendly.** Fast navigation, minimal distraction, and layouts suitable for large monitors.
- **Privacy-aware.** Keep sensitive personal records out of the screen-shareable Interview Workspace.
- **Incremental development.** Implement working vertical slices rather than generating the complete application at once.

### Non-goals

Scout is not a commercial applicant-tracking system, a recruiter tool, a LinkedIn replacement, a job-application bot, or a production SaaS platform.

It does not need enterprise architecture, user permissions, distributed services, containerization, or sophisticated deployment.

---

## 2. Technology decisions

### Proposed stack

| Area | Choice |
|---|---|
| Language | C# |
| Runtime | Current supported .NET release |
| Backend | ASP.NET Core |
| UI | Blazor Web App with server-side interactivity |
| ORM | Entity Framework Core |
| Database | SQLite |
| File storage | Local directories |
| Editor | VS Code |
| Development agent | Codex |
| Operating system | Windows 11 |
| Audio recording | External recorder initially; optional native integration later |
| Transcription | Optional local speech-to-text tool |
| AI analysis | Optional provider integration, independently configurable |

The exact supported .NET and package versions should be verified when implementation begins.

### Why this stack?

The application primarily consists of CRUD screens, document management, reference data, and interview-specific views.

ASP.NET Core and Blazor allow this to be implemented in a single C# ecosystem without a separate JavaScript frontend build system.

The recording subsystem does not determine the main application architecture because external audio capture and file import are acceptable.

Razor Pages or ASP.NET Core with HTMX are also reasonable alternatives. Prefer whichever produces the simplest maintainable solution; do not combine multiple UI paradigms without a compelling reason.

### Deployment and execution

- Run locally on Windows 11.
- Bind the web server to localhost only.
- Use one SQLite database initially.
- Store files in a configured local directory.
- Support launching Scout through `dotnet run` during development.
- Desktop installers, Windows services, and automatic startup are not required.
- No Docker or remote database.
- No authentication screens.

Local-only does not mean security is irrelevant: validate file uploads and paths, avoid exposing the server on the LAN, and do not expose arbitrary local files through HTTP endpoints.

### Configuration

Configure at minimum:

- Database location.
- Asset directory.
- Optional audio import directory.
- Optional external transcription executable/model.
- Optional LLM provider and credentials.

Use configuration files or environment variables as appropriate. Do not embed API keys in source code, database records, or committed configuration.

---

## 3. Domain model

The following entities are conceptual. Codex should refine their structure into a practical relational model rather than reproduce them mechanically.

### 3.1 Company

Represents an employer, whether or not any vacancy or application currently exists.

Suggested attributes:

- Name
- Website
- Description
- Headquarters / locations
- Industry and operational domains (multiple)
- Relevant technologies
- Remote-work information
- General salary information, if known
- Personal interest / fit assessment
- Notes
- Links
- Created and updated timestamps
- Archived flag

Company information should distinguish verified facts from personal impressions and unknowns where useful.

A company may have multiple positions, contacts, and applications.

### 3.2 Position

Represents a particular job opportunity at a company.

Suggested attributes:

- Company
- Job title
- Original vacancy URL
- Job description (preserved snapshot)
- Relevant technologies
- Salary range / currency
- Remote and office-attendance requirements
- Employment type
- Location
- Personal notes and assessment
- Discovery date
- Publication/closing date, if known
- Status: discovered, interested, rejected by user, expired, archived

A position may exist without an application.

Do not automatically overwrite the original job description when revisiting a URL.

### 3.3 Application

Represents the user's application for one position.

Suggested attributes:

- Position
- Date applied
- Status: planned, applied, screening, interviewing, offer, rejected, withdrawn, accepted
- Submitted CV/document references
- Notes
- Outcome and dates
- Follow-up actions

Application statuses should be easy to modify. The timeline is the primary record of what actually happened.

### 3.4 Contact

Represents an individual associated with a company.

Suggested attributes:

- Name
- Role
- Company
- Email
- Phone, if available
- LinkedIn/profile URL
- Notes

Contacts can be associated with interview rounds and timeline events.

Do not require contact details for an interview to exist.

### 3.5 Application timeline event

A chronological record of actions and developments.

Event types may include:

- Application submitted
- Email received
- Email sent manually outside Scout
- Recruiter call
- Interview
- Coding challenge
- Documents received
- Offer
- Rejection
- Personal note
- Other

An event includes its date/time, description, associated contacts, and optional attachments.

Events should be flexible enough to represent unusual recruitment processes.

### 3.6 Next action

A lightweight task associated with an application or position.

Fields:

- Title
- Description
- Optional due date
- Status: open / completed
- Optional related interview or timeline event

Examples:

- Call recruiter to clarify remote policy.
- Prepare STAR example about mentoring.
- Send CV manually.
- Ask about training budget.
- Follow up after an interview.

No automatic reminders or outbound messages are required in the MVP.

### 3.7 Document / attachment

Reusable document metadata associated with another entity.

Typical files:

- CVs
- Cover letters
- Job descriptions
- Emails exported as files
- Coding challenges
- Interview preparation documents
- PDFs received from employers
- Audio recordings
- Transcripts

Store binary content on disk and metadata in SQLite.

Suggested metadata:

- Original filename
- Stored relative path
- Content type
- File size
- Import timestamp
- Description
- Document category
- Associated entity
- Optional hash for duplicate detection

Multiple attachments per entity are required.

Files must not be silently overwritten or deleted.

### 3.8 CV

A managed CV document with optional language and technology focus.

Examples:

- General CV
- Java / Spring CV
- C# / .NET CV
- Platform engineering CV

For MVP:

- Import already prepared PDF files.
- List available versions.
- Preview or open a PDF.
- Select the CV used for an application.
- Record when it was submitted.
- Preserve the exact submitted file.
- Never replace an application attachment when the source CV changes.

Native CV editing or PDF templating is not required.

### 3.9 Interview round

Represents a planned or completed interaction belonging to an application.

Suggested attributes:

- Application
- Interview type (HR, engineering, culture, technical, final, other)
- Scheduled date/time
- Contacts/participants
- Preparation notes
- Status
- Interview notes
- Prepared questions
- Related professional experiences
- Attachments, recordings, transcripts, analyses

An application can contain multiple interview rounds.

### 3.10 Question library

There are **two logically separate kinds of questions**.

**A. Questions the user wants to ask employers**

Examples:

- What is the salary range?
- How often is office attendance required?
- Can I use a MacBook?
- Are Rider or IntelliJ licenses provided?
- What is the training budget?
- How does a feature move from idea to production?
- Who makes technical decisions?
- How is AI-assisted coding used?

**B. Questions interviewers might ask the user**

Examples:

- Tell me about a conflict with a colleague.
- Describe a situation where you mentored someone.
- How did you handle an unsuccessful technical decision?
- Tell me about a system you owned.
- What would you do differently today?

Suggested fields:

- Question text
- Type: ask employer / prepare answer
- Category
- Language (German or English)
- Suggested interview types
- Intent / what to learn
- Optional follow-up questions
- Tags
- Notes
- Archived flag

The same question should be reusable across companies and interviews.

The answer given by a particular employer belongs to the specific interview/application, not to the global question definition.

### 3.11 Professional experience library

Stores real experiences that can support answers to behavioral and technical interview questions.

Suggested fields:

- Title
- Short memory cue
- Background / situation
- Task or responsibility
- Actions taken
- Result
- Reflection / lessons learned
- Tags and categories
- Optional longer notes

Use STAR as a helpful structure, not a mandatory format for every example.

A professional experience may be linked to multiple interview questions.

For example, one technical conflict might be relevant to questions about leadership, disagreements, communication, autonomy, and lessons learned.

### 3.12 Interview question instance

Connects a reusable question to a particular interview.

Suggested fields:

- Interview round
- Question
- Intended or actual context
- Asked / not asked
- Answer or notes
- Follow-up required
- Display order

This separation is important: the global library holds the reusable question, while the interview instance preserves what happened in a particular conversation.

---

## 4. Main application features

### 4.1 Dashboard

Display practical information:

- Active applications.
- Upcoming interviews.
- Open next actions.
- Recently changed applications.
- Recently added companies and positions.

Do not spend time on analytics or complex visualizations in MVP.

### 4.2 Company and position management

Provide straightforward CRUD interfaces with search, filtering, and links between entities.

Allow manual entry of companies and positions.

Support preserving job descriptions by pasting text or importing a file.

Web lookups, extraction, domain categorization, and automated vacancy discovery are deferred.

### 4.3 Application management

Each application page should present:

- Company and position.
- Application status.
- Dates.
- Submitted documents and CV.
- Contacts.
- Timeline.
- Next actions.
- Interview rounds.
- Notes.

Avoid requiring unnecessary fields before a record can be saved.

### 4.4 Document management

Support importing and viewing common file types.

For MVP, prioritize PDF, TXT, Markdown, audio, and common email export formats as opaque attachments.

Full email parsing and indexing can come later.

Allow users to associate existing imported files with multiple entities without duplicating content unnecessarily.

Preserve original files.

---

## 5. Interview Workspace

This is a key feature of Scout.

It is a dedicated view launched from an interview round using a **Start Interview Workspace** action.

The Workspace must support a real interview while keeping information easy to find.

### 5.1 Display requirements

The user has two 32-inch 4K monitors, potentially three.

Typical workflow:

- One display contains the video call.
- Another contains the job description, CV, questions, and notes.
- An additional monitor may be available.

The Workspace should:

- Open in a separate browser window or tab.
- Support wide displays and multi-column layouts.
- Have independently collapsible sections.
- Remember panel visibility and sizing, where practical.
- Provide readable text and efficient keyboard navigation.
- Prioritize rapid lookup rather than dense dashboards.
- Work without depending on AI or network connectivity.

Suggested panels:

1. Position and job description.
2. Submitted CV.
3. Interview-round details.
4. Questions to ask the employer.
5. Expected questions and professional experiences.
6. Notes.

Milestone 4 provides the Workspace without audio setup or consent prompts. Recording status/controls, consent decisions, and audio import/linking are deferred to Milestone 5. Existing generic opaque audio attachments remain available without adding a recording workflow.

Start with a simple CSS grid layout. Drag-and-drop panel arrangement is optional and can be deferred.

### 5.2 Screen-sharing privacy boundary

**Critical requirement: the Interview Workspace must be safe to share on screen without exposing unrelated personal information.**

It must not display:

- Other companies.
- Other applications.
- Other recruiters or contact details.
- Email addresses or phone numbers unless explicitly needed for this interview.
- Notes or assessments about unrelated employers.
- Global Scout navigation exposing unrelated entities.
- Personal or private information unrelated to the current interview.

Important implementation rules:

- Build the Workspace as an isolated, interview-scoped view.
- Retrieve only data needed for the active interview.
- Avoid loading unrelated entities and hiding them only through CSS.
- Do not display unexpected notifications containing unrelated content.
- Do not include global search results or application summaries.
- Make notes and professional experiences explicitly selectable for Workspace inclusion.
- Use neutral browser titles and labels if useful.
- Provide a clear visual indication that the user is in the interview-specific view.

The currently selected CV and position description are allowed, as they directly concern the interview.

Because some preparation notes or professional experiences may themselves contain sensitive information, **the user must control which panels and entries are visible**. A private preparation section, if added, must not appear in the screen-shareable Workspace.

The goal is to make sharing the Scout Workspace safe, not to make an unconditional guarantee that everything displayed on the user's desktop is safe to share.

### 5.3 Questions to ask the employer

Before an interview, the user selects questions from the reusable library.

Within the Workspace:

- Show selected questions.
- Group or filter them by category.
- Mark questions as asked.
- Record answers or quick notes.
- Keep unanswered questions readily visible.
- Allow ad hoc questions to be added.

Optionally show the reason a question is being asked and suggested follow-ups.

A question answered during an earlier round should retain that answer when referenced later, without automatically modifying the global question library.

### 5.4 Expected questions and answer preparation

The user can select expected questions and associate professional experiences.

During the interview:

- Search questions or experiences quickly.
- Show short memory cues first.
- Expand the complete STAR notes when needed.
- Keep the interface compact and readable.

This feature is intended as a memory aid, not live AI-generated interview coaching.

### 5.5 Notes

The Workspace must allow manual notes throughout an interview.

Manual notes are a first-class source of information, not a fallback with reduced functionality.

Notes should support:

- Free-form text.
- Reliable saving during the interview.
- Optional timestamps.
- Review and editing afterward.
- Later AI analysis, independently of audio.

Ensure note changes are protected against accidental browser refresh or navigation, for example through frequent saving and draft recovery.

---

## 6. Audio recording and consent

**Milestone 5 scope:** the initial audio workflow, all recording-consent functionality, recording metadata, external-audio import/linking, and related storage, size-limit and failure-handling work are deferred from Milestone 4. M4 adds no audio/consent UI, entities, migrations or placeholder services. Native capture remains a separate future improvement, as described in section 6.3 and Milestone 6.

Audio-specific limits, working-directory linking rules, consent persistence and retry behavior must be settled when planning M5; the current generic document limit is unchanged in M4.

Audio is optional, not required for an interview round.

### 6.1 Consent model

Consent is obtained outside Scout.

Scout must never attempt to infer consent or start recording automatically.

Starting in Milestone 5, the audio workflow offers two explicit actions:

**Consent received — use external recorder**

The user confirms that consent has been obtained from all participants, then operates the external recorder separately. Scout records the acknowledgment without claiming capture has started. A future integrated recorder may start only after an explicit user action and only if that integration is available and configured.

**Continue without recording**

The interview proceeds in notes-only mode with no audio capture initiated by Scout.

Both flows support subsequent notes review and AI analysis.

The application must not require a recording in order to mark an interview completed.

Permission to record and permission to transfer data to an external processing service are separate matters.

### 6.2 MVP recording strategy

**MVP must not depend on implementing a native audio recorder.**

The user has already demonstrated that Windows recording software can capture microphone audio and PC playback simultaneously in a simple test.

Initial supported workflow (Milestone 5):

1. Obtain recording consent outside Scout.
2. Record using a suitable external tool.
3. Export an audio file.
4. Import the file into the interview round.
5. Optionally transcribe afterward.
6. Review transcript and/or create manual notes.

Support importing common formats such as MP3, WAV, and M4A where the chosen transcription tool supports them.

Allow an existing recording file to be linked from the configured working directory.

Scout should not claim to control external recording software unless an actual integration has been implemented.

### 6.3 Future native recording

A later Windows recorder may use an appropriate library such as NAudio.

Potential capabilities:

- Select microphone device.
- Select playback device.
- Capture both sides of the call.
- Keep separate tracks where practical.
- Show input levels.
- Start / stop recording.
- Surface recording failures.
- Import saved output into the interview round.

This is a separate subsystem and must not become a dependency of company, application, document, or interview preparation functionality.

### 6.4 iPhone interviews

No iOS application is planned.

The user can obtain a recording through an appropriate external method where consent and platform capabilities permit it.

Scout only needs to import the resulting file.

---

## 7. Transcription and AI processing

Three stages must remain independent.

### Stage A: Source acquisition

Accept:

- Manually entered interview notes.
- Imported plain-text transcript.
- Imported audio recording.
- Optional future native recorder output.

### Stage B: Speech-to-text

Optional.

Convert supported audio into a transcript using a local or explicitly configured transcription provider.

For the initial implementation, a command-line transcription tool such as whisper.cpp is a plausible option.

Desirable transcript properties:

- Full text.
- Optional timestamps.
- Optional speaker labels, when available.
- Reference to the original audio.
- Processing status and errors.

Speaker identification must not be treated as reliable unless supported and validated.

Preserve the original audio and the transcript separately.

Transcription must be retryable without modifying the recording.

### Stage C: AI analysis

Optional and independent from speech-to-text.

Input can be a transcript **or manual notes**.

Possible outputs:

- Interview summary.
- Questions asked.
- Answers provided.
- Confirmed or reported facts.
- Salary and remote-work information.
- Follow-up actions.
- Concerns or uncertainties.
- Potential new entries for the question library.
- Suggestions for refining professional-experience answers.

AI analysis is supplementary and must not block manual application management.

### Data provenance

AI-generated conclusions should preserve links to their source.

Where timestamps exist, reference them. Where only manual notes exist, link back to the note.

Separate:

- Source material.
- AI suggestions.
- User-approved structured facts.

Never silently promote an AI inference into a verified company fact.

Avoid presenting subjective sentiment analysis as reliable evidence of an interviewer's intentions.

### Remote AI providers

Remote provider support is deferred until privacy and configuration are settled.

No recording or transcript should be uploaded to a remote service without a separate explicit processing action and suitable authorization.

No external provider should be enabled by default.

---

## 8. Data persistence and file safety

### Database

Use a single local SQLite database through EF Core.

Support migrations.

Prefer simple relational modeling with explicit relationships.

Do not prematurely introduce multiple databases, event sourcing, or distributed messaging.

### Assets

Use a configured root directory, for example:

`D:\ScoutData`

A possible structure:

- `database/`
- `documents/`
- `cvs/`
- `audio/`
- `transcripts/`
- `imports/`
- `exports/`

The exact folder structure is an implementation detail.

### Preservation requirements

- Do not delete files from the working directory.
- Do not silently overwrite existing files.
- Prefer immutable imported originals.
- Use unique stored filenames or generated identifiers.
- Preserve the original filename in metadata.
- Allow records to be archived rather than destructively deleted.
- Do not implement automatic filesystem cleanup.
- Clearly distinguish deleting a database association from deleting an actual file.
- Leave source recordings intact after transcription or analysis.

Consider simple database and asset backups, but no backup service is required initially.

Use safe file-path handling and ensure imported filenames cannot escape the configured asset directory.

---

## 9. MVP scope

The first useful release should support:

### Company and vacancy management

- Create, edit, view, archive, and search companies.
- Create, edit, view, archive, and search positions.
- Link positions to companies.
- Preserve vacancy descriptions.

### Application tracking

- Create applications for positions.
- Track status, dates, and notes.
- Associate contacts.
- Show chronological events.
- Track next actions.
- Create interview rounds.

### Document and CV management

- Import and list documents.
- Attach documents to relevant records.
- Import prepared CV PDFs.
- Select a submitted CV for each application.
- Preserve the specific submitted CV.
- View or open associated files.

### Interview preparation

- Maintain questions to ask employers.
- Maintain expected interview questions.
- Maintain professional experience/STAR examples.
- Categorize and search entries.
- Prepare a set of questions and experiences for an interview.

### Interview Workspace

- Open a dedicated screen-shareable view.
- Show interview-specific position details and CV.
- Show prepared questions.
- Mark questions asked and record answers.
- Search prepared experience examples.
- Take and save notes.
- Support a notes-only interview.

### Audio and consent (Milestone 5)

- Explicit acknowledgment of externally obtained recording consent.
- Continue with manual notes without requiring audio.
- Import externally recorded audio or link a recording from the configured working directory.
- Preserve original files and handle unavailable sources and import failures.

### Explicitly not required for initial usability

- Native audio capture.
- Automated speech-to-text.
- AI summaries or chat.
- Automatic job discovery.
- Web scraping.
- CV generation.
- Cover-letter generation.
- Email integration.
- Calendar integration.
- Mobile applications.

---

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

---

## 11. UX guidelines

Scout is a personal tool, not a product for general distribution.

Prioritize:

- Fast navigation.
- Efficient keyboard interactions.
- Readable information hierarchy.
- Clear entity relationships.
- Minimal modal dialogs.
- Searchable catalogs.
- Meaningful default sorting.
- Good use of large desktop displays.
- Consistent visual design.
- Avoiding unnecessary fields and workflows.

The standard Scout interface may show sensitive personal information.

The Interview Workspace must use an explicitly restricted presentation model.

Do not expose unrelated personal data through hidden menus, tooltips, autocomplete, notifications, or accidentally prefetched records.

There is no need to support mobile layouts beyond basic browser functionality.

---

## 12. Engineering guidance for Codex

### General expectations

- Follow established ASP.NET Core and EF Core conventions.
- Prefer straightforward domain-oriented organization over excessive abstractions.
- Implement useful vertical slices.
- Keep business logic out of UI components when it becomes nontrivial.
- Use dependency injection for genuine boundaries, such as file storage and transcription providers.
- Avoid creating generic repositories over EF Core unless a concrete need exists.
- Introduce interfaces at meaningful external boundaries rather than for every class.
- Avoid speculative extensibility.
- Use current supported dependencies and verify their documentation.
- Keep code readable and maintainable.
- Handle database, file, and import failures gracefully.

### Tests

Prioritize tests for:

- Entity relationships and persistence.
- Application status transitions where applicable.
- Correct association of CV versions with submissions.
- File import and path safety.
- Preservation of documents and recordings.
- Interview-scoped data retrieval.
- Separation of notes, transcripts, and AI-generated information.
- Prevention of unintended external communication.

Avoid elaborate test infrastructure for trivial CRUD components.

### Implementation process

Codex should:

1. Read this specification.
2. Inspect the repository and identify existing implementation state.
3. Propose the smallest next milestone or vertical slice.
4. Explain material architectural decisions and tradeoffs.
5. Implement the agreed slice.
6. Build and run relevant tests.
7. Report what works, what remains incomplete, and any limitations.
8. Update project documentation when design decisions change.

Do not implement the entire roadmap in a single pass.

Do not silently replace agreed requirements with assumptions.

---

## 13. Important constraints

These requirements take precedence over convenience or feature expansion.

1. **Never automatically send applications, emails, messages, or form submissions.**
2. **Never automatically record an interview.**
3. **Interview recording requires explicit initiation after consent has been obtained.**
4. **Notes-only interviews must be fully supported.**
5. **Audio capture, transcription, and AI analysis are separate capabilities.**
6. **Remote AI processing must not happen implicitly.**
7. **The Interview Workspace must exclude unrelated personal information by design.**
8. **Original files and recordings must not be deleted or silently overwritten.**
9. **The application must remain useful without AI or external services.**
10. **Simplicity and immediate utility outweigh architectural sophistication.**

---

## 14. Definition of success

Scout is successful when the user can:

- Maintain a list of interesting employers and their positions.
- Track actual applications, contacts, interviews, next actions, and outcomes.
- Retrieve the exact CV and documents submitted for a position.
- Prepare interview questions and professional examples in an organized library.
- Open an interview-specific workspace during a video call.
- Find relevant prepared information quickly under interview pressure.
- Take notes without disrupting the interview.
- Import a consented audio recording when available.
- Continue entirely through manual notes when recording is unavailable.
- Preserve and revisit what was learned from previous interviews.
- Use all core functionality locally without maintaining spreadsheets.

**Guiding principle:** Build the smallest version of Scout that genuinely improves the next job application and interview. Expand it based on actual use, not hypothetical possibilities.

