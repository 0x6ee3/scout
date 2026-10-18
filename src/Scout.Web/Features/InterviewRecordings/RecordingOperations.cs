using System.ComponentModel.DataAnnotations;
using System.Security.Cryptography;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Scout.Web.Data;
using Scout.Web.Domain;
using Scout.Web.Storage;

namespace Scout.Web.Features.InterviewRecordings;

public sealed class RecordingOperations(IDbContextFactory<ScoutDbContext> factory, DocumentFileStore files)
{
    public async Task<int> RegisterLinkAsync(int round, string path, LinkedRecordingStore links, bool consent, string? note, Guid token, CancellationToken ct = default)
    {
        Validate(Path.GetFileName(path), consent, note, token);
        var stored = await links.InspectAsync(path, ct);
        return await RegisterAsync(round, null, path, Path.GetFileName(path), stored, consent, note, token, ct);
    }
    internal static string Hash(object value) => Convert.ToHexString(SHA256.HashData(JsonSerializer.SerializeToUtf8Bytes(value)));
    internal static async Task ActiveRound(ScoutDbContext db, int round)
    {
        if (!await db.Interviews.AnyAsync(x => x.Id == round && !x.IsArchived))
            throw new ValidationException("Restore an existing interview before adding review material.");
    }
    internal static void Note(string? note)
    {
        if (note?.Length > 1000) throw new ValidationException("Consent notes allow at most 1,000 characters.");
    }
    public async Task ChooseAsync(int round, RecordingChoice choice, string? note = null)
    {
        Note(note);
        if (!Enum.IsDefined(choice)) throw new ValidationException("Choose a recording option.");
        await using var db = await factory.CreateDbContextAsync();
        await using var tx = await db.Database.BeginTransactionAsync();
        await ActiveRound(db, round);
        db.RecordingDecisions.Add(Decision(round, choice, note));
        await db.SaveChangesAsync();
        await tx.CommitAsync();
    }
    private static InterviewRecordingDecision Decision(int round, RecordingChoice choice, string? note) => new()
    { InterviewRoundId = round, Choice = choice, PrivateNote = note, AcknowledgedAtUtc = DateTime.UtcNow, CreatedAtUtc = DateTime.UtcNow };

    public async Task<IReadOnlyList<InterviewRecordingDecision>> HistoryAsync(int round)
    {
        await using var db = await factory.CreateDbContextAsync();
        return await db.RecordingDecisions.AsNoTracking().Where(x => x.InterviewRoundId == round).OrderByDescending(x => x.Id).ToListAsync();
    }
    public async Task<IReadOnlyList<InterviewRecording>> ListAsync(int round, bool includeArchived = false)
    {
        await using var db = await factory.CreateDbContextAsync();
        return await db.Recordings.AsNoTracking().Where(x => x.InterviewRoundId == round && (includeArchived || x.ArchivedAtUtc == null)).OrderBy(x => x.Id).ToListAsync();
    }
    public async Task<int> RegisterDocumentAsync(int round, int documentId, bool consent, string? note, Guid token, CancellationToken ct = default)
        => await RegisterAsync(round, documentId, null, null, null, consent, note, token, ct);

    public async Task<int> UploadAsync(int round, string name, Stream input, bool consent, string? note, Guid token, CancellationToken ct = default)
    {
        Validate(name, consent, note, token);
        await using (var db = await factory.CreateDbContextAsync(ct))
        {
            var existing = await db.Recordings.AsNoTracking().SingleOrDefaultAsync(x => x.RegistrationToken == token, ct);
            if (existing is not null)
            {
                var sha = await AudioHash(input, ct);
                var payload = Hash(new { round, documentId = (int?)null, path = (string?)null, name, sha, note });
                return Resolve(existing, payload);
            }
            await ActiveRound(db, round);
        }
        var stored = await files.ImportAsync(name, input, ct);
        return await RegisterAsync(round, null, null, name, stored, consent, note, token, ct);
    }

