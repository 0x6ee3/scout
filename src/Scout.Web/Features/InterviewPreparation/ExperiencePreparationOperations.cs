using System.ComponentModel.DataAnnotations;
using Microsoft.EntityFrameworkCore;
using Scout.Web.Domain;

namespace Scout.Web.Features.InterviewPreparation;

public sealed class ExperiencePreparationEditModel
{
    [StringLength(100000)]
    public string? PreparationNotes
    {
        get; set;
    }
}

public sealed partial class PreparationOperations
{
    public async Task<IReadOnlyList<InterviewExperience>> ExperiencesAsync(int roundId)
    {
        await using var db = await factory.CreateDbContextAsync();
        await RequireRound(db, roundId);
        return await db.InterviewExperiences.AsNoTracking().Include(x => x.ProfessionalExperience).ThenInclude(x => x.Tags)
            .Where(x => x.InterviewRoundId == roundId).OrderBy(x => x.DisplayOrder).ThenBy(x => x.Id).ToListAsync();
    }

    public async Task SelectExperiencesAsync(int roundId, IEnumerable<int> selected)
    {
        var ids = selected.Distinct().ToArray();
        await using var db = await factory.CreateDbContextAsync();
        await using var transaction = await db.Database.BeginTransactionAsync();
        await RequireRound(db, roundId, true);
        var sources = await db.ProfessionalExperiences.Where(x => ids.Contains(x.Id)).ToListAsync();
        var rows = await db.InterviewExperiences.Where(x => x.InterviewRoundId == roundId).ToListAsync();
        if (sources.Count != ids.Length || sources.Any(x => x.IsArchived && !rows.Any(r => r.ProfessionalExperienceId == x.Id && !r.IsArchived)))
            throw new ValidationException("Choose existing active experiences. Restore archived sources before adding them.");
        var order = rows.Where(x => !x.IsArchived).Select(x => x.DisplayOrder).DefaultIfEmpty(-1).Max() + 1;
        foreach (var source in sources.OrderBy(x => x.Id))
        {
            var row = rows.SingleOrDefault(x => x.ProfessionalExperienceId == source.Id);
            if (row is not null && !row.IsArchived)
                continue;
            var now = DateTime.UtcNow;
            if (row is null)
            {
                row = new()
                {
                    InterviewRoundId = roundId,
                    ProfessionalExperienceId = source.Id,
                    CreatedAtUtc = now
                };
                db.InterviewExperiences.Add(row);
            }
            row.IsArchived = false;
            row.DisplayOrder = order++;
            row.UpdatedAtUtc = now;
        }
        await db.SaveChangesAsync();
        await transaction.CommitAsync();
    }

    public async Task SaveExperienceAsync(int roundId, int id, ExperiencePreparationEditModel input)
    {
        InputRules.Validate(input);
        await using var db = await factory.CreateDbContextAsync();
        await RequireRound(db, roundId);
        var row = await db.InterviewExperiences.SingleOrDefaultAsync(x => x.Id == id && x.InterviewRoundId == roundId) ?? throw new ValidationException("Selected experience does not belong to this interview.");
        row.PreparationNotes = InputRules.Text(input.PreparationNotes);
        row.UpdatedAtUtc = DateTime.UtcNow;
        await db.SaveChangesAsync();
    }

    public async Task<bool> ArchiveExperienceAsync(int roundId, int id, bool archived)
    {
        await using var db = await factory.CreateDbContextAsync();
        await RequireRound(db, roundId, !archived);
        var row = await db.InterviewExperiences.Include(x => x.ProfessionalExperience).SingleOrDefaultAsync(x => x.Id == id && x.InterviewRoundId == roundId) ?? throw new ValidationException("Selected experience does not belong to this interview.");
        if (row.IsArchived == archived)
            return true;
        if (!archived)
        {
            if (row.ProfessionalExperience.IsArchived)
                throw new ValidationException("Restore the source experience first.");
            row.DisplayOrder = (await db.InterviewExperiences.Where(x => x.InterviewRoundId == roundId && !x.IsArchived).Select(x => (int?)x.DisplayOrder).MaxAsync() ?? -1) + 1;
        }
        if (archived)
            await Scout.Web.Features.InterviewWorkspace.WorkspaceOperations.InvalidateAsync(db, roundId, id, true);
        row.IsArchived = archived;
        row.UpdatedAtUtc = DateTime.UtcNow;
        await db.SaveChangesAsync();
        return true;
    }

    public async Task<bool> MoveExperienceAsync(int roundId, int id, int direction)
    {
        if (direction is not (-1 or 1))
            throw new ValidationException("Choose move up or move down.");
        await using var db = await factory.CreateDbContextAsync();
        await using var transaction = await db.Database.BeginTransactionAsync();
        await RequireRound(db, roundId, true);
        var rows = await db.InterviewExperiences.Where(x => x.InterviewRoundId == roundId && !x.IsArchived).OrderBy(x => x.DisplayOrder).ThenBy(x => x.Id).ToListAsync();
        var index = rows.FindIndex(x => x.Id == id);
        if (index < 0)
            throw new ValidationException("Choose an active experience in this interview.");
        var target = index + direction;
        if (target < 0 || target >= rows.Count)
            return true;
        (rows[index], rows[target]) = (rows[target], rows[index]);
        for (var i = 0; i < rows.Count; i++)
        {
            rows[i].DisplayOrder = i;
            rows[i].UpdatedAtUtc = DateTime.UtcNow;
        }
        await db.SaveChangesAsync();
        await transaction.CommitAsync();
        return true;
    }
}
