using System.ComponentModel.DataAnnotations;
using Microsoft.EntityFrameworkCore;
using Scout.Web.Data;
using Scout.Web.Domain;
using Scout.Web.Storage;
namespace Scout.Web.Features.Documents;

public enum DocumentOwnerKind
{
    None, Company, Position, Application, Event, Interview
}
public readonly record struct DocumentOwner(DocumentOwnerKind Kind, int Id);
public sealed class DocumentEditModel
{
    [StringLength(100000)]
    public string? Description
    {
        get; set;
    }
    [StringLength(200)]
    public string? Category
    {
        get; set;
    }
}
public sealed class DocumentOperations(IDbContextFactory<ScoutDbContext> factory, DocumentFileStore files)
{
    public async Task<IReadOnlyList<DocumentListItem>> ListAsync(ArchiveFilter archive = ArchiveFilter.Active, string? search = null, string? category = null)
    {
        await using var db = await factory.CreateDbContextAsync();
        var q = db.Documents.AsNoTracking().Where(d => archive == ArchiveFilter.All || d.IsArchived == (archive == ArchiveFilter.Archived));
        if (!string.IsNullOrWhiteSpace(category))
            q = q.Where(d => d.Category == category);
        var rows = await q.Select(d => new DocumentListItem(d.Id, d.OriginalName, d.Description, d.Category, d.ContentType, d.Size, d.ImportedAtUtc, d.IsArchived)).ToListAsync();
        return rows.Where(d => string.IsNullOrWhiteSpace(search) || $"{d.OriginalName} {d.Description}".Contains(search.Trim(), StringComparison.OrdinalIgnoreCase)).OrderByDescending(d => d.ImportedAtUtc).ThenBy(d => d.Id).ToList();
    }
    public async Task<Document?> GetAsync(int id)
    {
        await using var db = await factory.CreateDbContextAsync();
        return await db.Documents.AsNoTracking().SingleOrDefaultAsync(d => d.Id == id);
    }
    public async Task<int> ImportAsync(string originalName, Stream input, DocumentEditModel m, DocumentOwner owner = default, CancellationToken token = default, Scout.Web.Features.Cvs.CvEditModel? preparedCv = null)
    {
        InputRules.Validate(m);
        if (preparedCv is not null)
        {
            InputRules.Validate(preparedCv);
            if (string.IsNullOrWhiteSpace(preparedCv.DisplayName) || !string.Equals(Path.GetExtension(originalName), ".pdf", StringComparison.OrdinalIgnoreCase))
                throw new ValidationException("A prepared CV needs a label and a PDF file.");
        }
        if (string.IsNullOrWhiteSpace(originalName) || originalName.Length > 255)
            throw new ValidationException("Choose a filename of at most 255 characters.");
        await using (var check = await factory.CreateDbContextAsync(token))
            await ValidateOwnerAsync(check, owner);
        var stored = await files.ImportAsync(originalName, input, token);
        var now = DateTime.UtcNow;
        var d = new Document { OriginalName = originalName, StoredRelativePath = stored.RelativePath, ContentType = stored.ContentType, Size = stored.Size, Sha256 = stored.Sha256, ImportedAtUtc = now, CreatedAtUtc = now, UpdatedAtUtc = now, Description = InputRules.Text(m.Description), Category = InputRules.Trim(m.Category) };
        try
        {
            await using var db = await factory.CreateDbContextAsync(token);
            await ValidateOwnerAsync(db, owner);
            db.Documents.Add(d);
            AddLink(db, owner, d);
            if (preparedCv is not null)
                db.PreparedCvs.Add(new()
                {
                    Document = d,
                    DisplayName = preparedCv.DisplayName.Trim(),
                    Language = InputRules.Trim(preparedCv.Language),
                    TechnologyFocus = InputRules.Trim(preparedCv.TechnologyFocus),
                    CreatedAtUtc = now,
                    UpdatedAtUtc = now
                });
            await db.SaveChangesAsync(token);
            return d.Id;
        }
        catch (Exception ex) when (ex is DbUpdateException or Microsoft.Data.Sqlite.SqliteException or ValidationException or OperationCanceledException or IOException or UnauthorizedAccessException)
        {
            // A commit may have succeeded before its acknowledgement failed. Resolve this exact import before offering a new attempt.
            try
            {
                await using var check = await factory.CreateDbContextAsync();
                var found = await check.Documents.AsNoTracking().SingleOrDefaultAsync(x => x.StoredRelativePath == stored.RelativePath);
                if (found is not null)
                    return found.Id;
            }
            catch (Exception lookup) when (lookup is Microsoft.Data.Sqlite.SqliteException or DbUpdateException or IOException or UnauthorizedAccessException) { }
            throw new DocumentImportException($"The file is retained at assets/{stored.RelativePath}; its registration could not be confirmed. Your draft is retained. Check storage access before trying another import.", stored.RelativePath, ex);
        }
    }
    public async Task<bool> UpdateAsync(int id, DocumentEditModel m)
    {
        InputRules.Validate(m);
        await using var db = await factory.CreateDbContextAsync();
        var d = await db.Documents.FindAsync(id);
        if (d is null)
            return false;
        d.Description = InputRules.Text(m.Description);
        d.Category = InputRules.Trim(m.Category);
        d.UpdatedAtUtc = DateTime.UtcNow;
        await db.SaveChangesAsync();
        return true;
    }
    public async Task<bool> SetArchivedAsync(int id, bool archived)
    {
        await using var db = await factory.CreateDbContextAsync();
        var d = await db.Documents.FindAsync(id);
        if (d is null)
            return false;
        if (d.IsArchived != archived)
        {
            d.IsArchived = archived;
            d.UpdatedAtUtc = DateTime.UtcNow;
            await db.SaveChangesAsync();
        }
        return true;
    }

