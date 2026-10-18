using System.ComponentModel.DataAnnotations;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Scout.Web.Data;
using Scout.Web.Domain;
namespace Scout.Web.Features.InterviewWorkspace;

public sealed partial class WorkspaceOperations(IDbContextFactory<ScoutDbContext> factory)
{
    // Private review is deliberately separate from the Workspace projections.
    public async Task<WorkspaceSetupData?> SetupAsync(int id)
    {
        await using var db = await factory.CreateDbContextAsync();
        await using var transaction = await db.Database.BeginTransactionAsync();
        return await SetupCoreAsync(db, id);
    }

    private static async Task<WorkspaceSetupData?> SetupCoreAsync(ScoutDbContext db, int id)
    {
        var round = await db.Interviews.AsNoTracking().Include(x => x.Application).ThenInclude(x => x.Position).ThenInclude(x => x.Company).SingleOrDefaultAsync(x => x.Id == id);
        if (round is null)
            return null;
        var questions = await db.InterviewQuestions.AsNoTracking().Include(x => x.EmployerQuestion).ThenInclude(x => x!.Tags).Where(x => x.InterviewRoundId == id && !x.IsArchived).OrderBy(x => x.DisplayOrder).ToListAsync();
        var experiences = await db.InterviewExperiences.AsNoTracking().Include(x => x.ProfessionalExperience).ThenInclude(x => x.Tags).Where(x => x.InterviewRoundId == id && !x.IsArchived).OrderBy(x => x.DisplayOrder).ToListAsync();
        var cvs = await db.SubmissionFiles.Where(x => x.Submission.ApplicationId == round.ApplicationId && !x.Submission.IsIncorrect && x.Role == SubmissionFileRole.CV).Select(x => new CvChoice(x.SubmissionId + "/" + x.DocumentId, x.LabelSnapshot, x.Submission.SubmittedOn)).ToListAsync();
        var prior = await db.InterviewQuestions.Where(x => x.InterviewRound.ApplicationId == round.ApplicationId && x.InterviewRoundId != id && x.Answer != null).Select(x => new PriorAnswerChoice(x.Id, x.InterviewRound.Title, x.InterviewRound.ScheduledAtUtc, x.Text, x.Answer)).ToListAsync();
        var revision = await db.Set<InterviewWorkspaceConfiguration>().Where(x => x.InterviewRoundId == id).Select(x => (int?)x.ConfigurationRevision).SingleOrDefaultAsync() ?? 0;
        return new(round, round.Application.Position.Company.Name, revision, await db.Set<InterviewWorkspaceConfiguration>().Where(x => x.InterviewRoundId == id).Select(x => (Guid?)x.WorkspaceId).SingleOrDefaultAsync(), questions, experiences, cvs, prior) { RecordingChoicePreview = await ChoiceStatusAsync(db, id), RecordingDecisionRevision = await db.RecordingDecisions.Where(x => x.InterviewRoundId == id).Select(x => (int?)x.Id).MaxAsync() ?? 0 };
    }

