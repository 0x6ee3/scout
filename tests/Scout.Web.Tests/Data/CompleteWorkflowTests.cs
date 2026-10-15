using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Scout.Web.Data;
using Scout.Web.Domain;
using Scout.Web.Features;
using Scout.Web.Features.Companies;
using Scout.Web.Features.Positions;

namespace Scout.Web.Tests.Data;

public sealed class CompleteWorkflowTests
{
    [Fact]
    public async Task FreshMigrationsSupportFullWorkflowAndStoppedFileCopyReopens()
    {
        await using var db = await TemporaryDatabase.CreateAsync();
        var company = await db.Companies.CreateAsync(new()
        {
            Name = "München",
            Description = "Reference\ntext",
            PersonalNotes = "Separate notes"
        });
        var other = await db.Companies.CreateAsync(new()
        {
            Name = "Other"
        });
        var position = await db.Positions.CreateAsync(new()
        {
            CompanyId = company,
            Title = "Engineer",
            JobDescriptionSnapshot = "Original\nStelle",
            Status = PositionStatus.Interested
        });
        await db.Positions.CreateAsync(new()
        {
            CompanyId = other,
            Title = "Another"
        });
        await db.Positions.UpdateAsync(position, new()
        {
            CompanyId = company,
            Title = "Senior engineer",
            JobDescriptionSnapshot = "Ignored replacement",
            PersonalNotes = "Follow up",
            Status = PositionStatus.Expired
        });
        await db.Companies.SetArchivedAsync(company, true);
        await db.Positions.SetArchivedAsync(position, true);
        Assert.Single(await db.Positions.ListAsync(company, ArchiveFilter.Archived, PositionStatus.Expired, "MÜNCHEN"));
        await db.Companies.SetArchivedAsync(company, false);
        await db.Positions.SetArchivedAsync(position, false);
        var savedCompany = await db.Companies.GetAsync(company);
        var savedPosition = await db.Positions.GetAsync(position);
        await using (var context = await db.Factory.CreateDbContextAsync(TestContext.Current.CancellationToken))
        {
            await context.Database.MigrateAsync(TestContext.Current.CancellationToken);
            Assert.False(context.Database.HasPendingModelChanges());
            Assert.Equal(context.Database.GetMigrations(), await context.Database.GetAppliedMigrationsAsync(TestContext.Current.CancellationToken));
        }
        Assert.Equal(savedCompany, await db.Companies.GetAsync(company));
        Assert.Equal(savedPosition, await db.Positions.GetAsync(position));
        // Every source context is closed, and the test factory disables pooling.
        var copyPath = Path.Combine(db.Paths.Root, "backup.db");
        File.Copy(db.Paths.DatabasePath, copyPath);
        var connection = new SqliteConnectionStringBuilder { DataSource = copyPath, Mode = SqliteOpenMode.ReadOnly, Pooling = false };
        var factory = new PooledDbContextFactory<ScoutDbContext>(new DbContextOptionsBuilder<ScoutDbContext>().UseSqlite(connection.ToString()).Options);
        Assert.Equal(savedCompany, await new CompanyOperations(factory).GetAsync(company));
        Assert.Equal(savedPosition, await new PositionOperations(factory).GetAsync(position));
        await using var copied = await factory.CreateDbContextAsync(TestContext.Current.CancellationToken);
        Assert.Equal("ok", Assert.Single(await copied.Database.SqlQueryRaw<string>("PRAGMA integrity_check").ToListAsync(TestContext.Current.CancellationToken)));
    }

    [Fact]
    public async Task FailedPositionSaveAndArchivePreserveStoredDataAndDraft()
    {
        await using var db = await TemporaryDatabase.CreateAsync();
        var company = await db.Companies.CreateAsync(new()
        {
            Name = "Parent"
        });
        var id = await db.Positions.CreateAsync(new()
        {
            CompanyId = company,
            Title = "Saved",
            JobDescriptionSnapshot = "Original"
        });
        var saved = await db.Positions.GetAsync(id);
        var connection = new SqliteConnectionStringBuilder(ScoutDatabase.ConnectionString(db.Paths)) { Mode = SqliteOpenMode.ReadOnly, Pooling = false };
        var factory = new PooledDbContextFactory<ScoutDbContext>(new DbContextOptionsBuilder<ScoutDbContext>().UseSqlite(connection.ToString()).Options);
        var positions = new PositionOperations(factory);
        var draft = new PositionEditModel { CompanyId = company, Title = " Unsaved ", PersonalNotes = "Draft\nNotizen" };
        await Assert.ThrowsAsync<DbUpdateException>(() => positions.UpdateAsync(id, draft));
        await Assert.ThrowsAsync<DbUpdateException>(() => positions.CreateAsync(draft));
        await Assert.ThrowsAsync<DbUpdateException>(() => positions.SetArchivedAsync(id, true));
        await Assert.ThrowsAsync<DbUpdateException>(() => new CompanyOperations(factory).SetArchivedAsync(company, true));
        Assert.Equal(" Unsaved ", draft.Title);
        Assert.Equal("Draft\nNotizen", draft.PersonalNotes);
        Assert.Equal(saved, await db.Positions.GetAsync(id));
        Assert.Single(await db.Positions.ListAsync());
    }
}

