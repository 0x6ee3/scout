# Scout.Scribe — Product

Status: recording experiment, quiet single-instance tray startup, compact recording panel and persistent settings are implemented. Real-device acceptance, Windows sign-in acceptance and remaining M1 reliability work are pending; M1 is not yet delivered. No transcription/extraction features are implemented. This living document describes the .NET product; consult the [Scribe v1 cycle](../../cycles/scout-scribe-v1/README.md) for milestone scope and evidence.

The independent [Scribe.js Electron experiment](../../../src/Scribe.js/README.md) has its own runtime, settings and verification. Its pinned microphone and combined-output choices do not change the .NET baseline; see [ADRs 0010–0012](../../engineering/decisions/README.md).

Scout.Scribe turns consented conversations into reviewable local material: recorded audio, transcripts and proposed facts. It targets the latest generally available Windows 11 release only, primarily through the tray with small settings dialogs and later processing review windows as needed. Older Windows versions are unsupported; a possible macOS application is separate. The name covers the complete workflow rather than capture alone.

## Capabilities by milestone

| Scribe v1 milestone | Intended outcome |
|---|---|
| M1 — Recording | Explicitly start/stop default-microphone and application-playback capture, see clear tray status, preserve playable audio and handle application interruption. |
| M2 — Local transcription | Turn a completed or externally supplied recording into a reviewed, portable UTF-8 transcript using local speech-to-text. |
| M3 — Fact extraction and review | Process a transcript or manually supplied text with a local LLM; inspect, correct, reject or approve source-linked proposals and save a separate reviewed result. |

Capture works without transcription or LLM tools. Transcription accepts existing audio; extraction accepts existing text. Launch never starts recording or downloads models. Consent is obtained outside Scribe before Record, with no prompt or acknowledgment storage. The tray distinguishes recording from idle. Each Start resolves Windows' default microphone; Stop and Start after changing it. Playback uses application loopback before Windows listening-volume processing, across all output devices, excluding Scribe and its descendants. No device selector, inverse-volume gain or normalization is applied. The current experiment saves separate `microphone.mp3` and `playback.mp3` files in a timestamped session directory. Failure preserves partial outputs; the user restarts manually into a new session. Later local processing is explicitly selected or enabled through an opt-in post-stop transcription setting. See [ADR 0008](../../engineering/decisions/0008-scribe-playback-before-listening-volume.md).

M1 uses .NET 10 Windows Forms on Windows x64, NAudio native-format capture and independent Windows Media Foundation conversion to two mono MP3s at 256 kbps each. Typical 60–90 minute interviews produce roughly 230–346 MB total before small overheads. Continuously write with five-second checkpoints for both files; committed audio must remain playable after an application crash without repair, with the remaining tail measured during implementation. Crash handlers attempt cleanup but are not the preservation mechanism. Whole-system crash, blue-screen and power-outage recovery are outside M1. See the [audio durability decision](../../engineering/decisions/0005-scribe-audio-durability.md), [capture/default-device successor](../../engineering/decisions/0006-scribe-default-devices-and-capture-stack.md) and [native/separate-output decision](../../engineering/decisions/0007-scribe-native-separate-capture.md).

## Relationship with Scout.Web

Scribe runs independently of Web and never needs its database or interview IDs. Web remains usable with other audio/transcript tools or manual notes alone. Start with manual import of finalized audio and UTF-8 TXT through Web's existing workflows; the user selects the interview and any optional recording association there. Web retains its per-file recording acknowledgment; Scribe does not collect one. Integration and interview-aware recording reminders are unplanned.

LLM proposals are not verified facts. The initial result is a separate reviewed text artifact; the user can save it or copy approved information into Web. Automatic structured transfer, browser control and richer provenance ingestion require later integration design.

## Boundaries

Preserve original audio and text, expose failures clearly and retain available recovery data. Keep sources, model output and reviewed results distinct. No implicit remote processing, automatic promotion of claims, source overwrite or dependence on Web's release schedule. Additional operating systems, browser integration and advanced features remain in the [wishlist](../../FEATURE_WISHLIST.md).

See the [technical design](TECHNICAL.md), [cycle roadmap](../../cycles/scout-scribe-v1/ROADMAP.md) and [recording plan](../../cycles/scout-scribe-v1/milestones/MILESTONE_1_PLAN.md) for proposed implementation and acceptance details.
