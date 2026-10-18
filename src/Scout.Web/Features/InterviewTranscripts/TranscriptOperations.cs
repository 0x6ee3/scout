using System.ComponentModel.DataAnnotations;
using System.Security.Cryptography;
using System.Text;
using Microsoft.EntityFrameworkCore;
using Scout.Web.Data;
using Scout.Web.Domain;
using Scout.Web.Features.InterviewRecordings;
using Scout.Web.Storage;

namespace Scout.Web.Features.InterviewTranscripts;

public sealed class TranscriptOperations(IDbContextFactory<ScoutDbContext> factory, DocumentFileStore files)
{
    public const int MaximumBytes = 5 * 1024 * 1024;
    public const int MaximumCharacters = 1_000_000;
    public static string TextHash(string text) => Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(text)));
    public async Task<IReadOnlyList<InterviewTranscript>> ListAsync(int round, bool includeArchived = false)
    {
        await using var db = await factory.CreateDbContextAsync();
        return await db.Transcripts.AsNoTracking().Where(x => x.InterviewRoundId == round && (includeArchived || x.ArchivedAtUtc == null)).OrderBy(x => x.Id).ToListAsync();
    }
    public async Task<InterviewTranscript?> GetAsync(int round, int id)
    {
        await using var db = await factory.CreateDbContextAsync();
        return await db.Transcripts.AsNoTracking().SingleOrDefaultAsync(x => x.InterviewRoundId == round && x.Id == id);
    }
    public async Task<int> ImportAsync(int round, string name, Stream input, int? recording, Guid token, CancellationToken ct = default)
    {
        if (!string.Equals(Path.GetExtension(name),".txt",StringComparison.OrdinalIgnoreCase) || name.Length > 255) throw new ValidationException("Choose a UTF-8 TXT transcript with a name up to 255 characters.");
        using var buffer = new MemoryStream();
        var chunk = new byte[65536]; int count;
        while ((count = await input.ReadAsync(chunk,ct)) != 0)
        {
            if (buffer.Length + count > MaximumBytes) throw new ValidationException("Transcript files allow at most 5 MiB.");
            await buffer.WriteAsync(chunk.AsMemory(0,count),ct);
        }
        string text;
        try { text = new UTF8Encoding(false,true).GetString(buffer.GetBuffer(),0,(int)buffer.Length); }
        catch (DecoderFallbackException) { throw new ValidationException("The transcript must be valid UTF-8."); }
        Validate(text,token);
        var payload = RecordingOperations.Hash(new { round, name, recording, text });
        var existing = await RetryAsync(token,payload); if (existing.HasValue) return existing.Value;
        await using (var check = await factory.CreateDbContextAsync(ct)) { await RecordingOperations.ActiveRound(check,round); await Recording(check,round,recording); }
        buffer.Position = 0;
        var stored = await files.ImportAsync(name,buffer,ct);
        var document = new Document { OriginalName=name, StoredRelativePath=stored.RelativePath,ContentType=stored.ContentType,Size=stored.Size,Sha256=stored.Sha256,ImportedAtUtc=DateTime.UtcNow,CreatedAtUtc=DateTime.UtcNow,UpdatedAtUtc=DateTime.UtcNow };
        return await SaveAsync(new() { InterviewRoundId=round,Origin=TranscriptOrigin.Imported,Text=text,Sha256=TextHash(text),OriginalDocument=document,RecordingId=recording,SubmissionToken=token,PayloadHash=payload,CreatedAtUtc=DateTime.UtcNow },ct);
    }
    public async Task<int> CorrectAsync(int round, int parent, string text, Guid token, CancellationToken ct = default)
    {
        Validate(text,token);
        var payload = RecordingOperations.Hash(new { round, parent, text });
        var existing = await RetryAsync(token,payload); if(existing.HasValue) return existing.Value;
        return await SaveAsync(new() { InterviewRoundId=round,Origin=TranscriptOrigin.EditedCopy,ParentTranscriptId=parent,Text=text,Sha256=TextHash(text),SubmissionToken=token,PayloadHash=payload,CreatedAtUtc=DateTime.UtcNow },ct);
    }
    private static void Validate(string text,Guid token)
    {
        if(token == Guid.Empty || text.Length is < 1 or > MaximumCharacters) throw new ValidationException("Transcript text requires 1–1,000,000 characters and a submission token.");
    }
    private static async Task Recording(ScoutDbContext db,int round,int? recording)
    {
        if(recording.HasValue && !await db.Recordings.AnyAsync(x=>x.Id==recording && x.InterviewRoundId==round)) throw new ValidationException("Choose a recording belonging to this interview.");
    }
    private async Task<int?> RetryAsync(Guid token,string payload)
    {
        await using var db=await factory.CreateDbContextAsync();
        var row=await db.Transcripts.AsNoTracking().SingleOrDefaultAsync(x=>x.SubmissionToken==token);
        return row is null ? null : row.PayloadHash==payload ? row.Id : throw new ValidationException("This submission token was already used with different content.");
    }
    private async Task<int> SaveAsync(InterviewTranscript row,CancellationToken ct)
    {
        await using var db=await factory.CreateDbContextAsync(ct);
        await using var tx=await db.Database.BeginTransactionAsync(ct);
        var retry=await db.Transcripts.SingleOrDefaultAsync(x=>x.SubmissionToken==row.SubmissionToken,ct);
        if(retry is not null) return retry.PayloadHash==row.PayloadHash ? retry.Id : throw new ValidationException("Submission token content changed.");
        await RecordingOperations.ActiveRound(db,row.InterviewRoundId);
        if(row.ParentTranscriptId.HasValue)
        {
            var parent=await db.Transcripts.SingleOrDefaultAsync(x=>x.Id==row.ParentTranscriptId && x.InterviewRoundId==row.InterviewRoundId,ct) ?? throw new ValidationException("Choose a parent transcript in this interview.");
            row.OriginalDocumentId=parent.OriginalDocumentId; row.RecordingId=parent.RecordingId;
        }
        await Recording(db,row.InterviewRoundId,row.RecordingId);
        if(row.Origin==TranscriptOrigin.Imported) db.InterviewDocuments.Add(new(){InterviewRoundId=row.InterviewRoundId,Document=row.OriginalDocument!});
        db.Transcripts.Add(row);
        try { await db.SaveChangesAsync(ct);await tx.CommitAsync(ct);return row.Id; }
        catch(Exception ex) when(ex is DbUpdateException or Microsoft.Data.Sqlite.SqliteException or OperationCanceledException or IOException or UnauthorizedAccessException)
        {
            await tx.DisposeAsync();
            var found=await RetryAsync(row.SubmissionToken,row.PayloadHash);if(found.HasValue)return found.Value;
            throw new IOException("Transcript registration could not be confirmed. Originals are retained. Retry the same token before a new import.",ex);
        }
    }
    public async Task ArchiveAsync(int round,int id,bool archive)
    {
        await using var db=await factory.CreateDbContextAsync();var row=await db.Transcripts.SingleOrDefaultAsync(x=>x.Id==id && x.InterviewRoundId==round) ?? throw new ValidationException("Transcript not found.");
        row.ArchivedAtUtc=archive?DateTime.UtcNow:null;await db.SaveChangesAsync();
    }
}
