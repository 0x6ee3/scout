# Scout user guide

Use this guide for implemented application and interview workflows. See the [quick start](../../../README.md#quick-start) to launch Scout, [operations](OPERATIONS.md) for storage and recovery, and the [documentation overview](../../README.md) for requirements and planned work.

## Manage an application

1. Add a company (name required), then a position (company/title required). The first nonempty pasted vacancy description becomes read-only with a capture timestamp. Keep later observations in personal notes.
2. Add an application from a position. Multiple attempts are allowed, with optional labels. Application status is separate from opportunity status and archive state. Creation/status changes append timeline facts in the same commit; status alone does not prove files were sent.
3. Add company contacts and associate them with applications, events, or interviews. The overview deduplicates contacts and labels their origins. Archived contacts remain on existing links; restore them before adding new links.
4. Enter historical events with a date and optional time. Manual events can be corrected/archived without changing application status. System facts link to their owning interview/submission where applicable; add a clarifying note instead of rewriting them.
5. Create next actions for an application or a position before applying. Due dates are optional. Complete/reopen explicitly; archiving does not claim completion. The global catalog keeps open work visible under archived parents and sorts dated work first.
6. Add interview rounds, participants, scheduling, preparation, and manually saved notes. Scheduled requires a time. Historical completion can omit its time, participants, notes, files, and audio. Lifecycle history records when the record changed, without inventing when an interview happened or changing application status.
7. Import documents/prepared CVs. Use **Record externally sent submission**, select the exact files/date, review, then **Record submission**. Status/date-applied changes are explicit options. Historical submissions may have no files. Mark mistakes incorrect with a reason while retaining evidence; record corrections separately.

Application statuses: Planned, Applied, Screening, Interviewing, Offer, Rejected, Withdrawn, Accepted. Any valid transition is allowed. Opportunity statuses remain Discovered, Interested, Rejected by me, Expired. Dates and notes do not force status.

Timed forms label the machine's local zone. Invalid daylight-saving times are rejected; repeated times require an offset choice. Date-only values have no zone conversion.

Application.PositionId, Contact.CompanyId, interview ownership, and action ownership stay fixed. A position's company cannot change after any application exists, including archived attempts. Archive a mistaken record and create one under the correct owner. Other general edits use last successful save wins.

Archive/restore is independent and never cascades. New children require active direct parents; new applications also require active company/position records. Existing active applications remain usable under archived ancestors. Existing files/history remain readable after archival. The only permanent-delete action is for reusable employer questions; their interview-specific copies and answers are preserved.

## Prepare an interview

1. In **Employer questions**, curate reusable wording, intent, suggested follow-ups, private library notes, and tags. Answers belong to interviews, not this catalog.
2. In **STAR experiences**, save a title and any useful Situation, Task, Action, Result, memory cue, reflection, likely prompts, and notes. Only the title is required. These are your own experiences; sample stories are fictional.
3. Tags are lowercase single words or hyphen-separated words, up to 20 per entry and 50 characters per tag. Type a tag and press Enter or Add tag; existing tags from both catalogs appear as suggestions. Spaces/underscores/repeated hyphens are rejected. Badges use consistent colors. Search includes longer notes/STAR fields; tag and archive filters combine with search and persist through back links.
4. From a round, choose **Prepare interview**. Select employer questions and STAR examples independently; checked additions survive picker filtering. Save additions explicitly. Picker search covers question wording or experience title/cue and tags; use the full catalogs to search inside longer content.
5. Use **Edit wording / answer** for a round-specific question copy, answer box, and private preparation notes. Initial selection copies wording/intent/follow-ups, never private library notes. Later library edits do not rewrite prepared copies. The same source can be used in another round with an independent answer.
6. Selected experiences show current library content. Editing STAR text updates all selections; **Edit rehearsal notes** saves private notes for just this round. Library editors explain this distinction.
7. Move up/down saves order. Remove from preparation archives the selection; show removed items to restore it without losing wording/answers/notes. New/restored selections require an active round and source. Existing selections remain editable under archived sources/ancestors; a deleted-source question can still be restored from its round.
8. **Delete question permanently** removes only its reusable definition and tags, after an explicit confirmation showing the number of retained interview copies. Those copies keep their wording, answers, notes, and order with a **Source question deleted** label. There is no recycle bin or implicit merge with a later similar question. Experiences and other records still use archive/restore.

These are private management pages with manual saving and navigation guards. General form drafts do not survive reload or backend shutdown. Workspace live answers and notes have separate autosave and browser draft recovery, described below. There is currently no language setting, category system or separate expected-question catalog.

## Search and forms

Management pages use the desktop width: context at left, primary work in the center and related resources at right where useful. Named breadcrumbs show your place; existing back links retain catalog filters. Sparse pages use fewer groups, and narrower windows reduce columns. The top navigation keeps the same destinations.

Use the action strip below an entity's heading for its main actions, section-heading actions for Add/Manage/Attach, and compact item icons for edit, open, download or unlink. Hover or keyboard focus reveals labels; important workflow actions keep visible text. Archive/restore is in **More**. Removing a file link preserves its bytes and other associations.

Small edits and selectors open in centered dialogs. Save explicitly, then acknowledge the confirmed result with **Done**. Escape/Close on modified input asks whether to keep editing or discard. A failed save keeps the draft and visible error. Application/contact and preparation editors also retain full-page routes; large editors remain pages. Downloads start directly without claiming completion. Workspace autosave continues with quiet save status, without a dialog for each edit.

Catalog search is literal `OrdinalIgnoreCase` substring matching: `MÜNCHEN` matches `münchen`, and `%`/`_` are ordinary characters. Structured filters combine and persist through catalog details/edit/back query links. Full timeline is chronological, untimed entries before timed entries on the same day, then ID; the overview shows newest first. Accent folding, pagination, and full-text indexing are deferred.

Forms validate in UI and operations, block duplicate in-flight saves, retain drafts on recoverable failures, and warn before leaving modified input. Short labels are normally 200 characters, long notes 100,000, URLs 2,048, contact email/phone 320/100, CV language/focus 20/200. Nonblank long text preserves whitespace and displays encoded. General management forms require manual saving and have no durable browser draft recovery. Workspace live answers and notes use the saving and recovery workflow below; private answer/note editors also reject stale revisions.

## Files and submission evidence

Imports accept **one file up to 1 GiB for MP3/WAV/M4A audio**, or **50 MiB for PDF, TXT, MD, EML and MSG**. Transcript import has the smaller limit below. PDFs require a PDF header. Imported originals are preserved; see [storage and file recovery](OPERATIONS.md#file-imports-and-recovery) for failure handling.

One document can attach to multiple companies, positions, applications, events, and interviews. **Remove link** removes only that association. Bytes, path, hash, size, name, and import facts stay fixed; description/category/archive are editable. Changing the source does not change Scout's copy. Changed bytes require a new import/CV version.

Each prepared CV represents one imported PDF. Submission files reference immutable documents and preserve the confirmed labels. Later renaming, new versions, or archival do not change earlier evidence. The same confirmation identity/details returns its existing submission on retry; different details require a new form. Nothing is transmitted.

PDF preview uses the native browser viewer with download fallback. Native rendering was not confirmed in the in-app browser (blank surface); use **Download original** if needed. Generic text/Markdown document preview remains encoded plain text, capped at **256 KiB** with a truncation notice. Email and generic audio attachments offer downloads; registered interview recordings also have private players. Missing or changed files produce recoverable errors; Scout never substitutes files. There is no transcription, audio conversion, mail processing, deep PDF inspection or malware scanner.

## Recordings and private transcript review

Interview details offer **Continue without recording** and **Consent received — use external recorder**. Both append dated decisions; neither starts capture or blocks notes/completion. Optional explanatory notes and decision history are private. An acknowledgment records the time entered in Scout; it does not establish when historical consent was obtained.

To register audio, choose exactly one upload, existing audio attachment belonging to this round, or relative path beneath `<Scout root>/recordings/` (for example `interview/sample.wav`). Confirm that externally obtained consent covers that selected file, then select **Register recording**. Existing attachments are reused without copying. File presence alone never supplies consent. Archived rounds remain readable but must be restored before adding review material.

Registered audio has native play/pause, seek and volume controls on private interview details, including completed rounds. Nothing autoplays. Playback needs the running backend and a browser supporting the actual codec. A failure message offers guidance; **Download verified recording** remains usable when verified bytes are readable even if the browser cannot decode them. Unavailable files have no download link; if a file changes after the page loads, the endpoint still rejects it. Reopen the interview after restoring bytes. Changed bytes require new registration and acknowledgment. Metadata archival never deletes files or earlier decisions.

Import an externally prepared **UTF-8 TXT transcript**, at most **5 MiB and 1,000,000 decoded characters**, without needing a recording, consent choice or provider. An optional source recording must belong to this round. Open the transcript to read it, download the original TXT, or **Save corrected copy**. Each correction has a parent link and retains the original-file association; earlier text remains unchanged. Large corrections use an ordinary HTTP form, without sending the textarea through Workspace autosave. If submission fails, use browser Back to recover text and retry the same form; check the list before starting another copy. Private forms do not have Workspace draft recovery.

Long notes, answers, preparation, STAR fields, position descriptions and transcripts support basic Markdown: headings, paragraphs/line breaks, lists, bold/italic, blockquotes and literal code. Links/images display inert labels without fetching destinations. Raw HTML is text and Mermaid fences are code. Use **Plain-text view** to recover literal punctuation. Existing editors still edit source text; **Preview formatting** or **Preview current source** renders only on request. Formatting does not rewrite stored text. Excessive/complex input falls back to complete plain text.

## Interview Workspace

From an interview's details or preparation, choose **Start Interview Workspace**. The private review starts with nothing selected and replaces the previous approval. Review exact text and the submitted CV before approving; source changes during review require reloading and reviewing again. Close old Workspace tabs before changing visibility. After approval, use **Open Interview Workspace** in a separate tab/window.

Only approved content appears in the Workspace. Position/description, prepared wording, selected STAR fields and attributed earlier answers are reviewed copies; refresh them through private review. Enabled live answers/interview notes remain visible as their contents change. Preparation/rehearsal/library notes are excluded unless explicitly included. Selected submitted CV bytes are verified and never replaced by a newer CV. Use **Download submitted CV** if native PDF preview is blank.

Mark asked/follow-up state independently of answer text. Add ad hoc questions directly to this round. Search/tag matching and asked/unanswered/follow-up filters cover included entries only. Collapsing panels and choosing Balanced/Notes wide affect layout, not permissions. An optional, default-off recording-choice panel can be approved privately. It shows only a narrow status and commands, never recordings, players, transcripts, consent notes or decision history.

### Saving, conflicts and draft recovery

Live edits store browser drafts on input, autosave after 750 ms idle (with a 3-second attempt interval during continuous typing), and save on blur or **Save now**. Ctrl+S saves live answer/note changes; ad hoc creation still requires **Add question**. **Draft stored in this browser** is distinct from **Saved to Scout**. A stale save shows both versions and requires an explicit decision. Private question/interview editors also reject stale answer/note revisions; copy retained form text before reloading.

After refresh or backend restart, recover current-review drafts with **Restore draft**, **Keep saved version** or **Copy draft**. Copy controls also offer a text download. Recovery of excluded/older-review drafts is available only on the private review page: copy wanted text into the private interview/preparation editor, save, then explicitly approve its inclusion. Drafts are separated by Workspace, round, review, target and browser tab. They are not automatically expired.

Browser drafts require completed IndexedDB writes in the same browser profile/origin/port and can be lost when browser data is cleared or evicted. **Draft stored in this browser** does not mean **Saved to Scout**. General management forms remain manual-save forms. See [browser drafts and reconnecting](OPERATIONS.md#browser-drafts-and-reconnecting) for availability limits.

Old or offline Workspace tabs may retain previously approved content. Close old tabs before changing visibility. Screen-share privacy covers the Workspace view, not other desktop windows. Archived rounds reopen read-only.

## Planned capabilities

Web milestones 1–6 are implemented; the [release baseline](../../cycles/scout-web-v1/RELEASE_SUMMARY.md) records verification and remaining physical-display acceptance. The independent [Scout.Scribe v1 cycle](../../cycles/scout-scribe-v1/README.md) has an implemented M1 recording experiment and tray/settings, with remaining reliability and hardware acceptance pending. Local transcription and fact extraction/review remain planned for Scribe M2–M3. Existing audio and TXT imports remain the handoff. See the [portfolio roadmap](../../ROADMAP.md) and [feature wishlist](../../FEATURE_WISHLIST.md), which carries no delivery commitments.
