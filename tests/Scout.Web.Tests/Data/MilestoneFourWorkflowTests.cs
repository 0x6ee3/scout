using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Scout.Web.Configuration;
using Scout.Web.Data;
using Scout.Web.Domain;
using Scout.Web.Features.Applications;
using Scout.Web.Features.Cvs;
using Scout.Web.Features.Documents;
using Scout.Web.Features.Experiences;
using Scout.Web.Features.InterviewWorkspace;
using Scout.Web.Features.Questions;
using Scout.Web.Features.Submissions;
using Scout.Web.Storage;
using Scout.Web.Tests.InterviewWorkspace;
namespace Scout.Web.Tests.Data;

public sealed class MilestoneFourWorkflowTests
{
    [Fact]
    public async Task PopulatedM3UpgradeAndStoppedRestorePreserveEvidenceSnapshotsAndCanonicalCapture()
    {
        await using var f = await TemporaryDatabase.CreateAsync("20261015221434_InterviewExperiencePreparation");
        var company = await f.Companies.CreateAsync(new()
        {
            Name = "Fictional M3"
        });
        var position = await f.Positions.CreateAsync(new()
        {
            CompanyId = company,
            Title = "Engineer"
        });
        var app = await new ApplicationOperations(f.Factory).CreateAsync(new()
        {
            PositionId = position
        });
        var source = await new QuestionOperations(f.Factory).SaveAsync(null, new()
        {
            Text = "Library original",
            Tags = ["team"]
        });
        var story = await new ExperienceOperations(f.Factory).SaveAsync(null, new()
        {
            Title = "STAR",
            Situation = "Approved situation",
            Reflection = "Private reflection"
        });
        await using (var legacy = await f.Factory.CreateDbContextAsync(TestContext.Current.CancellationToken))
        {
            foreach (var id in new[] { 1, 2 })
                await legacy.Database.ExecuteSqlInterpolatedAsync($"INSERT INTO Interviews (Id,ApplicationId,Title,Type,Status,PreparationNotes,InterviewNotes,IsArchived,CreatedAtUtc,UpdatedAtUtc) VALUES ({id},{app},'Legacy round','Other','Planned','Private preparation','M3 notes',0,'2026-10-01','2026-10-01')", TestContext.Current.CancellationToken);
            foreach (var id in new[] { 1, 2 })
                await legacy.Database.ExecuteSqlInterpolatedAsync($"INSERT INTO InterviewQuestions (Id,InterviewRoundId,EmployerQuestionId,Text,Answer,PreparationNotes,DisplayOrder,IsArchived,CreatedAtUtc,UpdatedAtUtc) VALUES ({id},{id},{source},'Round wording','Existing answer','Private question notes',0,0,'2026-10-01','2026-10-01')", TestContext.Current.CancellationToken);
            await legacy.Database.ExecuteSqlInterpolatedAsync($"INSERT INTO InterviewExperiences (InterviewRoundId,ProfessionalExperienceId,PreparationNotes,DisplayOrder,IsArchived,CreatedAtUtc,UpdatedAtUtc) VALUES (1,{story},'Private rehearsal',0,0,'2026-10-01','2026-10-01')", TestContext.Current.CancellationToken);
        }
        var files = new DocumentFileStore(f.Paths);
        var docs = new DocumentOperations(f.Factory, files);
        var cvOps = new CvOperations(f.Factory, docs);
        var cv = await cvOps.ImportAsync("exact.pdf", new MemoryStream("%PDF-1.4\nExact M3 evidence"u8.ToArray()), new()
        {
            DisplayName = "M3 CV"
        }, TestContext.Current.CancellationToken);
        var document = (await cvOps.GetAsync(cv))!.Document;
        var submission = await new SubmissionOperations(f.Factory, files).RecordAsync(new()
        {
            ApplicationId = app,
            PreparedCvId = cv
        });
        await using (var db = await f.Factory.CreateDbContextAsync(TestContext.Current.CancellationToken))
        {
            await Integrity(db);
            await db.Database.MigrateAsync(TestContext.Current.CancellationToken);
            await Integrity(db);
            Assert.False(db.Database.HasPendingModelChanges());
            Assert.Equal(db.Database.GetMigrations().Count(), (await db.Database.GetAppliedMigrationsAsync(TestContext.Current.CancellationToken)).Count());
            var row = await db.InterviewQuestions.SingleAsync(x => x.Id == 1, TestContext.Current.CancellationToken);
            Assert.Equal("Existing answer", row.Answer);
            Assert.Equal("Private question notes", row.PreparationNotes);
            Assert.Equal(0, row.ContentRevision);
            Assert.False(row.IsAsked);
            Assert.Equal(InterviewQuestionOrigin.Library, row.Origin);
        }
        var ops = new WorkspaceOperations(f.Factory);
        var setup = (await ops.SetupAsync(1))!;
        await ops.ReviewedApproveAsync(1, new()
        {
            InterviewNotes = true,
            CvKey = $"{submission}/{document.Id}",
            Questions = [new() { Id = 1, Include = true, Answer = true }],
            Experiences = [new() { Id = setup.Experiences.Single().Id, Include = true, Situation = true }],
            PriorAnswerIds = [2]
        });
        var view = (await ops.GetAsync(1))!;
        Assert.Equal("Existing answer", Assert.Single(view.PriorAnswers).Answer);
        await ops.SaveQuestionAsync(1, 1, 1, new(0, true, true, true, "M4 answer"));
        await ops.SaveNotesAsync(1, 1, 0, "M4 notes");
        await new QuestionOperations(f.Factory).DeleteAsync(source);
        Assert.NotNull(await ops.GetAsync(1));
        await new ExperienceOperations(f.Factory).SaveAsync(story, new()
        {
            Title = "Changed",
            Situation = "Changed"
        });
        Assert.Equal("Approved situation", (await ops.GetAsync(1))!.Experiences[0].Situation);
        SqliteConnection.ClearAllPools();
        var paths = Directory.GetFiles(f.Paths.Root, "*", SearchOption.AllDirectories);
        var restored = StoragePaths.FromRoot(Path.Combine(f.Paths.Root, "restored"));
        foreach (var path in paths)
        {
            var target = Path.Combine(restored.Root, Path.GetRelativePath(f.Paths.Root, path));
            Directory.CreateDirectory(Path.GetDirectoryName(target)!);
            File.Copy(path, target);
            Assert.Equal(await File.ReadAllBytesAsync(path, TestContext.Current.CancellationToken), await File.ReadAllBytesAsync(target, TestContext.Current.CancellationToken));
        }
        await using var copy = new ScoutDbContext(new DbContextOptionsBuilder<ScoutDbContext>().UseSqlite(new SqliteConnectionStringBuilder(ScoutDatabase.ConnectionString(restored)) { Pooling = false }.ToString()).Options);
        await Integrity(copy);
        var q = await copy.InterviewQuestions.SingleAsync(x => x.Id == 1, TestContext.Current.CancellationToken);
        Assert.Null(q.EmployerQuestionId);
        Assert.Equal("M4 answer", q.Answer);
        Assert.True(q.IsAsked);
        Assert.Equal("M4 notes", (await copy.Interviews.SingleAsync(x => x.Id == 1, TestContext.Current.CancellationToken)).InterviewNotes);
        Assert.Equal("Existing answer", (await copy.Set<WorkspacePriorAnswer>().SingleAsync(TestContext.Current.CancellationToken)).Answer);
        Assert.Equal("Round wording", (await copy.Set<WorkspaceQuestion>().SingleAsync(TestContext.Current.CancellationToken)).Text);
        Assert.Equal(view.WorkspaceId, (await copy.Set<InterviewWorkspaceConfiguration>().SingleAsync(TestContext.Current.CancellationToken)).WorkspaceId);
        var restoredDoc = await copy.Documents.SingleAsync(TestContext.Current.CancellationToken);
        Assert.Equal(document.Sha256, restoredDoc.Sha256);
        await using var read = await new DocumentFileStore(restored).OpenVerifiedAsync(restoredDoc, TestContext.Current.CancellationToken);
        Assert.Equal(document.Size, read.Length);
        Assert.False(await DevelopmentSamples.SeedAsync(copy, true, true, new(restored)));
    }
    private static async Task Integrity(ScoutDbContext db)
    {
        Assert.Equal("ok", Assert.Single(await db.Database.SqlQueryRaw<string>("PRAGMA integrity_check").ToListAsync(TestContext.Current.CancellationToken)));
        await db.Database.OpenConnectionAsync(TestContext.Current.CancellationToken);
        try
        {
            await using var command = db.Database.GetDbConnection().CreateCommand();
            command.CommandText = "PRAGMA foreign_key_check";
            await using var reader = await command.ExecuteReaderAsync(TestContext.Current.CancellationToken);
            Assert.False(await reader.ReadAsync(TestContext.Current.CancellationToken));
        }
        finally { await db.Database.CloseConnectionAsync(); }
    }
}
