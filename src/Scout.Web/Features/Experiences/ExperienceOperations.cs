using System.ComponentModel.DataAnnotations;
using Microsoft.EntityFrameworkCore;
using Scout.Web.Data;
using Scout.Web.Domain;

namespace Scout.Web.Features.Experiences;

public sealed class ExperienceEditModel
{
    [Required, StringLength(200)] public string Title { get; set; } = "";
    [StringLength(1000)] public string? MemoryCue { get; set; }
    [StringLength(100000)] public string? Situation { get; set; }
    [StringLength(100000)] public string? Task { get; set; }
    [StringLength(100000)] public string? Action { get; set; }
    [StringLength(100000)] public string? Result { get; set; }
    [StringLength(100000)] public string? Reflection { get; set; }
    [StringLength(100000)] public string? UsefulFor { get; set; }
    [StringLength(100000)] public string? Notes { get; set; }
    public List<string> Tags { get; set; } = [];
}

public sealed record ExperienceListItem(int Id, string Title, string? MemoryCue, bool IsArchived, IReadOnlyList<string> Tags);

public sealed class ExperienceOperations(IDbContextFactory<ScoutDbContext> factory)
{
    public async Task<ProfessionalExperience?> GetAsync(int id)
    {
        await using var db = await factory.CreateDbContextAsync();
        return await db.ProfessionalExperiences.AsNoTracking().Include(x => x.Tags).SingleOrDefaultAsync(x => x.Id == id);
    }

    public async Task<IReadOnlyList<ExperienceListItem>> ListAsync(ArchiveFilter archive = ArchiveFilter.Active, string? search = null, string? tag = null)
    {
        await using var db = await factory.CreateDbContextAsync();
        var query = db.ProfessionalExperiences.AsNoTracking().Where(x => archive == ArchiveFilter.All || x.IsArchived == (archive == ArchiveFilter.Archived));
        if (!string.IsNullOrWhiteSpace(tag)) { var key = Tags.Normalize([tag]).Single(); query = query.Where(x => x.Tags.Any(t => t.Tag == key)); }
        var needle = search?.Trim(); var searching = !string.IsNullOrEmpty(needle);
        var rows = await query.Select(x => new { x.Id, x.Title, x.MemoryCue, x.IsArchived,
            Situation = searching ? x.Situation : null, Task = searching ? x.Task : null, Action = searching ? x.Action : null,
            Result = searching ? x.Result : null, Reflection = searching ? x.Reflection : null, UsefulFor = searching ? x.UsefulFor : null,
            Notes = searching ? x.Notes : null, Tags = x.Tags.Select(t => t.Tag).ToList() }).ToListAsync();
        return rows.Where(x => !searching || new[] { x.Title, x.MemoryCue, x.Situation, x.Task, x.Action, x.Result, x.Reflection, x.UsefulFor, x.Notes }.Concat(x.Tags).Any(v => v?.Contains(needle!, StringComparison.OrdinalIgnoreCase) == true))
            .OrderBy(x => x.Title, StringComparer.OrdinalIgnoreCase).ThenBy(x => x.Id)
            .Select(x => new ExperienceListItem(x.Id, x.Title, x.MemoryCue, x.IsArchived, x.Tags.Order().ToList())).ToList();
    }

    public async Task<int> SaveAsync(int? id, ExperienceEditModel input)
    {
        InputRules.Validate(input);
        if (string.IsNullOrWhiteSpace(input.Title)) throw new ValidationException("Enter an experience title.");
        var tags = Tags.Normalize(input.Tags);
        await using var db = await factory.CreateDbContextAsync();
        var row = id.HasValue ? await db.ProfessionalExperiences.Include(x => x.Tags).SingleOrDefaultAsync(x => x.Id == id) ?? throw new ValidationException("Experience no longer exists.")
            : new ProfessionalExperience { CreatedAtUtc = DateTime.UtcNow };
        row.Title = input.Title.Trim(); row.MemoryCue = InputRules.Text(input.MemoryCue);
        row.Situation = InputRules.Text(input.Situation); row.Task = InputRules.Text(input.Task); row.Action = InputRules.Text(input.Action);
        row.Result = InputRules.Text(input.Result); row.Reflection = InputRules.Text(input.Reflection); row.UsefulFor = InputRules.Text(input.UsefulFor); row.Notes = InputRules.Text(input.Notes); row.UpdatedAtUtc = DateTime.UtcNow;
        db.ExperienceTags.RemoveRange(row.Tags.Where(x => !tags.Contains(x.Tag)));
        foreach (var tag in tags.Except(row.Tags.Select(x => x.Tag))) row.Tags.Add(new() { Tag = tag });
        if (!id.HasValue) db.ProfessionalExperiences.Add(row);
        await db.SaveChangesAsync(); return row.Id;
    }

    public async Task<bool> SetArchivedAsync(int id, bool archived)
    {
        await using var db = await factory.CreateDbContextAsync();
        var row = await db.ProfessionalExperiences.FindAsync(id); if (row is null) return false;
        if (row.IsArchived != archived) { row.IsArchived = archived; row.UpdatedAtUtc = DateTime.UtcNow; await db.SaveChangesAsync(); }
        return true;
    }
}
