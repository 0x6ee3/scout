using System.ComponentModel.DataAnnotations;
using Microsoft.EntityFrameworkCore;
using Scout.Web.Data;
using Scout.Web.Domain;

namespace Scout.Web.Features.Companies;

public sealed class CompanyOperations(IDbContextFactory<ScoutDbContext> contextFactory)
{
    public async Task<IReadOnlyList<CompanyListItem>> ListAsync(ArchiveFilter archive = ArchiveFilter.Active, string? search = null)
    {
        await using var context = await contextFactory.CreateDbContextAsync();
        var items = await context.Companies.AsNoTracking()
            .Where(company => archive == ArchiveFilter.All || company.IsArchived == (archive == ArchiveFilter.Archived))
            .Select(company => new CompanyListItem(company.Id, company.Name, company.Location, company.IsArchived))
            .ToListAsync();
        var text = search?.Trim() ?? string.Empty;
        return items.Where(c => c.Name.Contains(text, StringComparison.OrdinalIgnoreCase)
                || (c.Location?.Contains(text, StringComparison.OrdinalIgnoreCase) ?? false))
            .OrderBy(c => c.Name, StringComparer.OrdinalIgnoreCase).ThenBy(c => c.Id).ToList();
    }

    public async Task<CompanyDetails?> GetAsync(int id)
    {
        await using var context = await contextFactory.CreateDbContextAsync();
        return await context.Companies.AsNoTracking()
            .Where(company => company.Id == id)
            .Select(company => new CompanyDetails(company.Id, company.Name,
                company.WebsiteUrl, company.Location, company.Description, company.PersonalNotes, company.IsArchived,
                company.CreatedAtUtc, company.UpdatedAtUtc))
            .SingleOrDefaultAsync();
    }

    public async Task<int> CreateAsync(CompanyEditModel model)
    {
        var values = NormalizeAndValidate(model);
        var now = DateTime.UtcNow;
        var company = new Company { CreatedAtUtc = now, UpdatedAtUtc = now };
        CopyEditableValues(values, company);
        await using var context = await contextFactory.CreateDbContextAsync();
        context.Companies.Add(company);
        await context.SaveChangesAsync();
        return company.Id;
    }

    public async Task<bool> UpdateAsync(int id, CompanyEditModel model)
    {
        var values = NormalizeAndValidate(model);
        await using var context = await contextFactory.CreateDbContextAsync();
        var company = await context.Companies.SingleOrDefaultAsync(company => company.Id == id);
        if (company is null)
            return false;

        CopyEditableValues(values, company);
        company.UpdatedAtUtc = DateTime.UtcNow;
        await context.SaveChangesAsync();
        return true;
    }

    public async Task<bool> SetArchivedAsync(int id, bool archived)
    {
        await using var context = await contextFactory.CreateDbContextAsync();
        var company = await context.Companies.SingleOrDefaultAsync(c => c.Id == id);
        if (company is null)
            return false;
        if (company.IsArchived == archived)
            return true;
        company.IsArchived = archived;
        company.UpdatedAtUtc = DateTime.UtcNow;
        await context.SaveChangesAsync();
        return true;
    }

    private static CompanyEditModel NormalizeAndValidate(CompanyEditModel model)
    {
        // Keep the caller's draft intact, including after a failed write.
        var values = new CompanyEditModel
        {
            Name = model.Name?.Trim() ?? string.Empty,
            WebsiteUrl = OptionalTrimmed(model.WebsiteUrl),
            Location = OptionalTrimmed(model.Location),
            Description = string.IsNullOrWhiteSpace(model.Description) ? null : model.Description,
            PersonalNotes = string.IsNullOrWhiteSpace(model.PersonalNotes) ? null : model.PersonalNotes
        };
        Validator.ValidateObject(values, new ValidationContext(values), validateAllProperties: true);
        return values;
    }

    private static string? OptionalTrimmed(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    private static void CopyEditableValues(CompanyEditModel values, Company company)
    {
        company.Name = values.Name;
        company.WebsiteUrl = values.WebsiteUrl;
        company.Location = values.Location;
        company.Description = values.Description;
        company.PersonalNotes = values.PersonalNotes;
    }
}

public sealed record CompanyListItem(int Id, string Name, string? Location, bool IsArchived);

public sealed record CompanyDetails(int Id, string Name, string? WebsiteUrl, string? Location,
    string? Description, string? PersonalNotes, bool IsArchived,
    DateTime CreatedAtUtc, DateTime UpdatedAtUtc);
