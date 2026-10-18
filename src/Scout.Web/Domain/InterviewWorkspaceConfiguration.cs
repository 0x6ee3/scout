namespace Scout.Web.Domain;

public sealed class InterviewWorkspaceConfiguration
{
    public bool RecordingChoiceEnabled { get; set; }
    public int InterviewRoundId
    {
        get; set;
    }
    public InterviewRound InterviewRound { get; set; } = null!;
    public Guid WorkspaceId { get; set; } = Guid.NewGuid();
    public int ConfigurationRevision
    {
        get; set;
    }
    public bool RequiresReview
    {
        get; set;
    }
    public DateTime ApprovedAtUtc
    {
        get; set;
    }
    public string? RoundTitle
    {
        get; set;
    }
    public string? CompanyName
    {
        get; set;
    }
    public string? PositionTitle
    {
        get; set;
    }
    public DateTime? ScheduledAtUtc
    {
        get; set;
    }
    public string? JobDescription
    {
        get; set;
    }
    public string? PreparationNotes
    {
        get; set;
    }
    public bool InterviewNotesEnabled
    {
        get; set;
    }
    public int? SubmissionId
    {
        get; set;
    }
    public int? DocumentId
    {
        get; set;
    }
    public SubmissionFile? Cv
    {
        get; set;
    }
}
public sealed class WorkspaceQuestion
{
    public int InterviewRoundId
    {
        get; set;
    }
    public int InterviewQuestionId
    {
        get; set;
    }
    public InterviewWorkspaceConfiguration Configuration { get; set; } = null!;
    public InterviewQuestion Question { get; set; } = null!;
    public string Text { get; set; } = "";
    public string? Intent
    {
        get; set;
    }
    public string? FollowUpQuestions
    {
        get; set;
    }
    public string? PreparationNotes
    {
        get; set;
    }
    public string TagsJson { get; set; } = "[]";
    public bool AnswerEnabled
    {
        get; set;
    }
    public int DisplayOrder
    {
        get; set;
    }
}
public sealed class WorkspaceExperience
{
    public int InterviewRoundId
    {
        get; set;
    }
    public int InterviewExperienceId
    {
        get; set;
    }
    public InterviewWorkspaceConfiguration Configuration { get; set; } = null!;
    public InterviewExperience Experience { get; set; } = null!;
    public string Title { get; set; } = "";
    public string? MemoryCue
    {
        get; set;
    }
    public string? Situation
    {
        get; set;
    }
    public string? Task
    {
        get; set;
    }
    public string? Action
    {
        get; set;
    }
    public string? Result
    {
        get; set;
    }
    public string? Reflection
    {
        get; set;
    }
    public string? UsefulFor
    {
        get; set;
    }
    public string? Notes
    {
        get; set;
    }
    public string? PreparationNotes
    {
        get; set;
    }
    public string TagsJson { get; set; } = "[]";
    public int DisplayOrder
    {
        get; set;
    }
}
public sealed class WorkspacePriorAnswer
{
    public int Id
    {
        get; set;
    }
    public int InterviewRoundId
    {
        get; set;
    }
    public InterviewWorkspaceConfiguration Configuration { get; set; } = null!;
    public int SourceQuestionId
    {
        get; set;
    }
    public InterviewQuestion SourceQuestion { get; set; } = null!;
    public string RoundTitle { get; set; } = "";
    public DateTime? ScheduledAtUtc
    {
        get; set;
    }
    public string Text { get; set; } = "";
    public string? Answer
    {
        get; set;
    }
}
