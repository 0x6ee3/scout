using System.ComponentModel.DataAnnotations;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Scout.Web.Domain;
using Scout.Web.Features.InterviewWorkspace;
using Scout.Web.Features.InterviewPreparation;
using Scout.Web.Features.Questions;
using Scout.Web.Features.Experiences;
using Scout.Web.Tests.Data;
using Scout.Web.Tests.InterviewPreparation;
namespace Scout.Web.Tests.InterviewWorkspace;

public sealed class WorkspaceTests
{
    [Fact]
    public async Task DefaultsExcludeEverythingAndApprovalFreezesOnlyChosenFields()
    {
        await using var f = await TemporaryDatabase.CreateAsync();
        var (round, other) = await PreparationTests.Rounds(f);
        var library = new QuestionOperations(f.Factory);
        var prep = new PreparationOperations(f.Factory);
        var ops = new WorkspaceOperations(f.Factory);
        var q = await library.SaveAsync(null, new()
        {
            Text = "Approved wording",
            Intent = "EXCLUDED_INTENT",
            Notes = "EXCLUDED_LIBRARY",
            Tags = ["team"]
        });
        await prep.SelectQuestionsAsync(round, [q]);
        await prep.SelectQuestionsAsync(other, [q]);
        var selected = Assert.Single(await prep.QuestionsAsync(round));
        await prep.SaveQuestionAsync(round, selected.Id, new()
        {
            Text = selected.Text,
            Answer = "EXCLUDED_ANSWER",
            PreparationNotes = "EXCLUDED_PREPARATION"
        });
        var experiences = new ExperienceOperations(f.Factory);
        var e = await experiences.SaveAsync(null, new()
        {
            Title = "Approved title",
            Situation = "Approved situation",
            Reflection = "EXCLUDED_REFLECTION"
        });
        await prep.SelectExperiencesAsync(round, [e]);
        var selection = Assert.Single(await prep.ExperiencesAsync(round));
        Assert.Null(await ops.GetAsync(round));
        await ops.ReviewedApproveAsync(round, new());
        var empty = await ops.GetAsync(round);
        Assert.NotNull(empty);
        Assert.Empty(empty.Questions);
        Assert.Null(empty.RoundTitle);
        Assert.False(empty.NotesEnabled);
        Assert.Null(empty.InterviewNotes);
        await ops.ReviewedApproveAsync(round, new()
        {
            ExpectedRevision = 1,
            Questions = [new() { Id = selected.Id, Include = true }],
            Experiences = [new() { Id = selection.Id, Include = true, Situation = true }]
        });
        await library.SaveAsync(q, new()
        {
            Text = "New library wording"
        });
        await experiences.SaveAsync(e, new()
        {
            Title = "Changed",
            Situation = "Changed",
            Reflection = "New private text"
        });
        var view = await ops.GetAsync(round);
        Assert.NotNull(view);
        Assert.Equal("Approved wording", Assert.Single(view.Questions).Text);
        Assert.Equal("Approved situation", Assert.Single(view.Experiences).Situation);
        Assert.DoesNotContain("EXCLUDED", JsonSerializer.Serialize(view));
        Assert.Null(await ops.GetAsync(round, 1));
        Assert.Null(await ops.GetAsync(other));
        await library.DeleteAsync(q);
        Assert.NotNull(await ops.GetAsync(round));
        await prep.ArchiveQuestionAsync(round, selected.Id, true);
        Assert.Null(await ops.GetAsync(round));
    }
    [Fact]
    public async Task ForeignSelectionsAndStaleReviewRollBackTheWholeApproval()
    {
        await using var f = await TemporaryDatabase.CreateAsync();
        var (first, second) = await PreparationTests.Rounds(f);
        var ops = new WorkspaceOperations(f.Factory);
        var prep = new PreparationOperations(f.Factory);
        var q = await new QuestionOperations(f.Factory).SaveAsync(null, new()
        {
            Text = "Other round"
        });
        await prep.SelectQuestionsAsync(second, [q]);
        var foreign = Assert.Single(await prep.QuestionsAsync(second));
        await Assert.ThrowsAsync<ValidationException>(() => ops.ReviewedApproveAsync(first, new() { Questions = [new() { Id = foreign.Id, Include = true }] }));
        Assert.Null(await ops.GetAsync(first));
        await ops.ReviewedApproveAsync(first, new());
        await Assert.ThrowsAsync<ValidationException>(() => ops.ReviewedApproveAsync(first, new()));
        Assert.Equal(1, (await ops.GetAsync(first))!.Revision);
        await Assert.ThrowsAsync<ValidationException>(() => ops.ReviewedApproveAsync(first, new() { ExpectedRevision = 1, CvKey = "1/999999" }));
        Assert.Equal(1, (await ops.GetAsync(first))!.Revision);
        await Assert.ThrowsAsync<ValidationException>(() => ops.ReviewedApproveAsync(first, new() { ExpectedRevision = 1, PriorAnswerIds = [foreign.Id] }));
    }
    [Fact]
    public async Task DatabaseRejectsCrossRoundInclusionsAndHasFourWorkspaceTables()
    {
        await using var f = await TemporaryDatabase.CreateAsync();
        var (first, second) = await PreparationTests.Rounds(f);
        var ops = new WorkspaceOperations(f.Factory);
        await ops.ReviewedApproveAsync(first, new());
        await using var db = await f.Factory.CreateDbContextAsync(TestContext.Current.CancellationToken);
        var question = new InterviewQuestion { InterviewRoundId = second, Text = "Foreign" };
        db.Add(question);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);
        db.Add(new WorkspaceQuestion { InterviewRoundId = first, InterviewQuestionId = question.Id, Text = "Foreign" });
        await Assert.ThrowsAsync<DbUpdateException>(() => db.SaveChangesAsync(TestContext.Current.CancellationToken));
    }
}
