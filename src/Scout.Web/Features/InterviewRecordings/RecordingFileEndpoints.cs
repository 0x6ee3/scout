using Microsoft.EntityFrameworkCore;
using Scout.Web.Data;
using Scout.Web.Domain;
using Scout.Web.Storage;

namespace Scout.Web.Features.InterviewRecordings;

public static class RecordingFileEndpoints
{
    public static void MapRecordingFileEndpoints(this WebApplication app)
    {
        app.MapMethods("/interviews/{round:int}/recordings/{id:int}/{action}", ["GET", "HEAD"], Read);
    }
    private static async Task<IResult> Read(int round, int id, string action, HttpContext context, IDbContextFactory<ScoutDbContext> factory, DocumentFileStore files, LinkedRecordingStore links)
    {
        context.Response.Headers.CacheControl = "private, no-store";
        context.Response.Headers.XContentTypeOptions = "nosniff";
        if (action is not ("media" or "download")) return Results.NotFound();
        await using var db = await factory.CreateDbContextAsync(context.RequestAborted);
        var row = await db.Recordings.AsNoTracking().Include(x => x.Document).Include(x => x.ConsentDecision).SingleOrDefaultAsync(x => x.Id == id && x.InterviewRoundId == round, context.RequestAborted);
        if (row is null || row.ConsentDecision.InterviewRoundId != round || row.ConsentDecision.Choice != RecordingChoice.ExternalConsentAcknowledged) return Results.NotFound();
        try
        {
            FileStream stream;
            if (row.StorageKind == RecordingStorage.ManagedDocument)
            {
                if (row.Document is null || row.WorkingRelativePath is not null || !await db.InterviewDocuments.AnyAsync(x => x.InterviewRoundId == round && x.DocumentId == row.DocumentId, context.RequestAborted)) return Results.NotFound();
                if (row.Size != row.Document.Size || row.Sha256 != row.Document.Sha256 || row.ContentType != row.Document.ContentType || !DocumentFileStore.IsAudio(Path.GetExtension(row.Document.StoredRelativePath))) throw new IOException("Recording metadata changed.");
                stream = await files.OpenVerifiedAsync(row.Document, context.RequestAborted);
            }
            else stream = await links.OpenVerifiedAsync(row, context.RequestAborted);
            return Results.File(stream, row.ContentType, fileDownloadName: action == "download" ? "recording" + Path.GetExtension(row.OriginalName).ToLowerInvariant() : null, enableRangeProcessing: true);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            return Results.Text("Recording unavailable. Check the original file or restore a backup. Changed bytes need new registration.", statusCode: StatusCodes.Status409Conflict);
        }
    }
}
