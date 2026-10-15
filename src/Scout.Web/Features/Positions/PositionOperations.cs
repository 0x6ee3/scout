using System.ComponentModel.DataAnnotations;
using Microsoft.EntityFrameworkCore;
using Scout.Web.Data;
using Scout.Web.Domain;

namespace Scout.Web.Features.Positions;

public sealed class PositionOperations(IDbContextFactory<ScoutDbContext> contextFactory)
{
    public async Task<IReadOnlyList<PositionListItem>> ListAsync(int? companyId = null, ArchiveFilter archive = ArchiveFilter.Active,
        PositionStatus? status = null, string? search = null)
    {
        await using var context = await contextFactory.CreateDbContextAsync();
        var query = context.Positions.AsNoTracking()
            .Where(p => archive == ArchiveFilter.All || p.IsArchived == (archive == ArchiveFilter.Archived));
        if (companyId.HasValue)
            query = query.Where(p => p.CompanyId == companyId);
        if (status.HasValue)
            query = query.Where(p => p.Status == status);
        var items = await query
            .Select(p => new PositionListItem(p.Id, p.Title, p.CompanyId, p.Company.Name, p.Company.IsArchived,
                p.Location, p.Status, p.DiscoveredOn, p.IsArchived)).ToListAsync();
        var text = search?.Trim() ?? string.Empty;
        return items.Where(p => p.Title.Contains(text, StringComparison.OrdinalIgnoreCase)
                || p.CompanyName.Contains(text, StringComparison.OrdinalIgnoreCase)
                || (p.Location?.Contains(text, StringComparison.OrdinalIgnoreCase) ?? false))
            .OrderByDescending(p => p.DiscoveredOn).ThenBy(p => p.Title, StringComparer.OrdinalIgnoreCase)
            .ThenBy(p => p.Id).ToList();
    }

    public async Task<IReadOnlyList<CompanyOption>> CompanyOptionsAsync(int? includeCompanyId = null)
    {
        await using var context = await contextFactory.CreateDbContextAsync();
        return await context.Companies.AsNoTracking().Where(c => !c.IsArchived || c.Id == includeCompanyId)
            .OrderBy(c => c.Name).ThenBy(c => c.Id)
            .Select(c => new CompanyOption(c.Id, c.Name, c.IsArchived)).ToListAsync();
    }

    public async Task<PositionDetails?> GetAsync(int id)
    {
        await using var context = await contextFactory.CreateDbContextAsync();
        return await context.Positions.AsNoTracking().Where(p => p.Id == id)
            .Select(p => new PositionDetails(p.Id, p.CompanyId, p.Company.Name, p.Company.IsArchived, p.Title,
                p.OriginalVacancyUrl, p.JobDescriptionSnapshot, p.DescriptionCapturedAtUtc, p.Location,
                p.RemoteWorkNotes, p.EmploymentType, p.PersonalNotes, p.Status, p.DiscoveredOn, p.ClosingOn,
                p.IsArchived, p.CreatedAtUtc, p.UpdatedAtUtc)).SingleOrDefaultAsync();
    }

    public async Task<int> CreateAsync(PositionEditModel draft)
    {
        var values = NormalizeAndValidate(draft);
        await using var context = await contextFactory.CreateDbContextAsync();
        await RequireActiveCompanyAsync(context, values.CompanyId);
        var now = DateTime.UtcNow;
        var position = new Position { CreatedAtUtc = now, UpdatedAtUtc = now };
        CopyEditableValues(values, position, now);
        context.Positions.Add(position);
        await context.SaveChangesAsync();
        return position.Id;
    }