    internal static async Task ValidateOwnerAsync(ScoutDbContext db, DocumentOwner owner)
    {
        var valid = owner.Kind switch
        {
            DocumentOwnerKind.None => owner.Id == 0,
            DocumentOwnerKind.Company => await db.Companies.AnyAsync(x => x.Id == owner.Id && !x.IsArchived),
            DocumentOwnerKind.Position => await db.Positions.AnyAsync(x => x.Id == owner.Id && !x.IsArchived),
            DocumentOwnerKind.Application => await db.Applications.AnyAsync(x => x.Id == owner.Id && !x.IsArchived),
            DocumentOwnerKind.Event => await db.TimelineEvents.AnyAsync(x => x.Id == owner.Id && !x.IsArchived),
            DocumentOwnerKind.Interview => await db.Interviews.AnyAsync(x => x.Id == owner.Id && !x.IsArchived),
            _ => false
        };
        if (!valid)
            throw new ValidationException("Choose an existing active attachment owner. Restore it before adding files.");
    }
    private static void AddLink(ScoutDbContext db, DocumentOwner owner, Document d)
    {
        switch (owner.Kind)
        {
            case DocumentOwnerKind.Company:
                db.CompanyDocuments.Add(new()
                {
                    CompanyId = owner.Id,
                    Document = d
                });
                break;
            case DocumentOwnerKind.Position:
                db.PositionDocuments.Add(new()
                {
                    PositionId = owner.Id,
                    Document = d
                });
                break;
            case DocumentOwnerKind.Application:
                db.ApplicationDocuments.Add(new()
                {
                    ApplicationId = owner.Id,
                    Document = d
                });
                break;
            case DocumentOwnerKind.Event:
                db.TimelineEventDocuments.Add(new()
                {
                    EventId = owner.Id,
                    Document = d
                });
                break;
            case DocumentOwnerKind.Interview:
                db.InterviewDocuments.Add(new()
                {
                    InterviewRoundId = owner.Id,
                    Document = d
                });
                break;
        }
    }
    public async Task LinkAsync(DocumentOwner owner, int documentId)
    {
        if (owner.Kind == DocumentOwnerKind.None)
            throw new ValidationException("Choose an attachment owner.");
        await using var db = await factory.CreateDbContextAsync();
        if (await Links(db, owner).AnyAsync(d => d.Id == documentId))
            return;
        await ValidateOwnerAsync(db, owner);
        var d = await db.Documents.FindAsync(documentId);
        if (d is null || d.IsArchived)
            throw new ValidationException("Choose an active document.");
        AddLink(db, owner, d);
        await db.SaveChangesAsync();
    }
    public async Task<bool> UnlinkAsync(DocumentOwner owner, int id)
    {
        await using var db = await factory.CreateDbContextAsync();
        object? link = owner.Kind switch
        {
            DocumentOwnerKind.Company => await db.CompanyDocuments.FindAsync(owner.Id, id),
            DocumentOwnerKind.Position => await db.PositionDocuments.FindAsync(owner.Id, id),
            DocumentOwnerKind.Application => await db.ApplicationDocuments.FindAsync(owner.Id, id),
            DocumentOwnerKind.Event => await db.TimelineEventDocuments.FindAsync(owner.Id, id),
            DocumentOwnerKind.Interview => await db.InterviewDocuments.FindAsync(owner.Id, id),
            _ => null
        };
        if (link is not null)
        {
            db.Remove(link);
            await db.SaveChangesAsync();
        }
        return true;
    }
    private static IQueryable<Document> Links(ScoutDbContext db, DocumentOwner o) => o.Kind switch { DocumentOwnerKind.Company => db.CompanyDocuments.Where(l => l.CompanyId == o.Id).Select(l => l.Document), DocumentOwnerKind.Position => db.PositionDocuments.Where(l => l.PositionId == o.Id).Select(l => l.Document), DocumentOwnerKind.Application => db.ApplicationDocuments.Where(l => l.ApplicationId == o.Id).Select(l => l.Document), DocumentOwnerKind.Event => db.TimelineEventDocuments.Where(l => l.EventId == o.Id).Select(l => l.Document), DocumentOwnerKind.Interview => db.InterviewDocuments.Where(l => l.InterviewRoundId == o.Id).Select(l => l.Document), _ => db.Documents.Where(d => false) };
    public async Task<IReadOnlyList<DocumentListItem>> LinkedAsync(DocumentOwner owner)
    {
        await using var db = await factory.CreateDbContextAsync();
        return await Links(db, owner).AsNoTracking().Select(d => new DocumentListItem(d.Id, d.OriginalName, d.Description, d.Category, d.ContentType, d.Size, d.ImportedAtUtc, d.IsArchived)).ToListAsync();
    }
    public async Task<IReadOnlyList<DocumentRelation>> RelationsAsync(int id)
    {
        await using var db = await factory.CreateDbContextAsync();
        var rows = new List<DocumentRelation>();
        rows.AddRange(await db.CompanyDocuments.Where(l => l.DocumentId == id).Select(l => new DocumentRelation(l.Company.Name, "/companies/" + l.CompanyId)).ToListAsync());
        rows.AddRange(await db.PositionDocuments.Where(l => l.DocumentId == id).Select(l => new DocumentRelation(l.Position.Title, "/positions/" + l.PositionId)).ToListAsync());
        rows.AddRange(await db.ApplicationDocuments.Where(l => l.DocumentId == id).Select(l => new DocumentRelation("Application #" + l.ApplicationId, "/applications/" + l.ApplicationId)).ToListAsync());
        rows.AddRange(await db.TimelineEventDocuments.Where(l => l.DocumentId == id).Select(l => new DocumentRelation("Event #" + l.EventId, "/applications/" + l.Event.ApplicationId + "/timeline")).ToListAsync());
        rows.AddRange(await db.InterviewDocuments.Where(l => l.DocumentId == id).Select(l => new DocumentRelation(l.InterviewRound.Title, "/interviews/" + l.InterviewRoundId)).ToListAsync());
        return rows;
    }
}
public sealed record DocumentListItem(int Id, string OriginalName, string? Description, string? Category, string ContentType, long Size, DateTime ImportedAtUtc, bool IsArchived);
public sealed record DocumentRelation(string Label, string Url);
