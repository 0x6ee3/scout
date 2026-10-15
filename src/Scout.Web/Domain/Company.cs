namespace Scout.Web.Domain;

public sealed class Company
{
    public int Id
    {
        get; set;
    }
    public string Name { get; set; } = string.Empty;
    public string? WebsiteUrl
    {
        get; set;
    }
    public string? Location
    {
        get; set;
    }
    public string? Description
    {
        get; set;
    }
    public string? PersonalNotes
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
