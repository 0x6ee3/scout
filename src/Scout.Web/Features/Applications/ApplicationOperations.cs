using System.ComponentModel.DataAnnotations;
using Microsoft.EntityFrameworkCore;
using Scout.Web.Data;
using Scout.Web.Domain;

namespace Scout.Web.Features.Applications;

public sealed class ApplicationOperations(IDbContextFactory<ScoutDbContext> factory)
{
    public async Task<IReadOnlyList<ApplicationListItem>> ListAsync(int? positionId = null, int? companyId = null,
        ArchiveFilter archive = ArchiveFilter.Active, ApplicationStatus? status = null, string? search = null)
    {
        await using var db = await factory.CreateDbContextAsync();
        var query = db.Applications.AsNoTracking().Where(a => archive == ArchiveFilter.All || a.IsArchived == (archive == ArchiveFilter.Archived));
        if (positionId.HasValue)
            query = query.Where(a => a.PositionId == positionId);
        if (companyId.HasValue)
            query = query.Where(a => a.Position.CompanyId == companyId);
        if (status.HasValue)
            query = query.Where(a => a.Status == status);
        var rows = await query.Select(a => new ApplicationListItem(a.Id, a.PositionId, a.Position.Title,
            a.Position.CompanyId, a.Position.Company.Name, a.Position.IsArchived, a.Position.Company.IsArchived,
            a.AttemptLabel, a.Status, a.IsArchived, a.UpdatedAtUtc)).ToListAsync();
        var text = search?.Trim() ?? "";
        return rows.Where(a => a.Title.Contains(text, StringComparison.OrdinalIgnoreCase)
            || a.CompanyName.Contains(text, StringComparison.OrdinalIgnoreCase)
            || (a.AttemptLabel?.Contains(text, StringComparison.OrdinalIgnoreCase) ?? false))
            .OrderByDescending(a => a.UpdatedAtUtc).ThenBy(a => a.Id).ToList();
    }

    public async Task<JobApplication?> GetAsync(int id)
    {
        await using var db = await factory.CreateDbContextAsync();
        return await db.Applications.AsNoTracking().Include(a => a.Position).ThenInclude(p => p.Company).SingleOrDefaultAsync(a => a.Id == id);
    }

    public async Task<int> CreateAsync(ApplicationEditModel draft)
    {
        var values = Normalize(draft);
        await using var db = await factory.CreateDbContextAsync();
        if (!await db.Positions.AnyAsync(p => p.Id == values.PositionId && !p.IsArchived && !p.Company.IsArchived))
            throw new ValidationException("Choose an active position at an active company. Restore archived records first.");
        var now = DateTime.UtcNow;
        var application = new JobApplication { PositionId = values.PositionId, CreatedAtUtc = now, UpdatedAtUtc = now };
        Copy(values, application);
        db.Applications.Add(application);
        db.TimelineEvents.Add(SystemEvent(application, TimelineKind.ApplicationCreated, $"Application created with status {application.Status}.", now, null, application.Status));
        await db.SaveChangesAsync();
        return application.Id;
    }

    public async Task<bool> UpdateAsync(int id, ApplicationEditModel draft)
    {
        var values = Normalize(draft);
        await using var db = await factory.CreateDbContextAsync();
        var application = await db.Applications.SingleOrDefaultAsync(a => a.Id == id);
        if (application is null)
            return false;
        if (values.PositionId != application.PositionId)
            throw new ValidationException("An application's position cannot be changed. Archive this attempt and create one for the correct position.");
        var previous = application.Status;
        var now = DateTime.UtcNow;
        Copy(values, application);
        application.UpdatedAtUtc = now;
        if (previous != application.Status)
            db.TimelineEvents.Add(SystemEvent(application, TimelineKind.StatusChanged, $"Status changed from {previous} to {application.Status}.", now, previous, application.Status));
        await db.SaveChangesAsync();
        return true;
    }

    public async Task<bool> SetArchivedAsync(int id, bool archived)
    {
        await using var db = await factory.CreateDbContextAsync();
        var record = await db.Applications.SingleOrDefaultAsync(a => a.Id == id);
        if (record is null)
            return false;
        if (record.IsArchived == archived)
            return true;
        record.IsArchived = archived;
        record.UpdatedAtUtc = DateTime.UtcNow;
        await db.SaveChangesAsync();
        return true;
    }

    internal static ApplicationTimelineEvent SystemEvent(JobApplication application, TimelineKind kind, string description,
        DateTime now, ApplicationStatus? previous = null, ApplicationStatus? next = null) => new()
        {
            Application = application,
            EventDate = DateOnly.FromDateTime(now.ToLocalTime()),
            OccurredAtUtc = now,
            Kind = kind,
            Description = description,
            IsSystem = true,
            PreviousStatus = previous,
            NewStatus = next,
            CreatedAtUtc = now,
            UpdatedAtUtc = now
        };

    private static ApplicationEditModel Normalize(ApplicationEditModel d) => InputRules.Validate(new ApplicationEditModel
    {
        PositionId = d.PositionId,
        AttemptLabel = InputRules.Trim(d.AttemptLabel),
        Status = d.Status,
        AppliedOn = d.AppliedOn,
        OutcomeOn = d.OutcomeOn,
        OutcomeNotes = InputRules.Text(d.OutcomeNotes),
        Notes = InputRules.Text(d.Notes)
    });
    private static void Copy(ApplicationEditModel d, JobApplication a)
    {
        a.AttemptLabel = d.AttemptLabel;
        a.Status = d.Status;
        a.AppliedOn = d.AppliedOn;
        a.OutcomeOn = d.OutcomeOn;
        a.OutcomeNotes = d.OutcomeNotes;
        a.Notes = d.Notes;
    }
}

public sealed record ApplicationListItem(int Id, int PositionId, string Title, int CompanyId, string CompanyName,
    bool PositionIsArchived, bool CompanyIsArchived, string? AttemptLabel, ApplicationStatus Status, bool IsArchived, DateTime UpdatedAtUtc);
