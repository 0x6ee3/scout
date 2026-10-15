namespace Scout.Web.Domain;

public enum ApplicationStatus
{
    Planned, Applied, Screening, Interviewing, Offer, Rejected, Withdrawn, Accepted
}

public sealed class JobApplication
{
    public int Id
    {
        get; set;
    }
    public int PositionId
    {
        get; set;
    }
    public Position Position { get; set; } = null!;
    public string? AttemptLabel
    {
        get; set;
    }
    public ApplicationStatus Status
    {
        get; set;
    }
    public DateOnly? AppliedOn
    {
        get; set;
    }
    public DateOnly? OutcomeOn
    {
        get; set;
    }
    public string? OutcomeNotes
    {
        get; set;
    }
    public string? Notes
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
