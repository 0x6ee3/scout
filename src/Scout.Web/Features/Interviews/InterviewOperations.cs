using System.ComponentModel.DataAnnotations;
using Microsoft.EntityFrameworkCore;
using Scout.Web.Data;
using Scout.Web.Domain;
using Scout.Web.Features.Contacts;
namespace Scout.Web.Features.Interviews;

public sealed class InterviewEditModel
{
    public int NotesRevision
    {
        get; set;
    }
    [Range(1, int.MaxValue)]
    public int ApplicationId
    {
        get; set;
    }
    [Required, StringLength(200)] public string Title { get; set; } = "Interview";
    [EnumDataType(typeof(InterviewType))] public InterviewType Type { get; set; } = InterviewType.Other;
    [EnumDataType(typeof(InterviewStatus))]
    public InterviewStatus Status
    {
        get; set;
    }
    public DateTime? ScheduledLocal
    {
        get; set;
    }
    public int? ScheduledOffset
    {
        get; set;
    }
    public DateTime? CompletedLocal
    {
        get; set;
    }
    public int? CompletedOffset
    {
        get; set;
    }
    [StringLength(2048)]
    public string? LocationOrMeetingUrl
    {
        get; set;
    }
    [StringLength(100000)]
    public string? PreparationNotes
    {
        get; set;
    }
    [StringLength(100000)]
    public string? InterviewNotes
    {
        get; set;
    }
    public HashSet<int> ContactIds { get; set; } = [];
}

public sealed class InterviewOperations(IDbContextFactory<ScoutDbContext> factory)
{
    public async Task<IReadOnlyList<InterviewListItem>> ListAsync(int applicationId, ArchiveFilter archive = ArchiveFilter.Active)
    {
        await using var db = await factory.CreateDbContextAsync();
        return await db.Interviews.AsNoTracking().Where(r => r.ApplicationId == applicationId && (archive == ArchiveFilter.All || r.IsArchived == (archive == ArchiveFilter.Archived)))
            .OrderBy(r => r.Id).Select(r => new InterviewListItem(r.Id, r.Title, r.Type, r.Status, r.ScheduledAtUtc, r.IsArchived)).ToListAsync();
    }
    public async Task<InterviewRound?> GetAsync(int id)
    {
        await using var db = await factory.CreateDbContextAsync();
        return await db.Interviews.AsNoTracking().Include(r => r.Application).ThenInclude(a => a.Position).SingleOrDefaultAsync(r => r.Id == id);
    }
    public async Task<HashSet<int>> SelectionAsync(int id)
    {
        await using var db = await factory.CreateDbContextAsync();
        return (await db.InterviewParticipants.Where(p => p.InterviewRoundId == id).Select(p => p.ContactId).ToListAsync()).ToHashSet();
    }
    public async Task<int> SaveAsync(int? id, InterviewEditModel m)
    {
        InputRules.Validate(m);
        if (string.IsNullOrWhiteSpace(m.Title))
            throw new ValidationException("Enter an interview title.");
        var schedule = LocalTime.ToUtc(m.ScheduledLocal, m.ScheduledOffset);
        var completed = LocalTime.ToUtc(m.CompletedLocal, m.CompletedOffset);
        if (m.Status == InterviewStatus.Scheduled && schedule is null)
            throw new ValidationException("A scheduled interview needs a date and time.");
        await using var db = await factory.CreateDbContextAsync();
        var app = await db.Applications.Include(a => a.Position).SingleOrDefaultAsync(a => a.Id == m.ApplicationId) ?? throw new ValidationException("Application no longer exists.");
        if (!id.HasValue && app.IsArchived)
            throw new ValidationException("Restore the application before adding interviews.");
        var r = id.HasValue ? await db.Interviews.FindAsync(id.Value) ?? throw new ValidationException("Interview no longer exists.") : new InterviewRound { ApplicationId = m.ApplicationId, CreatedAtUtc = DateTime.UtcNow };
        if (r.ApplicationId != m.ApplicationId)
            throw new ValidationException("Interview ownership cannot change.");
        var links = id.HasValue ? await db.InterviewParticipants.Where(p => p.InterviewRoundId == id).ToListAsync() : [];
        await ContactOperations.ValidateSelectionAsync(db, app.Position.CompanyId, m.ContactIds, links.Select(l => l.ContactId));
        var old = r.Status;
        var now = DateTime.UtcNow;
        r.Title = m.Title.Trim();
        r.Type = m.Type;
        r.Status = m.Status;
        r.ScheduledAtUtc = schedule;
        r.CompletedAtUtc = completed;
        r.LocationOrMeetingUrl = InputRules.Trim(m.LocationOrMeetingUrl);
        r.PreparationNotes = InputRules.Text(m.PreparationNotes);
        if (id.HasValue && r.NotesRevision != m.NotesRevision)
            throw new ValidationException("Interview notes changed elsewhere. Copy your draft, reload and review before saving.");
        if (r.InterviewNotes != InputRules.Text(m.InterviewNotes))
            r.NotesRevision++;
        r.InterviewNotes = InputRules.Text(m.InterviewNotes);
        r.UpdatedAtUtc = now;
        if (!id.HasValue)
            db.Interviews.Add(r);
        db.InterviewParticipants.RemoveRange(links.Where(l => !m.ContactIds.Contains(l.ContactId)));
        db.InterviewParticipants.AddRange(m.ContactIds.Except(links.Select(l => l.ContactId)).Select(c => new InterviewParticipant { InterviewRound = r, ContactId = c }));
        if (!id.HasValue || old != r.Status)
            db.TimelineEvents.Add(new()
            {
                ApplicationId = r.ApplicationId,
                InterviewRound = r,
                EventDate = DateOnly.FromDateTime(now.ToLocalTime()),
                OccurredAtUtc = now,
                Kind = TimelineKind.Interview,
                IsSystem = true,
                Description = !id.HasValue ? $"Interview record created as {r.Status}: {r.Title}." : $"Interview record marked {r.Status} (previously {old}): {r.Title}. This records the change, not the time the interview happened.",
                CreatedAtUtc = now,
                UpdatedAtUtc = now
            });
        await db.SaveChangesAsync();
        return r.Id;
    }
    public async Task<bool> SetArchivedAsync(int id, bool archived)
    {
        await using var db = await factory.CreateDbContextAsync();
        var r = await db.Interviews.FindAsync(id);
        if (r is null)
            return false;
        if (r.IsArchived != archived)
        {
            r.IsArchived = archived;
            r.UpdatedAtUtc = DateTime.UtcNow;
            await db.SaveChangesAsync();
        }
        return true;
    }
}
public sealed record InterviewListItem(int Id, string Title, InterviewType Type, InterviewStatus Status, DateTime? ScheduledAtUtc, bool IsArchived);
