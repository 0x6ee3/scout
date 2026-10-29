# Scout — Domain model

Conceptual entities and relationships for companies, applications, interviews, documents and preparation libraries.

[Documentation overview](../../README.md). Section numbers are local to this document.

## 1. Domain model

This is a conceptual overview, not an exhaustive schema reference. Web v1 implements the workflows in the [user guide](USER_GUIDE.md); actual entities and EF migrations define persistence. The [M3 delivery](../../cycles/scout-web-v1/milestones/MILESTONE_3_DELIVERY.md#delivered-behavior-and-architecture) records the narrower implemented preparation model: employer questions and STAR experiences, without an expected-question catalog, category/language model or question-to-experience graph.

### 1.1 Company

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

### 1.2 Position

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
- Opportunity status: discovered, interested, rejected by user, expired
- Archived flag, independent of opportunity status

A position may exist without an application.

Do not automatically overwrite the original job description when revisiting a URL.

### 1.3 Application

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

### 1.4 Contact

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

### 1.5 Application timeline event

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

### 1.6 Next action

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

### 1.7 Document / attachment

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
- SHA-256 hash for integrity verification; imports are not automatically deduplicated

Multiple attachments per entity are required.

Files must not be silently overwritten or deleted.

### 1.8 CV

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

### 1.9 Interview round

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
- Attachments, recordings and immutable transcript/correction records

Generated analyses are outside Web v1; Scribe M3 plans separate reviewed extraction output.

An application can contain multiple interview rounds.

### 1.10 Question library

Web v1 implements a reusable catalog of **questions the user wants to ask employers**.

Examples:

- What is the salary range?
- How often is office attendance required?
- Can I use a MacBook?
- Are Rider or IntelliJ licenses provided?
- What is the training budget?
- How does a feature move from idea to production?
- Who makes technical decisions?
- How is AI-assisted coding used?

Implemented catalog fields include:

- Question text
- Intent / what to learn
- Optional follow-up questions
- Tags
- Notes
- Archived flag

The same question should be reusable across companies and interviews.

The answer given by a particular employer belongs to the specific interview/application, not to the global question definition.

The original concept also proposed a separate expected-interviewer-question catalog and category/language fields. Those were excluded from the approved M3 scope. Rehearsal prompts belong to STAR experience text; there is no separate expected-question entity or language setting. Free-form German and English content is supported.

### 1.11 Professional experience library

Stores real experiences that can support answers to behavioral and technical interview questions.

Suggested fields:

- Title
- Short memory cue
- Background / situation
- Task or responsibility
- Actions taken
- Result
- Reflection / lessons learned
- Tags
- Optional longer notes

Use STAR as a helpful structure, not a mandatory format for every example.

An experience can be selected for multiple interview rounds, with round-specific rehearsal notes. Selections reference current library content. Likely prompts help rehearse topics such as leadership or disagreements; there is no question-to-experience association graph.

### 1.12 Interview question instance

Preserves a round-specific copy of a selected employer question; ad hoc questions can also be created directly in the round. Later library edits do not rewrite prepared copies, and deleting a reusable definition preserves its interview copies and answers.

Suggested fields:

- Interview round
- Optional source question reference
- Copied wording, intent and follow-up prompts
- Private preparation notes
- Asked / not asked
- Answer or notes
- Follow-up required
- Display order

This separation is important: the global library holds the reusable question, while the interview instance preserves what happened in a particular conversation.

---
