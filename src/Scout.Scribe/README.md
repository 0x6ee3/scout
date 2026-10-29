# Scout.Scribe recording shell

Windows 11 x64 / .NET 10 Windows Forms application using NAudio 3.3.0 and Windows Media Foundation. The tray shell builds on the Scribe M1 capture experiment; broader hardware acceptance remains open.

Build and launch from the repository root:

```powershell
dotnet restore Scout.slnx
dotnet build src/Scout.Scribe/Scout.Scribe.csproj --no-restore
& .\src\Scout.Scribe\bin\Debug\net10.0-windows\win-x64\Scout.Scribe.exe
```

Launch is idle and shows only the tray icon. Left-click the icon to open a compact dark recording panel near the pointer, with Start/Stop, elapsed time, Folder and Details. Escape or closing the panel hides it and keeps Scribe running. Right-click retains Start, Stop, Open output folder and Exit, with separate Show recording and Settings entries. A second interactive launch brings the existing panel forward. Windows may place the icon under the taskbar's overflow arrow.

Settings opens a separate window with Save/Cancel. The recording folder defaults to `<Documents>\Scout.Scribe\recordings` and is saved in `%LOCALAPPDATA%\Scout.Scribe\settings.json`; changing it affects future sessions only. Stop recording before changing the folder. Invalid saved settings are reported in Details and require saving Settings before recording. Launch at sign-in is opt-in and uses the current user's `Software\Microsoft\Windows\CurrentVersion\Run\Scout.Scribe` registry value. Launch `Scout.Scribe.exe` directly to enable it, and disable/re-enable it after moving the executable. Sign-in startup stays idle in the tray. No startup registration is changed until Save; Cancel discards edits.

Only clicking Start recording opens capture devices. Each Start resolves the current Windows default microphone in its native format. Playback uses Windows application loopback before listening-volume attenuation, across all output devices, excluding Scribe and its descendants. Its virtual source uses 48 kHz stereo float. Independent WMF conversion produces `microphone.mp3` and `playback.mp3` (each mono, 48 kHz, 256 kbps), with five-second checkpoints and no mixing or volume compensation. Stop and Start after changing the default microphone; playback has no pinned endpoint. The tray shows a gray square when idle/finalizing and a red circle while recording. Choose Exit to stop/finalize and quit. Capture failures bring the panel forward when no dialog is open and retain diagnostics/partial output.

See the [experiment guide and evidence](../../docs/cycles/scout-scribe-v1/milestones/MILESTONE_1_CAPTURE_EXPERIMENT.md) for synthetic crash tests, inspection commands, output files, manual hardware acceptance and known limits. Keep the complete build output folder if copying the executable; .NET 10 Desktop Runtime is required.
