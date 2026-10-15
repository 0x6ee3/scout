using System.ComponentModel.DataAnnotations;
using Microsoft.EntityFrameworkCore;
using Scout.Web.Domain;
using Scout.Web.Features.Companies;
using Scout.Web.Features.Positions;
using Scout.Web.Tests.Data;

namespace Scout.Web.Tests.Positions;

public sealed class PositionPersistenceTests
{
    [Fact]
    public async Task MultiplePositionsRoundTripWithDefaultsWithoutChangingCompany()
    {
        await using var db = await TemporaryDatabase.CreateAsync();
        var companyId = await db.Companies.CreateAsync(new()
        {
            Name = "München & Söhne"
        });
        var originalCompany = await db.Companies.GetAsync(companyId);
        var draft = new PositionEditModel { CompanyId = companyId, Title = " Entwickler " };
        var first = await db.Positions.CreateAsync(draft);
        var second = await db.Positions.CreateAsync(new()
        {
            CompanyId = companyId,
            Title = "Architect"
        });
        var saved = Assert.IsType<PositionDetails>(await db.Positions.GetAsync(first));
        Assert.Equal("Entwickler", saved.Title);
        Assert.Equal(PositionStatus.Discovered, saved.Status);
        Assert.Equal(draft.DiscoveredOn, saved.DiscoveredOn);
        Assert.Null(saved.JobDescriptionSnapshot);
        Assert.Null(saved.DescriptionCapturedAtUtc);
        Assert.Equal(DateTimeKind.Utc, saved.CreatedAtUtc.Kind);
        Assert.Equal(saved.CreatedAtUtc, saved.UpdatedAtUtc);
        Assert.Equal(originalCompany, await db.Companies.GetAsync(companyId));
        Assert.Equal(2, (await db.Positions.ListAsync(companyId)).Count);
        Assert.NotEqual(first, second);
        await using var context = await db.Factory.CreateDbContextAsync(TestContext.Current.CancellationToken);
        Assert.Single(await context.Companies.ToListAsync(TestContext.Current.CancellationToken));
        var status = await context.Database.SqlQuery<string>($"SELECT Status AS Value FROM Positions WHERE Id = {first}").SingleAsync(TestContext.Current.CancellationToken);
        Assert.Equal("Discovered", status);
    }

    [Fact]
    public async Task SnapshotCanBeFilledOnceAndSurvivesOtherEditsAndCompanyReassignment()
    {
        await using var db = await TemporaryDatabase.CreateAsync();
        var firstCompany = await db.Companies.CreateAsync(new()
        {
            Name = "First"
        });
        var secondCompany = await db.Companies.CreateAsync(new()
        {
            Name = "Second"
        });
        var id = await db.Positions.CreateAsync(new()
        {
            CompanyId = firstCompany,
            Title = "Original"
        });
        var created = (await db.Positions.GetAsync(id))!.CreatedAtUtc;
        var draft = new PositionEditModel
        {
            CompanyId = firstCompany,
            Title = "Entwickler",
            JobDescriptionSnapshot = "  Wir suchen:\r\nC# & .NET\n<b>Original</b>  ",
            OriginalVacancyUrl = " https://example.com/jobs ",
            Location = " München ",
            EmploymentType = " Vollzeit ",
            RemoteWorkNotes = "2 Tage\nRemote",
            PersonalNotes = "Meine Notizen\nSecond line",
            Status = PositionStatus.Interested,
            DiscoveredOn = new DateOnly(2025, 12, 31),
            ClosingOn = new DateOnly(2026, 1, 15)
        };
        Assert.True(await db.Positions.UpdateAsync(id, draft));
        var captured = (await db.Positions.GetAsync(id))!;
        Assert.Equal(draft.JobDescriptionSnapshot, captured.JobDescriptionSnapshot);
        Assert.NotNull(captured.DescriptionCapturedAtUtc);
        Assert.Equal(DateTimeKind.Utc, captured.DescriptionCapturedAtUtc.Value.Kind);
        Assert.Equal(draft.DiscoveredOn, captured.DiscoveredOn);
        Assert.Equal(draft.ClosingOn, captured.ClosingOn);
        Assert.Equal("https://example.com/jobs", captured.OriginalVacancyUrl);
        Assert.Equal("München", captured.Location);
        Assert.Equal("Vollzeit", captured.EmploymentType);
        Assert.Equal(draft.RemoteWorkNotes, captured.RemoteWorkNotes);
        Assert.Equal(draft.PersonalNotes, captured.PersonalNotes);
        draft.CompanyId = secondCompany;
        draft.Title = "Changed";
        draft.JobDescriptionSnapshot = "Overwrite attempt";
        draft.PersonalNotes = "Corrections";
        draft.Status = PositionStatus.Expired;
        Assert.True(await db.Positions.UpdateAsync(id, draft));
        var saved = (await db.Positions.GetAsync(id))!;
        Assert.Equal(captured.JobDescriptionSnapshot, saved.JobDescriptionSnapshot);
        Assert.Equal(captured.DescriptionCapturedAtUtc, saved.DescriptionCapturedAtUtc);
        Assert.Equal(created, saved.CreatedAtUtc);
        Assert.Equal(secondCompany, saved.CompanyId);
        Assert.Equal("Corrections", saved.PersonalNotes);
        Assert.Equal(PositionStatus.Expired, saved.Status);
        draft.JobDescriptionSnapshot = null;
        Assert.True(await db.Positions.UpdateAsync(id, draft));
        Assert.Equal(captured.JobDescriptionSnapshot, (await db.Positions.GetAsync(id))!.JobDescriptionSnapshot);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(99999)]
    public async Task MissingCompanyCannotBeUsed(int companyId)
    {
        await using var db = await TemporaryDatabase.CreateAsync();
        await Assert.ThrowsAsync<ValidationException>(() => db.Positions.CreateAsync(new() { CompanyId = companyId, Title = "No orphan" }));
        Assert.Empty(await db.Positions.ListAsync());
    }

