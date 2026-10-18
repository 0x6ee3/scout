using System.Security.Cryptography;
using System.Text.RegularExpressions;
using Scout.Web.Configuration;
using Scout.Web.Domain;
namespace Scout.Web.Storage;

public sealed partial class DocumentFileStore(StoragePaths paths)
{
    public const long MaximumBytes = 50L * 1024 * 1024;
    public const long MaximumAudioBytes = 1024L * 1024 * 1024;
    public static bool IsAudio(string extension) => extension.ToLowerInvariant() is ".mp3" or ".wav" or ".m4a";
    public static long LimitFor(string name) => IsAudio(Path.GetExtension(name)) ? MaximumAudioBytes : MaximumBytes;
    public const int PreviewBytes = 256 * 1024;
    private static readonly IReadOnlyDictionary<string, string> Types = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
    {
        [".pdf"] = "application/pdf",
        [".txt"] = "text/plain",
        [".md"] = "text/plain",
        [".eml"] = "message/rfc822",
        [".msg"] = "application/vnd.ms-outlook",
        [".mp3"] = "audio/mpeg",
        [".wav"] = "audio/wav",
        [".m4a"] = "audio/mp4"
    };
    public static string MimeFor(string extension) => Types.TryGetValue(extension, out var type) ? type : throw new System.ComponentModel.DataAnnotations.ValidationException("Allowed files: PDF, TXT, MD, EML, MSG, MP3, WAV, M4A.");
    public static bool CanPreview(Document d) => d.ContentType is "application/pdf" or "text/plain";

    [GeneratedRegex(@"^documents/[0-9a-f]{32}/original\.(pdf|txt|md|eml|msg|mp3|wav|m4a)$", RegexOptions.CultureInvariant)]
    private static partial Regex ManagedPath();

    public string Resolve(string relative)
    {
        if (!ManagedPath().IsMatch(relative))
            throw new IOException("The stored document path is invalid.");
        var root = Path.GetFullPath(paths.AssetDirectory);
        var target = Path.GetFullPath(Path.Combine(root, relative.Replace('/', Path.DirectorySeparatorChar)));
        if (!target.StartsWith(root + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase))
            throw new IOException("Document path is outside Scout assets.");
        CheckReparse(root);
        var current = root;
        foreach (var part in Path.GetRelativePath(root, target).Split(Path.DirectorySeparatorChar))
        {
            current = Path.Combine(current, part);
            CheckReparse(current);
        }
        return target;
    }
    private static void CheckReparse(string path)
    {
        try
        {
            if ((File.GetAttributes(path) & FileAttributes.ReparsePoint) != 0)
                throw new IOException("Linked files and directories are not allowed in managed document storage.");
        }
        catch (FileNotFoundException) { }
        catch (DirectoryNotFoundException) { }
    }

    public async Task<StoredFile> ImportAsync(string originalName, Stream input, CancellationToken cancellationToken = default, Guid? storageId = null)
    {
        var extension = Path.GetExtension(originalName).ToLowerInvariant();
        var mime = MimeFor(extension);
        var relative = $"documents/{(storageId ?? Guid.NewGuid()):N}/original{extension}";
        var path = Resolve(relative);
        var partial = path + ".partial";
        try
        {
            Directory.CreateDirectory(Path.GetDirectoryName(path)!);
            Resolve(relative);
            CheckReparse(partial);
            if (File.Exists(path))
                throw new IOException("A retained file already uses this storage identifier.");
            long size = 0;
            byte[] header = new byte[5];
            var headerCount = 0;
            using var hash = IncrementalHash.CreateHash(HashAlgorithmName.SHA256);
            await using (var output = new FileStream(partial, FileMode.CreateNew, FileAccess.Write, FileShare.None, 64 * 1024, FileOptions.Asynchronous))
            {
                var buffer = new byte[64 * 1024];
                int count;
                while ((count = await input.ReadAsync(buffer, cancellationToken)) != 0)
                {
                    if (size + count > LimitFor(originalName))
                        throw new IOException("The file exceeds its import limit (1 GiB audio; 50 MiB other files).");
                    var take = Math.Min(5 - headerCount, count);
                    buffer.AsSpan(0, take).CopyTo(header.AsSpan(headerCount));
                    headerCount += take;
                    hash.AppendData(buffer, 0, count);
                    await output.WriteAsync(buffer.AsMemory(0, count), cancellationToken);
                    size += count;
                }
                if (size == 0)
                    throw new IOException("The selected file is empty.");
                if (extension == ".pdf" && !header.AsSpan().SequenceEqual("%PDF-"u8))
                    throw new IOException("This file does not have a PDF header.");
                await output.FlushAsync(cancellationToken);
                output.Flush(flushToDisk: true);
            }
            cancellationToken.ThrowIfCancellationRequested();
            Resolve(relative);
            CheckReparse(partial);
            File.Move(partial, path, overwrite: false);
            return new(relative, mime, size, Convert.ToHexString(hash.GetHashAndReset()));
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or OperationCanceledException)
        {
            throw new DocumentImportException($"Import did not finish. Any retained file is kept at assets/{relative} (or .partial). Re-select the source to try a new import.", relative, ex);
        }
    }

    public async Task<FileStream> OpenVerifiedAsync(Document document, CancellationToken token = default)
    {
        var path = Resolve(document.StoredRelativePath);
        var extension = Path.GetExtension(path);
        if (document.ContentType != MimeFor(extension) || document.Size < 1 || document.Size > LimitFor(path))
            throw new IOException("The stored document metadata is invalid.");
        var stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read, 64 * 1024, FileOptions.Asynchronous | FileOptions.SequentialScan);
        try
        {
            Resolve(document.StoredRelativePath);
            if (stream.Length != document.Size)
                throw new IOException("The document size changed outside Scout. Restore it from a backup.");
            var hash = Convert.ToHexString(await SHA256.HashDataAsync(stream, token));
            if (!string.Equals(hash, document.Sha256, StringComparison.Ordinal))
                throw new IOException("The document content changed outside Scout. Restore it from a backup.");
            stream.Position = 0;
            return stream;
        }
        catch { await stream.DisposeAsync(); throw; }
    }
}
public sealed record StoredFile(string RelativePath, string ContentType, long Size, string Sha256);
public sealed class DocumentImportException(string message, string relativePath, Exception inner) : IOException(message, inner)
{
    public string RelativePath { get; } = relativePath;
}
