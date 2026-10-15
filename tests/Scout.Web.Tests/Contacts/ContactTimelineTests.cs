using System.ComponentModel.DataAnnotations;
using Microsoft.EntityFrameworkCore;
using Scout.Web.Domain;
using Scout.Web.Features;
using Scout.Web.Features.Applications;
using Scout.Web.Features.Contacts;
using Scout.Web.Features.Timeline;
using Scout.Web.Tests.Data;
namespace Scout.Web.Tests.Contacts;

public sealed class ContactTimelineTests
{
    [Fact]
    public async Task ContactAndEventOwnershipArchiveAndChronology()
    {
        await using var db = await TemporaryDatabase.CreateAsync();
        var c = await db.Companies.CreateAsync(new()
        {
            Name = "Employer"
        });
        var other = await db.Companies.CreateAsync(new()
        {
            Name = "Other"
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
        var contacts = new ContactOperations(db.Factory);
        var timeline = new TimelineOperations(db.Factory);
        var first = await contacts.SaveAsync(null, new()
        {
            CompanyId = c,
            Name = "Müller",
            Email = "first@example.test"
        });
        var second = await contacts.SaveAsync(null, new()
        {
            CompanyId = c,
            Name = "Second"
        });
        var foreign = await contacts.SaveAsync(null, new()
        {
            CompanyId = other,
            Name = "Foreign"
        });
        await Assert.ThrowsAsync<ValidationException>(() => contacts.SaveAsync(first, new() { CompanyId = other, Name = "Moved" }));
        await Assert.ThrowsAsync<ValidationException>(() => contacts.SaveApplicationContactsAsync(a, [first, foreign]));
        Assert.Empty(await contacts.ApplicationSelectionAsync(a));
        await contacts.SaveApplicationContactsAsync(a, [first]);
        var date = new DateOnly(2026, 8, 1);
        var e = await timeline.SaveAsync(null, new()
        {
            ApplicationId = a,
            EventDate = date,
            Kind = TimelineKind.Rejection,
            Description = "Historisch\nEnglish <b>plain text</b>",
            ContactIds = [first, second]
        });
        await contacts.SetArchivedAsync(first, true);
        await contacts.SaveApplicationContactsAsync(a, [first]);
        await Assert.ThrowsAsync<ValidationException>(() => timeline.SaveAsync(null, new() { ApplicationId = a, Description = "New", ContactIds = [first] }));
        await timeline.SaveAsync(e, new()
        {
            ApplicationId = a,
            EventDate = date,
            Description = "Corrected",
            ContactIds = [first, second]
        });
        Assert.Null((await timeline.GetAsync(e))!.OccurredAtUtc);
        Assert.Equal(ApplicationStatus.Planned, (await apps.GetAsync(a))!.Status);
        Assert.Equal(e, (await timeline.ListAsync(a))[0].Id);
        Assert.Equal(2, (await contacts.RelatedAsync(a)).Count);
        Assert.Contains("Application", (await contacts.RelatedAsync(a)).Single(x => x.Id == first).Origin);
        await timeline.SetArchivedAsync(e, true);
        Assert.Single(await timeline.ListAsync(a, ArchiveFilter.Archived));
        var system = (await timeline.ListAsync(a)).Single();
        await Assert.ThrowsAsync<ValidationException>(() => timeline.SetArchivedAsync(system.Id, true));
        await Assert.ThrowsAsync<ValidationException>(() => timeline.SaveAsync(system.Id, new() { ApplicationId = a, Description = "Rewrite" }));
        Assert.Single(await contacts.ListAsync(c, ArchiveFilter.All, "MÜLLER"));
    }

    [Fact]
    public void TimeConversionRejectsMissingAndDisambiguatesRepeatedTimes()
    {
        var zone = TimeZoneInfo.FindSystemTimeZoneById("Europe/Berlin");
        Assert.Throws<ValidationException>(() => LocalTime.ToUtc(new DateTime(2026, 3, 29, 2, 30, 0), zone: zone));
        var repeated = new DateTime(2026, 10, 25, 2, 30, 0);
        Assert.Throws<ValidationException>(() => LocalTime.ToUtc(repeated, zone: zone));
        Assert.Equal(TimeSpan.FromHours(1), LocalTime.ToUtc(repeated, 60, zone) - LocalTime.ToUtc(repeated, 120, zone));
        Assert.Null(LocalTime.ToUtc(null, zone: zone));
    }

    [Fact]
    public async Task ContactLinkFailureRollsBackTheEvent()
    {
        await using var db = await TemporaryDatabase.CreateAsync();
        var c = await db.Companies.CreateAsync(new()
        {
            Name = "Company"
        });
        var p = await db.Positions.CreateAsync(new()
        {
            CompanyId = c,
            Title = "Position"
        });
        var a = await new ApplicationOperations(db.Factory).CreateAsync(new()
        {
            PositionId = p
        });
        var contact = await new ContactOperations(db.Factory).SaveAsync(null, new()
        {
            CompanyId = c,
            Name = "Contact"
        });
        await using var context = await db.Factory.CreateDbContextAsync(TestContext.Current.CancellationToken);
        await context.Database.ExecuteSqlRawAsync("CREATE TRIGGER RejectContact BEFORE INSERT ON TimelineEventContacts BEGIN SELECT RAISE(ABORT, 'Test'); END;", TestContext.Current.CancellationToken);
        await Assert.ThrowsAsync<DbUpdateException>(() => new TimelineOperations(db.Factory).SaveAsync(null, new() { ApplicationId = a, Description = "Must roll back", ContactIds = [contact] }));
        Assert.Single(await context.TimelineEvents.ToListAsync(TestContext.Current.CancellationToken));
    }
}
