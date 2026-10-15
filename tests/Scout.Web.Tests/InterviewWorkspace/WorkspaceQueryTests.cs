using System.Data.Common;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Scout.Web.Data;
using Scout.Web.Features.InterviewWorkspace;
using Scout.Web.Features.InterviewPreparation;
using Scout.Web.Features.Questions;
using Scout.Web.Tests.Data;
using Scout.Web.Tests.InterviewPreparation;
namespace Scout.Web.Tests.InterviewWorkspace;

public sealed class WorkspaceQueryTests
{
    [Fact]
    public async Task SharedReadsAndDeniedWritesNeverSelectPrivateSourceEntitiesOrHiddenNotes()
    {
        await using var f = await TemporaryDatabase.CreateAsync();
        var (first, _) = await PreparationTests.Rounds(f);
        var preparation = new PreparationOperations(f.Factory);
        var q = await new QuestionOperations(f.Factory).SaveAsync(null, new()
        {
            Text = "Included question",
            Notes = "CANARY library notes"
        });
        await preparation.SelectQuestionsAsync(first, [q]);
        var selected = Assert.Single(await preparation.QuestionsAsync(first));
        await preparation.SaveQuestionAsync(first, selected.Id, new()
        {
            Text = "Included question",
            Answer = "CANARY answer",
            PreparationNotes = "CANARY rehearsal"
        });
        var ops = new WorkspaceOperations(f.Factory);
        await ops.ReviewedApproveAsync(first, new()
        {
            Questions = [new() { Id = selected.Id, Include = true }]
        });
        var recorder = new Queries();
        var options = new DbContextOptionsBuilder<ScoutDbContext>().UseSqlite(new SqliteConnectionStringBuilder(ScoutDatabase.ConnectionString(f.Paths)) { Pooling = false }.ToString()).AddInterceptors(recorder).Options;
        var guarded = new WorkspaceOperations(new Factory(options));
        var view = (await guarded.GetAsync(first))!;
        Assert.Null(view.InterviewNotes);
        Assert.Null(Assert.Single(view.Questions).Answer);
        var sql = string.Join("\n", recorder.Commands);
        Assert.DoesNotContain("PersonalNotes", sql);
        Assert.DoesNotContain("LocationOrMeetingUrl", sql);
        Assert.DoesNotContain("FROM \"ProfessionalExperiences\"", sql);
        Assert.DoesNotContain("FROM \"EmployerQuestions\"", sql);
        // Answer reads are restricted to enabled inclusions in SQL, rather than selecting all answers and hiding them.
        foreach (var command in recorder.Commands.Where(x => x.Contains(".\"Answer\"", StringComparison.Ordinal) && x.Contains("JOIN \"InterviewQuestions\"", StringComparison.Ordinal)))
            Assert.Contains("\"AnswerEnabled\"", command);
        Assert.DoesNotContain(recorder.Commands, x => x.Contains("SELECT \"i\".\"InterviewNotes\"", StringComparison.Ordinal));
        recorder.Commands.Clear();
        await Assert.ThrowsAsync<System.ComponentModel.DataAnnotations.ValidationException>(() => guarded.SaveQuestionAsync(first, 1, selected.Id, new(1, true, false, true, "Hidden write")));
        Assert.DoesNotContain(recorder.Commands, x => x.Contains(".\"Answer\"", StringComparison.Ordinal));
    }
    private sealed class Factory(DbContextOptions<ScoutDbContext> options) : IDbContextFactory<ScoutDbContext>
    {
        public ScoutDbContext CreateDbContext() => new(options);
    }
    private sealed class Queries : DbCommandInterceptor
    {
        public List<string> Commands { get; } = [];
        public override ValueTask<InterceptionResult<DbDataReader>> ReaderExecutingAsync(DbCommand command, CommandEventData eventData, InterceptionResult<DbDataReader> result, CancellationToken cancellationToken = default)
        {
            Commands.Add(command.CommandText);
            return base.ReaderExecutingAsync(command, eventData, result, cancellationToken);
        }
    }
}
