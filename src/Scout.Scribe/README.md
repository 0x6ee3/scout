# Scout.Scribe capture experiment

Windows 11 x64 / .NET 10 Windows Forms experiment using NAudio 3.3.0 and Windows Media Foundation. This is Scribe M1 package 1, not the finished tray application.

Build and launch from the repository root:

```powershell
dotnet restore Scout.slnx
dotnet build src/Scout.Scribe/Scout.Scribe.csproj --no-restore
& .\src\Scout.Scribe\bin\Debug\net10.0-windows\win-x64\Scout.Scribe.exe
```

Launch is idle; only clicking Start recording opens capture devices. Each Start resolves the current Windows default microphone in its native format. Playback uses Windows application loopback before listening-volume attenuation, across all output devices, excluding Scribe and its descendants. Its virtual source uses 48 kHz stereo float. Independent WMF conversion produces `microphone.mp3` and `playback.mp3` (each mono, 48 kHz, 256 kbps), with five-second checkpoints and no mixing or volume compensation. Stop and Start after changing the default microphone; playback has no pinned endpoint. The tray shows a gray square when idle/finalizing and a red circle while recording. Its menu provides Start, Stop, Show status and Exit. Closing the window hides it and keeps the tray running; choose Exit to stop/finalize and quit. A second interactive launch brings the existing window forward. Windows may place the icon under the taskbar's overflow arrow.

See the [experiment guide and evidence](../../docs/cycles/scout-scribe-v1/milestones/MILESTONE_1_CAPTURE_EXPERIMENT.md) for synthetic crash tests, inspection commands, output files, manual hardware acceptance and known limits. Keep the complete build output folder if copying the executable; .NET 10 Desktop Runtime is required.
