using System.ComponentModel.DataAnnotations;
using Microsoft.EntityFrameworkCore;
using Scout.Web.Domain;
using Scout.Web.Features.InterviewWorkspace;
using Scout.Web.Features.InterviewPreparation;
using Scout.Web.Features.Interviews;
using Scout.Web.Tests.InterviewPreparation;
using Scout.Web.Tests.Data;
namespace Scout.Web.Tests.InterviewWorkspace;

public sealed class CaptureTests
{
    [Fact]
    public async Task CaptureProtectsStaleFormsAndRetriesAndPreservesCanonicalFields()
    {
        await using var f = await TemporaryDatabase.CreateAsync();
        var (first, second) = await PreparationTests.Rounds(f);
        var ops = new WorkspaceOperations(f.Factory);
        await ops.ReviewedApproveAsync(first, new()
        {
            InterviewNotes = true
        });
        var token = Guid.NewGuid();
        var id = await ops.AddQuestionAsync(first, 1, token, "Live question");
        Assert.Equal(id, await ops.AddQuestionAsync(first, 1, token, "Live question"));
        await Assert.ThrowsAsync<ValidationException>(() => ops.AddQuestionAsync(first, 1, token, "Different"));
        var save = await ops.SaveQuestionAsync(first, 1, id, new(0, true, true, true, "Answer"));
        Assert.True(save.Saved);
        Assert.Equal(1, save.Revision);
        var retry = await ops.SaveQuestionAsync(first, 1, id, new(0, true, true, true, "Answer"));
        Assert.True(retry.Saved);
        Assert.Equal(1, retry.Revision);
        var conflict = await ops.SaveQuestionAsync(first, 1, id, new(0, false, false, true, "Stale"));
        Assert.True(conflict.Conflict);
        Assert.Equal("Answer", conflict.Text);
        var prep = new PreparationOperations(f.Factory);
        await Assert.ThrowsAsync<ValidationException>(() => prep.SaveQuestionAsync(first, id, new() { Text = "Stale wording" }));
        await prep.SaveQuestionAsync(first, id, new()
        {
            ContentRevision = 1,
            Text = "Private edit",
            Answer = "Private answer"
        });
        Assert.True((await ops.SaveQuestionAsync(first, 1, id, new(1, true, true, true, "Stale workspace"))).Conflict);
        var notes = await ops.SaveNotesAsync(first, 1, 0, "Live notes");
        Assert.True(notes.Saved);
        var rounds = new InterviewOperations(f.Factory);
        var r = (await rounds.GetAsync(first))!;
        await Assert.ThrowsAsync<ValidationException>(() => rounds.SaveAsync(first, new() { ApplicationId = r.ApplicationId, Title = r.Title, InterviewNotes = "Stale notes" }));
        Assert.True((await ops.SaveNotesAsync(first, 1, 0, "Other notes")).Conflict);
        var view = (await ops.GetAsync(first))!;
        Assert.Equal("Live notes", view.InterviewNotes);
        Assert.Equal("Live question", Assert.Single(view.Questions).Text);
        Assert.Equal(InterviewQuestionOrigin.AdHoc, view.Questions[0].Origin);
        await Assert.ThrowsAsync<ValidationException>(() => ops.SaveQuestionAsync(second, 1, id, new(0, false, false, true, "Foreign")));
        await prep.ArchiveQuestionAsync(first, id, true);
        Assert.Null(await ops.GetAsync(first));
        Assert.False(await ops.IsCurrentAsync(first, 1));
        await prep.ArchiveQuestionAsync(first, id, false);
        Assert.Null(await ops.GetAsync(first));
        await using var db = await f.Factory.CreateDbContextAsync(TestContext.Current.CancellationToken);
        Assert.Equal(2, await db.Set<InterviewWorkspaceConfiguration>().Where(x => x.InterviewRoundId == first).Select(x => x.ConfigurationRevision).SingleAsync(TestContext.Current.CancellationToken));
    }
    [Fact]
    public async Task DisabledFieldsArchivesAndNewApprovalRejectWrites()
    {
        await using var f = await TemporaryDatabase.CreateAsync();
        var (first, _) = await PreparationTests.Rounds(f);
        var ops = new WorkspaceOperations(f.Factory);
        await ops.ReviewedApproveAsync(first, new());
        var id = await ops.AddQuestionAsync(first, 1, Guid.NewGuid(), "Question");
        await Assert.ThrowsAsync<ValidationException>(() => ops.SaveNotesAsync(first, 1, 0, "Hidden"));
        await using (var db = await f.Factory.CreateDbContextAsync(TestContext.Current.CancellationToken))
        {
            var included = await db.Set<WorkspaceQuestion>().SingleAsync(TestContext.Current.CancellationToken);
            included.AnswerEnabled = false;
            await db.SaveChangesAsync(TestContext.Current.CancellationToken);
        }
        await Assert.ThrowsAsync<ValidationException>(() => ops.SaveQuestionAsync(first, 1, id, new(0, true, false, true, "Hidden")));
        Assert.True((await ops.SaveQuestionAsync(first, 1, id, new(0, true, false, false, null))).Saved);
        await ops.ReviewedApproveAsync(first, new()
        {
            ExpectedRevision = 1
        });
        await Assert.ThrowsAsync<ValidationException>(() => ops.SaveQuestionAsync(first, 1, id, new(1, false, false, false, null)));
        await new InterviewOperations(f.Factory).SetArchivedAsync(first, true);
        Assert.True((await ops.GetAsync(first))!.ReadOnly);
        await Assert.ThrowsAsync<ValidationException>(() => ops.AddQuestionAsync(first, 2, Guid.NewGuid(), "Archived"));
    }
}
