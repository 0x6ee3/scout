using System.ComponentModel.DataAnnotations;

namespace Scout.Web.Features.Contacts;

public sealed class ContactEditModel
{
    [Range(1, int.MaxValue)]
    public int CompanyId
    {
        get; set;
    }
    [Required, StringLength(200)] public string Name { get; set; } = "";
    [StringLength(200)]
    public string? Role
    {
        get; set;
    }
    [EmailAddress, StringLength(320)]
    public string? Email
    {
        get; set;
    }
    [StringLength(100)]
    public string? Phone
    {
        get; set;
    }
    [HttpUrl, StringLength(2048)]
    public string? ProfileUrl
    {
        get; set;
    }
    [StringLength(100000)]
    public string? Notes
    {
        get; set;
    }
}
