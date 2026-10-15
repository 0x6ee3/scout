using System.ComponentModel.DataAnnotations;
using Microsoft.EntityFrameworkCore;
using Scout.Web.Data;
using Scout.Web.Domain;
using Scout.Web.Features.Documents;
namespace Scout.Web.Features.Cvs;

public sealed class CvEditModel
{
    [Required, StringLength(200)] public string DisplayName { get; set; } = "";
    [StringLength(20)]
    public string? Language
    {
        get; set;
    }
    [StringLength(200)]
    public string? TechnologyFocus
    {
        get; set;
    }
}
public sealed class CvOperations(IDbContextFactory<ScoutDbContext> factory, DocumentOperations documents)
{
    public async Task<IReadOnlyList<CvListItem>> ListAsync(ArchiveFilter archive = ArchiveFilter.Active, string? search = null)
    {
        await using var db = await factory.CreateDbContextAsync();
        var rows = await db.PreparedCvs.AsNoTracking().Where(c => archive == ArchiveFilter.All || c.IsArchived == (archive == ArchiveFilter.Archived)).Select(c => new CvListItem(c.Id, c.DocumentId, c.DisplayName, c.Language, c.TechnologyFocus, c.IsArchived, c.Document.IsArchived)).ToListAsync();
        return rows.Where(c => string.IsNullOrWhiteSpace(search) || $"{c.DisplayName} {c.Language} {c.TechnologyFocus}".Contains(search.Trim(), StringComparison.OrdinalIgnoreCase)).OrderByDescending(c => c.Id).ToList();
    }
    public async Task<PreparedCv?> GetAsync(int id)
    {
        await using var db = await factory.CreateDbContextAsync();
        return await db.PreparedCvs.AsNoTracking().Include(c => c.Document).SingleOrDefaultAsync(c => c.Id == id);
    }
    public async Task<int> ImportAsync(string name, Stream input, CvEditModel m, CancellationToken token = default)
    {
        var documentId = await documents.ImportAsync(name, input, new()
        {
            Category = "CV"
        }, token: token, preparedCv: m);
        await using var db = await factory.CreateDbContextAsync(token);
        return await db.PreparedCvs.Where(c => c.DocumentId == documentId).Select(c => c.Id).SingleAsync(token);
    }
    public async Task<bool> UpdateAsync(int id, CvEditModel m)
    {
        InputRules.Validate(m);
        if (string.IsNullOrWhiteSpace(m.DisplayName))
            throw new ValidationException("Enter a CV label.");
        await using var db = await factory.CreateDbContextAsync();
        var c = await db.PreparedCvs.FindAsync(id);
        if (c is null)
            return false;
        c.DisplayName = m.DisplayName.Trim();
        c.Language = InputRules.Trim(m.Language);
        c.TechnologyFocus = InputRules.Trim(m.TechnologyFocus);
        c.UpdatedAtUtc = DateTime.UtcNow;
        await db.SaveChangesAsync();
        return true;
    }
    public async Task<bool> SetArchivedAsync(int id, bool archived)
    {
        await using var db = await factory.CreateDbContextAsync();
        var c = await db.PreparedCvs.FindAsync(id);
        if (c is null)
            return false;
        if (c.IsArchived != archived)
        {
            c.IsArchived = archived;
            c.UpdatedAtUtc = DateTime.UtcNow;
            await db.SaveChangesAsync();
        }
        return true;
    }
}
public sealed record CvListItem(int Id, int DocumentId, string DisplayName, string? Language, string? TechnologyFocus, bool IsArchived, bool DocumentIsArchived);
