using System.Diagnostics;
using System.Globalization;
using System.Text.Json;
using NAudio.Wave;

namespace Scout.Scribe;

internal static class Program
{
    [STAThread]
    private static int Main(string[] args)
    {
        try
        {
            if (!OperatingSystem.IsWindowsVersionAtLeast(10, 0, 22000))
                throw new PlatformNotSupportedException("Scout.Scribe requires Windows 11.");
            if (args.Length > 0) return RunCommand(args).GetAwaiter().GetResult();
            using var instance = new Mutex(true, "Local\\Scout.Scribe.Interactive.v1", out var firstInstance);
            if (!firstInstance) { InstanceSignal.ShowExisting(); return 0; }
            try
            {
                ApplicationConfiguration.Initialize();
                Application.SetUnhandledExceptionMode(UnhandledExceptionMode.CatchException);
                using var form = new ExperimentForm();
                Application.ThreadException += (_, e) => form.HandleFailure(e.Exception);
                UnhandledExceptionEventHandler cleanup = (_, _) => { try { form.RemoveTrayIcon(); } catch { } };
                AppDomain.CurrentDomain.UnhandledException += cleanup;
                try { Application.Run(form); }
                finally { AppDomain.CurrentDomain.UnhandledException -= cleanup; }
            }
            finally { instance.ReleaseMutex(); }
            return 0;
        }
        catch (Exception error)
        {
            if (args.Length == 0) MessageBox.Show(error.ToString(), "Scribe experiment failed");
            else Console.Error.WriteLine(error);
            return 1;
        }
    }

    private static async Task<int> RunCommand(string[] args)
    {
        if (args.Length == 1 && args[0] == "--play-volume-probe-tone")
        {
            await PlaybackVolumeProbe.PlayExternalTone();
            return 0;
        }
        if (args.Length is 1 or 2 or 4 && args[0] == "--verify-playback-volume" &&
            (args.Length == 1 || args[1] == "--external-tone") && (args.Length != 4 || args[2] == "--output"))
        {
            Console.WriteLine(JsonSerializer.Serialize(await PlaybackVolumeProbe.Run(args.Length >= 2, args.Length == 4 ? args[3] : null), new JsonSerializerOptions { WriteIndented = true }));
            return 0;
        }
        if (args.Length == 2 && args[0] == "--capabilities")
        {
            var types = MediaFoundationEncoder.GetOutputMediaTypes(NAudio.MediaFoundation.AudioSubtypes.MFAudioFormat_MP3);
            try
            {
                File.WriteAllText(Path.GetFullPath(args[1]), JsonSerializer.Serialize(types.Select(t => new
                { sampleRate = t.SampleRate, channels = t.ChannelCount, bitrate = t.AverageBytesPerSecond * 8 }),
                    new JsonSerializerOptions { WriteIndented = true }));
            }
            finally { foreach (var type in types) type.Dispose(); }
            return 0;
        }
        if (args.Length == 2 && args[0] == "--inspect")
        {
            var path = Path.GetFullPath(args[1]);
            using var reader = new MediaFoundationReader(path);
            var buffer = new byte[reader.WaveFormat.AverageBytesPerSecond];
            long decoded = 0;
            int count;
            while ((count = reader.Read(buffer)) > 0) decoded += count;
            var frameInfo = Mp3Inspection.ReadFrames(path);
            File.WriteAllText(path + ".inspection.json", JsonSerializer.Serialize(new
            {
                path, decodedBytes = decoded, decodedSeconds = decoded / (double)reader.WaveFormat.AverageBytesPerSecond,
                format = reader.WaveFormat.ToString(), frameInfo, inspectedUtc = DateTimeOffset.UtcNow
            }, new JsonSerializerOptions { WriteIndented = true }));
            return decoded > 0 ? 0 : 1;
        }
        if (args.Length is not (4 or 6) || args[0] != "--synthetic-seconds" || args[2] != "--output" ||
            (args.Length == 6 && args[4] != "--terminate-after"))
            throw new ArgumentException("Use --synthetic-seconds <seconds> --output <folder> [--terminate-after <seconds>], --inspect <mp3>, --verify-playback-volume [--external-tone [--output <folder>]], or --play-volume-probe-tone. The two volume-probe commands open playback devices; verification temporarily lowers/restores Windows volume. They never open a microphone.");
        var seconds = double.Parse(args[1], CultureInfo.InvariantCulture);
        if (!double.IsFinite(seconds) || seconds is < 1 or > 7200) throw new ArgumentOutOfRangeException(nameof(seconds));
        var killAfter = args.Length == 6 ? double.Parse(args[5], CultureInfo.InvariantCulture) : 0;
        if (args.Length == 6 && (!double.IsFinite(killAfter) || killAfter < 1 || killAfter >= seconds))
            throw new ArgumentException("Termination time must be at least one second and before the synthetic stop time.");
        var session = new CaptureSession(args[3], synthetic: true);
        await session.Started;
        using var cancelTimer = new CancellationTokenSource();
        var timer = Task.Run(async () =>
        {
            try
            {
                await Task.Delay(TimeSpan.FromSeconds(killAfter > 0 ? killAfter : seconds), cancelTimer.Token);
                if (killAfter > 0) Process.GetCurrentProcess().Kill(); // deliberately bypass cleanup, synthetic mode only
                else session.Stop();
            }
            catch (OperationCanceledException) when (cancelTimer.IsCancellationRequested) { }
        });
        var result = await session.Completion;
        cancelTimer.Cancel();
        await timer;
        return result.Completed ? 0 : 1;
    }
}
