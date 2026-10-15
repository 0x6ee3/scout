namespace Scout.Web.Domain;

public sealed class Document
{
    public int Id
    {
        get; set;
    }
    public string OriginalName { get; set; } = "";
    public string StoredRelativePath { get; set; } = "";
    public string ContentType { get; set; } = "";
    public long Size
    {
        get; set;
    }
    public string Sha256 { get; set; } = "";
    public DateTime ImportedAtUtc
    {
        get; set;
    }
    public string? Description
    {
        get; set;
    }
    public string? Category
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
public sealed class CompanyDocument
{
    public int CompanyId
    {
        get; set;
    }
    public Company Company { get; set; } = null!; public int DocumentId
    {
        get; set;
    }
    public Document Document { get; set; } = null!;
}
public sealed class PositionDocument
{
    public int PositionId
    {
        get; set;
    }
    public Position Position { get; set; } = null!; public int DocumentId
    {
        get; set;
    }
    public Document Document { get; set; } = null!;
}
public sealed class ApplicationDocument
{
    public int ApplicationId
    {
        get; set;
    }
    public JobApplication Application { get; set; } = null!; public int DocumentId
    {
        get; set;
    }
    public Document Document { get; set; } = null!;
}
public sealed class TimelineEventDocument
{
    public int EventId
    {
        get; set;
    }
    public ApplicationTimelineEvent Event { get; set; } = null!; public int DocumentId
    {
        get; set;
    }
    public Document Document { get; set; } = null!;
}
public sealed class InterviewDocument
{
    public int InterviewRoundId
    {
        get; set;
    }
    public InterviewRound InterviewRound { get; set; } = null!; public int DocumentId
    {
        get; set;
    }
    public Document Document { get; set; } = null!;
}
