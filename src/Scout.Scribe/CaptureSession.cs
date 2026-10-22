using System.Diagnostics;
using NAudio.Wave;

namespace Scout.Scribe;

public sealed record SessionResult(string Directory, bool Completed, string? Error);

public sealed class CaptureSession
{
    private readonly TaskCompletionSource started = new(TaskCreationOptions.RunContinuationsAsynchronously);
    private readonly TaskCompletionSource stop = new(TaskCreationOptions.RunContinuationsAsynchronously);
    private readonly object gate = new();
    private string? failure;
    private bool stopRequested;
    public Task Started => started.Task;
    public string DirectoryPath { get; }
    public Task<SessionResult> Completion { get; }
    public event Action<string>? Status;

    public CaptureSession(string root, bool synthetic = false)
        : this(root, synthetic ? SyntheticCaptureSource.OpenPair : WindowsCaptureSource.OpenDefaults, synthetic) { }

    internal CaptureSession(string root, Func<ICaptureSource[]> openSources, bool synthetic = false)
    {
        DirectoryPath = Path.Combine(Path.GetFullPath(root), $"{DateTime.Now:yyyy-MM-dd_HH-mm-ss-fff}_{Guid.NewGuid():N}");
        Directory.CreateDirectory(DirectoryPath);
        Completion = Task.Factory.StartNew(() =>
        {
            try { return Run(openSources, synthetic); }
            catch (Exception error)
            {
                started.TrySetException(error);
                return new SessionResult(DirectoryPath, false, error.ToString());
            }
        }, CancellationToken.None, TaskCreationOptions.LongRunning, TaskScheduler.Default);
    }

    public void Stop()
    {
        lock (gate) stopRequested = true;
        stop.TrySetResult();
    }

    private void Fail(Exception error)
    {
        lock (gate) failure ??= error.ToString();
        Stop();
    }

    private SessionResult Run(Func<ICaptureSource[]> openSources, bool synthetic)
    {
        using var journal = new SessionJournal(Path.Combine(DirectoryPath, "events.jsonl"));
        using var checkpointCancellation = new CancellationTokenSource();
        ICaptureSource[] sources = [];
        var tracks = new List<RecordingTrack>();
        Task? checkpoints = null;
        try
        {
            sources = openSources();
            if (sources.Length != 2) throw new InvalidOperationException("Expected microphone and playback sources.");
            var origin = Stopwatch.GetTimestamp();
            for (var index = 0; index < sources.Length; index++)
            {
                var source = sources[index];
                var name = index == 0 ? "microphone" : "playback";
                var track = new RecordingTrack(DirectoryPath, name, source.WaveFormat, origin);
                tracks.Add(track);
                source.DataAvailable += (buffer, flags, _, _) =>
                {
                    try { track.Input.AddPacket(buffer, flags); }
                    catch (Exception error) { Fail(error); }
                };
                source.RecordingStopped += (_, args) =>
                {
                    lock (gate)
                        if (!stopRequested) Fail(args.Exception ?? new IOException($"{name} device stopped unexpectedly."));
                };
                journal.Write("device", new { source = name, device = source.Name, source.DeviceId,
                    nativeFormat = source.WaveFormat.ToString(), file = name + ".mp3" });
                Status?.Invoke($"{name}: {source.Name}; native {source.WaveFormat}; output {name}.mp3");
            }
            // Queue native PCM unchanged; convert and encode independently downstream.
            foreach (var track in tracks) track.Begin(Fail);
            foreach (var source in sources) source.Start();
            Task.WhenAll(tracks.Select(track => track.Input.Ready.Task)).WaitAsync(TimeSpan.FromSeconds(15)).GetAwaiter().GetResult();
            lock (gate)
                if (failure is not null) throw new IOException(failure);
            journal.Write("recording", new { synthetic, pid = Environment.ProcessId, sampleRate = 48_000,
                channels = 1, bitrate = RecordingTrack.Bitrate, checkpointSeconds = 5,
                files = tracks.Select(track => Path.GetFileName(track.Path)).ToArray(),
                windows = Environment.OSVersion.VersionString, runtime = Environment.Version.ToString(),
                naudio = typeof(WasapiRecorder).Assembly.GetName().Version?.ToString() });
            checkpoints = Task.Run(async () =>
            {
                try
                {
                    using var timer = new PeriodicTimer(TimeSpan.FromSeconds(5));
                    while (await timer.WaitForNextTickAsync(checkpointCancellation.Token))
                    {
                        foreach (var track in tracks) journal.Write("checkpoint", track.Checkpoint());
                        Status?.Invoke("Checkpoint: microphone.mp3 and playback.mp3 flushed.");
                    }
                }
                catch (OperationCanceledException) when (checkpointCancellation.IsCancellationRequested) { }
                catch (Exception error) { Fail(error); }
            });
            started.TrySetResult();
            stop.Task.GetAwaiter().GetResult();
        }
        catch (Exception error) { Fail(error); started.TrySetException(error); }
        finally
        {
            Stop();
            checkpointCancellation.Cancel();
            checkpoints?.GetAwaiter().GetResult();
            foreach (var source in sources)
            {
                try { source.Stop(); }
                catch (Exception error) { Fail(error); }
                try { source.Dispose(); }
                catch (Exception error) { Fail(error); }
            }
            foreach (var track in tracks) track.Input.Stop();
            Task.WhenAll(tracks.Select(track => track.Encoding)).GetAwaiter().GetResult();
            foreach (var track in tracks)
            {
                try { journal.Write("final-track", track.Checkpoint()); }
                catch (Exception error) { Fail(error); }
                finally { track.Dispose(); }
            }
        }
        journal.Write(failure is null ? "completed" : "failed", new { error = failure });
        return new(DirectoryPath, failure is null, failure);
    }
}
