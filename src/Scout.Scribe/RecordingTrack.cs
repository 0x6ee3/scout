using NAudio.MediaFoundation;
using NAudio.Wave;

namespace Scout.Scribe;

internal sealed class RecordingTrack : IDisposable
{
    internal static readonly WaveFormat OutputFormat = new(48_000, 16, 1);
    internal const int Bitrate = 256_000;
    public string Name { get; }
    public string Path { get; }
    public NativeCaptureStream Input { get; }
    private readonly CheckpointStream output;
    public Task Encoding { get; private set; } = Task.CompletedTask;
    public RecordingTrack(string directory, string name, WaveFormat nativeFormat, long origin)
    {
        Name = name;
        Path = System.IO.Path.Combine(directory, name + ".mp3");
        Input = new NativeCaptureStream(nativeFormat, origin);
        try { output = new CheckpointStream(Path); }
        catch { Input.Dispose(); throw; }
    }
    public void Begin(Action<Exception> fail)
    {
        Encoding = Task.Factory.StartNew(() =>
        {
            try
            {
                MediaFoundationApi.Startup();
                using var type = MediaFoundationEncoder.SelectMediaType(AudioSubtypes.MFAudioFormat_MP3, OutputFormat, Bitrate)
                    ?? throw new NotSupportedException("Windows MP3 encoder unavailable.");
                type.SetUInt32(MediaFoundationAttributes.MF_MT_AUDIO_AVG_BYTES_PER_SECOND, Bitrate / 8);
                using var converter = new MediaFoundationResampler(Input, OutputFormat);
                using var encoder = new MediaFoundationEncoder(type) { DefaultReadBufferSize = OutputFormat.AverageBytesPerSecond / 10 };
                encoder.Encode(output, converter, TranscodeContainerTypes.MFTranscodeContainerType_MP3);
                output.Checkpoint();
            }
            catch (Exception error)
            {
                var failure = new IOException($"{Name} conversion/encoding failed.", error);
                Input.Ready.TrySetException(failure);
                fail(failure);
            }
        }, CancellationToken.None, TaskCreationOptions.LongRunning, TaskScheduler.Default);
    }
    public object Checkpoint()
    {
        var checkpoint = output.Checkpoint();
        return new { source = Name, file = System.IO.Path.GetFileName(Path), checkpoint.Bytes, checkpoint.Milliseconds,
            nativeFormat = Input.WaveFormat.ToString(), audioSeconds = Input.AudioSeconds,
            nativePackets = Input.Packets, nativeBytes = Input.ReceivedBytes,
            emptyPackets = Input.EmptyPackets, timestampErrors = Input.TimestampErrors, queuedNativeBytes = Input.BufferedBytes };
    }
    public void Dispose() { output.Dispose(); Input.Dispose(); }
}
