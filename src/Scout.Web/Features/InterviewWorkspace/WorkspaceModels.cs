using Scout.Web.Domain;
namespace Scout.Web.Features.InterviewWorkspace;

public sealed class WorkspaceApproval
{
    public int ExpectedRevision
    {
        get; set;
    }
    public string ExpectedReviewStamp { get; set; } = "";
    public bool RoundDetails
    {
        get; set;
    }
    public bool JobDescription
    {
        get; set;
    }
    public bool PreparationNotes
    {
        get; set;
    }
    public bool InterviewNotes
    {
        get; set;
    }
    public string CvKey { get; set; } = "";
    public List<QuestionApproval> Questions { get; set; } = [];
    public List<ExperienceApproval> Experiences { get; set; } = [];
    public HashSet<int> PriorAnswerIds { get; set; } = [];
}
public sealed class QuestionApproval
{
    public int Id
    {
        get; set;
    }
    public bool Include
    {
        get; set;
    }
    public bool Intent
    {
        get; set;
    }
    public bool FollowUps
    {
        get; set;
    }
    public bool PreparationNotes
    {
        get; set;
    }
    public bool Answer
    {
        get; set;
    }
}
public sealed class ExperienceApproval
{
    public int Id
    {
        get; set;
    }
    public bool Include
    {
        get; set;
    }
    public bool Cue
    {
        get; set;
    }
    public bool Situation
    {
        get; set;
    }
    public bool Task
    {
        get; set;
    }
    public bool Action
    {
        get; set;
    }
    public bool Result
    {
        get; set;
    }
    public bool Reflection
    {
        get; set;
    }
    public bool UsefulFor
    {
        get; set;
    }
    public bool Notes
    {
        get; set;
    }
    public bool PreparationNotes
    {
        get; set;
    }
}
public sealed record CvChoice(string Key, string Label, DateOnly SubmittedOn);
public sealed record PriorAnswerChoice(int Id, string RoundTitle, DateTime? ScheduledAtUtc, string Text, string? Answer);
public sealed record WorkspaceSetupData(InterviewRound Round, string CompanyName, int Revision, Guid? WorkspaceId, IReadOnlyList<InterviewQuestion> Questions, IReadOnlyList<InterviewExperience> Experiences, IReadOnlyList<CvChoice> Cvs, IReadOnlyList<PriorAnswerChoice> PriorAnswers)
{
    // Detect source changes between the visible private preview and approval.
    public string ReviewStamp => Convert.ToHexString(System.Security.Cryptography.SHA256.HashData(System.Text.Json.JsonSerializer.SerializeToUtf8Bytes(new
    {
        Round.Id,
        Round.ApplicationId,
        Round.Title,
        Round.IsArchived,
        Round.ScheduledAtUtc,
        CompanyName,
        PositionTitle = Round.Application.Position.Title,
        Round.Application.Position.JobDescriptionSnapshot,
        Round.PreparationNotes,
        Round.InterviewNotes,
        Questions = Questions.Select(q => new { q.Id, q.Text, q.Intent, q.FollowUpQuestions, q.PreparationNotes, q.Answer, q.DisplayOrder, Tags = q.EmployerQuestion?.Tags.Select(t => t.Tag).Order().ToArray() }),
        Experiences = Experiences.Select(e => new { e.Id, e.PreparationNotes, e.DisplayOrder, Story = new { e.ProfessionalExperience.Title, e.ProfessionalExperience.MemoryCue, e.ProfessionalExperience.Situation, e.ProfessionalExperience.Task, e.ProfessionalExperience.Action, e.ProfessionalExperience.Result, e.ProfessionalExperience.Reflection, e.ProfessionalExperience.UsefulFor, e.ProfessionalExperience.Notes, Tags = e.ProfessionalExperience.Tags.Select(t => t.Tag).Order().ToArray() } }),
        Cvs,
        PriorAnswers
    })));
}
public sealed record WorkspaceView(Guid WorkspaceId, int RoundId, int Revision, DateTime ApprovedAtUtc, bool ReadOnly, string? RoundTitle, string? CompanyName, string? PositionTitle, DateTime? ScheduledAtUtc, string? JobDescription, string? PreparationNotes, bool NotesEnabled, string? InterviewNotes, bool HasCv, int NotesRevision, IReadOnlyList<WorkspaceQuestionView> Questions, IReadOnlyList<WorkspaceExperienceView> Experiences, IReadOnlyList<PriorAnswerChoice> PriorAnswers);
public sealed record WorkspaceQuestionView(int Id, string Text, string? Intent, string? FollowUps, string? PreparationNotes, string TagsJson, bool AnswerEnabled, string? Answer, int ContentRevision, bool IsAsked, bool FollowUpRequired, InterviewQuestionOrigin Origin);
public sealed record WorkspaceExperienceView(int Id, string Title, string? Cue, string? Situation, string? Task, string? Action, string? Result, string? Reflection, string? UsefulFor, string? Notes, string? PreparationNotes, string TagsJson);
