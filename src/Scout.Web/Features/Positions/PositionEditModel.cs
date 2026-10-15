using System.ComponentModel.DataAnnotations;
using Scout.Web.Domain;

namespace Scout.Web.Features.Positions;

public sealed class PositionEditModel
{
    [Range(1, int.MaxValue, ErrorMessage = "Choose a company.")]
    public int CompanyId
    {
        get; set;
    }
    [Required(ErrorMessage = "Enter a position title.")]
    [StringLength(200, MinimumLength = 1, ErrorMessage = "Use between 1 and 200 characters for the title.")]
    public string Title { get; set; } = string.Empty;
    [HttpUrl, StringLength(2048, ErrorMessage = "Use at most 2,048 characters for the vacancy URL.")]
    public string? OriginalVacancyUrl
    {
        get; set;
    }
    [StringLength(100_000, ErrorMessage = "Use at most 100,000 characters for the vacancy description.")]
    public string? JobDescriptionSnapshot
    {
        get; set;
    }
    [StringLength(300, ErrorMessage = "Use at most 300 characters for the location.")]
    public string? Location
    {
        get; set;
    }
    [StringLength(100_000, ErrorMessage = "Use at most 100,000 characters for remote-work notes.")]
    public string? RemoteWorkNotes
    {
        get; set;
    }
    [StringLength(100, ErrorMessage = "Use at most 100 characters for the employment type.")]
    public string? EmploymentType
    {
        get; set;
    }
    [StringLength(100_000, ErrorMessage = "Use at most 100,000 characters for personal notes.")]
    public string? PersonalNotes
    {
        get; set;
    }
    [EnumDataType(typeof(PositionStatus), ErrorMessage = "Choose a valid opportunity status.")]
    public PositionStatus Status { get; set; } = PositionStatus.Discovered;
    public DateOnly DiscoveredOn { get; set; } = DateOnly.FromDateTime(DateTime.Today);
    public DateOnly? ClosingOn
    {
        get; set;
    }
}
