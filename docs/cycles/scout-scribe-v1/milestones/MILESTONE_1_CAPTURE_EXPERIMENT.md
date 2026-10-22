# Scribe M1 package 1 — Capture experiment

Date: 22 October 2026. Status: separate-output experiment and tray shell implemented. Application-loopback playback volume independence is verified on LG/AMD hardware and decoded MP3s; broader hardware acceptance remains open. This is not M1 delivery. Saved settings, further reliability verification and delivery remain in the [M1 plan](MILESTONE_1_PLAN.md). [ADR 0008](../../../engineering/decisions/0008-scribe-playback-before-listening-volume.md) supersedes endpoint-only playback in the earlier native/separate-output experiment.

## Build and launch

Requires Windows 11 x64, .NET 10 SDK to build and .NET 10 Desktop Runtime to run a copied build folder. NAudio is pinned to **3.3.0** through NuGet. Encoding uses Windows Media Foundation; FFmpeg is not required. No Web process, database, model, server or device configuration inside Scribe is required.

From the repository root:

```powershell
dotnet restore Scout.slnx
dotnet build src/Scout.Scribe/Scout.Scribe.csproj --no-restore
& .\src\Scout.Scribe\bin\Debug\net10.0-windows\win-x64\Scout.Scribe.exe
```

The experiment opens a small window, idle, and registers a tray icon. It does not start capture on launch. Select the output folder if desired, then explicitly click **Start recording** in the window or tray. Settings are not yet persisted. The tray uses a **gray square** when idle/starting/finalizing and a **red circle** while recording, with textual tooltip status. Double-click the icon or choose Show settings/status to restore the window. Closing the window hides it while the tray continues running. Choose **Exit** in the tray menu to quit; during capture it offers stop/finalize first. A second interactive launch signals the existing window instead of creating competing capture. Windows may put the icon under the taskbar's overflow arrow; drag it to the visible taskbar area if desired. Graceful exit removes it; abrupt process death can still leave a stale icon.

Default output is `Documents/Scout.Scribe/recordings/<timestamp>_<unique suffix>/`:

- `microphone.mp3`: microphone only, 48 kHz mono MP3 at 256 kbps.
- `playback.mp3`: application audio across output devices before listening-volume processing, excluding Scribe and its descendants; same MP3 format. No microphone mixing or gain compensation.
- `events.jsonl`: actual device names/IDs, native formats, output names, start/environment information, per-file checkpoint byte counts/flush durations, native packet/queue statistics and completion/failure. A final `completed` event distinguishes normal finalization. Files may be unfinalized after forced termination. The journal contains no consent acknowledgment.

Each Start resolves the current normal Windows default microphone, captures its native mix format and retains that endpoint. Stop and Start after changing the microphone default. Playback uses `WithProcessLoopback` with Scribe's process tree excluded. It is independent of the default render endpoint and includes application audio routed to other outputs. The virtual loopback device has no native endpoint mix format, so request 48 kHz stereo float. Source descriptions and actual input formats appear in the status log and journal. Application/player volume still applies; this is listening-volume independence, not loudness normalization.

Each native-format queue feeds its own WMF resampler and MP3 encoder. Queues are bounded to four seconds; overflow fails the session visibly. Paced reads use a 300ms startup cushion and fill missing loopback audio with silence. Silent packets become zero samples; empty callbacks are ignored and counted. Uncertain timestamps are counted without rejecting audio or attempting cross-source synchronization. No gain adjustment, normalization or noise processing is requested. Relative timing and prolonged silent/resumed playback still require hardware validation.

A dedicated worker per file converts and encodes while recording, without a complete intermediate WAV. File writes and five-second `Flush(true)` checkpoints are serialized outside capture callbacks. Encoder finalization happens only on stop/EOF, never at each checkpoint. The timer alone does not establish the lost-tail bound. Stopping releases capture devices, drains/finalizes both encoders and closes both files.

Per-source packet counts and received native-byte counts distinguish actual input from generated silence. Nonempty packets must be aligned to their native frame size. A device/encoder/writer failure stops the session and retains available partial files; there is no automatic device migration or file merging.

## Synthetic checks without microphone access

All command modes below avoid capture-device enumeration and never open a microphone. Synthetic mode generates a 440 Hz microphone source in 44.1 kHz mono PCM16 and an 880 Hz playback source in 48 kHz stereo float, in real time. It does not play sound through speakers.

