using System.ComponentModel.DataAnnotations;
using System.Security.Cryptography;
using System.Text;
using Microsoft.EntityFrameworkCore;
using Scout.Web.Features.Documents;
using Scout.Web.Storage;
using Scout.Web.Tests.Data;
namespace Scout.Web.Tests.Documents;

public sealed class DocumentTests
{
    [Fact]
    public async Task InvalidOwnerWritesNothingAndSameSizeTamperIsDetected()
    {
        await using var db = await TemporaryDatabase.CreateAsync();
        var store = new DocumentFileStore(db.Paths);
        var docs = new DocumentOperations(db.Factory, store);
        await Assert.ThrowsAsync<ValidationException>(() => docs.ImportAsync("file.txt", new MemoryStream("one"u8.ToArray()), new(), new(DocumentOwnerKind.Application, 999), TestContext.Current.CancellationToken));
        Assert.Empty(Directory.GetFiles(db.Paths.AssetDirectory, "*", SearchOption.AllDirectories));
        var id = await docs.ImportAsync("file.txt", new MemoryStream("one"u8.ToArray()), new(), token: TestContext.Current.CancellationToken);
        var document = (await docs.GetAsync(id))!;
        await File.WriteAllBytesAsync(store.Resolve(document.StoredRelativePath), "two"u8.ToArray(), TestContext.Current.CancellationToken);
        await Assert.ThrowsAsync<IOException>(() => store.OpenVerifiedAsync(document, TestContext.Current.CancellationToken));
    }
    [Fact]
    public async Task ImportReuseUnlinkArchiveAndTamperDetection()
    {
        await using var db = await TemporaryDatabase.CreateAsync();
        var files = new DocumentFileStore(db.Paths);
        var docs = new DocumentOperations(db.Factory, files);
        var c = await db.Companies.CreateAsync(new()
        {
            Name = "Company"
        });
        var p = await db.Positions.CreateAsync(new()
        {
            CompanyId = c,
            Title = "Position"
        });
        var bytes = Encoding.UTF8.GetBytes("München\n<script>plain</script>");
        var id = await docs.ImportAsync("../../untrusted\"name.txt", new MemoryStream(bytes), new()
        {
            Description = "Notes"
        }, new(DocumentOwnerKind.Company, c), TestContext.Current.CancellationToken);
        var d = (await docs.GetAsync(id))!;
        Assert.Equal(Convert.ToHexString(SHA256.HashData(bytes)), d.Sha256);
        Assert.Equal(bytes.Length, d.Size);
        await docs.LinkAsync(new(DocumentOwnerKind.Position, p), id);
        await docs.LinkAsync(new(DocumentOwnerKind.Position, p), id);
        Assert.Single(await docs.LinkedAsync(new(DocumentOwnerKind.Position, p)));
        await docs.UnlinkAsync(new(DocumentOwnerKind.Company, c), id);
        Assert.Empty(await docs.LinkedAsync(new(DocumentOwnerKind.Company, c)));
        Assert.True(File.Exists(files.Resolve(d.StoredRelativePath)));
        await docs.SetArchivedAsync(id, true);
        await using (var read = await files.OpenVerifiedAsync(d, TestContext.Current.CancellationToken))
        {
            using var output = new MemoryStream();
            await read.CopyToAsync(output, TestContext.Current.CancellationToken);
            Assert.Equal(bytes, output.ToArray());
            await Assert.ThrowsAsync<IOException>(async () => { await using var write = new FileStream(files.Resolve(d.StoredRelativePath), FileMode.Open, FileAccess.Write, FileShare.None); });
        }
        await Assert.ThrowsAsync<ValidationException>(() => docs.LinkAsync(new(DocumentOwnerKind.Company, c), id));
        await File.WriteAllTextAsync(files.Resolve(d.StoredRelativePath), "changed", TestContext.Current.CancellationToken);
        await Assert.ThrowsAsync<IOException>(() => files.OpenVerifiedAsync(d, TestContext.Current.CancellationToken));
        File.Delete(files.Resolve(d.StoredRelativePath));
        await Assert.ThrowsAsync<FileNotFoundException>(() => files.OpenVerifiedAsync(d, TestContext.Current.CancellationToken));
    }
    [Theory]
    [InlineData("../database/scout.db")]
    [InlineData("documents/../scout.db")]
    [InlineData("C:/outside.txt")]
    [InlineData("//host/share/a.txt")]
    [InlineData("\\\\?\\C:\\a.txt")]
    [InlineData("documents/11111111111111111111111111111111/original.txt:stream")]
    [InlineData("documents/11111111111111111111111111111111/../original.txt")]
    public async Task UntrustedPathsAreRejected(string path)
    {
        await using var db = await TemporaryDatabase.CreateAsync();
        Assert.Throws<IOException>(() => new DocumentFileStore(db.Paths).Resolve(path));
    }
    [Fact]
    public async Task InvalidFilesInterruptedImportsAndCollisionsRetainEvidence()
    {
        await using var db = await TemporaryDatabase.CreateAsync();
        var store = new DocumentFileStore(db.Paths);
        var docs = new DocumentOperations(db.Factory, store);
        await Assert.ThrowsAsync<ValidationException>(() => docs.ImportAsync("file.html", new MemoryStream("html"u8.ToArray()), new(), token: TestContext.Current.CancellationToken));
        await Assert.ThrowsAsync<DocumentImportException>(() => store.ImportAsync("fake.pdf", new MemoryStream("not pdf"u8.ToArray()), TestContext.Current.CancellationToken));
        await Assert.ThrowsAsync<DocumentImportException>(() => store.ImportAsync("file.txt", new FailureStream(), TestContext.Current.CancellationToken));
        await Assert.ThrowsAsync<DocumentImportException>(() => store.ImportAsync("large.txt", new RepeatingStream(DocumentFileStore.MaximumBytes + 1), TestContext.Current.CancellationToken));
        using var cancelled = new CancellationTokenSource();
        cancelled.Cancel();
        await Assert.ThrowsAsync<DocumentImportException>(() => store.ImportAsync("cancelled.txt", new MemoryStream("bytes"u8.ToArray()), cancelled.Token));
        var token = Guid.NewGuid();
        var good = await store.ImportAsync("file.txt", new MemoryStream("original"u8.ToArray()), cancellationToken: TestContext.Current.CancellationToken, storageId: token);
        await Assert.ThrowsAsync<DocumentImportException>(() => store.ImportAsync("file.txt", new MemoryStream("replacement"u8.ToArray()), cancellationToken: TestContext.Current.CancellationToken, storageId: token));
        Assert.Equal("original", await File.ReadAllTextAsync(store.Resolve(good.RelativePath), TestContext.Current.CancellationToken));
        await using var ctx = await db.Factory.CreateDbContextAsync(TestContext.Current.CancellationToken);
        await ctx.Database.ExecuteSqlRawAsync("CREATE TRIGGER RejectDocument BEFORE INSERT ON Documents BEGIN SELECT RAISE(ABORT, 'Test'); END;", TestContext.Current.CancellationToken);
        var ex = await Assert.ThrowsAsync<DocumentImportException>(() => docs.ImportAsync("orphan.txt", new MemoryStream("retained"u8.ToArray()), new(), token: TestContext.Current.CancellationToken));
        Assert.True(File.Exists(store.Resolve(ex.RelativePath)));
        Assert.Empty(await docs.ListAsync());
        Assert.NotEmpty(Directory.GetFiles(db.Paths.AssetDirectory, "*.partial", SearchOption.AllDirectories));
    }
    [Fact]
    public async Task LinkedStorageDirectoryIsRejected()
    {
        await using var db = await TemporaryDatabase.CreateAsync();
        var store = new DocumentFileStore(db.Paths);
        var outside = Path.Combine(db.Paths.Root, "outside");
        Directory.CreateDirectory(outside);
        var link = Path.Combine(db.Paths.AssetDirectory, "documents");
        if (OperatingSystem.IsWindows())
        {
            var start = new System.Diagnostics.ProcessStartInfo("cmd.exe") { UseShellExecute = false, CreateNoWindow = true, RedirectStandardOutput = true, RedirectStandardError = true };
            foreach (var arg in new[] { "/c", "mklink", "/J", link, outside })
                start.ArgumentList.Add(arg);
            using var process = System.Diagnostics.Process.Start(start)!;
            await process.WaitForExitAsync(TestContext.Current.CancellationToken);
            Assert.Equal(0, process.ExitCode);
        }
        else
            Directory.CreateSymbolicLink(link, outside);
        try
        {
            Assert.Throws<IOException>(() => store.Resolve($"documents/{Guid.NewGuid():N}/original.txt"));
            Assert.Empty(Directory.GetFiles(outside));
        }
        finally { Directory.Delete(link); }
    }
    private sealed class FailureStream : MemoryStream
    {
        public override ValueTask<int> ReadAsync(Memory<byte> buffer, CancellationToken cancellationToken = default) => throw new IOException("Injected read failure");
    }
    private sealed class RepeatingStream(long remaining) : Stream
    {
        public override ValueTask<int> ReadAsync(Memory<byte> buffer, CancellationToken token = default)
        {
            var n = (int)Math.Min(buffer.Length, remaining);
            buffer.Span[..n].Fill(1);
            remaining -= n;
            return ValueTask.FromResult(n);
        }
        public override bool CanRead => true; public override bool CanSeek => false; public override bool CanWrite => false; public override long Length => throw new NotSupportedException(); public override long Position
        {
            get => throw new NotSupportedException(); set => throw new NotSupportedException();
        }
        public override int Read(byte[] b, int o, int c) => throw new NotSupportedException(); public override void Flush() => throw new NotSupportedException(); public override long Seek(long o, SeekOrigin s) => throw new NotSupportedException(); public override void SetLength(long v) => throw new NotSupportedException(); public override void Write(byte[] b, int o, int c) => throw new NotSupportedException();
    }
}