    public async Task<bool> UpdateAsync(int id, PositionEditModel draft)
    {
        var values = NormalizeAndValidate(draft);
        await using var context = await contextFactory.CreateDbContextAsync();
        var position = await context.Positions.SingleOrDefaultAsync(p => p.Id == id);
        if (position is null)
            return false;
        if (position.CompanyId != values.CompanyId)
        {
            if (await context.Applications.AnyAsync(a => a.PositionId == id))
                throw new ValidationException("This position has application history. Its company cannot be changed; archive it and create a position for the correct company.");
            await RequireActiveCompanyAsync(context, values.CompanyId);
        }
        var now = DateTime.UtcNow;
        CopyEditableValues(values, position, now);
        position.UpdatedAtUtc = now;
        await context.SaveChangesAsync();
        return true;
    }

    public async Task<bool> SetArchivedAsync(int id, bool archived)
    {
        await using var context = await contextFactory.CreateDbContextAsync();
        var position = await context.Positions.SingleOrDefaultAsync(p => p.Id == id);
        if (position is null)
            return false;
        if (position.IsArchived == archived)
            return true;
        position.IsArchived = archived;
        position.UpdatedAtUtc = DateTime.UtcNow;
        await context.SaveChangesAsync();
        return true;
    }

    private static async Task RequireActiveCompanyAsync(ScoutDbContext context, int id)
    {
        if (!await context.Companies.AnyAsync(c => c.Id == id && !c.IsArchived))
            throw new ValidationException("Choose an existing active company. Restore an archived company before adding positions to it.");
    }

    private static PositionEditModel NormalizeAndValidate(PositionEditModel draft)
    {
        var values = new PositionEditModel
        {
            CompanyId = draft.CompanyId,
            Title = draft.Title?.Trim() ?? string.Empty,
            OriginalVacancyUrl = TrimOptional(draft.OriginalVacancyUrl),
            Location = TrimOptional(draft.Location),
            EmploymentType = TrimOptional(draft.EmploymentType),
            RemoteWorkNotes = TextOptional(draft.RemoteWorkNotes),
            PersonalNotes = TextOptional(draft.PersonalNotes),
            JobDescriptionSnapshot = TextOptional(draft.JobDescriptionSnapshot),
            Status = draft.Status,
            DiscoveredOn = draft.DiscoveredOn,
            ClosingOn = draft.ClosingOn
        };
        Validator.ValidateObject(values, new ValidationContext(values), validateAllProperties: true);
        return values;
    }

    private static string? TrimOptional(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    private static string? TextOptional(string? value) => string.IsNullOrWhiteSpace(value) ? null : value;

    private static void CopyEditableValues(PositionEditModel values, Position position, DateTime now)
    {
        position.CompanyId = values.CompanyId;
        position.Title = values.Title;
        position.OriginalVacancyUrl = values.OriginalVacancyUrl;
        position.Location = values.Location;
        position.EmploymentType = values.EmploymentType;
        position.RemoteWorkNotes = values.RemoteWorkNotes;
        position.PersonalNotes = values.PersonalNotes;
        position.Status = values.Status;
        position.DiscoveredOn = values.DiscoveredOn;
        position.ClosingOn = values.ClosingOn;
        // The first nonempty description is immutable, even for callers outside the UI.
        if (position.JobDescriptionSnapshot is null && values.JobDescriptionSnapshot is not null)
        {
            position.JobDescriptionSnapshot = values.JobDescriptionSnapshot;
            position.DescriptionCapturedAtUtc = now;
        }
    }
}

public sealed record CompanyOption(int Id, string Name, bool IsArchived);
public sealed record PositionListItem(int Id, string Title, int CompanyId, string CompanyName, bool CompanyIsArchived,
    string? Location, PositionStatus Status, DateOnly DiscoveredOn, bool IsArchived);
public sealed record PositionDetails(int Id, int CompanyId, string CompanyName, bool CompanyIsArchived, string Title,
    string? OriginalVacancyUrl, string? JobDescriptionSnapshot, DateTime? DescriptionCapturedAtUtc, string? Location,
    string? RemoteWorkNotes, string? EmploymentType, string? PersonalNotes, PositionStatus Status, DateOnly DiscoveredOn,
    DateOnly? ClosingOn, bool IsArchived, DateTime CreatedAtUtc, DateTime UpdatedAtUtc);
