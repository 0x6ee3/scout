namespace Scout.Web.Domain;

public enum TimelineKind
{
    ApplicationCreated, StatusChanged, ApplicationSubmitted, EmailSentManually, EmailReceived,
    RecruiterCall, Interview, CodingChallenge, DocumentsReceived, Offer, Rejection, PersonalNote, Other
}

public sealed class ApplicationTimelineEvent
{
    public int? InterviewRoundId
    {
        get; set;
    }
    public int? SubmissionId
    {
        get; set;
    }
    public ApplicationSubmission? Submission
    {
        get; set;
    }
    public InterviewRound? InterviewRound
    {
        get; set;
    }
    public int Id
    {
        get; set;
    }
    public int ApplicationId
    {
        get; set;
    }
    public JobApplication Application { get; set; } = null!;
    public DateOnly EventDate
    {
        get; set;
    }
    public DateTime? OccurredAtUtc
    {
        get; set;
    }
    public TimelineKind Kind
    {
        get; set;
    }
    public string Description { get; set; } = string.Empty;
    public bool IsSystem
    {
        get; set;
    }
    public ApplicationStatus? PreviousStatus
    {
        get; set;
    }
    public ApplicationStatus? NewStatus
    {
        get; set;
    }
    public bool IsArchived
    {
        get; set;
    }
    public DateTime CreatedAtUtc
    {
        get; set;
    }
    public DateTime UpdatedAtUtc
    {
        get; set;
    }
}
