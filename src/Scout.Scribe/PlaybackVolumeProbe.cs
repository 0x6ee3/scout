using System.Runtime.InteropServices;
using NAudio.CoreAudioApi;
using NAudio.Wave;

namespace Scout.Scribe;

// Explicit, user-initiated hardware diagnostic. Never run by normal recording or tests.
internal static class PlaybackVolumeProbe
{
    internal static async Task<object> Run(bool externalTone = false, string? output = null)
    {
        using var devices = new MMDeviceEnumerator();
        using var device = devices.GetDefaultAudioEndpoint(DataFlow.Render, Role.Console);
        var volume = device.AudioEndpointVolume;
        var original = volume.MasterVolumeLevelScalar;
        if (volume.Mute || original < 0.02f)
            throw new InvalidOperationException("Diagnostic needs an unmuted output above 2%; it never increases the starting volume.");
        using var capture = new WasapiRecorderBuilder().WithDevice(device).WithLoopbackCapture().Build();
        using var application = WindowsCaptureSource.OpenApplicationPlayback();
        var signal = new ProbeTone();
        using var player = new WasapiPlayerBuilder().WithDevice(device).Build();
        var meter = new ToneMeter(capture.WaveFormat);
        var applicationMeter = new ToneMeter(application.WaveFormat);
        Exception? captureError = null;
        capture.DataAvailable += (data, flags, _, _) =>
        {
            if ((flags & AudioClientBufferFlags.Silent) == 0) meter.Add(data);
        };
        capture.RecordingStopped += (_, args) => captureError = args.Exception;
        application.DataAvailable += (data, flags, _, _) =>
        {
            if ((flags & AudioClientBufferFlags.Silent) == 0) applicationMeter.Add(data);
        };
        application.RecordingStopped += (_, args) => captureError ??= args.Exception;
        player.Init(signal);
        var phases = new List<object>();
        CaptureSession? recording = null;
        try
        {
            if (output is not null)
            {
                // Exercise the production two-file path without opening a microphone.
                recording = new CaptureSession(output, () =>
                    [new SyntheticCaptureSource("diagnostic synthetic microphone", new WaveFormat(44_100, 16, 1), 440),
                        WindowsCaptureSource.OpenApplicationPlayback()], synthetic: true);
                await recording.Started.WaitAsync(TimeSpan.FromSeconds(15)).ConfigureAwait(false);
            }
            capture.StartRecording();
            application.Start();
            if (!externalTone) player.Play();
            foreach (var level in new[] { original, original / 2, original })
            {
                volume.MasterVolumeLevelScalar = level;
                await Task.Delay(600).ConfigureAwait(false);
                meter.Reset();
                applicationMeter.Reset();
                await Task.Delay(1600).ConfigureAwait(false);
                if (captureError is not null) throw new IOException("Loopback diagnostic failed.", captureError);
                if (externalTone && Math.Abs(applicationMeter.Amplitude - ProbeTone.Amplitude) > ProbeTone.Amplitude * 0.05)
                    throw new IOException($"Application loopback tone amplitude {applicationMeter.Amplitude:F6} differs from expected {ProbeTone.Amplitude:F6} at Windows volume {volume.MasterVolumeLevelScalar:P0}.");
                phases.Add(new { volumePercent = volume.MasterVolumeLevelScalar * 100,
                    volumeDb = volume.MasterVolumeLevel, capturedToneAmplitude = meter.Amplitude,
                    applicationLoopbackAmplitude = applicationMeter.Amplitude });
            }
            return new { device = device.FriendlyName, nativeFormat = capture.WaveFormat.ToString(),
                generatedToneAmplitude = ProbeTone.Amplitude, frequencyHz = ToneMeter.Frequency,
                recordingDirectory = recording?.DirectoryPath, phases };
        }
        finally
        {
            try { player.Stop(); }
            finally
            {
                try { capture.StopRecording(); application.Stop(); }
                finally
                {
                    try { volume.MasterVolumeLevelScalar = original; }
                    finally
                    {
                        if (recording is not null)
                        {
                            recording.Stop();
                            var result = await recording.Completion.ConfigureAwait(false);
                            if (!result.Completed) throw new IOException(result.Error);
                        }
                    }
                }
            }
        }
    }

    internal static async Task PlayExternalTone()
    {
        using var player = new WasapiPlayerBuilder().Build();
        player.Init(new ProbeTone());
        player.Play();
        await Task.Delay(12_000).ConfigureAwait(false);
        player.Stop();
    }

    private sealed class ProbeTone : IWaveProvider
    {
        internal const float Amplitude = 0.05f;
        private long position;
        public WaveFormat WaveFormat { get; } = WaveFormat.CreateIeeeFloatWaveFormat(48_000, 2);
        public int Read(Span<byte> destination)
        {
            var floats = MemoryMarshal.Cast<byte, float>(destination);
            var frames = floats.Length / 2;
            for (var index = 0; index < frames; index++, position++)
                floats[index * 2] = floats[index * 2 + 1] = Amplitude * (float)Math.Sin(2 * Math.PI * ToneMeter.Frequency * position / 48_000);
            return frames * WaveFormat.BlockAlign;
        }
    }

    // Detect only the diagnostic frequency rather than measuring all desktop audio.
    internal sealed class ToneMeter
    {
        internal const int Frequency = 1000;
        private readonly WaveFormat format;
        private readonly object gate = new();
        private double real, imaginary;
        private long count;
        internal ToneMeter(WaveFormat format)
        {
            if (format.BitsPerSample != 32 || !(format.Encoding == WaveFormatEncoding.IeeeFloat ||
                format is WaveFormatExtensible extended && extended.SubFormat == new Guid("00000003-0000-0010-8000-00aa00389b71")))
                throw new NotSupportedException("This diagnostic requires native IEEE float capture.");
            this.format = format;
        }
        internal void Add(ReadOnlySpan<byte> bytes)
        {
            var floats = MemoryMarshal.Cast<byte, float>(bytes);
            lock (gate)
                for (var index = 0; index < floats.Length; index += format.Channels, count++)
                {
                    var phase = 2 * Math.PI * Frequency * count / format.SampleRate;
                    real += floats[index] * Math.Cos(phase);
                    imaginary += floats[index] * Math.Sin(phase);
                }
        }
        internal void Reset() { lock (gate) { real = imaginary = 0; count = 0; } }
        internal double Amplitude { get { lock (gate) return count == 0 ? 0 : 2 * Math.Sqrt(real * real + imaginary * imaginary) / count; } }
    }
}
