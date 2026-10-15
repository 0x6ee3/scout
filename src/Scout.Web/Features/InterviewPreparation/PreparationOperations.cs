using System.ComponentModel.DataAnnotations;
using Microsoft.EntityFrameworkCore;
using Scout.Web.Data;
using Scout.Web.Domain;

namespace Scout.Web.Features.InterviewPreparation;

public sealed class PreparedQuestionEditModel
{
    public int ContentRevision
    {
        get; set;
    }
    [Required, StringLength(4000)] public string Text { get; set; } = "";
    [StringLength(100000)]
    public string? Intent
    {
        get; set;
    }
    [StringLength(100000)]
    public string? FollowUpQuestions
    {
        get; set;
    }
    [StringLength(100000)]
    public string? PreparationNotes
    {
        get; set;
    }
    [StringLength(100000)]
    public string? Answer
    {
        get; set;
    }
}

public sealed class PreparationSelectionModel
{
    public HashSet<int> SelectedIds { get; set; } = [];
}

public sealed partial class PreparationOperations(IDbContextFactory<ScoutDbContext> factory)
{
    private static async Task<InterviewRound> RequireRound(ScoutDbContext db, int roundId, bool active = false)
    {
        var round = await db.Interviews.SingleOrDefaultAsync(x => x.Id == roundId) ?? throw new ValidationException("Interview no longer exists.");
        if (active && round.IsArchived)
            throw new ValidationException("Restore the interview before adding, restoring, or reordering preparation.");
        return round;
    }

    public async Task<IReadOnlyList<InterviewQuestion>> QuestionsAsync(int roundId)
    {
        await using var db = await factory.CreateDbContextAsync();
        await RequireRound(db, roundId);
        return await db.InterviewQuestions.AsNoTracking().Include(x => x.EmployerQuestion).Where(x => x.InterviewRoundId == roundId).OrderBy(x => x.DisplayOrder).ThenBy(x => x.Id).ToListAsync();
    }

    public async Task SelectQuestionsAsync(int roundId, IEnumerable<int> selected)
    {
        var ids = selected.Distinct().ToArray();
        await using var db = await factory.CreateDbContextAsync();
        await using var transaction = await db.Database.BeginTransactionAsync();
        await RequireRound(db, roundId, true);
        var sources = await db.EmployerQuestions.Where(x => ids.Contains(x.Id)).ToListAsync();
        var rows = await db.InterviewQuestions.Where(x => x.InterviewRoundId == roundId).ToListAsync();
        if (sources.Count != ids.Length || sources.Any(x => x.IsArchived && !rows.Any(r => r.EmployerQuestionId == x.Id && !r.IsArchived)))
            throw new ValidationException("Choose existing active questions. A source may have been deleted or archived.");
        var order = rows.Where(x => !x.IsArchived).Select(x => x.DisplayOrder).DefaultIfEmpty(-1).Max() + 1;
        foreach (var source in sources.OrderBy(x => x.Id))
        {
            var row = rows.SingleOrDefault(x => x.EmployerQuestionId == source.Id);
            if (row is not null && !row.IsArchived)
                continue;
            var now = DateTime.UtcNow;
            if (row is null)
            {
                row = new()
                {
                    InterviewRoundId = roundId,
                    EmployerQuestionId = source.Id,
                    Text = source.Text,
                    Intent = source.Intent,
                    FollowUpQuestions = source.FollowUpQuestions,
                    CreatedAtUtc = now
                };
                db.InterviewQuestions.Add(row);
            }
            row.IsArchived = false;
            row.DisplayOrder = order++;
            row.UpdatedAtUtc = now;
        }
        await db.SaveChangesAsync();
        await transaction.CommitAsync();
    }

    public async Task SaveQuestionAsync(int roundId, int id, PreparedQuestionEditModel input)
    {
        InputRules.Validate(input);
        if (string.IsNullOrWhiteSpace(input.Text))
            throw new ValidationException("Enter prepared question wording.");
        await using var db = await factory.CreateDbContextAsync();
        await RequireRound(db, roundId);
        var row = await db.InterviewQuestions.SingleOrDefaultAsync(x => x.Id == id && x.InterviewRoundId == roundId) ?? throw new ValidationException("Prepared question does not belong to this interview or no longer exists.");
        if (row.ContentRevision != input.ContentRevision)
            throw new ValidationException("This question changed elsewhere. Copy your draft, reload and review before saving.");
        row.ContentRevision++;
        row.Text = input.Text.Trim();
        row.Intent = InputRules.Text(input.Intent);
        row.FollowUpQuestions = InputRules.Text(input.FollowUpQuestions);
        row.PreparationNotes = InputRules.Text(input.PreparationNotes);
        row.Answer = InputRules.Text(input.Answer);
        row.UpdatedAtUtc = DateTime.UtcNow;
        await db.SaveChangesAsync();
    }

    public async Task<bool> ArchiveQuestionAsync(int roundId, int id, bool archived)
    {
        await using var db = await factory.CreateDbContextAsync();
        await RequireRound(db, roundId, !archived);
        var row = await db.InterviewQuestions.Include(x => x.EmployerQuestion).SingleOrDefaultAsync(x => x.Id == id && x.InterviewRoundId == roundId) ?? throw new ValidationException("Prepared question does not belong to this interview.");
        if (row.IsArchived == archived)
            return true;
        if (!archived)
        {
            if (row.EmployerQuestion?.IsArchived == true)
                throw new ValidationException("Restore the source question first.");
            row.DisplayOrder = (await db.InterviewQuestions.Where(x => x.InterviewRoundId == roundId && !x.IsArchived).Select(x => (int?)x.DisplayOrder).MaxAsync() ?? -1) + 1;
        }
        if (archived)
            await Scout.Web.Features.InterviewWorkspace.WorkspaceOperations.InvalidateAsync(db, roundId, id, false);
        row.IsArchived = archived;
        row.UpdatedAtUtc = DateTime.UtcNow;
        await db.SaveChangesAsync();
        return true;
    }

    public async Task<bool> MoveQuestionAsync(int roundId, int id, int direction)
    {
        if (direction is not (-1 or 1))
            throw new ValidationException("Choose move up or move down.");
        await using var db = await factory.CreateDbContextAsync();
        await using var transaction = await db.Database.BeginTransactionAsync();
        await RequireRound(db, roundId, true);
        var rows = await db.InterviewQuestions.Where(x => x.InterviewRoundId == roundId && !x.IsArchived).OrderBy(x => x.DisplayOrder).ThenBy(x => x.Id).ToListAsync();
        var index = rows.FindIndex(x => x.Id == id);
        if (index < 0)
            throw new ValidationException("Choose an active question in this interview.");
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
