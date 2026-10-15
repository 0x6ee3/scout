using System.ComponentModel.DataAnnotations;
using Microsoft.EntityFrameworkCore;
using Scout.Web.Features;
using Scout.Web.Features.Questions;
using Scout.Web.Tests.Data;

namespace Scout.Web.Tests.Questions;

public sealed class QuestionTests
{
    [Theory]
    [InlineData("team conflict")][InlineData("team--conflict")][InlineData("-team")][InlineData("team_")][InlineData("")]
    public void RejectsInvalidTags(string value) => Assert.Throws<ValidationException>(() => Tags.Normalize([value]));

    [Fact]
    public async Task CatalogPreservesTextFiltersAndNormalizesTags()
    {
        await using var fixture = await TemporaryDatabase.CreateAsync();
        var ops = new QuestionOperations(fixture.Factory);
        var id = await ops.SaveAsync(null, new() { Text = "  Wie ist die Führung?  ", Intent = "  lines\nkeep  ", Notes = "<script>literal</script>", Tags = [" Team-Conflict ", "team-conflict", "FÜHRUNG", "c-sharp"] });
        var row = (await ops.GetAsync(id))!;
        Assert.Equal("Wie ist die Führung?", row.Text); Assert.Equal("  lines\nkeep  ", row.Intent);
        Assert.Equal(3, row.Tags.Count);
        Assert.Single(await ops.ListAsync(search: "FÜHRUNG", tag: "TEAM-CONFLICT"));
        Assert.Single(await ops.ListAsync(search: "<script>"));
        Assert.Empty(await ops.ListAsync(search: "%"));
        await ops.SetArchivedAsync(id, true);
        Assert.Empty(await ops.ListAsync()); Assert.Single(await ops.ListAsync(ArchiveFilter.Archived));
        await ops.SetArchivedAsync(id, false);
        await ops.SaveAsync(id, new() { Text = "Updated", Tags = ["leadership"] });
        Assert.Equal(new[] { "leadership" }, await new TagOperations(fixture.Factory).SuggestionsAsync());
        Assert.True(await ops.DeleteAsync(id)); Assert.Null(await ops.GetAsync(id));
        await using var db = await fixture.Factory.CreateDbContextAsync(TestContext.Current.CancellationToken);
        Assert.Empty(await db.EmployerQuestionTags.ToListAsync(TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task InvalidChangesDoNotPartiallySave()
    {
        await using var fixture = await TemporaryDatabase.CreateAsync(); var ops = new QuestionOperations(fixture.Factory);
        var id = await ops.SaveAsync(null, new() { Text = "Original", Tags = ["team"] });
        await Assert.ThrowsAsync<ValidationException>(() => ops.SaveAsync(id, new() { Text = "Changed", Tags = ["bad tag"] }));
        await Assert.ThrowsAsync<ValidationException>(() => ops.SaveAsync(null, new() { Text = "   " }));
        await Assert.ThrowsAsync<ValidationException>(() => ops.SaveAsync(null, new() { Text = new string('x', 4001) }));
        Assert.Equal("Original", (await ops.GetAsync(id))!.Text);
        Assert.Throws<ValidationException>(() => Tags.Normalize(Enumerable.Range(0, 21).Select(x => $"tag-{x}")));
        Assert.Equal(Tags.Color("team"), Tags.Color("team"));
    }
}
