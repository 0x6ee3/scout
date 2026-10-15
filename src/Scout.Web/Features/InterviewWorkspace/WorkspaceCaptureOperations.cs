using System.ComponentModel.DataAnnotations;
using Microsoft.EntityFrameworkCore;
using Scout.Web.Data;
using Scout.Web.Domain;
namespace Scout.Web.Features.InterviewWorkspace;

public sealed record QuestionPatch(int ExpectedRevision, bool IsAsked, bool FollowUpRequired, bool IncludeAnswer, string? Answer);
public sealed record CaptureResult(bool Saved, bool Conflict, int Revision, string? Text, bool IsAsked = false, bool FollowUpRequired = false, int? CreatedId = null, bool ReviewRequired = false);
public sealed partial class WorkspaceOperations
{
    internal static async Task InvalidateAsync(ScoutDbContext db, int roundId, int itemId, bool experience)
    {
        var included = experience ? await db.Set<WorkspaceExperience>().AnyAsync(x => x.InterviewRoundId == roundId && x.InterviewExperienceId == itemId) : await db.Set<WorkspaceQuestion>().AnyAsync(x => x.InterviewRoundId == roundId && x.InterviewQuestionId == itemId);
        if (!included)
            return;
        var c = await db.Set<InterviewWorkspaceConfiguration>().SingleAsync(x => x.InterviewRoundId == roundId);
        c.RequiresReview = true;
        c.ConfigurationRevision++;
    }
    public async Task<bool> IsCurrentAsync(int id, int configurationRevision, bool? readOnly = null)
    {
        await using var db = await factory.CreateDbContextAsync();
        return await AllowedAsync(db, id, configurationRevision) && (!readOnly.HasValue || await db.Interviews.AnyAsync(x => x.Id == id && x.IsArchived == readOnly.Value));
    }
    private static async Task RequireEditable(ScoutDbContext db, int id, int configurationRevision)
    {
        if (!await AllowedAsync(db, id, configurationRevision) || !await db.Interviews.AnyAsync(x => x.Id == id && !x.IsArchived))
            throw new ValidationException("Workspace changed or is read only. Close it and review privately. Drafts remain in this browser.");
    }
    public async Task<CaptureResult> SaveQuestionAsync(int roundId, int configurationRevision, int questionId, QuestionPatch patch)
    {
        if (patch.Answer?.Length > 100000)
            throw new ValidationException("Answers must be at most 100,000 characters.");
        await using var db = await factory.CreateDbContextAsync();
        await using var transaction = await db.Database.BeginTransactionAsync();
        await RequireEditable(db, roundId, configurationRevision);
        var included = await db.Set<WorkspaceQuestion>().Where(x => x.InterviewRoundId == roundId && x.InterviewQuestionId == questionId).Select(x => new { x.AnswerEnabled }).SingleOrDefaultAsync() ?? throw new ValidationException("Question is not included.");
        if (patch.IncludeAnswer && !included.AnswerEnabled)
            throw new ValidationException("This answer is not enabled.");
        // Never retrieve a disabled answer, even for a permission check or conflict response.
        var state = await db.InterviewQuestions.Where(x => x.Id == questionId && x.InterviewRoundId == roundId).Select(x => new { x.ContentRevision, x.IsAsked, x.FollowUpRequired }).SingleAsync();
        var answer = patch.IncludeAnswer ? await db.InterviewQuestions.Where(x => x.Id == questionId && x.InterviewRoundId == roundId).Select(x => x.Answer).SingleAsync() : null;
        if (state.IsAsked == patch.IsAsked && state.FollowUpRequired == patch.FollowUpRequired && (!patch.IncludeAnswer || answer == patch.Answer))
            return new(true, false, state.ContentRevision, answer, state.IsAsked, state.FollowUpRequired);
        if (state.ContentRevision != patch.ExpectedRevision)
            return new(false, true, state.ContentRevision, answer, state.IsAsked, state.FollowUpRequired);
        var row = new InterviewQuestion { Id = questionId, InterviewRoundId = roundId, ContentRevision = state.ContentRevision };
        db.Attach(row);
        row.ContentRevision++;
        row.IsAsked = patch.IsAsked;
        row.FollowUpRequired = patch.FollowUpRequired;
        row.UpdatedAtUtc = DateTime.UtcNow;
        db.Entry(row).Property(x => x.ContentRevision).IsModified = true;
        db.Entry(row).Property(x => x.IsAsked).IsModified = true;
        db.Entry(row).Property(x => x.FollowUpRequired).IsModified = true;
        db.Entry(row).Property(x => x.UpdatedAtUtc).IsModified = true;
        if (patch.IncludeAnswer)
        {
            row.Answer = patch.Answer;
            db.Entry(row).Property(x => x.Answer).IsModified = true;
        }
        await db.SaveChangesAsync();
        await transaction.CommitAsync();
        return new(true, false, row.ContentRevision, patch.IncludeAnswer ? patch.Answer : null, patch.IsAsked, patch.FollowUpRequired);
    }
    public async Task<CaptureResult> SaveNotesAsync(int id, int configurationRevision, int expectedRevision, string? text)
    {
        if (text?.Length > 100000)
            throw new ValidationException("Notes must be at most 100,000 characters.");
        await using var db = await factory.CreateDbContextAsync();
        await using var transaction = await db.Database.BeginTransactionAsync();
        await RequireEditable(db, id, configurationRevision);
        if (!await db.Set<InterviewWorkspaceConfiguration>().AnyAsync(x => x.InterviewRoundId == id && x.InterviewNotesEnabled))
            throw new ValidationException("Interview notes are not enabled.");
        var state = await db.Interviews.Where(x => x.Id == id).Select(x => new { x.NotesRevision, x.InterviewNotes }).SingleAsync();
        if (state.InterviewNotes == text)
            return new(true, false, state.NotesRevision, text);
        if (state.NotesRevision != expectedRevision)
            return new(false, true, state.NotesRevision, state.InterviewNotes);
        var row = new InterviewRound { Id = id, NotesRevision = expectedRevision };
        db.Attach(row);
        row.InterviewNotes = text;
        row.NotesRevision++;
        row.UpdatedAtUtc = DateTime.UtcNow;
        db.Entry(row).Property(x => x.InterviewNotes).IsModified = true;
        db.Entry(row).Property(x => x.NotesRevision).IsModified = true;
        db.Entry(row).Property(x => x.UpdatedAtUtc).IsModified = true;
        await db.SaveChangesAsync();
        await transaction.CommitAsync();
        return new(true, false, row.NotesRevision, text);
    }
    public async Task<int> AddQuestionAsync(int id, int configurationRevision, Guid creationToken, string text)
    {
        if (creationToken == Guid.Empty || string.IsNullOrWhiteSpace(text) || text.Trim().Length > 4000)
            throw new ValidationException("Enter a question of 1–4,000 characters.");
        text = text.Trim();
        await using var db = await factory.CreateDbContextAsync();
        await using var transaction = await db.Database.BeginTransactionAsync();
        await RequireEditable(db, id, configurationRevision);
        var existing = await db.InterviewQuestions.Where(x => x.InterviewRoundId == id && x.CreationToken == creationToken).Select(x => new { x.Id, x.Text }).SingleOrDefaultAsync();
        if (existing is not null)
        {
            if (existing.Text != text)
                throw new ValidationException("This creation token already registered different wording.");
            return existing.Id;
        }
        var order = (await db.InterviewQuestions.Where(x => x.InterviewRoundId == id).MaxAsync(x => (int?)x.DisplayOrder) ?? -1) + 1;
        var now = DateTime.UtcNow;
        var q = new InterviewQuestion { InterviewRoundId = id, Origin = InterviewQuestionOrigin.AdHoc, CreationToken = creationToken, Text = text, DisplayOrder = order, CreatedAtUtc = now, UpdatedAtUtc = now };
        db.Add(q);
        await db.SaveChangesAsync();
        db.Add(new WorkspaceQuestion { InterviewRoundId = id, InterviewQuestionId = q.Id, Text = text, AnswerEnabled = true, DisplayOrder = order });
        await db.SaveChangesAsync();
        await transaction.CommitAsync();
        return q.Id;
    }
}
