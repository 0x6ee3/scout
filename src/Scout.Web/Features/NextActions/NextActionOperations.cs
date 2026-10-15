using System.ComponentModel.DataAnnotations;
using Microsoft.EntityFrameworkCore;
using Scout.Web.Data;
using Scout.Web.Domain;
namespace Scout.Web.Features.NextActions;

public sealed class NextActionEditModel
{
    public int? ApplicationId
    {
        get; set;
    }
    public int? PositionId
    {
        get; set;
    }
    public int? InterviewRoundId
    {
        get; set;
    }
    public int? TimelineEventId
    {
        get; set;
    }
    [Required, StringLength(200)] public string Title { get; set; } = "";
    [StringLength(100000)]
    public string? Description
    {
        get; set;
    }
    public DateOnly? DueOn
    {
        get; set;
    }
}
public enum DueFilter
{
    Any, Overdue, Today, Upcoming, Undated
}
public sealed class NextActionOperations(IDbContextFactory<ScoutDbContext> factory)
{
    public async Task<IReadOnlyList<NextActionListItem>> ListAsync(int? applicationId = null, int? positionId = null, int? companyId = null, NextActionStatus? status = NextActionStatus.Open, DueFilter due = DueFilter.Any, ArchiveFilter archive = ArchiveFilter.Active, string? search = null)
    {
        await using var db = await factory.CreateDbContextAsync();
        var q = db.NextActions.AsNoTracking();
        if (applicationId.HasValue)
            q = q.Where(a => a.ApplicationId == applicationId);
        if (positionId.HasValue)
            q = q.Where(a => a.PositionId == positionId);
        if (companyId.HasValue)
            q = q.Where(a => a.ApplicationId.HasValue ? a.Application!.Position.CompanyId == companyId : a.Position!.CompanyId == companyId);
        if (status.HasValue)
            q = q.Where(a => a.Status == status);
        if (archive != ArchiveFilter.All)
            q = q.Where(a => a.IsArchived == (archive == ArchiveFilter.Archived));
        var today = DateOnly.FromDateTime(DateTime.Today);
        q = due switch
        {
            DueFilter.Overdue => q.Where(a => a.DueOn < today),
            DueFilter.Today => q.Where(a => a.DueOn == today),
            DueFilter.Upcoming => q.Where(a => a.DueOn > today),
            DueFilter.Undated => q.Where(a => a.DueOn == null),
            _ => q
        };
        var rows = await q.Select(a => new NextActionListItem(a.Id, a.Title, a.DueOn, a.Status, a.IsArchived, a.ApplicationId, a.PositionId, a.ApplicationId.HasValue ? a.Application!.Position.Title : a.Position!.Title, a.ApplicationId.HasValue ? a.Application!.Position.Company.Name : a.Position!.Company.Name, a.ApplicationId.HasValue ? a.Application!.IsArchived || a.Application.Position.IsArchived || a.Application.Position.Company.IsArchived : a.Position!.IsArchived || a.Position.Company.IsArchived)).ToListAsync();
        return rows.Where(a => string.IsNullOrWhiteSpace(search) || $"{a.Title} {a.PositionTitle} {a.CompanyName}".Contains(search.Trim(), StringComparison.OrdinalIgnoreCase)).OrderBy(a => a.DueOn is null).ThenBy(a => a.DueOn).ThenBy(a => a.Id).ToList();
    }
    public async Task<NextAction?> GetAsync(int id)
    {
        await using var db = await factory.CreateDbContextAsync();
        return await db.NextActions.AsNoTracking().SingleOrDefaultAsync(a => a.Id == id);
    }
    public async Task<int> SaveAsync(int? id, NextActionEditModel m)
    {
        InputRules.Validate(m);
        if (string.IsNullOrWhiteSpace(m.Title))
            throw new ValidationException("Enter a title.");
        if (m.ApplicationId.HasValue == m.PositionId.HasValue)
            throw new ValidationException("Choose exactly one application or position owner.");
        if (m.InterviewRoundId.HasValue && m.TimelineEventId.HasValue || m.PositionId.HasValue && (m.InterviewRoundId.HasValue || m.TimelineEventId.HasValue))
            throw new ValidationException("Choose at most one interview or event within the application.");
        await using var db = await factory.CreateDbContextAsync();
        var a = id.HasValue ? await db.NextActions.FindAsync(id.Value) ?? throw new ValidationException("Action no longer exists.") : new NextAction { ApplicationId = m.ApplicationId, PositionId = m.PositionId, CreatedAtUtc = DateTime.UtcNow };
        if (a.ApplicationId != m.ApplicationId || a.PositionId != m.PositionId)
            throw new ValidationException("Action ownership cannot change.");
        if (m.ApplicationId.HasValue)
        {
            var app = await db.Applications.FindAsync(m.ApplicationId.Value);
            if (app is null || !id.HasValue && app.IsArchived)
                throw new ValidationException("Choose an active application.");
        }
        else
        {
            var pos = await db.Positions.FindAsync(m.PositionId!.Value);
            if (pos is null || !id.HasValue && pos.IsArchived)
                throw new ValidationException("Choose an active position.");
        }
        if (m.InterviewRoundId.HasValue && !await db.Interviews.AnyAsync(r => r.Id == m.InterviewRoundId && r.ApplicationId == m.ApplicationId && (!r.IsArchived || r.Id == a.InterviewRoundId)))
            throw new ValidationException("Choose an interview of this application.");
        if (m.TimelineEventId.HasValue && !await db.TimelineEvents.AnyAsync(e => e.Id == m.TimelineEventId && e.ApplicationId == m.ApplicationId && (!e.IsArchived || e.Id == a.TimelineEventId)))
            throw new ValidationException("Choose an event of this application.");
        a.Title = m.Title.Trim();
        a.Description = InputRules.Text(m.Description);
        a.DueOn = m.DueOn;
        a.InterviewRoundId = m.InterviewRoundId;
        a.TimelineEventId = m.TimelineEventId;
        a.UpdatedAtUtc = DateTime.UtcNow;
        if (!id.HasValue)
            db.NextActions.Add(a);
        await db.SaveChangesAsync();
        return a.Id;
    }
    public async Task<bool> SetCompletedAsync(int id, bool completed)
    {
        await using var db = await factory.CreateDbContextAsync();
        var a = await db.NextActions.FindAsync(id);
        if (a is null)
            return false;
        var status = completed ? NextActionStatus.Completed : NextActionStatus.Open;
        if (a.Status != status)
        {
            a.Status = status;
            a.CompletedAtUtc = completed ? DateTime.UtcNow : null;
            a.UpdatedAtUtc = DateTime.UtcNow;
            await db.SaveChangesAsync();
        }
        return true;
    }
    public async Task<bool> SetArchivedAsync(int id, bool archived)
    {
        await using var db = await factory.CreateDbContextAsync();
        var a = await db.NextActions.FindAsync(id);
        if (a is null)
            return false;
        if (a.IsArchived != archived)
        {
            a.IsArchived = archived;
            a.UpdatedAtUtc = DateTime.UtcNow;
            await db.SaveChangesAsync();
        }
        return true;
    }
}
public sealed record NextActionListItem(int Id, string Title, DateOnly? DueOn, NextActionStatus Status, bool IsArchived, int? ApplicationId, int? PositionId, string PositionTitle, string CompanyName, bool ParentIsArchived);
