using System.ComponentModel.DataAnnotations;
using Microsoft.EntityFrameworkCore;
using Scout.Web.Domain;
using Scout.Web.Features.Applications;
using Scout.Web.Features.Interviews;
using Scout.Web.Features.InterviewPreparation;
using Scout.Web.Features.Questions;
using Scout.Web.Tests.Data;

namespace Scout.Web.Tests.InterviewPreparation;

public sealed class PreparationTests
{
    internal static async Task<(int First, int Second)> Rounds(TemporaryDatabase fixture)
    {
        var company = await fixture.Companies.CreateAsync(new() { Name = "Fictional employer" });
        var position = await fixture.Positions.CreateAsync(new() { CompanyId = company, Title = "Engineer" });
        var app = await new ApplicationOperations(fixture.Factory).CreateAsync(new() { PositionId = position });
        var rounds = new InterviewOperations(fixture.Factory);
        return (await rounds.SaveAsync(null, new() { ApplicationId = app, Title = "First" }), await rounds.SaveAsync(null, new() { ApplicationId = app, Title = "Second" }));
    }

    [Fact]
    public async Task CopiesAreIndependentAndDeletionPreservesTheirContentAndTimestamp()
    {
        await using var fixture = await TemporaryDatabase.CreateAsync(); var (first, second) = await Rounds(fixture);
        var library = new QuestionOperations(fixture.Factory); var ops = new PreparationOperations(fixture.Factory);
        var source = await library.SaveAsync(null, new() { Text = "Original", Intent = "Intent", FollowUpQuestions = "Follow", Notes = "Secret library notes", Tags = ["team"] });
        await ops.SelectQuestionsAsync(first, [source, source]); await ops.SelectQuestionsAsync(first, [source]); await ops.SelectQuestionsAsync(second, [source]);
        var row = Assert.Single(await ops.QuestionsAsync(first)); Assert.Null(row.PreparationNotes); Assert.Null(row.Answer);
        await ops.SaveQuestionAsync(first, row.Id, new() { Text = "Local wording", Answer = "  Answer\nkept  ", PreparationNotes = "Private rehearsal" });
        var saved = Assert.Single(await ops.QuestionsAsync(first));
        await library.SaveAsync(source, new() { Text = "Changed library" });
        Assert.Equal("Original", Assert.Single(await ops.QuestionsAsync(second)).Text);
        Assert.Equal(2, await library.SelectionCountAsync(source));
        await library.DeleteAsync(source);
        var detached = Assert.Single(await ops.QuestionsAsync(first));
        Assert.Null(detached.EmployerQuestionId); Assert.Equal("Local wording", detached.Text); Assert.Equal(saved.UpdatedAtUtc, detached.UpdatedAtUtc); Assert.Equal("  Answer\nkept  ", detached.Answer);
        await ops.ArchiveQuestionAsync(first, detached.Id, true); await ops.ArchiveQuestionAsync(first, detached.Id, false);
        Assert.Equal("Private rehearsal", Assert.Single(await ops.QuestionsAsync(first)).PreparationNotes);
        await Assert.ThrowsAsync<ValidationException>(() => ops.SelectQuestionsAsync(first, [source]));
    }

    [Fact]
    public async Task OwnershipArchivesOrderingAndInvalidMultiAddAreEnforced()
    {
        await using var fixture = await TemporaryDatabase.CreateAsync(); var (first, second) = await Rounds(fixture);
        var library = new QuestionOperations(fixture.Factory); var ops = new PreparationOperations(fixture.Factory);
        var a = await library.SaveAsync(null, new() { Text = "A" }); var b = await library.SaveAsync(null, new() { Text = "B" });
        await Assert.ThrowsAsync<ValidationException>(() => ops.SelectQuestionsAsync(first, [a, int.MaxValue])); Assert.Empty(await ops.QuestionsAsync(first));
        await ops.SelectQuestionsAsync(first, [a, b]); var rows = await ops.QuestionsAsync(first); var aid = rows[0].Id;
        await Assert.ThrowsAsync<ValidationException>(() => ops.SaveQuestionAsync(second, aid, new() { Text = "Wrong owner" }));
        await Assert.ThrowsAsync<ValidationException>(() => ops.ArchiveQuestionAsync(second, aid, true));
        await Assert.ThrowsAsync<ValidationException>(() => ops.MoveQuestionAsync(second, aid, 1));
        await ops.MoveQuestionAsync(first, aid, 1); Assert.Equal(new[] { b, a }, (await ops.QuestionsAsync(first)).Select(x => x.EmployerQuestionId!.Value));
        await ops.ArchiveQuestionAsync(first, aid, true); await library.SetArchivedAsync(a, true);
        await Assert.ThrowsAsync<ValidationException>(() => ops.ArchiveQuestionAsync(first, aid, false));
        await library.SetArchivedAsync(a, false); await ops.SelectQuestionsAsync(first, [a]);
        Assert.Equal(aid, (await ops.QuestionsAsync(first)).Single(x => x.EmployerQuestionId == a).Id);
        await new InterviewOperations(fixture.Factory).SetArchivedAsync(first, true);
        await Assert.ThrowsAsync<ValidationException>(() => ops.SelectQuestionsAsync(first, [b]));
        await ops.SaveQuestionAsync(first, aid, new() { Text = "Still editable", Answer = "Saved" });
    }

    [Fact]
    public async Task DatabaseEnforcesSelectionIdentityAndDeletionRollback()
    {
        await using var fixture = await TemporaryDatabase.CreateAsync(); var (first, _) = await Rounds(fixture);
        var library = new QuestionOperations(fixture.Factory); var ops = new PreparationOperations(fixture.Factory);
        var source = await library.SaveAsync(null, new() { Text = "A", Tags = ["keep"] }); await ops.SelectQuestionsAsync(first, [source]);
        await using(var db = await fixture.Factory.CreateDbContextAsync(TestContext.Current.CancellationToken))
        {
            db.InterviewQuestions.Add(new() { InterviewRoundId = first, EmployerQuestionId = source, Text = "Duplicate" });
            await Assert.ThrowsAsync<DbUpdateException>(() => db.SaveChangesAsync(TestContext.Current.CancellationToken));
        }
        await using(var db = await fixture.Factory.CreateDbContextAsync(TestContext.Current.CancellationToken))
            await db.Database.ExecuteSqlRawAsync("CREATE TRIGGER block_question_delete BEFORE DELETE ON EmployerQuestions BEGIN SELECT RAISE(ABORT, 'test rollback'); END;", TestContext.Current.CancellationToken);
        await Assert.ThrowsAsync<DbUpdateException>(() => library.DeleteAsync(source));
        Assert.Single((await library.GetAsync(source))!.Tags); Assert.Equal(source, Assert.Single(await ops.QuestionsAsync(first)).EmployerQuestionId);
    }
}
