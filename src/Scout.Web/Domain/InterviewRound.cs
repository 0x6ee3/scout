namespace Scout.Web.Domain;

public enum InterviewType
{
    HR, Engineering, Culture, Technical, Final, Other
}
public enum InterviewStatus
{
    Planned, Scheduled, Completed, Cancelled
}
public sealed class InterviewRound
{
    public int Id
    {
        get; set;
    }
    public int ApplicationId
    {
        get; set;
    }
    public JobApplication Application { get; set; } = null!;
    public string Title { get; set; } = "Interview";
    public InterviewType Type { get; set; } = InterviewType.Other;
    public InterviewStatus Status
    {
        get; set;
    }
    public DateTime? ScheduledAtUtc
    {
        get; set;
    }
    public DateTime? CompletedAtUtc
    {
        get; set;
    }
    public string? LocationOrMeetingUrl
    {
        get; set;
    }
    public string? PreparationNotes
    {
        get; set;
    }
    public int NotesRevision { get; set; }
    public string? InterviewNotes
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
public sealed class InterviewParticipant
{
    public int InterviewRoundId
    {
        get; set;
    }
    public InterviewRound InterviewRound { get; set; } = null!;
    public int ContactId
    {
        get; set;
    }
    public Contact Contact { get; set; } = null!;
}
