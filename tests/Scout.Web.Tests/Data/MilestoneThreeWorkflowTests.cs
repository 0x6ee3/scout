using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Scout.Web.Configuration;
using Scout.Web.Data;
using Scout.Web.Domain;
using Scout.Web.Features.Cvs;
using Scout.Web.Features.Documents;
using Scout.Web.Features.Experiences;
using Scout.Web.Features.Interviews;
using Scout.Web.Features.InterviewPreparation;
using Scout.Web.Features.Questions;
using Scout.Web.Features.Submissions;
using Scout.Web.Storage;
using Scout.Web.Tests.InterviewPreparation;

namespace Scout.Web.Tests.Data;

public sealed class MilestoneThreeWorkflowTests
{
    [Theory]
    [InlineData(false)][InlineData(true)]
    public async Task StandaloneLibraryContentPreventsSeeding(bool experience)
    {
        await using var fixture = await TemporaryDatabase.CreateAsync();
        if(experience) await new ExperienceOperations(fixture.Factory).SaveAsync(null, new() { Title = "Personal example" });
        else await new QuestionOperations(fixture.Factory).SaveAsync(null, new() { Text = "Personal question" });
        await using var db = await fixture.Factory.CreateDbContextAsync(TestContext.Current.CancellationToken);
        Assert.False(await DevelopmentSamples.SeedAsync(db, true, true, new(fixture.Paths)));
        Assert.Empty(await db.Companies.ToListAsync(TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task SamplesIncludeFictionalPartialAndFullStarAndDoNotOverwriteEdits()
    {
        await using var fixture = await TemporaryDatabase.CreateAsync();
        await using(var db = await fixture.Factory.CreateDbContextAsync(TestContext.Current.CancellationToken))
        {
            Assert.True(await DevelopmentSamples.SeedAsync(db, true, true, new(fixture.Paths)));
            Assert.Equal(2, await db.EmployerQuestions.CountAsync(TestContext.Current.CancellationToken));
            Assert.Equal(2, await db.ProfessionalExperiences.CountAsync(TestContext.Current.CancellationToken));
            Assert.Contains(await db.ProfessionalExperiences.ToListAsync(TestContext.Current.CancellationToken), x => x.Situation is null && x.Title.StartsWith("[Sample]"));
            var prepared = await db.InterviewQuestions.SingleAsync(TestContext.Current.CancellationToken); Assert.Null(prepared.PreparationNotes); Assert.StartsWith("[Sample]", prepared.Answer);
            var story = await db.ProfessionalExperiences.SingleAsync(x => x.Situation != null, TestContext.Current.CancellationToken); story.Result = "Personal revision"; await db.SaveChangesAsync(TestContext.Current.CancellationToken);
        }
        await using(var db = await fixture.Factory.CreateDbContextAsync(TestContext.Current.CancellationToken))
        {
            Assert.False(await DevelopmentSamples.SeedAsync(db, true, true, new(fixture.Paths)));
            Assert.Equal("Personal revision", (await db.ProfessionalExperiences.SingleAsync(x => x.Situation != null, TestContext.Current.CancellationToken)).Result);
            await Integrity(db);
        }
    }

    [Fact]
    public async Task M2UpgradeAndStoppedWholeRootRestorePreserveEvidenceAndPreparation()
    {
        await using var fixture = await TemporaryDatabase.CreateAsync("20261015171814_CvsAndSubmissions");
        // Seed the historical schema with SQL: current interview writers require M4 columns.
        var company=await fixture.Companies.CreateAsync(new(){Name="Legacy company"});
        var position=await fixture.Positions.CreateAsync(new(){CompanyId=company,Title="Legacy position"});
        var application=await new Scout.Web.Features.Applications.ApplicationOperations(fixture.Factory).CreateAsync(new(){PositionId=position});
        await using(var legacy=await fixture.Factory.CreateDbContextAsync(TestContext.Current.CancellationToken))
        {
            await legacy.Database.ExecuteSqlInterpolatedAsync($"INSERT INTO Interviews (ApplicationId, Title, Type, Status, PreparationNotes, InterviewNotes, IsArchived, CreatedAtUtc, UpdatedAtUtc) VALUES ({application}, 'First', 'Other', 'Planned', 'Existing M2 preparation', 'Existing notes only', 0, '2026-10-01', '2026-10-01')",TestContext.Current.CancellationToken);
            await legacy.Database.ExecuteSqlInterpolatedAsync($"INSERT INTO Interviews (ApplicationId, Title, Type, Status, IsArchived, CreatedAtUtc, UpdatedAtUtc) VALUES ({application}, 'Second', 'Other', 'Planned', 0, '2026-10-01', '2026-10-01')",TestContext.Current.CancellationToken);
        }
        var first=1;var second=2;
        var originalRound=new InterviewRound{Id=first,ApplicationId=application,Application=new(){PositionId=position}};
        var originalPosition = (await fixture.Positions.GetAsync(originalRound.Application.PositionId))!;
        var files = new DocumentFileStore(fixture.Paths); var documents = new DocumentOperations(fixture.Factory, files);
        var cv = await new CvOperations(fixture.Factory, documents).ImportAsync("original.pdf", new MemoryStream("%PDF-1.4\nM2 immutable bytes"u8.ToArray()), new() { DisplayName = "Original submitted CV" }, TestContext.Current.CancellationToken);
        var doc = (await new CvOperations(fixture.Factory, documents).GetAsync(cv))!.Document;
        var submission = await new SubmissionOperations(fixture.Factory, files).RecordAsync(new() { ApplicationId = originalRound.ApplicationId, PreparedCvId = cv });
        int events;
        await using(var db = await fixture.Factory.CreateDbContextAsync(TestContext.Current.CancellationToken))
        {
            events = await db.TimelineEvents.CountAsync(TestContext.Current.CancellationToken);
            await Integrity(db);
            await db.Database.MigrateAsync(TestContext.Current.CancellationToken);
            Assert.False(db.Database.HasPendingModelChanges());
            Assert.False(await DevelopmentSamples.SeedAsync(db, true, true, files));
            Assert.Equal(events, await db.TimelineEvents.CountAsync(TestContext.Current.CancellationToken));
            await Integrity(db);
        }
        Assert.Equal(originalPosition, await fixture.Positions.GetAsync(originalRound.Application.PositionId));
        var library = new QuestionOperations(fixture.Factory); var experiences = new ExperienceOperations(fixture.Factory); var preparation = new PreparationOperations(fixture.Factory);
        var q = await library.SaveAsync(null, new() { Text = "Original wording", Tags = ["team"] });
        var q2 = await library.SaveAsync(null, new() { Text = "Second question" });
        var e = await experiences.SaveAsync(null, new() { Title = "Fictional story", Situation = "Initial" });
        var e2 = await experiences.SaveAsync(null, new() { Title = "Partial story" });
        await preparation.SelectQuestionsAsync(first, [q, q2]); await preparation.SelectQuestionsAsync(second, [q]);
        await preparation.SelectExperiencesAsync(first, [e, e2]); await preparation.SelectExperiencesAsync(second, [e]);
        var prepared = (await preparation.QuestionsAsync(first)).Single(x => x.EmployerQuestionId == q);
        var selected = (await preparation.ExperiencesAsync(first)).Single(x => x.ProfessionalExperienceId == e);
        await preparation.SaveQuestionAsync(first, prepared.Id, new() { Text = "Round wording", Answer = "Round answer", PreparationNotes = "Private context" });
        await preparation.SaveExperienceAsync(first, selected.Id, new() { PreparationNotes = "Round rehearsal" });
        await library.SaveAsync(q, new() { Text = "Later wording" }); await library.DeleteAsync(q);
        await preparation.ArchiveQuestionAsync(first, prepared.Id, true); await preparation.ArchiveQuestionAsync(first, prepared.Id, false);
        await preparation.MoveQuestionAsync(first, prepared.Id, -1);
        await preparation.MoveExperienceAsync(first, selected.Id, 1);
        await experiences.SaveAsync(e, new() { Title = "Revised story", Situation = "Current STAR" }); await experiences.SetArchivedAsync(e, true);
        await preparation.ArchiveExperienceAsync(first, selected.Id, true);
        await experiences.SetArchivedAsync(e, false); await preparation.ArchiveExperienceAsync(first, selected.Id, false);

        // Operation contexts are closed; capture the source file list before creating its copied child root.
        SqliteConnection.ClearAllPools();
        var sourceFiles = Directory.GetFiles(fixture.Paths.Root, "*", SearchOption.AllDirectories);
        var restored = StoragePaths.FromRoot(Path.Combine(fixture.Paths.Root, "restored"));
        foreach(var source in sourceFiles)
        {
            var destination = Path.Combine(restored.Root, Path.GetRelativePath(fixture.Paths.Root, source));
            Directory.CreateDirectory(Path.GetDirectoryName(destination)!); File.Copy(source, destination, false);
        }
        var connectionString = new SqliteConnectionStringBuilder(ScoutDatabase.ConnectionString(restored)) { Pooling = false }.ToString();
        await using var copy = new ScoutDbContext(new DbContextOptionsBuilder<ScoutDbContext>().UseSqlite(connectionString).Options);
        await copy.Database.MigrateAsync(TestContext.Current.CancellationToken); await Integrity(copy);
        var restoredQuestion = await copy.InterviewQuestions.SingleAsync(x => x.Id == prepared.Id, TestContext.Current.CancellationToken);
        Assert.Null(restoredQuestion.EmployerQuestionId); Assert.Equal("Round wording", restoredQuestion.Text); Assert.Equal("Round answer", restoredQuestion.Answer); Assert.Equal("Private context", restoredQuestion.PreparationNotes); Assert.False(restoredQuestion.IsArchived);
        var otherQuestion = await copy.InterviewQuestions.SingleAsync(x => x.InterviewRoundId == second, TestContext.Current.CancellationToken);
        Assert.Equal("Original wording", otherQuestion.Text); Assert.Null(otherQuestion.Answer);
        var restoredExperience = await copy.InterviewExperiences.Include(x => x.ProfessionalExperience).SingleAsync(x => x.Id == selected.Id, TestContext.Current.CancellationToken);
        Assert.Equal("Current STAR", restoredExperience.ProfessionalExperience.Situation); Assert.Equal("Round rehearsal", restoredExperience.PreparationNotes); Assert.False(restoredExperience.IsArchived);
        Assert.Equal(new[] { (int?)null, q2 }, (await copy.InterviewQuestions.Where(x => x.InterviewRoundId == first).OrderBy(x => x.DisplayOrder).ToListAsync(TestContext.Current.CancellationToken)).Select(x => x.EmployerQuestionId));
        Assert.Equal(new[] { e2, e }, (await copy.InterviewExperiences.Where(x => x.InterviewRoundId == first).OrderBy(x => x.DisplayOrder).ToListAsync(TestContext.Current.CancellationToken)).Select(x => x.ProfessionalExperienceId));
        Assert.Equal("Existing notes only", (await copy.Interviews.SingleAsync(x => x.Id == first, TestContext.Current.CancellationToken)).InterviewNotes);
        Assert.Equal("Existing M2 preparation", (await copy.Interviews.SingleAsync(x => x.Id == first, TestContext.Current.CancellationToken)).PreparationNotes);
        Assert.Equal(events, await copy.TimelineEvents.CountAsync(TestContext.Current.CancellationToken));
        Assert.Equal(submission, (await copy.Submissions.SingleAsync(TestContext.Current.CancellationToken)).Id);
        Assert.Equal("Original submitted CV", (await copy.SubmissionFiles.SingleAsync(TestContext.Current.CancellationToken)).LabelSnapshot);
        var restoredDoc = await copy.Documents.SingleAsync(TestContext.Current.CancellationToken);
        Assert.Equal(doc.Sha256, restoredDoc.Sha256);
        await using var read = await new DocumentFileStore(restored).OpenVerifiedAsync(restoredDoc, TestContext.Current.CancellationToken);
        Assert.Equal(doc.Size, read.Length);
    }

    private static async Task Integrity(ScoutDbContext db)
    {
        Assert.Equal("ok", Assert.Single(await db.Database.SqlQueryRaw<string>("PRAGMA integrity_check").ToListAsync(TestContext.Current.CancellationToken)));
        await db.Database.OpenConnectionAsync(TestContext.Current.CancellationToken);
        try
        {
            await using var command = db.Database.GetDbConnection().CreateCommand(); command.CommandText = "PRAGMA foreign_key_check";
            await using var reader = await command.ExecuteReaderAsync(TestContext.Current.CancellationToken); Assert.False(await reader.ReadAsync(TestContext.Current.CancellationToken));
        }
        finally { await db.Database.CloseConnectionAsync(); }
    }
}
