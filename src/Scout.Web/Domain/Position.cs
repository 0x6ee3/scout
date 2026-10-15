namespace Scout.Web.Domain;

public sealed class Position
{
    public int Id
    {
        get; set;
    }
    public int CompanyId
    {
        get; set;
    }
    public Company Company { get; set; } = null!;
    public string Title { get; set; } = string.Empty;
    public string? OriginalVacancyUrl
    {
        get; set;
    }
    public string? JobDescriptionSnapshot
    {
        get; set;
    }
    public DateTime? DescriptionCapturedAtUtc
    {
        get; set;
    }
    public string? Location
    {
        get; set;
    }
    public string? RemoteWorkNotes
    {
        get; set;
    }
    public string? EmploymentType
    {
        get; set;
    }
    public string? PersonalNotes
    {
        get; set;
    }
    public PositionStatus Status
    {
        get; set;
    }
    public DateOnly DiscoveredOn
    {
        get; set;
    }
    public DateOnly? ClosingOn
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
