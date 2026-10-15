using System.ComponentModel.DataAnnotations;
using Microsoft.EntityFrameworkCore;
using Scout.Web.Data;
using Scout.Web.Domain;
using Scout.Web.Features.Contacts;

namespace Scout.Web.Features.Timeline;

public sealed class TimelineEditModel
{
    [Range(1, int.MaxValue)]
    public int ApplicationId
    {
        get; set;
    }
    public DateOnly EventDate { get; set; } = DateOnly.FromDateTime(DateTime.Today);
    public DateTime? LocalOccurrence
    {
        get; set;
    }
    public int? OffsetMinutes
    {
        get; set;
    }
    [EnumDataType(typeof(TimelineKind))] public TimelineKind Kind { get; set; } = TimelineKind.PersonalNote;
    [Required, StringLength(100000)] public string Description { get; set; } = "";
    public HashSet<int> ContactIds { get; set; } = [];
}

public sealed class TimelineOperations(IDbContextFactory<ScoutDbContext> factory)
{
    public async Task<IReadOnlyList<ApplicationTimelineEvent>> ListAsync(int applicationId, ArchiveFilter archive = ArchiveFilter.Active)
    {
        await using var db = await factory.CreateDbContextAsync();
        var q = db.TimelineEvents.AsNoTracking().Where(e => e.ApplicationId == applicationId);
        if (archive != ArchiveFilter.All)
            q = q.Where(e => e.IsArchived == (archive == ArchiveFilter.Archived));
        return await q.OrderBy(e => e.EventDate).ThenBy(e => e.OccurredAtUtc).ThenBy(e => e.Id).ToListAsync();
    }
    public async Task<ApplicationTimelineEvent?> GetAsync(int id)
    {
        await using var db = await factory.CreateDbContextAsync();
        return await db.TimelineEvents.AsNoTracking().SingleOrDefaultAsync(e => e.Id == id);
    }
    public async Task<HashSet<int>> SelectionAsync(int id)
    {
        await using var db = await factory.CreateDbContextAsync();
        return (await db.TimelineEventContacts.Where(l => l.EventId == id).Select(l => l.ContactId).ToListAsync()).ToHashSet();
    }
    public async Task<int> SaveAsync(int? id, TimelineEditModel m)
    {
        InputRules.Validate(m);
        if (m.Kind is TimelineKind.ApplicationCreated or TimelineKind.StatusChanged)
            throw new ValidationException("Choose a manual event kind.");
        if (m.EventDate == default)
            throw new ValidationException("Enter an event date.");
        if (m.LocalOccurrence.HasValue && DateOnly.FromDateTime(m.LocalOccurrence.Value) != m.EventDate)
            throw new ValidationException("The occurrence time must have the same date as the event.");
        var utc = LocalTime.ToUtc(m.LocalOccurrence, m.OffsetMinutes);
        await using var db = await factory.CreateDbContextAsync();
        var app = await db.Applications.Include(a => a.Position).SingleOrDefaultAsync(a => a.Id == m.ApplicationId) ?? throw new ValidationException("Application no longer exists.");
        if (!id.HasValue && app.IsArchived)
            throw new ValidationException("Restore the application before adding events.");
        var e = id.HasValue ? await db.TimelineEvents.FindAsync(id.Value) ?? throw new ValidationException("Event no longer exists.") : new ApplicationTimelineEvent { ApplicationId = m.ApplicationId, CreatedAtUtc = DateTime.UtcNow };
        if (e.IsSystem || e.ApplicationId != m.ApplicationId)
            throw new ValidationException("System facts and event ownership cannot be changed. Add a clarifying note instead.");
        var links = id.HasValue ? await db.TimelineEventContacts.Where(l => l.EventId == id).ToListAsync() : [];
        await ContactOperations.ValidateSelectionAsync(db, app.Position.CompanyId, m.ContactIds, links.Select(l => l.ContactId));
        e.EventDate = m.EventDate;
        e.OccurredAtUtc = utc;
        e.Kind = m.Kind;
        e.Description = m.Description;
        e.UpdatedAtUtc = DateTime.UtcNow;
        if (!id.HasValue)
            db.TimelineEvents.Add(e);
        db.TimelineEventContacts.RemoveRange(links.Where(l => !m.ContactIds.Contains(l.ContactId)));
        db.TimelineEventContacts.AddRange(m.ContactIds.Except(links.Select(l => l.ContactId)).Select(c => new TimelineEventContact { Event = e, ContactId = c }));
        await db.SaveChangesAsync();
        return e.Id;
    }
    public async Task<bool> SetArchivedAsync(int id, bool archived)
    {
        await using var db = await factory.CreateDbContextAsync();
        var e = await db.TimelineEvents.FindAsync(id);
        if (e is null)
            return false;
        if (e.IsSystem)
            throw new ValidationException("System facts cannot be archived. Add a clarifying note instead.");
        if (e.IsArchived != archived)
        {
            e.IsArchived = archived;
            e.UpdatedAtUtc = DateTime.UtcNow;
            await db.SaveChangesAsync();
        }
        return true;
    }
}
