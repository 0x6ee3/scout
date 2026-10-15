using System.ComponentModel.DataAnnotations;
using Microsoft.EntityFrameworkCore;
using Scout.Web.Domain;
using Scout.Web.Features;
using Scout.Web.Features.Applications;
using Scout.Web.Features.Contacts;
using Scout.Web.Features.Interviews;
using Scout.Web.Features.NextActions;
using Scout.Web.Tests.Data;
namespace Scout.Web.Tests.Interviews;

public sealed class ActionsAndInterviewsTests
{
    [Fact]
    public async Task ActionsKeepOwnerDueDateAndRemainVisibleWithArchivedParents()
    {
        await using var db = await TemporaryDatabase.CreateAsync();
        var c = await db.Companies.CreateAsync(new()
        {
            Name = "Employer"
        });
        var p = await db.Positions.CreateAsync(new()
        {
            CompanyId = c,
            Title = "Engineer"
        });
        var apps = new ApplicationOperations(db.Factory);
        var app = await apps.CreateAsync(new()
        {
            PositionId = p
        });
        var other = await apps.CreateAsync(new()
        {
            PositionId = p
        });
        var actions = new NextActionOperations(db.Factory);
        var interviews = new InterviewOperations(db.Factory);
        var round = await interviews.SaveAsync(null, new()
        {
            ApplicationId = other
        });
        await Assert.ThrowsAsync<ValidationException>(() => actions.SaveAsync(null, new() { Title = "Wrong scope", ApplicationId = app, InterviewRoundId = round }));
        await Assert.ThrowsAsync<ValidationException>(() => actions.SaveAsync(null, new() { Title = "Two owners", ApplicationId = app, PositionId = p }));
        var date = DateOnly.FromDateTime(DateTime.Today).AddDays(-1);
        var id = await actions.SaveAsync(null, new()
        {
            Title = "Prepare",
            PositionId = p,
            DueOn = date
        });
        var undated = await actions.SaveAsync(null, new()
        {
            Title = "Follow up",
            ApplicationId = app
        });
        Assert.Equal(new[] { id, undated }, (await actions.ListAsync()).Select(x => x.Id));
        await actions.SetCompletedAsync(id, true);
        var completed = (await actions.GetAsync(id))!.CompletedAtUtc;
        await actions.SetCompletedAsync(id, true);
        Assert.Equal(completed, (await actions.GetAsync(id))!.CompletedAtUtc);
        await actions.SetCompletedAsync(id, false);
        Assert.Null((await actions.GetAsync(id))!.CompletedAtUtc);
        Assert.Equal(date, (await actions.GetAsync(id))!.DueOn);
        await Assert.ThrowsAsync<ValidationException>(() => actions.SaveAsync(id, new() { Title = "Move", ApplicationId = app }));
        await apps.SetArchivedAsync(app, true);
        await db.Positions.SetArchivedAsync(p, true);
        Assert.All(await actions.ListAsync(), x => Assert.True(x.ParentIsArchived));
        Assert.Single(await actions.ListAsync(due: DueFilter.Overdue));
        await using var ctx = await db.Factory.CreateDbContextAsync(TestContext.Current.CancellationToken);
        ctx.NextActions.Add(new()
        {
            Title = "Invalid",
            CreatedAtUtc = DateTime.UtcNow,
            UpdatedAtUtc = DateTime.UtcNow
        });
        await Assert.ThrowsAsync<DbUpdateException>(() => ctx.SaveChangesAsync(TestContext.Current.CancellationToken));
    }
    [Fact]
    public async Task NotesOnlyHistoricalCompletionAndLifecycleHistoryAreAtomic()
    {
        await using var db = await TemporaryDatabase.CreateAsync();
        var c = await db.Companies.CreateAsync(new()
        {
            Name = "Employer"
        });
        var p = await db.Positions.CreateAsync(new()
        {
            CompanyId = c,
            Title = "Engineer"
        });
        var apps = new ApplicationOperations(db.Factory);
        var a = await apps.CreateAsync(new()
        {
            PositionId = p
        });
        var ops = new InterviewOperations(db.Factory);
        await Assert.ThrowsAsync<ValidationException>(() => ops.SaveAsync(null, new() { ApplicationId = a, Status = InterviewStatus.Scheduled }));
        var id = await ops.SaveAsync(null, new()
        {
            ApplicationId = a,
            Status = InterviewStatus.Completed,
            InterviewNotes = "Notizen\nEnglish"
        });
        Assert.Null((await ops.GetAsync(id))!.CompletedAtUtc);
        Assert.Empty(await ops.SelectionAsync(id));
        await ops.SaveAsync(id, new()
        {
            NotesRevision = 1,
            ApplicationId = a,
            Status = InterviewStatus.Completed,
            InterviewNotes = "Notizen\nEnglish"
        });
        await using var ctx = await db.Factory.CreateDbContextAsync(TestContext.Current.CancellationToken);
        Assert.Single(await ctx.TimelineEvents.Where(e => e.InterviewRoundId == id).ToListAsync(TestContext.Current.CancellationToken));
        await ctx.Database.ExecuteSqlRawAsync("CREATE TRIGGER RejectLifecycle BEFORE INSERT ON TimelineEvents BEGIN SELECT RAISE(ABORT, 'Test'); END;", TestContext.Current.CancellationToken);
        await Assert.ThrowsAsync<DbUpdateException>(() => ops.SaveAsync(id, new() { NotesRevision = 1, ApplicationId = a, Status = InterviewStatus.Cancelled, InterviewNotes = "Must roll back" }));
        Assert.Equal("Notizen\nEnglish", (await ops.GetAsync(id))!.InterviewNotes);
        Assert.Equal(InterviewStatus.Completed, (await ops.GetAsync(id))!.Status);
        Assert.Equal(ApplicationStatus.Planned, (await apps.GetAsync(a))!.Status);
    }
}
