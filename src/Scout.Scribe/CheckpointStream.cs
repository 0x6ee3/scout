using System.Diagnostics;

namespace Scout.Scribe;

// WMF owns writes/seeks, while the checkpoint worker owns periodic Flush(true).
// Serializing those operations also protects WMF's final header updates.
public sealed class CheckpointStream : Stream
{
    private readonly FileStream file;
    private readonly object gate = new();
    public CheckpointStream(string path) => file = new(path, FileMode.CreateNew,
        FileAccess.ReadWrite, FileShare.Read, 16 * 1024, FileOptions.None);

    public (long Bytes, double Milliseconds) Checkpoint()
    {
        var started = Stopwatch.GetTimestamp();
        lock (gate)
        {
            file.Flush(flushToDisk: true);
            return (file.Length, Stopwatch.GetElapsedTime(started).TotalMilliseconds);
        }
    }

    public override bool CanRead => true;
    public override bool CanSeek => true;
    public override bool CanWrite => true;
    public override long Length { get { lock (gate) return file.Length; } }
    public override long Position { get { lock (gate) return file.Position; } set { lock (gate) file.Position = value; } }
    public override void Flush() { lock (gate) file.Flush(); }
    public override int Read(byte[] buffer, int offset, int count) { lock (gate) return file.Read(buffer, offset, count); }
    public override void Write(byte[] buffer, int offset, int count) { lock (gate) file.Write(buffer, offset, count); }
    public override void Write(ReadOnlySpan<byte> buffer) { lock (gate) file.Write(buffer); }
    public override long Seek(long offset, SeekOrigin origin) { lock (gate) return file.Seek(offset, origin); }
    public override void SetLength(long value) { lock (gate) file.SetLength(value); }
    protected override void Dispose(bool disposing)
    {
        if (disposing) lock (gate) file.Dispose();
        base.Dispose(disposing);
    }
}
