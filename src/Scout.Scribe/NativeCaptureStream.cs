using System.Buffers;
using System.Diagnostics;
using NAudio.CoreAudioApi;
using NAudio.Wave;

namespace Scout.Scribe;

// Each source has a standard NAudio native-format queue. Reads are paced so loopback
// silence stays silence in its own file rather than shortening the recording.
internal sealed class NativeCaptureStream : IWaveProvider, IDisposable
{
    private readonly BufferedWaveProvider buffer;
    private readonly ManualResetEvent stopped = new(false);
    private readonly long origin;
    private readonly bool paceReads;
    private long readFrames;
    private long endFrame = -1;
    private long packets, emptyPackets, receivedBytes, timestampErrors;
    public TaskCompletionSource Ready { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
    public NativeCaptureStream(WaveFormat format, long origin, bool paceReads = true)
    {
        WaveFormat = format;
        this.origin = origin;
        this.paceReads = paceReads;
        buffer = new BufferedWaveProvider(format, TimeSpan.FromSeconds(4)) { ReadFully = true, DiscardOnBufferOverflow = false };
    }
    public WaveFormat WaveFormat { get; }
    public long Packets => Interlocked.Read(ref packets);
    public long EmptyPackets => Interlocked.Read(ref emptyPackets);
    public long ReceivedBytes => Interlocked.Read(ref receivedBytes);
    public long TimestampErrors => Interlocked.Read(ref timestampErrors);
    public double AudioSeconds => Interlocked.Read(ref readFrames) / (double)WaveFormat.SampleRate;
    public int BufferedBytes => buffer.BufferedBytes;
    public void AddPacket(ReadOnlySpan<byte> bytes, AudioClientBufferFlags flags)
    {
        if (bytes.IsEmpty) { Interlocked.Increment(ref emptyPackets); return; }
        if (bytes.Length % WaveFormat.BlockAlign != 0) throw new InvalidDataException("Capture packet is not aligned to its native format.");
        if ((flags & AudioClientBufferFlags.Silent) != 0)
        {
            var zeros = ArrayPool<byte>.Shared.Rent(bytes.Length);
            try { zeros.AsSpan(0, bytes.Length).Clear(); buffer.AddSamples(zeros.AsSpan(0, bytes.Length)); }
            finally { ArrayPool<byte>.Shared.Return(zeros); }
        }
        else buffer.AddSamples(bytes);
        Interlocked.Increment(ref packets);
        Interlocked.Add(ref receivedBytes, bytes.Length);
        if ((flags & AudioClientBufferFlags.TimestampError) != 0) Interlocked.Increment(ref timestampErrors);
    }
    public void Stop()
    {
        var frames = Math.Max(Interlocked.Read(ref readFrames), (long)(Stopwatch.GetElapsedTime(origin).TotalSeconds * WaveFormat.SampleRate));
        Interlocked.CompareExchange(ref endFrame, frames, -1);
        stopped.Set();
    }
    public int Read(Span<byte> destination)
    {
        Ready.TrySetResult(); // WMF has opened its sink and is pulling native PCM.
        var first = Interlocked.Read(ref readFrames);
        var frames = Math.Min(destination.Length / WaveFormat.BlockAlign, Math.Max(1, WaveFormat.SampleRate / 10));
        var end = Interlocked.Read(ref endFrame);
        if (end >= 0) frames = (int)Math.Min(frames, end - first);
        if (frames <= 0) return 0;
        if (paceReads && end < 0)
        {
            var due = TimeSpan.FromSeconds((first + frames) / (double)WaveFormat.SampleRate + 0.3);
            var wait = due - Stopwatch.GetElapsedTime(origin);
            if (wait > TimeSpan.Zero) stopped.WaitOne(wait);
        }
        end = Interlocked.Read(ref endFrame);
        if (end >= 0) frames = (int)Math.Min(frames, end - first);
        var read = buffer.Read(destination[..(frames * WaveFormat.BlockAlign)]);
        Interlocked.Add(ref readFrames, read / WaveFormat.BlockAlign);
        return read;
    }
    public void Dispose() => stopped.Dispose();
}
