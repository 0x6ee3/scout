using Microsoft.EntityFrameworkCore;
using Scout.Web.Data;
using Scout.Web.Domain;
using Scout.Web.Features;

namespace Scout.Web.Tests.Data;

public sealed class DevelopmentSamplesTests
{
    [Fact]
    public async Task EmptyDevelopmentDatabaseSeedsOnceAndPreservesSubsequentEdits()
    {
        await using var db = await TemporaryDatabase.CreateAsync();
        await using (var context = await db.Factory.CreateDbContextAsync(TestContext.Current.CancellationToken))
            Assert.True(await DevelopmentSamples.SeedAsync(context, isDevelopment: true, enabled: true, new Scout.Web.Storage.DocumentFileStore(db.Paths)));
        var companies = await db.Companies.ListAsync(ArchiveFilter.All);
        var positions = await db.Positions.ListAsync(archive: ArchiveFilter.All);
        Assert.Equal(2, companies.Count);
        Assert.Equal(4, positions.Count);
        Assert.Single(companies, c => c.IsArchived);
        Assert.Single(positions, p => p.IsArchived);
        Assert.Equal(4, positions.Select(p => p.Status).Distinct().Count());
        Assert.Contains(positions, p => p.CompanyIsArchived && !p.IsArchived);
        foreach (var item in positions)
        {
            var detail = (await db.Positions.GetAsync(item.Id))!;
            Assert.Contains("\nWir suchen", detail.JobDescriptionSnapshot!);
            Assert.NotNull(detail.DescriptionCapturedAtUtc);
            Assert.EndsWith(".example/careers", detail.OriginalVacancyUrl!);
        }
        var company = companies[0];
        await db.Companies.UpdateAsync(company.Id, new()
        {
            Name = "Edited sample",
            PersonalNotes = "Keep this"
        });
        var edited = await db.Companies.GetAsync(company.Id);
        var positionsAfterEdit = await db.Positions.ListAsync(archive: ArchiveFilter.All);
        await using (var context = await db.Factory.CreateDbContextAsync(TestContext.Current.CancellationToken))
        {
            await context.Database.MigrateAsync(TestContext.Current.CancellationToken);
            Assert.False(await DevelopmentSamples.SeedAsync(context, true, true, new Scout.Web.Storage.DocumentFileStore(db.Paths)));
        }
        Assert.Equal(edited, await db.Companies.GetAsync(company.Id));
        Assert.Equal(positionsAfterEdit, await db.Positions.ListAsync(archive: ArchiveFilter.All));
    }

    [Theory]
    [InlineData(true, false)]
    [InlineData(false, true)]
    [InlineData(false, false)]
    public async Task DisabledOrNonDevelopmentNeverSeeds(bool development, bool enabled)
    {
        await using var db = await TemporaryDatabase.CreateAsync();
        await using var context = await db.Factory.CreateDbContextAsync(TestContext.Current.CancellationToken);
        Assert.False(await DevelopmentSamples.SeedAsync(context, development, enabled, new Scout.Web.Storage.DocumentFileStore(db.Paths)));
        Assert.Empty(await db.Companies.ListAsync(ArchiveFilter.All));
        Assert.Empty(await db.Positions.ListAsync(archive: ArchiveFilter.All));
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task NonemptyDatabaseNeverReceivesSamples(bool addPosition)
    {
        await using var db = await TemporaryDatabase.CreateAsync();
        var id = await db.Companies.CreateAsync(new()
        {
            Name = "Personal company"
        });
        await db.Companies.SetArchivedAsync(id, true);
        if (addPosition)
        {
            await db.Companies.SetArchivedAsync(id, false);
            await db.Positions.CreateAsync(new()
            {
                CompanyId = id,
                Title = "Personal position"
            });
        }
        var companies = await db.Companies.ListAsync(ArchiveFilter.All);
        var positions = await db.Positions.ListAsync(archive: ArchiveFilter.All);
        await using var context = await db.Factory.CreateDbContextAsync(TestContext.Current.CancellationToken);
        Assert.False(await DevelopmentSamples.SeedAsync(context, true, true, new Scout.Web.Storage.DocumentFileStore(db.Paths)));
        Assert.Equal(companies, await db.Companies.ListAsync(ArchiveFilter.All));
        Assert.Equal(positions, await db.Positions.ListAsync(archive: ArchiveFilter.All));
    }

    [Fact]
    public async Task FailedSeedRollsBackBothTables()
    {
        await using var db = await TemporaryDatabase.CreateAsync();
        await using (var context = await db.Factory.CreateDbContextAsync(TestContext.Current.CancellationToken))
        {
            await context.Database.ExecuteSqlRawAsync("CREATE TRIGGER RejectSamplePosition BEFORE INSERT ON Positions BEGIN SELECT RAISE(ABORT, 'Test write failure'); END;", TestContext.Current.CancellationToken);
            await Assert.ThrowsAsync<DbUpdateException>(() => DevelopmentSamples.SeedAsync(context, true, true, new Scout.Web.Storage.DocumentFileStore(db.Paths)));
        }
        Assert.Empty(await db.Companies.ListAsync(ArchiveFilter.All));
        Assert.Empty(await db.Positions.ListAsync(archive: ArchiveFilter.All));
    }
}

