using System.Diagnostics;
using NAudio.CoreAudioApi;
using NAudio.Wave;
using Scout.Scribe;

namespace Scout.Scribe.Tests;

public sealed class NativeCaptureStreamTests
{
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void QueuePreservesNativePacketBytes(bool floatingPoint)
    {
        var format = floatingPoint ? WaveFormat.CreateIeeeFloatWaveFormat(48_000, 2) : new WaveFormat(44_100, 16, 1);
        using var stream = new NativeCaptureStream(format, Stopwatch.GetTimestamp(), paceReads: false);
        var bytes = Enumerable.Range(0, 20 * format.BlockAlign).Select(index => (byte)index).ToArray();
        stream.AddPacket(bytes, AudioClientBufferFlags.TimestampError);
        var read = new byte[bytes.Length];
        Assert.Equal(bytes.Length, stream.Read(read));
        Assert.Equal(bytes, read);
        Assert.Same(format, stream.WaveFormat);
        Assert.Equal(1, stream.TimestampErrors);
    }

    [Fact]
    public void EmptyPacketsAreIgnoredAndSilentPacketsBecomeZeroes()
    {
        using var stream = new NativeCaptureStream(new WaveFormat(48_000, 16, 1), Stopwatch.GetTimestamp(), paceReads: false);
        stream.AddPacket([], AudioClientBufferFlags.None);
        stream.AddPacket(new byte[] { 1, 2, 3, 4 }, AudioClientBufferFlags.Silent);
        var read = new byte[4];
        Assert.Equal(4, stream.Read(read));
        Assert.Equal(new byte[4], read);
        Assert.Equal(1, stream.EmptyPackets);
        Assert.Equal(1, stream.Packets);
    }

    [Fact]
    public void QueueIsBoundedAndStopEndsReads()
    {
        var format = new WaveFormat(48_000, 16, 1);
        using var stream = new NativeCaptureStream(format, Stopwatch.GetTimestamp(), paceReads: false);
        Assert.Throws<InvalidOperationException>(() => stream.AddPacket(new byte[format.AverageBytesPerSecond * 5], AudioClientBufferFlags.None));
        stream.Stop();
        var read = new byte[format.AverageBytesPerSecond / 10];
        var iterations = 0;
        while (stream.Read(read) != 0) Assert.True(++iterations < 20);
    }
}
