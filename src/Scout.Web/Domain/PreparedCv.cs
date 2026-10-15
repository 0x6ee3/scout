namespace Scout.Web.Domain;

public sealed class PreparedCv
{
    public int Id
    {
        get; set;
    }
    public int DocumentId
    {
        get; set;
    }
    public Document Document { get; set; } = null!;
    public string DisplayName { get; set; } = "";
    public string? Language
    {
        get; set;
    }
    public string? TechnologyFocus
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
public enum SubmissionFileRole
{
    CV, CoverLetter, Other
}
public sealed class ApplicationSubmission
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
    public DateOnly SubmittedOn
    {
        get; set;
    }
    public string? Notes
    {
        get; set;
    }
    public DateTime RecordedAtUtc
    {
        get; set;
    }
    public Guid RecordToken
    {
        get; set;
    }
    public string PayloadHash { get; set; } = "";
    public bool IsIncorrect
    {
        get; set;
    }
    public string? CorrectionReason
    {
        get; set;
    }
}
public sealed class SubmissionFile
{
    public int SubmissionId
    {
        get; set;
    }
    public ApplicationSubmission Submission { get; set; } = null!;
    public int DocumentId
    {
        get; set;
    }
    public Document Document { get; set; } = null!;
    public SubmissionFileRole Role
    {
        get; set;
    }
    public string LabelSnapshot { get; set; } = "";
}
