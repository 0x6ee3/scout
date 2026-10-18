using System.Text;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Scout.Web.Configuration;
using Scout.Web.Data;
using Scout.Web.Domain;
using Scout.Web.Features.Applications;
using Scout.Web.Features.Cvs;
using Scout.Web.Features.Documents;
using Scout.Web.Features.Experiences;
using Scout.Web.Features.InterviewPreparation;
using Scout.Web.Features.InterviewRecordings;
using Scout.Web.Features.InterviewTranscripts;
using Scout.Web.Features.Interviews;
using Scout.Web.Features.InterviewWorkspace;
using Scout.Web.Features.Questions;
using Scout.Web.Features.Submissions;
using Scout.Web.Storage;
using Scout.Web.Tests.InterviewPreparation;
using Scout.Web.Tests.InterviewWorkspace;

namespace Scout.Web.Tests.Data;

public sealed class MilestoneFiveWorkflowTests
{
    [Fact]
    public async Task PopulatedM4UpgradeAndIndependentStoppedRootRestorePreserveAllSources()
    {
        var ct = TestContext.Current.CancellationToken;
        await using var f = await TemporaryDatabase.CreateAsync("20261015230317_WorkspaceCapture");
        var (first, second) = await PreparationTests.Rounds(f);
        var library = new QuestionOperations(f.Factory);
        var prep = new PreparationOperations(f.Factory);
        var source = await library.SaveAsync(null, new()
        {
            Text = "M4 original",
            Tags = ["team"]
        });
        await prep.SelectQuestionsAsync(first, [source]);
        await prep.SelectQuestionsAsync(second, [source]);
        var q = (await prep.QuestionsAsync(first)).Single();
        await prep.SaveQuestionAsync(first, q.Id, new()
        {
            Text = "Detached **wording**",
            Answer = "M4 answer\r\n",
            PreparationNotes = "private"
        });
        await library.DeleteAsync(source);
        await library.SaveAsync(null, new() { Text = "Retained employer catalog question", Tags = ["catalog"] });
        var otherCompany = await f.Companies.CreateAsync(new() { Name = "Other application canary" });
        var otherPosition = await f.Positions.CreateAsync(new() { CompanyId = otherCompany, Title = "Other position" });
        var otherApplication = await new ApplicationOperations(f.Factory).CreateAsync(new() { PositionId = otherPosition });
        await new InterviewOperations(f.Factory).SaveAsync(null, new() { ApplicationId = otherApplication, Title = "Unrelated private round" });
        var story = await new ExperienceOperations(f.Factory).SaveAsync(null, new()
        {
            Title = "M4 STAR",
            Situation = "Unchanged situation"
        });
        await prep.SelectExperiencesAsync(first, [story]);
        var files = new DocumentFileStore(f.Paths);
        var docs = new DocumentOperations(f.Factory, files);
        var cv = await new CvOperations(f.Factory, docs).ImportAsync("exact.pdf", new MemoryStream("%PDF-1.4\nunchanged evidence"u8.ToArray()), new()
        {
            DisplayName = "M4 CV"
        }, ct);
        int application;
        await using (var db = await f.Factory.CreateDbContextAsync(ct))
        {
            application = (await db.Interviews.FindAsync([first], ct))!.ApplicationId;
        }
        var submitted = await new SubmissionOperations(f.Factory, files).RecordAsync(new()
        {
            ApplicationId = application,
            PreparedCvId = cv
        });
        var attached = await docs.ImportAsync("generic.wav", new MemoryStream("existing audio is not consent"u8.ToArray()), new(), new(DocumentOwnerKind.Interview, first), ct);
        var workspaceId = Guid.NewGuid();
        await using (var legacy = await f.Factory.CreateDbContextAsync(ct))
        {
            await legacy.Database.ExecuteSqlInterpolatedAsync($"INSERT INTO InterviewWorkspaceConfiguration (InterviewRoundId,WorkspaceId,ConfigurationRevision,RequiresReview,ApprovedAtUtc,RoundTitle,InterviewNotesEnabled) VALUES ({first},{workspaceId},1,0,'2026-10-15','M4 snapshot',1)", ct);
            legacy.InterviewQuestions.Add(new()
            {
                InterviewRoundId = first,
                Origin = InterviewQuestionOrigin.AdHoc,
                Text = "M4 ad hoc",
                CreationToken = Guid.NewGuid(),
                ContentRevision = 3,
                IsAsked = true,
                Answer = "Revision three"
            });
            (await legacy.InterviewQuestions.FindAsync([q.Id], ct))!.IsArchived = true;
            await legacy.SaveChangesAsync(ct);
            var adHoc = await legacy.InterviewQuestions.SingleAsync(x => x.Origin == InterviewQuestionOrigin.AdHoc, ct);
            legacy.Add(new WorkspaceQuestion { InterviewRoundId = first, InterviewQuestionId = adHoc.Id, Text = "Approved M4 question copy", AnswerEnabled = true });
            var selectedExperience = await legacy.InterviewExperiences.SingleAsync(ct);
            legacy.Add(new WorkspaceExperience { InterviewRoundId = first, InterviewExperienceId = selectedExperience.Id, Title = "Approved M4 STAR copy", Situation = "Snapshot situation" });
            await legacy.SaveChangesAsync(ct);
            await legacy.Database.MigrateAsync(ct);
            Assert.Empty(await legacy.Recordings.ToListAsync(ct));
            Assert.Empty(await legacy.RecordingDecisions.ToListAsync(ct));
            Assert.Empty(await legacy.Transcripts.ToListAsync(ct));
            Assert.False((await legacy.Set<InterviewWorkspaceConfiguration>().SingleAsync(ct)).RecordingChoiceEnabled);
            Assert.Equal("M4 answer\r\n", (await legacy.InterviewQuestions.FindAsync([q.Id], ct))!.Answer);
            Assert.Equal(16, (await legacy.Database.GetAppliedMigrationsAsync(ct)).Count());
            Assert.False(legacy.Database.HasPendingModelChanges());
        }
        var recordings = new RecordingOperations(f.Factory, files);
        var links = new LinkedRecordingStore(f.Paths);
        Directory.CreateDirectory(links.Root);
        await File.WriteAllTextAsync(Path.Combine(links.Root, "working.wav"), "fictional working audio", ct);
        await recordings.RegisterDocumentAsync(first, attached, true, "Acknowledged now", Guid.NewGuid(), ct);
        await recordings.RegisterLinkAsync(first, "working.wav", links, true, null, Guid.NewGuid(), ct);
        await recordings.ChooseAsync(first, RecordingChoice.NotesOnly);
        var transcripts = new TranscriptOperations(f.Factory, files);
        var original = await transcripts.ImportAsync(second, "source.txt", new MemoryStream(Encoding.UTF8.GetBytes("# Exact transcript\r\n  *punctuation*  ")), null, Guid.NewGuid(), ct);
        var correction = await transcripts.CorrectAsync(second, original, "Corrected **copy**", Guid.NewGuid(), ct);
        await recordings.ArchiveAsync(first, (await recordings.ListAsync(first)).First().Id, true);
        var workspace = new WorkspaceOperations(f.Factory);
        Assert.Equal("M4 snapshot", (await workspace.GetAsync(first))!.RoundTitle);
        SqliteConnection.ClearAllPools();
        var paths = Directory.GetFiles(f.Paths.Root, "*", SearchOption.AllDirectories);
        var restored = StoragePaths.FromRoot(Path.Combine(f.Paths.Root, "independent-copy"));
        foreach (var path in paths)
        {
            var target = Path.Combine(restored.Root, Path.GetRelativePath(f.Paths.Root, path));
            Directory.CreateDirectory(Path.GetDirectoryName(target)!);
            File.Copy(path, target);
            Assert.Equal(await File.ReadAllBytesAsync(path, ct), await File.ReadAllBytesAsync(target, ct));
        }
        // Disable the original sources, proving the copy uses only restored relative paths.
        Directory.Move(f.Paths.AssetDirectory, f.Paths.AssetDirectory + "-offline");
        Directory.Move(links.Root, links.Root + "-offline");
        await using var copy = new ScoutDbContext(new DbContextOptionsBuilder<ScoutDbContext>().UseSqlite(new SqliteConnectionStringBuilder(ScoutDatabase.ConnectionString(restored)) { Pooling = false }.ToString()).Options);
        Assert.Equal("ok", Assert.Single(await copy.Database.SqlQueryRaw<string>("PRAGMA integrity_check").ToListAsync(ct)));
        await copy.Database.OpenConnectionAsync(ct);
        await using (var command = copy.Database.GetDbConnection().CreateCommand())
        {
            command.CommandText = "PRAGMA foreign_key_check";
            await using var reader = await command.ExecuteReaderAsync(ct);
            Assert.False(await reader.ReadAsync(ct));
        }
        await copy.Database.CloseConnectionAsync();
        foreach (var document in await copy.Documents.ToListAsync(ct))
        {
            await using var read = await new DocumentFileStore(restored).OpenVerifiedAsync(document, ct);
            Assert.Equal(document.Size, read.Length);
        }
        var linked = await copy.Recordings.SingleAsync(x => x.StorageKind == RecordingStorage.LinkedWorkingFile, ct);
        await using var linkedRead = await new LinkedRecordingStore(restored).OpenVerifiedAsync(linked, ct);
        Assert.True(linkedRead.Length > 0);
        Assert.Equal(3, await copy.RecordingDecisions.CountAsync(ct));
        Assert.Equal(2, await copy.Recordings.CountAsync(ct));
        Assert.Equal(original, (await copy.Transcripts.FindAsync([correction], ct))!.ParentTranscriptId);
        Assert.Equal(workspaceId, (await copy.Set<InterviewWorkspaceConfiguration>().SingleAsync(ct)).WorkspaceId);
        Assert.Equal("M4 snapshot", (await copy.Set<InterviewWorkspaceConfiguration>().SingleAsync(ct)).RoundTitle);
        Assert.Equal("Revision three", (await copy.InterviewQuestions.SingleAsync(x => x.Origin == InterviewQuestionOrigin.AdHoc, ct)).Answer);
        Assert.Equal("Approved M4 question copy", (await copy.Set<WorkspaceQuestion>().SingleAsync(ct)).Text);
        Assert.Equal("Snapshot situation", (await copy.Set<WorkspaceExperience>().SingleAsync(ct)).Situation);
        Assert.Single(await copy.EmployerQuestions.ToListAsync(ct));
        Assert.Equal(2, await copy.Applications.CountAsync(ct));
        Assert.Equal(submitted, (await copy.Submissions.SingleAsync(ct)).Id);
        Assert.False(await DevelopmentSamples.SeedAsync(copy, true, true, new(restored)));
    }
}
