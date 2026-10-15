using System.ComponentModel.DataAnnotations;
using Microsoft.EntityFrameworkCore;
using Scout.Web.Domain;
using Scout.Web.Features;
using Scout.Web.Features.Applications;
using Scout.Web.Tests.Data;

namespace Scout.Web.Tests.Applications;

public sealed class ApplicationTests
{
    [Fact]
    public async Task AttemptsStatusHistoryAndArchiveAreIndependent()
    {
        await using var db = await TemporaryDatabase.CreateAsync();
        var company = await db.Companies.CreateAsync(new()
        {
            Name = "München"
        });
        var position = await db.Positions.CreateAsync(new()
        {
            CompanyId = company,
            Title = "Engineer",
            JobDescriptionSnapshot = "Original"
        });
        var ops = new ApplicationOperations(db.Factory);
        var id = await ops.CreateAsync(new()
        {
            PositionId = position
        });
        await ops.CreateAsync(new()
        {
            PositionId = position,
            AttemptLabel = "Retry"
        });
        var original = (await ops.GetAsync(id))!;
        Assert.Null(original.AppliedOn);
        Assert.True(await ops.UpdateAsync(id, new()
        {
            PositionId = position,
            Status = ApplicationStatus.Accepted,
            Notes = "Notizen\nEnglish"
        }));
        Assert.True(await ops.UpdateAsync(id, new()
        {
            PositionId = position,
            Status = ApplicationStatus.Accepted,
            Notes = "Notizen\nEnglish"
        }));
        await using var context = await db.Factory.CreateDbContextAsync(TestContext.Current.CancellationToken);
        var events = await context.TimelineEvents.Where(e => e.ApplicationId == id).ToListAsync(TestContext.Current.CancellationToken);
        Assert.Equal(2, events.Count);
        var change = Assert.Single(events, e => e.Kind == TimelineKind.StatusChanged);
        Assert.Equal(ApplicationStatus.Planned, change.PreviousStatus);
        Assert.Equal(ApplicationStatus.Accepted, change.NewStatus);
        Assert.True(change.IsSystem);
        Assert.Equal(original.CreatedAtUtc, (await ops.GetAsync(id))!.CreatedAtUtc);
        await ops.SetArchivedAsync(id, true);
        var archived = (await ops.GetAsync(id))!;
        await ops.SetArchivedAsync(id, true);
        Assert.Equal(archived.UpdatedAtUtc, (await ops.GetAsync(id))!.UpdatedAtUtc);
        Assert.Equal(id, Assert.Single(await ops.ListAsync(archive: ArchiveFilter.Archived, search: "MÜNCHEN")).Id);
        Assert.Single(await ops.ListAsync());
        Assert.Equal("Original", (await db.Positions.GetAsync(position))!.JobDescriptionSnapshot);
        Assert.False(await ops.UpdateAsync(999, new()
        {
            PositionId = position
        }));
    }

    [Fact]
    public async Task InvalidOwnersAndStatusAreRejectedAndPositionOwnershipIsProtected()
    {
        await using var db = await TemporaryDatabase.CreateAsync();
        var company = await db.Companies.CreateAsync(new()
        {
            Name = "First"
        });
        var other = await db.Companies.CreateAsync(new()
        {
            Name = "Other"
        });
        var position = await db.Positions.CreateAsync(new()
        {
            CompanyId = company,
            Title = "Saved"
        });
        var ops = new ApplicationOperations(db.Factory);
        await Assert.ThrowsAsync<ValidationException>(() => ops.CreateAsync(new() { PositionId = 999 }));
        await Assert.ThrowsAsync<ValidationException>(() => ops.CreateAsync(new() { PositionId = position, Status = (ApplicationStatus)99 }));
        var id = await ops.CreateAsync(new()
        {
            PositionId = position
        });
        await Assert.ThrowsAsync<ValidationException>(() => ops.UpdateAsync(id, new() { PositionId = 999 }));
        await Assert.ThrowsAsync<ValidationException>(() => db.Positions.UpdateAsync(position, new() { CompanyId = other, Title = "Moved" }));
        await db.Companies.SetArchivedAsync(company, true);
        await Assert.ThrowsAsync<ValidationException>(() => ops.CreateAsync(new() { PositionId = position }));
        Assert.True(await ops.UpdateAsync(id, new()
        {
            PositionId = position,
            Status = ApplicationStatus.Withdrawn
        }));
        Assert.Equal(company, (await db.Positions.GetAsync(position))!.CompanyId);
    }

    [Fact]
    public async Task FailedTimelineInsertRollsBackApplicationStatus()
    {
        await using var db = await TemporaryDatabase.CreateAsync();
        var c = await db.Companies.CreateAsync(new()
        {
            Name = "Parent"
        });
        var p = await db.Positions.CreateAsync(new()
        {
            CompanyId = c,
            Title = "Position"
        });
        var ops = new ApplicationOperations(db.Factory);
        var id = await ops.CreateAsync(new()
        {
            PositionId = p
        });
        var original = (await ops.GetAsync(id))!;
        await using var context = await db.Factory.CreateDbContextAsync(TestContext.Current.CancellationToken);
        await context.Database.ExecuteSqlRawAsync("CREATE TRIGGER RejectEvent BEFORE INSERT ON TimelineEvents BEGIN SELECT RAISE(ABORT, 'Test'); END;", TestContext.Current.CancellationToken);
        await Assert.ThrowsAsync<DbUpdateException>(() => ops.UpdateAsync(id, new() { PositionId = p, Status = ApplicationStatus.Applied }));
        var after = (await ops.GetAsync(id))!;
        Assert.Equal(original.Status, after.Status);
        Assert.Equal(original.UpdatedAtUtc, after.UpdatedAtUtc);
    }
}
