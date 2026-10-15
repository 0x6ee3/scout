using System.ComponentModel.DataAnnotations;
using Microsoft.EntityFrameworkCore;
using Scout.Web.Domain;
using Scout.Web.Features;
using Scout.Web.Features.Positions;
using Scout.Web.Tests.Data;

namespace Scout.Web.Tests.Positions;

public sealed class ArchiveTests
{
    [Fact]
    public async Task CompanyArchiveIsIndependentAndRoundTripsWithoutChangingChildren()
    {
        await using var db = await TemporaryDatabase.CreateAsync();
        var company = await db.Companies.CreateAsync(new()
        {
            Name = "Parent",
            PersonalNotes = "Private"
        });
        var position = await db.Positions.CreateAsync(new()
        {
            CompanyId = company,
            Title = "Child",
            Status = PositionStatus.Interested,
            JobDescriptionSnapshot = "Original\nBeschreibung"
        });
        var originalCompany = (await db.Companies.GetAsync(company))!;
        var originalPosition = (await db.Positions.GetAsync(position))!;
        Assert.True(await db.Companies.SetArchivedAsync(company, true));
        var archived = (await db.Companies.GetAsync(company))!;
        Assert.Equal(originalCompany with
        {
            IsArchived = true,
            UpdatedAtUtc = archived.UpdatedAtUtc
        }, archived);
        Assert.Equal(originalPosition with
        {
            CompanyIsArchived = true
        }, await db.Positions.GetAsync(position));
        Assert.Empty(await db.Companies.ListAsync());
        Assert.Single(await db.Companies.ListAsync(ArchiveFilter.Archived));
        Assert.Single(await db.Positions.ListAsync());
        Assert.True((await db.Positions.ListAsync())[0].CompanyIsArchived);
        Assert.True(await db.Companies.SetArchivedAsync(company, true));
        Assert.Equal(archived, await db.Companies.GetAsync(company));
        Assert.True(await db.Companies.SetArchivedAsync(company, false));
        Assert.True(await db.Companies.SetArchivedAsync(company, false));
        Assert.Equal(originalPosition, await db.Positions.GetAsync(position));
        Assert.Single(await db.Companies.ListAsync());
        Assert.Empty(await db.Companies.ListAsync(ArchiveFilter.Archived));
    }

    [Fact]
    public async Task PositionArchivePreservesAllValuesAndDoesNotChangeCompany()
    {
        await using var db = await TemporaryDatabase.CreateAsync();
        var company = await db.Companies.CreateAsync(new()
        {
            Name = "Parent"
        });
        var position = await db.Positions.CreateAsync(new()
        {
            CompanyId = company,
            Title = "Child",
            Status = PositionStatus.RejectedByUser,
            JobDescriptionSnapshot = "Snapshot",
            PersonalNotes = "Notes",
            ClosingOn = new(2026, 12, 1)
        });
        var original = (await db.Positions.GetAsync(position))!;
        var parent = await db.Companies.GetAsync(company);
        Assert.True(await db.Positions.SetArchivedAsync(position, true));
        var archived = (await db.Positions.GetAsync(position))!;
        Assert.Equal(original with
        {
            IsArchived = true,
            UpdatedAtUtc = archived.UpdatedAtUtc
        }, archived);
        Assert.Empty(await db.Positions.ListAsync());
        Assert.Single(await db.Positions.ListAsync(archive: ArchiveFilter.Archived));
        Assert.Single(await db.Positions.ListAsync(company, ArchiveFilter.All));
        Assert.True(await db.Positions.SetArchivedAsync(position, true));
        Assert.Equal(archived, await db.Positions.GetAsync(position));
        Assert.True(await db.Positions.SetArchivedAsync(position, false));
        var restored = (await db.Positions.GetAsync(position))!;
        Assert.Equal(original with
        {
            UpdatedAtUtc = restored.UpdatedAtUtc
        }, restored);
        Assert.True(await db.Positions.SetArchivedAsync(position, false));
        Assert.Equal(restored, await db.Positions.GetAsync(position));
        Assert.Equal(parent, await db.Companies.GetAsync(company));
        await using var context = await db.Factory.CreateDbContextAsync(TestContext.Current.CancellationToken);
        Assert.Equal(1, await context.Companies.CountAsync(TestContext.Current.CancellationToken));
        Assert.Equal(1, await context.Positions.CountAsync(TestContext.Current.CancellationToken));
        Assert.False(await db.Positions.SetArchivedAsync(999, true));
        Assert.False(await db.Companies.SetArchivedAsync(999, false));
    }

    [Fact]
    public async Task ArchivedCompaniesCannotReceiveNewPositionsButExistingPositionsStayEditable()
    {
        await using var db = await TemporaryDatabase.CreateAsync();
        var active = await db.Companies.CreateAsync(new()
        {
            Name = "Active"
        });
        var archived = await db.Companies.CreateAsync(new()
        {
            Name = "Archived"
        });
        var otherArchived = await db.Companies.CreateAsync(new()
        {
            Name = "Other archived"
        });
        var position = await db.Positions.CreateAsync(new()
        {
            CompanyId = archived,
            Title = "Existing"
        });
        await db.Companies.SetArchivedAsync(archived, true);
        await db.Companies.SetArchivedAsync(otherArchived, true);
        Assert.Equal(active, Assert.Single(await db.Positions.CompanyOptionsAsync()).Id);
        Assert.Equal(2, (await db.Positions.CompanyOptionsAsync(archived)).Count);
        await Assert.ThrowsAsync<ValidationException>(() => db.Positions.CreateAsync(new() { CompanyId = archived, Title = "Blocked" }));
        Assert.True(await db.Positions.UpdateAsync(position, new()
        {
            CompanyId = archived,
            Title = "Edited",
            PersonalNotes = "Still editable"
        }));
        var saved = await db.Positions.GetAsync(position);
        await Assert.ThrowsAsync<ValidationException>(() => db.Positions.UpdateAsync(position, new() { CompanyId = otherArchived, Title = "Blocked" }));
        Assert.Equal(saved, await db.Positions.GetAsync(position));
        Assert.True(await db.Positions.UpdateAsync(position, new()
        {
            CompanyId = active,
            Title = "Reassigned"
        }));
        Assert.Equal(active, (await db.Positions.GetAsync(position))!.CompanyId);
        Assert.Equal(3, (await db.Companies.ListAsync(ArchiveFilter.All)).Count);
    }
}
