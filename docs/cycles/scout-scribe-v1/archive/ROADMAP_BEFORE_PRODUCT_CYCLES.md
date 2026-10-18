# Scout — Roadmap

Milestone goals, deliverables and acceptance criteria. Detailed plans and delivery evidence are indexed in the [documentation overview](../../../README.md#milestone-plans-and-delivery-records).

[Documentation overview](../../../README.md). Section numbers are local to this document.

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

Implemented: see the [M5 delivery summary](../../scout-web-v1/milestones/MILESTONE_5_DELIVERY.md) for package gates, restore evidence and remaining browser verification limits.

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

### Version 1 scope and release sequence

Decision: 16 October 2026. M5 is the functional baseline for Scout.Web v1. There is no further Scout.Web feature milestone 6. Preserve the [M5 delivery record](../../scout-web-v1/milestones/MILESTONE_5_DELIVERY.md), including its verification limits; functional scope completion does not assert that every release check has passed.

The remaining Scout.Web work is a separate UI polish pass: reduce excessive vertical layout, group actions in compact action bars and context menus, use clear icons with accessible names/tooltips, and use focused dialogs where they improve the task. Retain visible primary actions, keyboard access, reliable saving and Workspace privacy. Plan this pass separately; it is not part of recorder implementation.

The final planned capability milestone before calling the combined workflow v1 is M6, split into 6A and 6B below. Complete the recorder work and Web polish, then conduct several real interviews and assess usability before committing to further features. Scout.Web remains usable independently throughout.

### Milestone 6A — Scout.Recorder desktop recording

Goal: Record microphone and computer playback through an independently launched Windows tray application, then manually import the resulting audio into Scout.Web.

Deliverables:

- Separate `Scout.Recorder` executable/project with no dependency on the running Web application or its database.
- Tray start/stop, unmistakable recording/error status and a small device/settings window.
- Explicit start after consent acknowledgment; microphone and playback-device selection, levels and recoverable errors.
- Finalized, preserved audio files compatible with existing Scout.Web import limits.
- Device, long-session, interruption and file-handoff verification, operating instructions and delivery evidence.

Acceptance: With Scout.Web stopped, launch Recorder manually, record both sides of a consented mock interview, stop and find the saved audio. Start Scout.Web and import/play it through the existing recording workflow. Notes-only and other external-recorder workflows still work.

The [M6A plan](MILESTONE_6A_PLAN.md) records the architecture, code-impact assessment, artifact contract, implementation packages and acceptance checks. This is planned work; no executable or application-code changes accompany the plan.

### Milestone 6B — Scout.Recorder local transcription

Goal: Produce an independently usable transcript from a completed recording, then import it into Scout.Web using the existing TXT workflow.

Scope outline: optional local speech-to-text, any necessary conversion of processing copies, status/errors, cancellation/retry, preserved originals and UTF-8 TXT export. Target an opt-in automatic transcription step after recording stops, once configured; capture must remain useful without models or transcription. Support selecting an existing audio file as input. Choose and verify engines, models, hardware requirements and packaging in a separate 6B plan after 6A evidence. whisper.cpp/FFmpeg are candidates, not installed requirements.

Acceptance: Stop a recording, produce and review its local transcript, and import the TXT into an interview without requiring audio registration. A failed transcription preserves audio and can be retried. No AI summary or structured-data promotion is required.

Rough future ideas live in the separate [feature wishlist](../../../FEATURE_WISHLIST.md). They are not milestones or v1 commitments. Revisit them after real interview use.

---
