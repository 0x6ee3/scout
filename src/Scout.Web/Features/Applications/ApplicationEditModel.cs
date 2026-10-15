using System.ComponentModel.DataAnnotations;
using Scout.Web.Domain;

namespace Scout.Web.Features.Applications;

public sealed class ApplicationEditModel
{
    [Range(1, int.MaxValue, ErrorMessage = "Choose a position.")]
    public int PositionId
    {
        get; set;
    }
    [StringLength(200)]
    public string? AttemptLabel
    {
        get; set;
    }
    [EnumDataType(typeof(ApplicationStatus))]
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
    [StringLength(100000)]
    public string? OutcomeNotes
    {
        get; set;
    }
    [StringLength(100000)]
    public string? Notes
    {
        get; set;
    }
}
