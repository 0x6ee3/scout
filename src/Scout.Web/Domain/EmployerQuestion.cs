namespace Scout.Web.Domain;

public sealed class EmployerQuestion
{
    public int Id { get; set; }
    public string Text { get; set; } = "";
    public string? Intent { get; set; }
    public string? FollowUpQuestions { get; set; }
    public string? Notes { get; set; }
    public bool IsArchived { get; set; }
    public DateTime CreatedAtUtc { get; set; }
    public DateTime UpdatedAtUtc { get; set; }
    public List<EmployerQuestionTag> Tags { get; set; } = [];
}

public sealed class EmployerQuestionTag
{
    public int EmployerQuestionId { get; set; }
    public EmployerQuestion EmployerQuestion { get; set; } = null!;
    public string Tag { get; set; } = "";
}
