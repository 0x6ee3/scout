# Scout — Technical design and engineering

Technology decisions, transcription and AI processing, persistence, file safety, engineering guidance and governing constraints.

[Documentation overview](../../README.md). Section numbers are local to this document.

## 1. Technology decisions

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
| Audio recording | External tools today; independent Scout.Scribe tray executable planned in Scribe v1 M1 |
| Transcription | Optional local speech-to-text in Scout.Scribe, planned in Scribe v1 M2 |
| AI analysis | Local fact extraction and review planned in Scribe v1 M3; no Web engine required |

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
- Future processing executable/model settings belong to Scout.Scribe, not Web's v1 configuration.

Use configuration files or environment variables as appropriate. Do not embed API keys in source code, database records, or committed configuration.

---

## 2. Transcription and AI processing

**Revised product boundary (16 October 2026):** Scout.Web v1 contains implemented M1–M6; the [release baseline](../../cycles/scout-web-v1/RELEASE_SUMMARY.md) records checks and remaining physical-display acceptance. [Scout.Scribe v1](../../cycles/scout-scribe-v1/README.md) independently plans M1 recording, M2 local transcription and M3 local fact extraction and review. No capture engine, model configuration, processing worker or generated-analysis schema is required in Web. Automatic structured promotion into Web remains a [wishlist idea](../../FEATURE_WISHLIST.md).

M5 provides recording management/playback, manual TXT transcript import and preserved corrected copies, plus simple Markdown reading. It introduces no processing workers, provider configuration, run tables or generated-analysis records.

The stages below remain independent. Web accepts portable audio and TXT artifacts through its existing manual imports; it does not depend on Scribe or a processing provider. There is no new network API or shared database in Scribe M1. The single-database guidance below applies to Web; Scribe owns separate settings and files. See [Scribe's technical design](../scout-scribe/TECHNICAL.md) for processing ownership.

### Stage A: Source acquisition

Accept:

- Manually entered interview notes.
- Imported plain-text transcript.
- Imported audio recording.
- Scout.Scribe output once Scribe M1 is implemented, or output from other recording tools.

### Stage B: Speech-to-text

Optional.

In Scribe M2, Scout.Scribe will convert selected local audio into a transcript using a local speech-to-text engine. Export strict UTF-8 TXT compatible with Web's existing import; audio registration remains optional. Capture must work without processing tools.

whisper.cpp and FFmpeg conversion of a separate processing copy are candidates, subject to the later Scribe M2 plan and verification of current versions, hardware and Windows behavior. Preserve original audio. Aim for opt-in transcription after recording stops, plus existing-file input, with visible status, cancellation and retry.

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

Scribe v1 M3 plans local fact extraction and review; it is not part of Web v1. Input can be a transcript **or manually supplied interview text**. A local runtime such as Ollama is a candidate to evaluate in its detailed plan. Preserve source references and save a separate reviewed output. The broader outputs below are possibilities, not an approved M3 field list. Automatic structured Web updates remain outside this baseline.

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

## 3. Data persistence and file safety

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

## 4. Engineering guidance for Codex

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

## 5. Important constraints

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
