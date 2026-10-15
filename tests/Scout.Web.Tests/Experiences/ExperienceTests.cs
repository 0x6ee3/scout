using System.ComponentModel.DataAnnotations;
using Scout.Web.Features;
using Scout.Web.Features.Questions;
using Scout.Web.Features.Experiences;
using Scout.Web.Tests.Data;

namespace Scout.Web.Tests.Experiences;

public sealed class ExperienceTests
{
    [Fact]
    public async Task OptionalStarFieldsAndDeepSearchPersist()
    {
        await using var fixture = await TemporaryDatabase.CreateAsync(); var ops = new ExperienceOperations(fixture.Factory);
        var partial = await ops.SaveAsync(null, new() { Title = "Just a title" });
        Assert.Null((await ops.GetAsync(partial))!.Result);
        var id = await ops.SaveAsync(null, new() { Title = "Conflict", MemoryCue = "Recall meeting", Situation = "  Keep\nlines  ", Task = "Own decision", Action = "FÜHRUNG", Result = "Agreed", Reflection = "Try earlier", UsefulFor = "Describe mentoring", Notes = "<b>literal</b>", Tags = ["Team-Conflict"] });
        Assert.Equal("  Keep\nlines  ", (await ops.GetAsync(id))!.Situation);
        foreach(var needle in new[] { "führung", "mentoring", "<b>", "earlier", "own decision", "agreed" }) Assert.Single(await ops.ListAsync(search: needle));
        Assert.Empty(await ops.ListAsync(search: "%")); Assert.Single(await ops.ListAsync(tag: "TEAM-CONFLICT"));
        await ops.SetArchivedAsync(id, true); Assert.Single(await ops.ListAsync()); Assert.Single(await ops.ListAsync(ArchiveFilter.Archived));
        await ops.SetArchivedAsync(id, false);
        await Assert.ThrowsAsync<ValidationException>(() => ops.SaveAsync(id, new() { Title = "  " }));
        await Assert.ThrowsAsync<ValidationException>(() => ops.SaveAsync(id, new() { Title = "Changed", MemoryCue = new string('x', 1001) }));
        Assert.Equal("Conflict", (await ops.GetAsync(id))!.Title);
    }

    [Fact]
    public async Task SuggestionsUnionBothCatalogsIncludingArchivedAndSurviveQuestionDeletion()
    {
        await using var fixture = await TemporaryDatabase.CreateAsync(); var experiences = new ExperienceOperations(fixture.Factory); var questions = new QuestionOperations(fixture.Factory);
        var q = await questions.SaveAsync(null, new() { Text = "Question", Tags = ["shared", "question-only"] });
        var e = await experiences.SaveAsync(null, new() { Title = "Example", Tags = ["SHARED", "experience-only"] });
        await experiences.SetArchivedAsync(e, true);
        var tags = new TagOperations(fixture.Factory);
        Assert.Equal(new[] { "experience-only", "question-only", "shared" }, await tags.SuggestionsAsync());
        await questions.DeleteAsync(q);
        Assert.Equal(new[] { "experience-only", "shared" }, await tags.SuggestionsAsync());
    }
}
