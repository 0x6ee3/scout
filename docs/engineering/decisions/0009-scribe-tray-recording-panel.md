# 0009 — Scribe tray recording panel and separate settings

Date: 2026-10-22. Status: Accepted. Scope: Scout.Scribe v1 M1. Implementation: recording panel and persistent settings implemented; Windows sign-in and interactive hardware checks remain manual acceptance.

## Context

The capture experiment combined configuration, recording controls and diagnostic output in one window shown at launch. The user requested a quiet tray launch and a compact recording surface, using the supplied llama.cpp screenshots as visual direction. Settings should be a conventional window with recording-folder and launch-at-sign-in preferences.

## Decision

- Start idle with only the tray icon. A single left click opens a compact tool window near the pointer, kept within the monitor's working area. Show elapsed recording time and one primary Start/Stop action, with explicit starting, finalizing, saved and failure states. Closing or Escape hides the panel without stopping capture.
- Use dark charcoal surfaces, muted secondary text and a blue primary action. Retain native window controls, keyboard navigation and DPI scaling. Settings uses a separate normal dialog with Save/Cancel; diagnostics remain available in a separate Details window.
- Preserve the tray's recording, folder and exit actions, and give recording and settings separate entries. A second launch opens the existing recording panel. Exit during capture continues to offer stop/finalize before quitting.
- Persist the recording root under local application data; preserve the experiment's default `Documents/Scout.Scribe/recordings`. Folder edits apply to future sessions, are disabled during capture/finalization and never move existing files. Invalid saved settings block recording until the user saves corrected settings.
- Offer opt-in launch at Windows sign-in through the current user's Run registry key. Save applies it; Cancel does not. Automatic launch never starts recording. Moving the executable requires refreshing the startup registration. Windows policy can prevent reading or changing this setting; report that without losing access to the recording-folder preference.

## Alternatives considered

Keeping the experiment's combined window would leave diagnostics and configuration in the everyday recording flow. A larger always-visible main window adds little to a utility controlled mainly through its tray icon. A framework migration is unnecessary for this split; retain Windows Forms and the existing capture stack.

## Consequences

This refines the tray/settings UX in [0004](0004-scribe-recording-baseline.md) and [0005](0005-scribe-audio-durability.md), replacing the experiment's combined settings/status surface. It does not change the audio decisions in [0008](0008-scribe-playback-before-listening-volume.md). Every recording remains explicit, with no automatic resume, mixing or new processing.

Tests cover hidden startup/message-loop exit, panel hiding/reopening, duplicate-start prevention, synthetic recording/finalization, duration formatting beyond 24 hours and settings persistence/validation. Rendered previews check panel and settings layout. Real tray placement, Windows sign-in behavior, high-DPI monitor transitions and microphone/headset acceptance remain interactive checks; synthetic tests do not establish those behaviors.

## References

- [Scribe launch and settings guidance](../../../src/Scout.Scribe/README.md).
- [M1 capture experiment and hardware gates](../../cycles/scout-scribe-v1/milestones/MILESTONE_1_CAPTURE_EXPERIMENT.md).
