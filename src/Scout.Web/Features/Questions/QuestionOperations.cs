using System.ComponentModel.DataAnnotations;
using Microsoft.EntityFrameworkCore;
using Scout.Web.Data;
using Scout.Web.Domain;

namespace Scout.Web.Features.Questions;

public sealed class QuestionEditModel
{
    [Required, StringLength(4000)] public string Text { get; set; } = "";
    [StringLength(100000)] public string? Intent { get; set; }
    [StringLength(100000)] public string? FollowUpQuestions { get; set; }
    [StringLength(100000)] public string? Notes { get; set; }
    public List<string> Tags { get; set; } = [];
}

public sealed record QuestionListItem(int Id, string Text, bool IsArchived, IReadOnlyList<string> Tags);

public sealed class QuestionOperations(IDbContextFactory<ScoutDbContext> factory)
{
    public async Task<EmployerQuestion?> GetAsync(int id)
    {
        await using var db = await factory.CreateDbContextAsync();
        return await db.EmployerQuestions.AsNoTracking().Include(x => x.Tags).SingleOrDefaultAsync(x => x.Id == id);
    }

    public async Task<IReadOnlyList<QuestionListItem>> ListAsync(ArchiveFilter archive = ArchiveFilter.Active, string? search = null, string? tag = null)
    {
        await using var db = await factory.CreateDbContextAsync();
        var query = db.EmployerQuestions.AsNoTracking().Where(x => archive == ArchiveFilter.All || x.IsArchived == (archive == ArchiveFilter.Archived));
        if (!string.IsNullOrWhiteSpace(tag)) { var key = Tags.Normalize([tag]).Single(); query = query.Where(x => x.Tags.Any(t => t.Tag == key)); }
        var needle = search?.Trim();
        var rows = await query.Select(x => new { x.Id, x.Text, x.IsArchived,
            Intent = needle != null && needle != "" ? x.Intent : null,
            Follow = needle != null && needle != "" ? x.FollowUpQuestions : null,
            Notes = needle != null && needle != "" ? x.Notes : null,
            Tags = x.Tags.Select(t => t.Tag).ToList() }).ToListAsync();
        return rows.Where(x => string.IsNullOrEmpty(needle) || new[] { x.Text, x.Intent, x.Follow, x.Notes }.Concat(x.Tags).Any(v => v?.Contains(needle, StringComparison.OrdinalIgnoreCase) == true))
            .OrderBy(x => x.Text, StringComparer.OrdinalIgnoreCase).ThenBy(x => x.Id)
            .Select(x => new QuestionListItem(x.Id, x.Text, x.IsArchived, x.Tags.Order().ToList())).ToList();
    }

    public async Task<int> SaveAsync(int? id, QuestionEditModel input)
    {
        InputRules.Validate(input);
        if (string.IsNullOrWhiteSpace(input.Text)) throw new ValidationException("Enter a question.");
        var tags = Tags.Normalize(input.Tags);
        await using var db = await factory.CreateDbContextAsync();
        var row = id.HasValue ? await db.EmployerQuestions.Include(x => x.Tags).SingleOrDefaultAsync(x => x.Id == id) ?? throw new ValidationException("Question no longer exists.")
            : new EmployerQuestion { CreatedAtUtc = DateTime.UtcNow };
        row.Text = input.Text.Trim(); row.Intent = InputRules.Text(input.Intent); row.FollowUpQuestions = InputRules.Text(input.FollowUpQuestions); row.Notes = InputRules.Text(input.Notes); row.UpdatedAtUtc = DateTime.UtcNow;
        db.EmployerQuestionTags.RemoveRange(row.Tags.Where(x => !tags.Contains(x.Tag)));
        foreach (var tag in tags.Except(row.Tags.Select(x => x.Tag))) row.Tags.Add(new() { Tag = tag });
        if (!id.HasValue) db.EmployerQuestions.Add(row);
        await db.SaveChangesAsync();
        return row.Id;
    }

    public async Task<bool> SetArchivedAsync(int id, bool archived)
    {
        await using var db = await factory.CreateDbContextAsync();
        var row = await db.EmployerQuestions.FindAsync(id);
        if (row is null) return false;
        if (row.IsArchived != archived) { row.IsArchived = archived; row.UpdatedAtUtc = DateTime.UtcNow; await db.SaveChangesAsync(); }
        return true;
    }

    public async Task<bool> DeleteAsync(int id)
    {
        await using var db = await factory.CreateDbContextAsync();
        var row = await db.EmployerQuestions.Include(x => x.Tags).SingleOrDefaultAsync(x => x.Id == id);
        if (row is null) return false;
        db.EmployerQuestionTags.RemoveRange(row.Tags);
        db.EmployerQuestions.Remove(row);
        await db.SaveChangesAsync();
        return true;
    }

    public async Task<int> SelectionCountAsync(int id)
    {
        await using var db = await factory.CreateDbContextAsync();
        return await db.InterviewQuestions.CountAsync(x => x.EmployerQuestionId == id);
    }
}
