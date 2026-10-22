using System.Text;
using System.Text.Json;

namespace Scout.Scribe;

public sealed class SessionJournal : IDisposable
{
    private readonly StreamWriter writer;
    private readonly object gate = new();
    public SessionJournal(string path) => writer = new(new FileStream(path, FileMode.CreateNew,
        FileAccess.Write, FileShare.Read), new UTF8Encoding(false)) { AutoFlush = true };
    public void Write(string kind, object detail)
    {
        lock (gate) writer.WriteLine(JsonSerializer.Serialize(new { utc = DateTimeOffset.UtcNow, kind, detail }));
    }
    public void Dispose() { lock (gate) writer.Dispose(); }
}
