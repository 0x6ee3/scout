using System.Runtime.InteropServices;
using NAudio.CoreAudioApi;
using NAudio.Wave;

namespace Scout.Scribe;

internal interface ICaptureSource : IDisposable
{
    string Name { get; }
    string? DeviceId { get; }
    WaveFormat WaveFormat { get; }
    event CaptureDataAvailableHandler? DataAvailable;
    event EventHandler<StoppedEventArgs>? RecordingStopped;
    void Start();
    void Stop();
}

internal sealed class WindowsCaptureSource(WasapiRecorder recorder) : ICaptureSource
{
    public string Name => recorder.DeviceFriendlyName ?? "Application audio before listening volume (all output devices; excludes Scribe)";
    public string? DeviceId => recorder.DeviceId;
    public WaveFormat WaveFormat => recorder.WaveFormat;
    public event CaptureDataAvailableHandler? DataAvailable { add => recorder.DataAvailable += value; remove => recorder.DataAvailable -= value; }
    public event EventHandler<StoppedEventArgs>? RecordingStopped { add => recorder.RecordingStopped += value; remove => recorder.RecordingStopped -= value; }
    public void Start() => recorder.StartRecording();
    public void Stop() => recorder.StopRecording();
    public void Dispose() => recorder.Dispose();

    internal static ICaptureSource[] OpenDefaults()
    {
        // Resolve the current microphone on EVERY session. Application loopback
        // taps the application mix before endpoint listening-volume attenuation.
        var microphone = new WindowsCaptureSource(new WasapiRecorderBuilder().Build());
        try
        {
            return [microphone, OpenApplicationPlayback()];
        }
        catch { microphone.Dispose(); throw; }
    }

    internal static ICaptureSource OpenApplicationPlayback()
    {
        if (!OperatingSystem.IsWindowsVersionAtLeast(10, 0, 22000))
            throw new PlatformNotSupportedException("Application audio capture requires Windows 11.");
        return new WindowsCaptureSource(new WasapiRecorderBuilder()
            .WithProcessLoopback((uint)Environment.ProcessId, ProcessLoopbackMode.ExcludeTargetProcessTree)
            // The virtual application-loopback device has no native mix format.
            .WithFormat(WaveFormat.CreateIeeeFloatWaveFormat(48_000, 2))
            .BuildAsync().GetAwaiter().GetResult());
    }
}

// A device-free source for regression tests/CLI. The different formats and tones
// prove that both conversion pipelines work independently; nothing is played aloud.
internal sealed class SyntheticCaptureSource(string name, WaveFormat format, int frequency) : ICaptureSource
{
    private readonly CancellationTokenSource cancellation = new();
    private Task? capture;
    private bool disposed;
    public string Name => name;
    public string DeviceId => "synthetic:" + name;
    public WaveFormat WaveFormat => format;
    public event CaptureDataAvailableHandler? DataAvailable;
    public event EventHandler<StoppedEventArgs>? RecordingStopped;
    public void Start()
    {
        capture = Task.Run(async () =>
        {
            long position = 0;
            try
            {
                using var timer = new PeriodicTimer(TimeSpan.FromMilliseconds(20));
                while (await timer.WaitForNextTickAsync(cancellation.Token))
                {
                    var frames = format.SampleRate / 50;
                    var bytes = new byte[frames * format.BlockAlign];
                    FillTone(bytes, position, frames);
                    DataAvailable?.Invoke(bytes, AudioClientBufferFlags.None, position, 0);
                    position += frames;
                }
            }
            catch (OperationCanceledException) when (cancellation.IsCancellationRequested) { }
            catch (Exception error) { RecordingStopped?.Invoke(this, new StoppedEventArgs(error)); }
        });
    }
    private void FillTone(Span<byte> bytes, long position, int frames)
    {
        for (var frame = 0; frame < frames; frame++)
        {
            var value = 0.2 * Math.Sin(2 * Math.PI * frequency * (position + frame) / format.SampleRate);
            for (var channel = 0; channel < format.Channels; channel++)
            {
                var sample = frame * format.Channels + channel;
                if (format.Encoding == WaveFormatEncoding.IeeeFloat) MemoryMarshal.Cast<byte, float>(bytes)[sample] = (float)value;
                else MemoryMarshal.Cast<byte, short>(bytes)[sample] = (short)(value * short.MaxValue);
            }
        }
    }
    public void Stop() { if (disposed) return; cancellation.Cancel(); capture?.GetAwaiter().GetResult(); }
    public void Dispose() { if (disposed) return; Stop(); disposed = true; cancellation.Dispose(); }
    internal static ICaptureSource[] OpenPair() =>
        [new SyntheticCaptureSource("synthetic microphone", new WaveFormat(44_100, 16, 1), 440),
         new SyntheticCaptureSource("synthetic playback", WaveFormat.CreateIeeeFloatWaveFormat(48_000, 2), 880)];
}
