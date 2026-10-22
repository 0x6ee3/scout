using Scout.Scribe;

namespace Scout.Scribe.Tests;

public sealed class CheckpointStreamTests
{
    [Fact]
    public void CheckpointMakesCurrentBytesReadableAndAllowsFurtherWritesAndHeaderUpdates()
    {
        var directory = Path.Combine(Path.GetTempPath(), "Scout.Scribe.Tests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(directory);
        var path = Path.Combine(directory, "recording.mp3");
        try
        {
            using (var stream = new CheckpointStream(path))
            {
                stream.Write("first"u8);
                Assert.Equal(5, stream.Checkpoint().Bytes);
                using (var reader = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
                {
                    var bytes = new byte[5];
                    reader.ReadExactly(bytes);
                    Assert.Equal("first"u8.ToArray(), bytes);
                }
                stream.Write("second"u8);
                stream.Position = 0;
                stream.Write("FIRST"u8);
                Assert.Equal(11, stream.Checkpoint().Bytes);
                Assert.Throws<IOException>(() => new CheckpointStream(path));
            }
            Assert.Equal("FIRSTsecond", File.ReadAllText(path));
            File.Move(path, Path.Combine(directory, "released.mp3"));
        }
        finally { Directory.Delete(directory, recursive: true); }
    }
}
