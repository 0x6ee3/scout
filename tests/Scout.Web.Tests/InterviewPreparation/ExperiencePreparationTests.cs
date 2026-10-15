using System.ComponentModel.DataAnnotations;
using Microsoft.EntityFrameworkCore;
using Scout.Web.Domain;
using Scout.Web.Features.Experiences;
using Scout.Web.Features.Interviews;
using Scout.Web.Features.InterviewPreparation;
using Scout.Web.Tests.Data;

namespace Scout.Web.Tests.InterviewPreparation;

public sealed class ExperiencePreparationTests
{
    [Fact]
    public async Task LiveStoriesHaveIndependentNotesAndPreservedSelections()
    {
        await using var fixture = await TemporaryDatabase.CreateAsync(); var (first, second) = await PreparationTests.Rounds(fixture);
        var library = new ExperienceOperations(fixture.Factory); var ops = new PreparationOperations(fixture.Factory);
        var source = await library.SaveAsync(null, new() { Title = "Story", Situation = "Initial" });
        await ops.SelectExperiencesAsync(first, [source, source]); await ops.SelectExperiencesAsync(first, [source]); await ops.SelectExperiencesAsync(second, [source]);
        var row = Assert.Single(await ops.ExperiencesAsync(first));
        await ops.SaveExperienceAsync(first, row.Id, new() { PreparationNotes = "  Private\nrehearsal  " });
        await library.SaveAsync(source, new() { Title = "Revised", Situation = "Current STAR" });
        var updated = Assert.Single(await ops.ExperiencesAsync(first)); Assert.Equal("Current STAR", updated.ProfessionalExperience.Situation); Assert.Equal("  Private\nrehearsal  ", updated.PreparationNotes);
        Assert.Null(Assert.Single(await ops.ExperiencesAsync(second)).PreparationNotes);
        await library.SetArchivedAsync(source, true); Assert.True(Assert.Single(await ops.ExperiencesAsync(first)).ProfessionalExperience.IsArchived);
        await ops.SaveExperienceAsync(first, row.Id, new() { PreparationNotes = "Still editable" });
        await ops.ArchiveExperienceAsync(first, row.Id, true);
        await Assert.ThrowsAsync<ValidationException>(() => ops.ArchiveExperienceAsync(first, row.Id, false));
        await library.SetArchivedAsync(source, false); await ops.SelectExperiencesAsync(first, [source]);
        Assert.Equal(row.Id, Assert.Single(await ops.ExperiencesAsync(first)).Id);
        Assert.Equal("Still editable", Assert.Single(await ops.ExperiencesAsync(first)).PreparationNotes);
    }

    [Fact]
    public async Task ScopeOrderingActiveParentAndDatabaseUniquenessAreEnforced()
    {
        await using var fixture = await TemporaryDatabase.CreateAsync(); var (first, second) = await PreparationTests.Rounds(fixture);
        var library = new ExperienceOperations(fixture.Factory); var ops = new PreparationOperations(fixture.Factory);
        var a = await library.SaveAsync(null, new() { Title = "A" }); var b = await library.SaveAsync(null, new() { Title = "B" });
        await Assert.ThrowsAsync<ValidationException>(() => ops.SelectExperiencesAsync(first, [a, int.MaxValue])); Assert.Empty(await ops.ExperiencesAsync(first));
        await ops.SelectExperiencesAsync(first, [a, b]); var row = (await ops.ExperiencesAsync(first))[0];
        await Assert.ThrowsAsync<ValidationException>(() => ops.SaveExperienceAsync(second, row.Id, new()));
        await Assert.ThrowsAsync<ValidationException>(() => ops.ArchiveExperienceAsync(second, row.Id, true));
        await Assert.ThrowsAsync<ValidationException>(() => ops.MoveExperienceAsync(second, row.Id, 1));
        await ops.MoveExperienceAsync(first, row.Id, 1); Assert.Equal(new[] { b, a }, (await ops.ExperiencesAsync(first)).Select(x => x.ProfessionalExperienceId));
        await using(var db = await fixture.Factory.CreateDbContextAsync(TestContext.Current.CancellationToken)) { db.InterviewExperiences.Add(new() { InterviewRoundId = first, ProfessionalExperienceId = a }); await Assert.ThrowsAsync<DbUpdateException>(() => db.SaveChangesAsync(TestContext.Current.CancellationToken)); }
        await new InterviewOperations(fixture.Factory).SetArchivedAsync(first, true);
        await Assert.ThrowsAsync<ValidationException>(() => ops.SelectExperiencesAsync(first, [a]));
        await ops.SaveExperienceAsync(first, row.Id, new() { PreparationNotes = "Saved under archived round" });
        await Assert.ThrowsAsync<ValidationException>(() => ops.SaveExperienceAsync(first, row.Id, new() { PreparationNotes = new string('x', 100001) }));
    }
}
