using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;
using Scout.Web.Data;
using Scout.Web.Features.Questions;
using Scout.Web.Features.Experiences;
using Scout.Web.Features.InterviewPreparation;
using Scout.Web.Tests.Data;

namespace Scout.Web.Tests.InterviewWorkspace;

using Scout.Web.Tests.InterviewPreparation;
using Scout.Web.Features.InterviewWorkspace;

public sealed class WorkspaceRenderTests
{
    [Fact]
    public async Task ManagementPagesRenderBothSectionsEncodeTextAndRejectForeignItemRoutes()
    {
        await using var fixture = await TemporaryDatabase.CreateAsync();
        var (first, second) = await PreparationTests.Rounds(fixture);
        var questions = new QuestionOperations(fixture.Factory);
        var experiences = new ExperienceOperations(fixture.Factory);
        var preparation = new PreparationOperations(fixture.Factory);
        var q = await questions.SaveAsync(null, new()
        {
            Text = "<script>fictional</script>",
            Tags = ["team-conflict"]
        });
        var e = await experiences.SaveAsync(null, new()
        {
            Title = "STAR story",
            Situation = "<b>plain text</b>"
        });
        await preparation.SelectQuestionsAsync(first, [q]);
        await preparation.SelectQuestionsAsync(second, [q]);
        await preparation.SelectExperiencesAsync(first, [e]);
        var foreign = Assert.Single(await preparation.QuestionsAsync(second));
        await preparation.SaveQuestionAsync(second, foreign.Id, new()
        {
            Text = "Other round secret"
        });
        await using var host = new WebApplicationFactory<Program>().WithWebHostBuilder(b => { b.UseEnvironment("Testing"); b.ConfigureServices(s => { s.Replace(ServiceDescriptor.Singleton(fixture.Paths)); s.Replace(ServiceDescriptor.Singleton<IDbContextFactory<ScoutDbContext>>(fixture.Factory)); s.Replace(ServiceDescriptor.Scoped<NavigationManager, RenderNavigation>()); }); });
        await using (var db = await fixture.Factory.CreateDbContextAsync(TestContext.Current.CancellationToken))
        {
            var position = await db.Interviews.Where(x => x.Id == first).Select(x => x.Application.PositionId).SingleAsync(TestContext.Current.CancellationToken);
            var otherApplication = await new Scout.Web.Features.Applications.ApplicationOperations(fixture.Factory).CreateAsync(new()
            {
                PositionId = position,
                AttemptLabel = "Private application"
            });
            var otherRound = await new Scout.Web.Features.Interviews.InterviewOperations(fixture.Factory).SaveAsync(null, new()
            {
                ApplicationId = otherApplication,
                Title = "OTHER_APPLICATION_CANARY",
                InterviewNotes = "OTHER_APPLICATION_NOTES"
            });
            await preparation.SelectQuestionsAsync(otherRound, [q]);
            var otherQuestion = Assert.Single(await preparation.QuestionsAsync(otherRound));
            await preparation.SaveQuestionAsync(otherRound, otherQuestion.Id, new()
            {
                Text = "OTHER_APPLICATION_QUESTION",
                Answer = "OTHER_APPLICATION_ANSWER"
            });
            var guard = new WorkspaceOperations(fixture.Factory);
            await Assert.ThrowsAsync<System.ComponentModel.DataAnnotations.ValidationException>(() => guard.ReviewedApproveAsync(first, new() { PriorAnswerIds = [otherQuestion.Id] }));
        }
        // Scout disables prerendering: HTTP returns the interactive bootstrap, not page content.
        // Render components with the real host services to verify their management content.
        await using var scope = host.Services.CreateAsyncScope();
        await using var renderer = new HtmlRenderer(scope.ServiceProvider, scope.ServiceProvider.GetRequiredService<ILoggerFactory>());
        var operations = new WorkspaceOperations(fixture.Factory);
        var selected = Assert.Single(await preparation.QuestionsAsync(first));
        await operations.ReviewedApproveAsync(first, new()
        {
            Questions = [new() { Id = selected.Id, Include = true }]
        });
        var page = await renderer.Dispatcher.InvokeAsync(async () => (await renderer.RenderComponentAsync<Scout.Web.Components.Pages.Interviews.Workspace>(ParameterView.FromDictionary(new Dictionary<string, object?> { ["RoundKey"] = first.ToString() }))).ToHtmlString());
        Assert.Contains("&lt;script&gt;fictional&lt;/script&gt;", page);
        Assert.DoesNotContain("Other round secret", page);
        Assert.DoesNotContain("/companies", page);
        Assert.DoesNotContain("STAR story", page);
        var review = await renderer.Dispatcher.InvokeAsync(async () => (await renderer.RenderComponentAsync<Scout.Web.Components.Pages.Interviews.WorkspaceSetup>(ParameterView.FromDictionary(new Dictionary<string, object?> { ["Id"] = first }))).ToHtmlString());
        Assert.Contains("Review Interview Workspace", review);
        Assert.Contains("STAR story", review);
        var wrong = await renderer.Dispatcher.InvokeAsync(async () => (await renderer.RenderComponentAsync<Scout.Web.Components.Pages.Interviews.Workspace>(ParameterView.FromDictionary(new Dictionary<string, object?> { ["RoundKey"] = "invalid", ["UnknownPath"] = "wrong" }))).ToHtmlString());
        Assert.Contains("Workspace unavailable", wrong);
        Assert.DoesNotContain("/companies", wrong);
    }

    private sealed class RenderNavigation : NavigationManager
    {
        public RenderNavigation() => Initialize("http://localhost:5180/", "http://localhost:5180/");
        protected override void NavigateToCore(string uri, bool forceLoad) => throw new NotSupportedException("Static rendering does not navigate.");
    }
}
