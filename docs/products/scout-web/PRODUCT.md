# Scout — Product vision and features

Product vision, application features, Interview Workspace, recording workflows, MVP scope, UX and success criteria.

[Documentation overview](../../README.md). Section numbers are local to this document.

**Status:** Web v1 M1–M6 implemented; physical-display acceptance remains open. See the [release baseline](../../cycles/scout-web-v1/RELEASE_SUMMARY.md) and [user guide](USER_GUIDE.md) for delivered behavior.

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

## 2. Main application features

### 2.1 Dashboard

Display practical information:

- Active applications.
- Upcoming interviews.
- Open next actions.
- Recently changed applications.
- Recently added companies and positions.

Do not spend time on analytics or complex visualizations in MVP.

### 2.2 Company and position management

Provide straightforward CRUD interfaces with search, filtering, and links between entities.

Allow manual entry of companies and positions.

Support preserving job descriptions by pasting text or importing a file.

Web lookups, extraction, domain categorization, and automated vacancy discovery are deferred.

### 2.3 Application management

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

### 2.4 Document management

Support importing and viewing common file types.

For MVP, prioritize PDF, TXT, Markdown, audio, and common email export formats as opaque attachments.

Full email parsing and indexing can come later.

Allow users to associate existing imported files with multiple entities without duplicating content unnecessarily.

Preserve original files.

---

## 3. Interview Workspace

This is a key feature of Scout.

It is a dedicated view launched from an interview round using a **Start Interview Workspace** action.

The Workspace must support a real interview while keeping information easy to find.

### 3.1 Display requirements

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
5. Professional experiences and rehearsal prompts.
6. Notes.

