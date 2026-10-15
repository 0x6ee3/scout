using System.ComponentModel.DataAnnotations;
using Microsoft.EntityFrameworkCore;
using Scout.Web.Domain;
using Scout.Web.Features.Applications;
using Scout.Web.Features.Cvs;
using Scout.Web.Features.Documents;
using Scout.Web.Features.Submissions;
using Scout.Web.Storage;
using Scout.Web.Tests.Data;
namespace Scout.Web.Tests.Submissions;

public sealed class SubmissionTests
{
    [Fact]
    public async Task SubmittedFilesAndLabelsSurviveNewVersionsArchivalAndCorrections()
    {
        await using var db = await TemporaryDatabase.CreateAsync();
        var c = await db.Companies.CreateAsync(new()
        {
            Name = "Company"
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
        var files = new DocumentFileStore(db.Paths);
        var docs = new DocumentOperations(db.Factory, files);
        var cvs = new CvOperations(db.Factory, docs);
        var submissions = new SubmissionOperations(db.Factory, files);
        var source = Path.Combine(db.Paths.Root, "source.pdf");
        var original = "%PDF-1.4\nfirst"u8.ToArray();
        await File.WriteAllBytesAsync(source, original, TestContext.Current.CancellationToken);
        int cv;
        await using (var input = File.OpenRead(source))
            cv = await cvs.ImportAsync("CV.pdf", input, new()
            {
                DisplayName = "German CV",
                Language = "de"
            }, TestContext.Current.CancellationToken);
        var model = new SubmissionEditModel { ApplicationId = a, PreparedCvId = cv, SubmittedOn = new(2026, 8, 1), SetAppliedStatus = true, SetAppliedOnIfEmpty = true };
        var id = await submissions.RecordAsync(model);
        Assert.Equal(id, await submissions.RecordAsync(model));
        Assert.Single(await submissions.ListAsync(a));
        Assert.Equal(ApplicationStatus.Applied, (await apps.GetAsync(a))!.Status);
        Assert.Equal(new DateOnly(2026, 8, 1), (await apps.GetAsync(a))!.AppliedOn);
        model.Notes = "different retry";
        await Assert.ThrowsAsync<ValidationException>(() => submissions.RecordAsync(model));
        model.Notes = null;
        await cvs.UpdateAsync(cv, new()
        {
            DisplayName = "Renamed later"
        });
        await File.WriteAllBytesAsync(source, "%PDF-1.4\nsecond"u8.ToArray(), TestContext.Current.CancellationToken);
        int newer;
        await using (var input = File.OpenRead(source))
            newer = await cvs.ImportAsync("CV.pdf", input, new()
            {
                DisplayName = "New version"
            }, TestContext.Current.CancellationToken);
        var second = await submissions.RecordAsync(new()
        {
            ApplicationId = a,
            PreparedCvId = newer,
            SubmittedOn = new(2026, 9, 1),
            SetAppliedOnIfEmpty = true
        });
        var firstFile = Assert.Single(await submissions.FilesAsync(id));
        Assert.Equal("German CV", firstFile.LabelSnapshot);
        Assert.NotEqual(firstFile.DocumentId, Assert.Single(await submissions.FilesAsync(second)).DocumentId);
        await cvs.SetArchivedAsync(cv, true);
        await docs.SetArchivedAsync(firstFile.DocumentId, true);
        Assert.Equal(id, await submissions.RecordAsync(model));
        await using (var stream = await files.OpenVerifiedAsync(firstFile.Document, TestContext.Current.CancellationToken))
        {
            using var copy = new MemoryStream();
            await stream.CopyToAsync(copy, TestContext.Current.CancellationToken);
            Assert.Equal(original, copy.ToArray());
        }
        await Assert.ThrowsAsync<ValidationException>(() => submissions.RecordAsync(new() { ApplicationId = a, PreparedCvId = cv }));
        await submissions.MarkIncorrectAsync(id, "Wrong historical date");
        Assert.True((await submissions.GetAsync(id))!.IsIncorrect);
        Assert.Single(await submissions.FilesAsync(id));
        Assert.Equal(new DateOnly(2026, 8, 1), (await submissions.GetAsync(id))!.SubmittedOn);
        Assert.Equal(new DateOnly(2026, 8, 1), (await apps.GetAsync(a))!.AppliedOn);
        var historyOnly = await submissions.RecordAsync(new()
        {
            ApplicationId = a,
            SubmittedOn = new(2026, 7, 1)
        });
        Assert.Empty(await submissions.FilesAsync(historyOnly));
    }
    [Fact]
    public async Task CvAndSubmissionFailuresLeaveNoPartialDatabaseFacts()
    {
        await using var db = await TemporaryDatabase.CreateAsync();
        var store = new DocumentFileStore(db.Paths);
        var docs = new DocumentOperations(db.Factory, store);
        var cvs = new CvOperations(db.Factory, docs);
        await Assert.ThrowsAsync<ValidationException>(() => cvs.ImportAsync("not.txt", new MemoryStream("text"u8.ToArray()), new() { DisplayName = "Invalid" }, TestContext.Current.CancellationToken));
        await using var ctx = await db.Factory.CreateDbContextAsync(TestContext.Current.CancellationToken);
        await ctx.Database.ExecuteSqlRawAsync("CREATE TRIGGER RejectCv BEFORE INSERT ON PreparedCvs BEGIN SELECT RAISE(ABORT, 'Test'); END;", TestContext.Current.CancellationToken);
        var ex = await Assert.ThrowsAsync<DocumentImportException>(() => cvs.ImportAsync("CV.pdf", new MemoryStream("%PDF-1.4\nretained"u8.ToArray()), new() { DisplayName = "Failed" }, TestContext.Current.CancellationToken));
        Assert.True(File.Exists(store.Resolve(ex.RelativePath)));
        Assert.Empty(await ctx.Documents.ToListAsync(TestContext.Current.CancellationToken));
        var c = await db.Companies.CreateAsync(new()
        {
            Name = "Company"
        });
        var p = await db.Positions.CreateAsync(new()
        {
            CompanyId = c,
            Title = "Position"
        });
        var apps = new ApplicationOperations(db.Factory);
        var a = await apps.CreateAsync(new()
        {
            PositionId = p
        });
        await ctx.Database.ExecuteSqlRawAsync("CREATE TRIGGER RejectSubmissionEvent BEFORE INSERT ON TimelineEvents BEGIN SELECT RAISE(ABORT, 'Test'); END;", TestContext.Current.CancellationToken);
        await Assert.ThrowsAsync<DbUpdateException>(() => new SubmissionOperations(db.Factory, store).RecordAsync(new() { ApplicationId = a, SetAppliedStatus = true, SetAppliedOnIfEmpty = true }));
        Assert.Empty(await ctx.Submissions.ToListAsync(TestContext.Current.CancellationToken));
        Assert.Equal(ApplicationStatus.Planned, (await apps.GetAsync(a))!.Status);
        Assert.Null((await apps.GetAsync(a))!.AppliedOn);
    }
    [Fact]
    public async Task SQLiteAllowsOnlyOneCvPerSubmissionAndRestrictsFileDeletion()
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
        var store = new DocumentFileStore(db.Paths);
        var docs = new DocumentOperations(db.Factory, store);
        var cvs = new CvOperations(db.Factory, docs);
        var first = await cvs.ImportAsync("one.pdf", new MemoryStream("%PDF-1.4\none"u8.ToArray()), new()
        {
            DisplayName = "One"
        }, TestContext.Current.CancellationToken);
        var second = await cvs.ImportAsync("two.pdf", new MemoryStream("%PDF-1.4\ntwo"u8.ToArray()), new()
        {
            DisplayName = "Two"
        }, TestContext.Current.CancellationToken);
        var submissions = new SubmissionOperations(db.Factory, store);
        var id = await submissions.RecordAsync(new()
        {
            ApplicationId = a,
            PreparedCvId = first
        });
        await using var ctx = await db.Factory.CreateDbContextAsync(TestContext.Current.CancellationToken);
        ctx.SubmissionFiles.Add(new()
        {
            SubmissionId = id,
            DocumentId = (await cvs.GetAsync(second))!.DocumentId,
            Role = SubmissionFileRole.CV,
            LabelSnapshot = "Second CV"
        });
        await Assert.ThrowsAsync<DbUpdateException>(() => ctx.SaveChangesAsync(TestContext.Current.CancellationToken));
        ctx.ChangeTracker.Clear();
        var d = (await submissions.FilesAsync(id)).Single().Document;
        ctx.Documents.Remove(d);
        await Assert.ThrowsAsync<DbUpdateException>(() => ctx.SaveChangesAsync(TestContext.Current.CancellationToken));
    }
}
