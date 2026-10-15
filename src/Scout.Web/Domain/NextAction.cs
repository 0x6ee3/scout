namespace Scout.Web.Domain;

public enum NextActionStatus
{
    Open, Completed
}
public sealed class NextAction
{
    public int Id
    {
        get; set;
    }
    public int? ApplicationId
    {
        get; set;
    }
    public JobApplication? Application
    {
        get; set;
    }
    public int? PositionId
    {
        get; set;
    }
    public Position? Position
    {
        get; set;
    }
    public int? InterviewRoundId
    {
        get; set;
    }
    public InterviewRound? InterviewRound
    {
        get; set;
    }
    public int? TimelineEventId
    {
        get; set;
    }
    public ApplicationTimelineEvent? TimelineEvent
    {
        get; set;
    }
    public string Title { get; set; } = "";
    public string? Description
    {
        get; set;
    }
    public DateOnly? DueOn
    {
        get; set;
    }
    public NextActionStatus Status
    {
        get; set;
    }
    public DateTime? CompletedAtUtc
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
