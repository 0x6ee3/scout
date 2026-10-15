using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Scout.Web.Configuration;
using Scout.Web.Data;
using Scout.Web.Domain;
using Scout.Web.Features.Applications;
using Scout.Web.Features.Contacts;
using Scout.Web.Features.Cvs;
using Scout.Web.Features.Documents;
using Scout.Web.Features.Interviews;
using Scout.Web.Features.NextActions;
using Scout.Web.Features.Submissions;
using Scout.Web.Features.Timeline;
using Scout.Web.Storage;
namespace Scout.Web.Tests.Data;

public sealed class MilestoneTwoWorkflowTests
{
    [Fact]
    public async Task M1UpgradePreservesIdsTimestampsAndImmutableVacancy()
    {
        await using var db = await TemporaryDatabase.CreateAsync("20261015161458_Positions");
        var company = await db.Companies.CreateAsync(new()
        {
            Name = "Personal M1 company"
        });
        var position = await db.Positions.CreateAsync(new()
        {
            CompanyId = company,
            Title = "Existing position",
            JobDescriptionSnapshot = "Unveränderlich\nOriginal"
        });
        var original = (await db.Positions.GetAsync(position))!;
        await using (var ctx = await db.Factory.CreateDbContextAsync(TestContext.Current.CancellationToken))
        {
            await ctx.Database.MigrateAsync(TestContext.Current.CancellationToken);
            Assert.False(await DevelopmentSamples.SeedAsync(ctx, true, true, new(db.Paths)));
            Assert.Equal(ctx.Database.GetMigrations(), await ctx.Database.GetAppliedMigrationsAsync(TestContext.Current.CancellationToken));
            Assert.False(ctx.Database.HasPendingModelChanges());
        }
        Assert.Equal(original, await db.Positions.GetAsync(position));
        var app = await new ApplicationOperations(db.Factory).CreateAsync(new()
        {
            PositionId = position
        });
        Assert.Equal(position, (await new ApplicationOperations(db.Factory).GetAsync(app))!.PositionId);
    }
    [Fact]
    public async Task StandaloneDocumentPreventsSeedingAndFreshSamplesHaveRealFiles()
    {
        await using var db = await TemporaryDatabase.CreateAsync();
        var store = new DocumentFileStore(db.Paths);
        var docs = new DocumentOperations(db.Factory, store);
        await docs.ImportAsync("personal.txt", new MemoryStream("keep"u8.ToArray()), new(), token: TestContext.Current.CancellationToken);
        await using (var ctx = await db.Factory.CreateDbContextAsync(TestContext.Current.CancellationToken))
            Assert.False(await DevelopmentSamples.SeedAsync(ctx, true, true, store));
        Assert.Empty(await db.Companies.ListAsync());
        await using var fresh = await TemporaryDatabase.CreateAsync();
        var freshStore = new DocumentFileStore(fresh.Paths);
        await using (var ctx = await fresh.Factory.CreateDbContextAsync(TestContext.Current.CancellationToken))
        {
            Assert.True(await DevelopmentSamples.SeedAsync(ctx, true, true, freshStore));
            Assert.Single(await ctx.Applications.ToListAsync(TestContext.Current.CancellationToken));
            Assert.Equal(2, await ctx.Contacts.CountAsync(TestContext.Current.CancellationToken));
            Assert.Single(await ctx.Submissions.ToListAsync(TestContext.Current.CancellationToken));
            var doc = await ctx.Documents.SingleAsync(TestContext.Current.CancellationToken);
            await using var read = await freshStore.OpenVerifiedAsync(doc, TestContext.Current.CancellationToken);
            Assert.True(read.Length > 0);
            Assert.False(await DevelopmentSamples.SeedAsync(ctx, true, true, freshStore));
        }
    }
    [Fact]
    public async Task CompleteWorkflowSurvivesStoppedWholeRootCopy()
    {
        await using var db = await TemporaryDatabase.CreateAsync();
        var c = await db.Companies.CreateAsync(new()
        {
            Name = "München example"
        });
        var p = await db.Positions.CreateAsync(new()
        {
            CompanyId = c,
            Title = "Engineer",
            JobDescriptionSnapshot = "Preserved vacancy"
        });
        var app = await new ApplicationOperations(db.Factory).CreateAsync(new()
        {
            PositionId = p
        });
        var contacts = new ContactOperations(db.Factory);
        var contact = await contacts.SaveAsync(null, new()
        {
            CompanyId = c,
            Name = "Recruiter"
        });
        await contacts.SaveApplicationContactsAsync(app, [contact]);
        var e = await new TimelineOperations(db.Factory).SaveAsync(null, new()
        {
            ApplicationId = app,
            EventDate = new(2026, 9, 1),
            Description = "Historical untimed event",
            ContactIds = [contact]
        });
        var round = await new InterviewOperations(db.Factory).SaveAsync(null, new()
        {
            ApplicationId = app,
            Status = InterviewStatus.Completed,
            InterviewNotes = "Notes only",
            ContactIds = [contact]
        });
        var action = await new NextActionOperations(db.Factory).SaveAsync(null, new()
        {
            ApplicationId = app,
            InterviewRoundId = round,
            Title = "Follow up",
            DueOn = new(2026, 10, 16)
        });
        var store = new DocumentFileStore(db.Paths);
        var docs = new DocumentOperations(db.Factory, store);
        var cv = await new CvOperations(db.Factory, docs).ImportAsync("CV.pdf", new MemoryStream("%PDF-1.4\nversion"u8.ToArray()), new()
        {
            DisplayName = "Exact version"
        }, TestContext.Current.CancellationToken);
        var doc = (await new CvOperations(db.Factory, docs).GetAsync(cv))!.Document;
        await docs.LinkAsync(new(DocumentOwnerKind.Company, c), doc.Id);
        await docs.LinkAsync(new(DocumentOwnerKind.Position, p), doc.Id);
        await docs.LinkAsync(new(DocumentOwnerKind.Application, app), doc.Id);
        await docs.LinkAsync(new(DocumentOwnerKind.Event, e), doc.Id);
        await docs.LinkAsync(new(DocumentOwnerKind.Interview, round), doc.Id);
        var submission = await new SubmissionOperations(db.Factory, store).RecordAsync(new()
        {
            ApplicationId = app,
            PreparedCvId = cv
        });
        // All operation contexts are closed. Retain every relative file, including failed imports.
        try
        {
            await store.ImportAsync("broken.pdf", new MemoryStream("invalid"u8.ToArray()), TestContext.Current.CancellationToken);
        }
        catch (DocumentImportException) { }
        SqliteConnection.ClearAllPools();
        var restored = StoragePaths.FromRoot(Path.Combine(db.Paths.Root, "restored"));
        Directory.CreateDirectory(restored.Root);
        var sourceFiles = Directory.GetFiles(db.Paths.Root, "*", SearchOption.AllDirectories);
        foreach (var source in sourceFiles)
        {
            var relative = Path.GetRelativePath(db.Paths.Root, source);
            var destination = Path.Combine(restored.Root, relative);
            Directory.CreateDirectory(Path.GetDirectoryName(destination)!);
            File.Copy(source, destination, overwrite: false);
        }
        await using var ctx2 = new ScoutDbContext(new DbContextOptionsBuilder<ScoutDbContext>().UseSqlite(new SqliteConnectionStringBuilder(ScoutDatabase.ConnectionString(restored)) { Pooling = false }.ToString()).Options);
        await ctx2.Database.MigrateAsync(TestContext.Current.CancellationToken);
        Assert.Equal(app, (await ctx2.Applications.SingleAsync(TestContext.Current.CancellationToken)).Id);
        Assert.Equal("Preserved vacancy", (await ctx2.Positions.SingleAsync(TestContext.Current.CancellationToken)).JobDescriptionSnapshot);
        Assert.Equal(action, (await ctx2.NextActions.SingleAsync(TestContext.Current.CancellationToken)).Id);
        Assert.Equal(submission, (await ctx2.Submissions.SingleAsync(TestContext.Current.CancellationToken)).Id);
        Assert.Equal("Notes only", (await ctx2.Interviews.SingleAsync(TestContext.Current.CancellationToken)).InterviewNotes);
        Assert.Equal(4, await ctx2.TimelineEvents.CountAsync(TestContext.Current.CancellationToken));
        Assert.Single(await ctx2.SubmissionFiles.ToListAsync(TestContext.Current.CancellationToken));
        Assert.Equal(5, (await docs.RelationsAsync(doc.Id)).Count);
        var restoredDoc = await ctx2.Documents.SingleAsync(TestContext.Current.CancellationToken);
        await using var read = await new DocumentFileStore(restored).OpenVerifiedAsync(restoredDoc, TestContext.Current.CancellationToken);
        Assert.Equal(doc.Sha256, restoredDoc.Sha256);
        Assert.NotEmpty(Directory.GetFiles(restored.AssetDirectory, "*.partial", SearchOption.AllDirectories));
        Assert.Equal("ok", Assert.Single(await ctx2.Database.SqlQueryRaw<string>("PRAGMA integrity_check").ToListAsync(TestContext.Current.CancellationToken)));
        await using var connection = new SqliteConnection(new SqliteConnectionStringBuilder(ScoutDatabase.ConnectionString(restored)) { Pooling = false }.ToString());
        await connection.OpenAsync(TestContext.Current.CancellationToken);
        await using var command = connection.CreateCommand();
        command.CommandText = "PRAGMA foreign_key_check";
        await using var reader = await command.ExecuteReaderAsync(TestContext.Current.CancellationToken);
        Assert.False(await reader.ReadAsync(TestContext.Current.CancellationToken));
    }
}

