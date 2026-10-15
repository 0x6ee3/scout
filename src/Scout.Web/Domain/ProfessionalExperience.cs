namespace Scout.Web.Domain;

public sealed class ProfessionalExperience
{
    public int Id { get; set; }
    public string Title { get; set; } = "";
    public string? MemoryCue { get; set; }
    public string? Situation { get; set; }
    public string? Task { get; set; }
    public string? Action { get; set; }
    public string? Result { get; set; }
    public string? Reflection { get; set; }
    public string? UsefulFor { get; set; }
    public string? Notes { get; set; }
    public bool IsArchived { get; set; }
    public DateTime CreatedAtUtc { get; set; }
    public DateTime UpdatedAtUtc { get; set; }
    public List<ExperienceTag> Tags { get; set; } = [];
}

public sealed class ExperienceTag
{
    public int ProfessionalExperienceId { get; set; }
    public ProfessionalExperience ProfessionalExperience { get; set; } = null!;
    public string Tag { get; set; } = "";
}