```powershell
dotnet src/Scout.Scribe/bin/Debug/net10.0-windows/win-x64/Scout.Scribe.dll --synthetic-seconds 8 --output .local/scribe-experiment/normal
dotnet src/Scout.Scribe/bin/Debug/net10.0-windows/win-x64/Scout.Scribe.dll --synthetic-seconds 20 --output .local/scribe-experiment/killed --terminate-after 12
```

The second command intentionally kills only its own process without finalizing; a nonzero exit code is expected. It cannot be used to auto-kill live microphone capture. Each run creates a new directory; previous output is retained.

Inspect a completed or interrupted file after its process has exited:

```powershell
dotnet src/Scout.Scribe/bin/Debug/net10.0-windows/win-x64/Scout.Scribe.dll --inspect '<full path to microphone.mp3>'
dotnet src/Scout.Scribe/bin/Debug/net10.0-windows/win-x64/Scout.Scribe.dll --inspect '<full path to playback.mp3>'
```

This decodes the selected file with WMF and writes `<filename>.inspection.json` beside it, including decoded duration, MP3 frame duration/bitrates/channel modes and incomplete-tail status. It does not repair or change the MP3. A decoder error exits nonzero. MP3 frame padding/encoder delay can make reported frame and decoded durations slightly different.

To save WMF's advertised MP3 formats:

```powershell
dotnet src/Scout.Scribe/bin/Debug/net10.0-windows/win-x64/Scout.Scribe.dll --capabilities .local/scribe-experiment/capabilities.json
```

Create the destination parent folder first. On the tested machine, advertised mono formats stopped at 128 kbps. Explicitly setting `MF_MT_AUDIO_AVG_BYTES_PER_SECOND` to 32,000 before encoding produced actual 256 kbps mono frames. The implementation requests that exact rate, with no silent bitrate or encoder fallback; tests inspect actual frame headers and decode the output.

## Hardware acceptance to perform

### Listening-volume diagnostic

Volume-probe commands are explicit hardware diagnostics, unlike the device-free commands above. Pause other playback and run only when a quiet 1 kHz tone and temporary output-volume changes are appropriate. Build Release first. Start the tone as a sibling process so the production Scribe-process exclusion does not exclude it:

```powershell
$app = (Resolve-Path 'src/Scout.Scribe/bin/Release/net10.0-windows/win-x64/Scout.Scribe.dll').Path
$tone = Start-Process dotnet -ArgumentList @($app, '--play-volume-probe-tone') -PassThru -WindowStyle Hidden
try {
    dotnet $app --verify-playback-volume --external-tone --output .local/scribe-volume-verification
} finally {
    if (-not $tone.WaitForExit(6000)) { $tone.Kill(); $tone.WaitForExit() }
    $tone.Dispose()
}
```

The tone process plays amplitude 0.05 for twelve seconds on the current default output. Verification compares old endpoint-loopback and production application-loopback levels over about seven seconds at the starting Windows volume, half that setting and the restored original setting. It fails if application-loopback amplitude differs from 0.05 by more than 5%. It never raises volume above the starting value. A `finally` restores volume on normal/error exit; a forced kill can bypass restoration. No microphone is opened. The optional output folder exercises the production two-file recording pipeline with a synthetic microphone and actual application playback, retaining both MP3s and a journal. JSON reports numerical amplitudes, actual Windows decibels and the recording directory. Probe commands never run during normal recording or automated tests. Without `--external-tone`, the command's own tone is deliberately excluded by application loopback, so use the sibling-process recipe to verify the fix.

### Recording checks

Use fictional speech and test sounds. Consent is handled before starting, outside Scribe. Start with Web stopped.

