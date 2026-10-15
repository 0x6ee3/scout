namespace Scout.Web.Domain;

public sealed class InterviewExperience
{
    public int Id { get; set; }
    public int InterviewRoundId { get; set; }
    public InterviewRound InterviewRound { get; set; } = null!;
    public int ProfessionalExperienceId { get; set; }
    public ProfessionalExperience ProfessionalExperience { get; set; } = null!;
    public string? PreparationNotes { get; set; }
    public int DisplayOrder { get; set; }
    public bool IsArchived { get; set; }
    public DateTime CreatedAtUtc { get; set; }
    public DateTime UpdatedAtUtc { get; set; }
}