    internal async Task<int> RegisterAsync(int round, int? documentId, string? path, string? name, StoredFile? stored, bool consent, string? note, Guid token, CancellationToken ct)
    {
        Note(note);
        if (!consent || token == Guid.Empty) throw new ValidationException("Confirm that externally obtained consent covers this selected recording.");
        await using var db = await factory.CreateDbContextAsync(ct);
        await using var tx = await db.Database.BeginTransactionAsync(ct);
        Document? document = null;
        if (documentId.HasValue)
        {
            document = await db.InterviewDocuments.Where(x => x.InterviewRoundId == round && x.DocumentId == documentId).Select(x => x.Document).SingleOrDefaultAsync(ct)
                ?? throw new ValidationException("Select an audio attachment belonging to this interview.");
            Validate(document.OriginalName, consent, note, token);
            await using var verified = await files.OpenVerifiedAsync(document, ct);
            name = document.OriginalName;
            stored = new(document.StoredRelativePath, document.ContentType, document.Size, document.Sha256);
        }
        Validate(name!, consent, note, token);
        var sha = stored!.Sha256;
        var payload = Hash(new { round, documentId, path, name, sha, note });
        var existing = await db.Recordings.SingleOrDefaultAsync(x => x.RegistrationToken == token, ct);
        if (existing is not null) return Resolve(existing, payload);
        await ActiveRound(db, round);
        if (document?.IsArchived == true) throw new ValidationException("Restore the attachment first.");
        if (document is null && path is null)
        {
            document = new() { OriginalName = name!, StoredRelativePath = stored.RelativePath, ContentType = stored.ContentType, Size = stored.Size, Sha256 = sha, ImportedAtUtc = DateTime.UtcNow, CreatedAtUtc = DateTime.UtcNow, UpdatedAtUtc = DateTime.UtcNow };
            db.InterviewDocuments.Add(new() { InterviewRoundId = round, Document = document });
        }
        var row = new InterviewRecording { InterviewRoundId = round, ConsentDecision = Decision(round, RecordingChoice.ExternalConsentAcknowledged, note), Document = document, StorageKind = path is null ? RecordingStorage.ManagedDocument : RecordingStorage.LinkedWorkingFile, WorkingRelativePath = path, OriginalName = name!, ContentType = stored.ContentType, Size = stored.Size, Sha256 = sha, RegistrationToken = token, PayloadHash = payload, CreatedAtUtc = DateTime.UtcNow };
        db.Recordings.Add(row);
        try
        {
            await db.SaveChangesAsync(ct);
            await tx.CommitAsync(ct);
            return row.Id;
        }
        catch (Exception ex) when (ex is DbUpdateException or Microsoft.Data.Sqlite.SqliteException or OperationCanceledException or IOException or UnauthorizedAccessException)
        {
            await tx.DisposeAsync();
            await using var check = await factory.CreateDbContextAsync();
            var found = await check.Recordings.AsNoTracking().SingleOrDefaultAsync(x => x.RegistrationToken == token);
            if (found is not null) return Resolve(found, payload);
            throw new IOException("Registration could not be confirmed. Retained files are preserved; retry with the same token before starting another import.", ex);
        }
    }
    private static int Resolve(InterviewRecording row, string payload) => row.PayloadHash == payload ? row.Id : throw new ValidationException("This submission token was already used with different content.");
    private static async Task<string> AudioHash(Stream input, CancellationToken ct)
    {
        using var hash = IncrementalHash.CreateHash(HashAlgorithmName.SHA256);
        var buffer = new byte[65536]; long size = 0; int count;
        while ((count = await input.ReadAsync(buffer, ct)) != 0)
        {
            size += count;
            if (size > DocumentFileStore.MaximumAudioBytes) throw new ValidationException("Audio allows at most 1 GiB.");
            hash.AppendData(buffer, 0, count);
        }
        if (size == 0) throw new ValidationException("Audio cannot be empty.");
        return Convert.ToHexString(hash.GetHashAndReset());
    }
    private static void Validate(string name, bool consent, string? note, Guid token)
    {
        Note(note);
        if (!consent || token == Guid.Empty) throw new ValidationException("Confirm externally obtained consent for the selected recording.");
        if (string.IsNullOrWhiteSpace(name) || name.Length > 255 || !DocumentFileStore.IsAudio(Path.GetExtension(name))) throw new ValidationException("Select MP3, WAV or M4A audio with a name up to 255 characters.");
    }
    public async Task ArchiveAsync(int round, int id, bool archive)
    {
        await using var db = await factory.CreateDbContextAsync();
        var row = await db.Recordings.SingleOrDefaultAsync(x => x.Id == id && x.InterviewRoundId == round) ?? throw new ValidationException("Recording not found.");
        row.ArchivedAtUtc = archive ? DateTime.UtcNow : null;
        await db.SaveChangesAsync();
    }
}
