using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text;
using Microsoft.Win32.SafeHandles;
using Scout.Web.Configuration;
using Scout.Web.Domain;

namespace Scout.Web.Storage;

public sealed class LinkedRecordingStore(StoragePaths paths)
{
    public string Root => Path.Combine(paths.Root, "recordings");
    public string Resolve(string relative)
    {
        if (string.IsNullOrWhiteSpace(relative) || Path.IsPathRooted(relative) || relative.Contains(':') || relative.Contains('\\') || relative.Split('/').Any(x => x is "" or "." or ".." || x.IndexOfAny(Path.GetInvalidFileNameChars()) >= 0 || x.EndsWith(' ') || x.EndsWith('.')))
            throw new IOException("Use a relative path beneath Scout/recordings, with forward slashes and no traversal.");
        if (!DocumentFileStore.IsAudio(Path.GetExtension(relative)))
            throw new IOException("Select MP3, WAV or M4A audio.");
        var path = Path.GetFullPath(Path.Combine(Root, relative));
        if (!path.StartsWith(Path.GetFullPath(Root) + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase))
            throw new IOException("Recording is outside the working directory.");
        for (var current = path; current is not null; current = Path.GetDirectoryName(current))
            if ((File.Exists(current) || Directory.Exists(current)) && (File.GetAttributes(current) & FileAttributes.ReparsePoint) != 0)
                throw new IOException("Linked or reparse-point paths are not allowed.");
        return path;
    }
    private FileStream Open(string relative)
    {
        var path = Resolve(relative);
        var stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read, 65536, FileOptions.Asynchronous | FileOptions.SequentialScan);
        try
        {
            Resolve(relative);
            // Check the opened handle as well as path components, closing the path/open race.
            if (OperatingSystem.IsWindows())
            {
                var final = new StringBuilder(32768);
                var length = GetFinalPathNameByHandle(stream.SafeFileHandle, final, (uint)final.Capacity, 0);
                if (length == 0 || length >= final.Capacity || !string.Equals(final.ToString(), @"\\?\" + path, StringComparison.OrdinalIgnoreCase))
                    throw new IOException("The opened recording does not match the checked working path.");
            }
            if (stream.Length < 1 || stream.Length > DocumentFileStore.MaximumAudioBytes)
                throw new IOException("Audio must be nonempty and at most 1 GiB.");
            return stream;
        }
        catch { stream.Dispose(); throw; }
    }
    public async Task<StoredFile> InspectAsync(string relative, CancellationToken ct = default)
    {
        await using var stream = Open(relative);
        return new(relative, DocumentFileStore.MimeFor(Path.GetExtension(relative)), stream.Length, Convert.ToHexString(await SHA256.HashDataAsync(stream, ct)));
    }
    public async Task<FileStream> OpenVerifiedAsync(InterviewRecording recording, CancellationToken ct = default)
    {
        if (recording.StorageKind != RecordingStorage.LinkedWorkingFile || recording.DocumentId.HasValue || recording.WorkingRelativePath is null || recording.ContentType != DocumentFileStore.MimeFor(Path.GetExtension(recording.WorkingRelativePath)))
            throw new IOException("Invalid recording metadata.");
        var stream = Open(recording.WorkingRelativePath);
        try
        {
            if (stream.Length != recording.Size || Convert.ToHexString(await SHA256.HashDataAsync(stream, ct)) != recording.Sha256)
                throw new IOException("Recording changed outside Scout. Register the changed file explicitly with a new acknowledgment.");
            stream.Position = 0;
            return stream;
        }
        catch { await stream.DisposeAsync(); throw; }
    }
    [DllImport("kernel32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
    private static extern uint GetFinalPathNameByHandle(SafeFileHandle handle, StringBuilder path, uint length, uint flags);
}
