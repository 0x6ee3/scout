using System.Runtime.InteropServices;
using NAudio.Wave;
using Scout.Scribe;

namespace Scout.Scribe.Tests;

public sealed class PlaybackVolumeProbeTests
{
    [Fact]
    public void MeterSeparatesProbeToneFromOtherAudioAndMeasuresAttenuation()
    {
        var format = new WaveFormatExtensible(48_000, 32, 2);
        var meter = new PlaybackVolumeProbe.ToneMeter(format);
        foreach (var amplitude in new[] { 0.05, 0.003, 0.05 })
        {
            meter.Reset();
            var bytes = new byte[format.AverageBytesPerSecond];
            var samples = MemoryMarshal.Cast<byte, float>(bytes.AsSpan());
            for (var frame = 0; frame < format.SampleRate; frame++)
                samples[frame * 2] = samples[frame * 2 + 1] = (float)(amplitude * Math.Sin(2 * Math.PI * 1000 * frame / format.SampleRate)
                    + 0.1 * Math.Sin(2 * Math.PI * 440 * frame / format.SampleRate));
            meter.Add(bytes);
            Assert.InRange(meter.Amplitude, amplitude * 0.999, amplitude * 1.001);
        }
    }
}