M4 introduced the Workspace; M5 added private recording registration/playback and an optional approved recording-choice panel. Recordings, transcripts and consent history remain outside the shared Workspace. See the [user guide](USER_GUIDE.md#interview-workspace) for the implemented boundary.

Start with a simple CSS grid layout. Drag-and-drop panel arrangement is optional and can be deferred.

### 3.2 Screen-sharing privacy boundary

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

### 3.3 Questions to ask the employer

Before an interview, the user selects questions from the reusable library.

Within the Workspace:

- Show selected questions.
- Search and filter prepared entries by tags and interview state; there is no category model.
- Mark questions as asked.
- Record answers or quick notes.
- Keep unanswered questions readily visible.
- Allow ad hoc questions to be added.

Optionally show the reason a question is being asked and suggested follow-ups.

A question answered during an earlier round should retain that answer when referenced later, without automatically modifying the global question library.

### 3.4 Expected questions and answer preparation

The user can select professional experiences and use their likely-prompt and STAR fields to prepare answers. Web v1 has no separate expected-question catalog or question-to-experience association graph; see the [M3 delivery scope](../../cycles/scout-web-v1/milestones/MILESTONE_3_DELIVERY.md#delivered-behavior-and-architecture).

During the interview:

- Search questions or experiences quickly.
- Show short memory cues first.
- Expand the complete STAR notes when needed.
- Keep the interface compact and readable.

This feature is intended as a memory aid, not live AI-generated interview coaching.

### 3.5 Notes

The Workspace must allow manual notes throughout an interview.

Manual notes are a first-class source of information, not a fallback with reduced functionality.

Notes should support:

- Free-form text.
- Reliable saving during the interview.
- Optional timestamps.
- Review and editing afterward.
- Later AI analysis, independently of audio.

Ensure note changes are protected against accidental browser refresh or navigation, for example through frequent saving and draft recovery.

### 3.6 Simple Markdown for long text (Milestone 5)

Long-text read views must support simple Markdown: paragraphs, headings, ordered/unordered lists, bold/italic, blockquotes and literal inline/fenced code. Apply this to interview notes/answers, transcripts, preparation wording/notes, STAR fields and position descriptions. Keep a plain-text view and ordinary source-text editing with a formatted preview; preserve exact stored strings and original imported files.

No rich-text editor, raw HTML, executable content, remote resource loading or complex extensions such as Mermaid are required. Render links/images as readable inert labels/alt text in the initial implementation. Code fences, including Mermaid, remain escaped text.

Formatting must preserve existing limits, autosave, draft recovery and screen-share boundaries. Workspace formatting uses only its already approved text projections; private transcripts, recording history and playback stay outside the shared Workspace.

---

## 4. Audio recording and consent

**Revised scope (16 October 2026):** M5 delivers Scout.Web's recording management, consent, audio import/linking, playback and manual transcript review. [Web v1 M6](../../cycles/scout-web-v1/milestones/MILESTONE_6_PLAN.md) is the UI overhaul that closes the intended Web 1.0 cycle. Native capture, local transcription and local fact extraction/review belong to the independent [Scout.Scribe v1 cycle](../../cycles/scout-scribe-v1/README.md), with its own milestones 1–3. No Scribe implementation is required for Web v1.

Delivered audio limits, linking, consent and retry behavior are described in the [user guide](USER_GUIDE.md#recordings-and-private-transcript-review) and [M5 delivery summary](../../cycles/scout-web-v1/milestones/MILESTONE_5_DELIVERY.md). The generic document limit was unchanged in M4.

Audio is optional, not required for an interview round.

### 4.1 Consent model

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

### 4.2 MVP recording strategy

**MVP must not depend on implementing a native audio recorder.**

The user has already demonstrated that Windows recording software can capture microphone audio and PC playback simultaneously in a simple test.

Initial supported workflow (Milestone 5):

1. Obtain recording consent outside Scout.
2. Record using a suitable external tool.
3. Export an audio file.
4. Import the file into the interview round.
5. Play the saved recording from the private interview-round details using media controls.
6. Review manual notes and/or an imported text transcript.

Optional local transcription is planned in Scout.Scribe v1 M2, followed by local fact extraction and review in Scribe M3. These are separate from Web's release scope.

Support importing common formats such as MP3, WAV, and M4A. Browser playback depends on the actual codec/container; an unsupported format must have useful feedback and a verified download option. M5 does not require conversion software.

Allow an existing recording file to be linked by a relative path beneath `<Scout root>/recordings/`.

Scout should not claim to control external recording software unless an actual integration has been implemented.

### 4.3 Scout.Scribe desktop companion

Scout.Scribe is an independent Windows desktop capture experiment, primarily operated through the tray. Its [product specification](../scout-scribe/PRODUCT.md) and [Scribe M1 plan](../../cycles/scout-scribe-v1/milestones/MILESTONE_1_PLAN.md) distinguish implemented recording/tray/settings from remaining reliability and hardware acceptance. Consent is obtained outside Scribe; recording always starts explicitly. Later milestones plan local processing and review.

Current .NET recording experiment:

- Resolve the Windows-default microphone on each Start, without an in-app selector.
- Capture application playback across output devices before listening-volume attenuation, excluding Scribe's process tree.
- Capture both sides of the call.
- Export separate microphone and playback MP3s; no mixing or gain compensation.
- Show recording state and diagnostics in the tray and recording panel, with separate persistent settings.
- Start / stop recording.
- Surface recording failures.
- Manually import saved output into the interview round using existing Web controls.

Scribe runs without Scout.Web and does not access its database. Scout.Web remains usable with Windows tools, Audacity or any compatible audio/transcript producer. Browser control is deferred. Scribe M2 will add optional local transcript export to the existing UTF-8 TXT import boundary. Scribe M3 produces a separate reviewed result; automatic structured Web updates require later integration design.

The independent [Scribe.js Electron experiment](../../../src/Scribe.js/README.md) additionally supports a pinned microphone and combined output. Those experiment-specific choices do not change the .NET capture baseline.

### 4.4 iPhone interviews

No iOS application is planned.

The user can obtain a recording through an appropriate external method where consent and platform capabilities permit it.

Scout only needs to import the resulting file.

### 4.5 Saved recording playback (Milestone 5)

After an interview has been completed and audio uploaded/registered, show media controls directly on its private interview-round details. Support user-initiated play/pause, seeking, elapsed/duration and browser-supported volume controls. Label multiple recordings clearly; never autoplay. Playback may also be used privately to verify an upload before completion, without changing interview status.

Use native browser audio controls and verified range-capable file streaming. A missing, changed or corrupt source must be reported without playing different bytes under the old recording metadata. Preserve the original file; handle unsupported browser codecs with clear feedback and a verified download option. Playback requires the local Scout server to be running. Native capture, audio conversion and waveform editing are not prerequisites.

---

## 5. MVP scope

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
- Maintain professional experience/STAR examples, including likely prompts for rehearsal.
- Tag and search entries; no separate expected-question catalog or category/language model is implemented.
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
- Import externally recorded audio or register a relative path beneath `<Scout root>/recordings/`.
- Preserve original files and handle unavailable sources and import failures.
- Play saved recordings directly on private interview-round details, including after completion, using native media controls.
- Import and privately review text transcripts, retaining originals and corrected copies.
- Render simple Markdown in long notes/transcripts and other long-text read views, with plain-text access.

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

## 6. UX guidelines

Scout is a personal tool, not a product for general distribution.

Prioritize:

- Fast navigation.
- Efficient keyboard interactions.
- Readable information hierarchy.
- Clear entity relationships.
- Central modal dialogs for focused tasks, confirmations and confirmed explicit-save outcomes; unobtrusive status for live autosave and repetitive interactions.
- Searchable catalogs.
- Meaningful default sorting.
- A 4K-first desktop composition, using three complementary columns on information-rich views while keeping text readable.
- Consistent visual design.
- Avoiding unnecessary fields and workflows.

**Web v1 M6 direction (16 October 2026):** keep the existing top navigation. Under it, provide visible entity breadcrumbs, title/status and a compact action strip. Arrange related work side by side on the user's 4K display; do not let a 900p/Full HD baseline constrain the main layout. Use consistent generic-action icons with accessible names/tooltips, visible labels for important actions and local action groups at section/item level. Open small editing/linking tasks and confirmations in central dialogs rather than below long forms. Preserve full-page routes for substantial editors and native browser reload/tab-close protection where required.

Explicit save outcomes must reflect confirmed results. Field guidance stays near fields, failed edits retain drafts, and routine Workspace autosave must not repeatedly interrupt the interview with dialogs. Existing archive/unlink actions retain their meaning; no permanent file deletion is introduced. Dialogs and breadcrumbs in the Workspace must respect its approved projection.

See the [M6 delivery](../../cycles/scout-web-v1/milestones/MILESTONE_6_DELIVERY.md) and [ADR 0003](../../engineering/decisions/0003-web-presentation-patterns.md) for the implemented composition and verification limits. Physical-display acceptance remains open; Scribe implementation is underway independently.

The standard Scout interface may show sensitive personal information.

The Interview Workspace must use an explicitly restricted presentation model.

Do not expose unrelated personal data through hidden menus, tooltips, autocomplete, notifications, or accidentally prefetched records.

There is no need to support mobile layouts beyond basic browser functionality.

---

## 7. Definition of success

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
