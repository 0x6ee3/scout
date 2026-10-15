namespace Scout.Web.Domain;

public sealed class Contact
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
    public string Name { get; set; } = "";
    public string? Role
    {
        get; set;
    }
    public string? Email
    {
        get; set;
    }
    public string? Phone
    {
        get; set;
    }
    public string? ProfileUrl
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

public sealed class ApplicationContact
{
    public int ApplicationId
    {
        get; set;
    }
    public JobApplication Application { get; set; } = null!;
    public int ContactId
    {
        get; set;
    }
    public Contact Contact { get; set; } = null!;
}

public sealed class TimelineEventContact
{
    public int EventId
    {
        get; set;
    }
    public ApplicationTimelineEvent Event { get; set; } = null!;
    public int ContactId
    {
        get; set;
    }
    public Contact Contact { get; set; } = null!;
}