1. Set Windows' normal default microphone to the Logitech camera microphone and playback to the actual call output. Start, speak a distinct phrase and play a different phrase from the PC. Include a notification and playback silence, then resume playback. Stop and listen to each file separately: voice in `microphone.mp3`, PC audio in `playback.mp3`, with preserved silence and resumed audio. Repeat after switching Windows defaults to the Bluetooth headset. Record journal device names. Acoustic pickup of speakers can still appear in the microphone file.
2. Check duration and relative timing near both ends of a sustained **90-minute** mock interview. Record CPU/memory, audible quality, per-source queue counts, checkpoint latency and file growth (approximately 173 MB per file, 346 MB total). No echo cancellation is added.
3. On a fictional recording, note the PID shown in the window and use numbered/time-marked phrases from both sources. After at least one checkpoint, end **only that PID** using Task Manager or `Stop-Process -Id <displayed PID> -Force`. Open both MP3s directly without repair. Compare the last surviving audio against termination and the checkpoint log. Repeat before the first checkpoint and around later checkpoints. A twelve-second synthetic pass does not prove all crash timings.
4. Relaunch idle and start a separate session. Disconnect the active microphone and check visible failure plus retained output. A default-microphone change must not silently redirect its active capture. Application playback is endpoint-independent; test output route changes separately. Broader failure/sleep/shell tests belong to package 3.
5. Manually import a normally finalized file into a fictional Web interview, acknowledge consent there, and verify playback, seek and download in the actual browser. Do not use interrupted files as proof of finalized export compatibility.

Power outages, blue screens and hardware-failure recovery are outside this acceptance scope. Share the journal/inspection results and observations for the next iteration; personal audio is unnecessary.

## Verification performed

Environment: Windows build **10.0.26300.0**, .NET SDK **10.0.402**, runtime **10.0.13**, NAudio **3.3.0**. No microphone or playback device was opened by automated checks.

| Check | Result |
|---|---|
| Solution build | Passed, zero warnings/errors. |
| Full .NET suite after application-loopback change | **165 passed**: 155 Web tests, 10 Scribe tests. Native byte preservation, silent/empty callbacks, bounded buffering/EOF, per-session resolution, independent source tones, MP3 format/decoding, two-file forced termination, checkpoint-stream and hidden tray lifecycle checks remain; the new probe-meter test verifies frequency isolation and measured amplitude. |
| Normal synthetic stop | Both files finalized, each mono/48 kHz/256 kbps and WMF-decodable; spectral checks confirm the intended tone and reject the other source's tone. Native inputs differ in rate, channels and sample encoding. |
| Synthetic process killed at twelve seconds | Both files had positive five-second checkpoint byte counts and decoded directly to between 7 and 13 seconds without repair or normal finalization; no completion/failure event was written. This short regression check is not a universal tail guarantee. |
| Earlier hardware feedback | The superseded mixed path encountered uncertain/empty callbacks, then produced microphone audio but missing PC playback. VLC played the output; modern Windows Media Player showed duration but did not audibly play for the user. These observations motivated independent native capture; they are not evidence that this refactor resolves hardware playback. |
| LG/AMD cause verified | Endpoint amplitude 0.00302865 at Windows 20% (-24.3544 dB), 0.00091461 at 10% (-34.7547 dB), restored 0.00302865. Native endpoint initialization without conversion flags behaved identically. This establishes listening-volume attenuation before MP3 encoding. |
| Application-loopback fix, same hardware | Captured amplitude 0.0500000 at 20%, 10% and restored 20%, using the production Scribe-process exclusion filter and external known tone. No inverse gain or normalization. |
| Production two-file pipeline, synthetic mic + live playback | Both MP3s finalized and decoded. Full one-second decoded playback windows measured amplitude 0.0500001 across the volume changes; synthetic mic stayed approximately 0.199928. No physical microphone was opened. Local evidence: `.local/scribe-volume-verification/result.json`, `decoded-levels.json` and the timestamped session. |
| Full microphone/call session, 90 minutes, output mute, Bluetooth and browser playback | **Not verified.** Requires the user's broader hardware/call/browser exercises above. |

Commands used for this change: `dotnet build Scout.slnx -c Release --no-restore --disable-build-servers -m:1 -p:CopyRetryCount=0` and `dotnet test --solution Scout.slnx -c Release --no-build`. The initially running app locked Debug output; Release built successfully. Offline tests ran with necessary filesystem/IPC permissions. No Web source or migrations changed.

The initial package's code/setup is ready for hardware testing. Package 1's real-device gates remain open, and the rest of M1 must not be described as delivered.

Live diagnostics ran only after the user confirmed readiness; Windows output was restored to 20%. Visible tray state and activation still require desktop checks; automated UI coverage uses an invisible window and suppresses tray registration.