    public async Task ApproveAsync(int id, WorkspaceApproval input)
    {
        await using var db = await factory.CreateDbContextAsync();
        await using var transaction = await db.Database.BeginTransactionAsync();
        var setup = await SetupCoreAsync(db, id) ?? throw new ValidationException("Interview no longer exists.");
        if (input.ExpectedReviewStamp != setup.ReviewStamp)
            throw new ValidationException("Preview content changed. Reload this private page and review the current text before approving.");
        if (setup.Round.IsArchived)
            throw new ValidationException("Restore this interview before reviewing its Workspace.");
        var c = await db.Set<InterviewWorkspaceConfiguration>().SingleOrDefaultAsync(x => x.InterviewRoundId == id);
        if ((c?.ConfigurationRevision ?? 0) != input.ExpectedRevision)
            throw new ValidationException("Workspace review changed. Reload this private page and review again.");
        if (c is null)
        {
            c = new()
            {
                InterviewRoundId = id
            };
            db.Add(c);
        }
        c.ConfigurationRevision++;
        c.RequiresReview = false;
        c.ApprovedAtUtc = DateTime.UtcNow;
        c.RoundTitle = input.RoundDetails ? setup.Round.Title : null;
        c.CompanyName = input.RoundDetails ? setup.CompanyName : null;
        c.PositionTitle = input.RoundDetails ? setup.Round.Application.Position.Title : null;
        c.ScheduledAtUtc = input.RoundDetails ? setup.Round.ScheduledAtUtc : null;
        c.JobDescription = input.JobDescription ? setup.Round.Application.Position.JobDescriptionSnapshot : null;
        c.PreparationNotes = input.PreparationNotes ? setup.Round.PreparationNotes : null;
        c.InterviewNotesEnabled = input.InterviewNotes;
        c.RecordingChoiceEnabled = input.RecordingChoice;
        c.SubmissionId = c.DocumentId = null;
        if (!string.IsNullOrEmpty(input.CvKey))
        {
            var cv = setup.Cvs.SingleOrDefault(x => x.Key == input.CvKey) ?? throw new ValidationException("Choose a submitted CV from this application.");
            var ids = cv.Key.Split('/');
            c.SubmissionId = int.Parse(ids[0]);
            c.DocumentId = int.Parse(ids[1]);
        }
        db.RemoveRange(await db.Set<WorkspaceQuestion>().Where(x => x.InterviewRoundId == id).ToListAsync());
        db.RemoveRange(await db.Set<WorkspaceExperience>().Where(x => x.InterviewRoundId == id).ToListAsync());
        db.RemoveRange(await db.Set<WorkspacePriorAnswer>().Where(x => x.InterviewRoundId == id).ToListAsync());
        await db.SaveChangesAsync();
        foreach (var choice in input.Questions.Where(x => x.Include))
        {
            var q = setup.Questions.SingleOrDefault(x => x.Id == choice.Id) ?? throw new ValidationException("Choose an active question in this interview.");
            db.Add(new WorkspaceQuestion { InterviewRoundId = id, InterviewQuestionId = q.Id, Text = q.Text, Intent = choice.Intent ? q.Intent : null, FollowUpQuestions = choice.FollowUps ? q.FollowUpQuestions : null, PreparationNotes = choice.PreparationNotes ? q.PreparationNotes : null, AnswerEnabled = choice.Answer, TagsJson = JsonSerializer.Serialize(Tags.Normalize(q.EmployerQuestion?.Tags.Select(x => x.Tag) ?? [])), DisplayOrder = q.DisplayOrder });
        }
        foreach (var choice in input.Experiences.Where(x => x.Include))
        {
            var e = setup.Experiences.SingleOrDefault(x => x.Id == choice.Id) ?? throw new ValidationException("Choose an active experience in this interview.");
            var source = e.ProfessionalExperience;
            db.Add(new WorkspaceExperience { InterviewRoundId = id, InterviewExperienceId = e.Id, Title = source.Title, MemoryCue = choice.Cue ? source.MemoryCue : null, Situation = choice.Situation ? source.Situation : null, Task = choice.Task ? source.Task : null, Action = choice.Action ? source.Action : null, Result = choice.Result ? source.Result : null, Reflection = choice.Reflection ? source.Reflection : null, UsefulFor = choice.UsefulFor ? source.UsefulFor : null, Notes = choice.Notes ? source.Notes : null, PreparationNotes = choice.PreparationNotes ? e.PreparationNotes : null, TagsJson = JsonSerializer.Serialize(Tags.Normalize(source.Tags.Select(x => x.Tag))), DisplayOrder = e.DisplayOrder });
        }
        foreach (var sourceId in input.PriorAnswerIds)
        {
            var prior = setup.PriorAnswers.SingleOrDefault(x => x.Id == sourceId) ?? throw new ValidationException("Choose an answer from another round of this application.");
            db.Add(new WorkspacePriorAnswer { InterviewRoundId = id, SourceQuestionId = prior.Id, RoundTitle = prior.RoundTitle, ScheduledAtUtc = prior.ScheduledAtUtc, Text = prior.Text, Answer = prior.Answer });
        }
        await db.SaveChangesAsync();
        await transaction.CommitAsync();
    }

    private static async Task<bool> AllowedAsync(ScoutDbContext db, int id, int? revision = null)
    {
        return await db.Set<InterviewWorkspaceConfiguration>().AnyAsync(x => x.InterviewRoundId == id && !x.RequiresReview && (!revision.HasValue || x.ConfigurationRevision == revision)
            && !db.Set<WorkspaceQuestion>().Any(q => q.InterviewRoundId == id && q.Question.IsArchived)
            && !db.Set<WorkspaceExperience>().Any(e => e.InterviewRoundId == id && e.Experience.IsArchived)
            && (x.SubmissionId == null || (x.Cv!.Role == SubmissionFileRole.CV && !x.Cv.Submission.IsIncorrect && x.Cv.Submission.ApplicationId == x.InterviewRound.ApplicationId)));
    }