    [Fact]
    public async Task DatabaseRejectsOrphanAndDeletingReferencedCompany()
    {
        await using var db = await TemporaryDatabase.CreateAsync();
        var companyId = await db.Companies.CreateAsync(new()
        {
            Name = "Parent"
        });
        await db.Positions.CreateAsync(new()
        {
            CompanyId = companyId,
            Title = "Child"
        });
        await using var context = await db.Factory.CreateDbContextAsync(TestContext.Current.CancellationToken);
        context.Positions.Add(new Position { CompanyId = 99999, Title = "Orphan" });
        await Assert.ThrowsAsync<DbUpdateException>(() => context.SaveChangesAsync(TestContext.Current.CancellationToken));
        context.ChangeTracker.Clear();
        var company = await context.Companies.SingleAsync(TestContext.Current.CancellationToken);
        context.Remove(company);
        await Assert.ThrowsAsync<DbUpdateException>(() => context.SaveChangesAsync(TestContext.Current.CancellationToken));
        Assert.Single(await db.Positions.ListAsync());
    }

    public static TheoryData<string, object> InvalidDrafts => new()
    {
        { nameof(PositionEditModel.Title), " " }, { nameof(PositionEditModel.Title), new string('x', 201) },
        { nameof(PositionEditModel.Status), (PositionStatus)99 }, { nameof(PositionEditModel.OriginalVacancyUrl), "javascript:alert(1)" },
        { nameof(PositionEditModel.OriginalVacancyUrl), new string('x', 2049) }, { nameof(PositionEditModel.Location), new string('x', 301) },
        { nameof(PositionEditModel.EmploymentType), new string('x', 101) }, { nameof(PositionEditModel.RemoteWorkNotes), new string('x', 100001) },
        { nameof(PositionEditModel.PersonalNotes), new string('x', 100001) }, { nameof(PositionEditModel.JobDescriptionSnapshot), new string('x', 100001) }
    };
    [Theory, MemberData(nameof(InvalidDrafts))]
    public async Task InvalidCreateAndUpdatePreserveSavedData(string field, object value)
    {
        await using var db = await TemporaryDatabase.CreateAsync();
        var companyId = await db.Companies.CreateAsync(new()
        {
            Name = "Parent"
        });
        var id = await db.Positions.CreateAsync(new()
        {
            CompanyId = companyId,
            Title = "Saved",
            JobDescriptionSnapshot = "Original"
        });
        var original = await db.Positions.GetAsync(id);
        var draft = new PositionEditModel { CompanyId = companyId, Title = "Unsaved" };
        typeof(PositionEditModel).GetProperty(field)!.SetValue(draft, value);
        await Assert.ThrowsAsync<ValidationException>(() => db.Positions.CreateAsync(draft));
        await Assert.ThrowsAsync<ValidationException>(() => db.Positions.UpdateAsync(id, draft));
        Assert.Equal(original, await db.Positions.GetAsync(id));
        Assert.Single(await db.Positions.ListAsync());
    }

    [Fact]
    public async Task MissingPositionDoesNotInsertAndBlankDescriptionCanBeAddedLater()
    {
        await using var db = await TemporaryDatabase.CreateAsync();
        var companyId = await db.Companies.CreateAsync(new()
        {
            Name = "Parent"
        });
        Assert.Null(await db.Positions.GetAsync(999));
        Assert.False(await db.Positions.UpdateAsync(999, new()
        {
            CompanyId = companyId,
            Title = "No insert"
        }));
        var id = await db.Positions.CreateAsync(new()
        {
            CompanyId = companyId,
            Title = "Saved",
            JobDescriptionSnapshot = "  \n"
        });
        Assert.Null((await db.Positions.GetAsync(id))!.DescriptionCapturedAtUtc);
        Assert.True(await db.Positions.UpdateAsync(id, new()
        {
            CompanyId = companyId,
            Title = "Saved",
            JobDescriptionSnapshot = "First snapshot"
        }));
        Assert.Equal("First snapshot", (await db.Positions.GetAsync(id))!.JobDescriptionSnapshot);
    }
}
