using System.ComponentModel.DataAnnotations;

namespace Scout.Web.Features.Companies;

public sealed class CompanyEditModel
{
    [Required(ErrorMessage = "Enter a company name.")]
    [StringLength(200, MinimumLength = 1, ErrorMessage = "Use between 1 and 200 characters.")]
    public string Name { get; set; } = string.Empty;

    [StringLength(2048, ErrorMessage = "Use at most 2,048 characters for the website.")]
    [HttpUrl]
    public string? WebsiteUrl
    {
        get; set;
    }

    [StringLength(300, ErrorMessage = "Use at most 300 characters for the location.")]
    public string? Location
    {
        get; set;
    }

    [StringLength(100_000, ErrorMessage = "Use at most 100,000 characters for the reference description.")]
    public string? Description
    {
        get; set;
    }

    [StringLength(100_000, ErrorMessage = "Use at most 100,000 characters for personal notes.")]
    public string? PersonalNotes
    {
        get; set;
    }
}
