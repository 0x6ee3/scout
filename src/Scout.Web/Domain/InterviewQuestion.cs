namespace Scout.Web.Domain;

public enum InterviewQuestionOrigin
{
    Library, AdHoc
}

public sealed class InterviewQuestion
{
    public InterviewQuestionOrigin Origin
    {
        get; set;
    }
    public bool IsAsked
    {
        get; set;
    }
    public bool FollowUpRequired
    {
        get; set;
    }
    public int ContentRevision
    {
        get; set;
    }
    public Guid? CreationToken
    {
        get; set;
    }
    public int Id
    {
        get; set;
    }
    public int InterviewRoundId
    {
        get; set;
    }
    public InterviewRound InterviewRound { get; set; } = null!;
    public int? EmployerQuestionId
    {
        get; set;
    }
    public EmployerQuestion? EmployerQuestion
    {
        get; set;
    }
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
    public string? Answer
    {
        get; set;
    }
    public int DisplayOrder
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