    public async Task<WorkspaceView?> GetAsync(int id, int? revision = null)
    {
        await using var db = await factory.CreateDbContextAsync();
        await using var transaction = await db.Database.BeginTransactionAsync();
        if (!await AllowedAsync(db, id, revision))
            return null;
        var c = await db.Set<InterviewWorkspaceConfiguration>().AsNoTracking().Where(x => x.InterviewRoundId == id).Select(x => new { x.WorkspaceId, x.ConfigurationRevision, x.ApprovedAtUtc, x.InterviewRound.IsArchived, x.RoundTitle, x.CompanyName, x.PositionTitle, x.ScheduledAtUtc, x.JobDescription, x.PreparationNotes, x.InterviewNotesEnabled, x.DocumentId, x.RecordingChoiceEnabled }).SingleAsync();
        var notes = c.InterviewNotesEnabled ? await db.Interviews.Where(x => x.Id == id).Select(x => x.InterviewNotes).SingleAsync() : null;
        var questions = await db.Set<WorkspaceQuestion>().AsNoTracking().Where(x => x.InterviewRoundId == id).OrderBy(x => x.DisplayOrder).ThenBy(x => x.InterviewQuestionId).Select(x => new WorkspaceQuestionView(x.InterviewQuestionId, x.Text, x.Intent, x.FollowUpQuestions, x.PreparationNotes, x.TagsJson, x.AnswerEnabled, null, x.Question.ContentRevision, x.Question.IsAsked, x.Question.FollowUpRequired, x.Question.Origin)).ToListAsync();
        var answers = await db.Set<WorkspaceQuestion>().Where(x => x.InterviewRoundId == id && x.AnswerEnabled).Select(x => new { x.InterviewQuestionId, x.Question.Answer }).ToDictionaryAsync(x => x.InterviewQuestionId, x => x.Answer);
        questions = questions.Select(x => x with { Answer = answers.GetValueOrDefault(x.Id) }).ToList();
        var experiences = await db.Set<WorkspaceExperience>().AsNoTracking().Where(x => x.InterviewRoundId == id).OrderBy(x => x.DisplayOrder).ThenBy(x => x.InterviewExperienceId).Select(x => new WorkspaceExperienceView(x.InterviewExperienceId, x.Title, x.MemoryCue, x.Situation, x.Task, x.Action, x.Result, x.Reflection, x.UsefulFor, x.Notes, x.PreparationNotes, x.TagsJson)).ToListAsync();
        var prior = await db.Set<WorkspacePriorAnswer>().Where(x => x.InterviewRoundId == id).Select(x => new PriorAnswerChoice(x.SourceQuestionId, x.RoundTitle, x.ScheduledAtUtc, x.Text, x.Answer)).ToListAsync();
        return new(c.WorkspaceId, id, c.ConfigurationRevision, c.ApprovedAtUtc, c.IsArchived, c.RoundTitle, c.CompanyName, c.PositionTitle, c.ScheduledAtUtc, c.JobDescription, c.PreparationNotes, c.InterviewNotesEnabled, notes, c.DocumentId.HasValue, await db.Interviews.Where(x => x.Id == id).Select(x => x.NotesRevision).SingleAsync(), questions, experiences, prior) { RecordingChoiceStatus = c.RecordingChoiceEnabled ? await ChoiceStatusAsync(db, id) : null };
    }

    public async Task<Document?> CvAsync(int id, int revision)
    {
        await using var db = await factory.CreateDbContextAsync();
        await using var transaction = await db.Database.BeginTransactionAsync();
        if (!await AllowedAsync(db, id, revision))
            return null;
        return await db.Set<InterviewWorkspaceConfiguration>().Where(x => x.InterviewRoundId == id && x.DocumentId != null).Select(x => new Document { StoredRelativePath = x.Cv!.Document.StoredRelativePath, ContentType = x.Cv.Document.ContentType, Sha256 = x.Cv.Document.Sha256, Size = x.Cv.Document.Size }).SingleOrDefaultAsync();
    }
}
