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

namespace Scout.Web.Tests.InterviewPreparation;

public sealed class PreparationHttpTests
{
    [Fact]
    public async Task ManagementPagesRenderBothSectionsEncodeTextAndRejectForeignItemRoutes()
    {
        await using var fixture = await TemporaryDatabase.CreateAsync(); var (first, second) = await PreparationTests.Rounds(fixture);
        var questions = new QuestionOperations(fixture.Factory); var experiences = new ExperienceOperations(fixture.Factory); var preparation = new PreparationOperations(fixture.Factory);
        var q = await questions.SaveAsync(null, new() { Text = "<script>fictional</script>", Tags = ["team-conflict"] });
        var e = await experiences.SaveAsync(null, new() { Title = "STAR story", Situation = "<b>plain text</b>" });
        await preparation.SelectQuestionsAsync(first, [q]); await preparation.SelectQuestionsAsync(second, [q]); await preparation.SelectExperiencesAsync(first, [e]);
        var foreign = Assert.Single(await preparation.QuestionsAsync(second)); await preparation.SaveQuestionAsync(second, foreign.Id, new() { Text = "Other round secret" });
        await using var host = new WebApplicationFactory<Program>().WithWebHostBuilder(b => { b.UseEnvironment("Testing"); b.ConfigureServices(s => { s.Replace(ServiceDescriptor.Singleton(fixture.Paths)); s.Replace(ServiceDescriptor.Singleton<IDbContextFactory<ScoutDbContext>>(fixture.Factory)); s.Replace(ServiceDescriptor.Scoped<NavigationManager, RenderNavigation>()); }); });
        // Scout disables prerendering: HTTP returns the interactive bootstrap, not page content.
        // Render components with the real host services to verify their management content.
        await using var scope = host.Services.CreateAsyncScope();
        await using var renderer = new HtmlRenderer(scope.ServiceProvider, scope.ServiceProvider.GetRequiredService<ILoggerFactory>());
        var page = await renderer.Dispatcher.InvokeAsync(async () => (await renderer.RenderComponentAsync<Scout.Web.Components.Pages.Interviews.Preparation>(ParameterView.FromDictionary(new Dictionary<string, object?> { ["Id"] = first }))).ToHtmlString());
        Assert.Contains("Questions to ask", page); Assert.Contains("STAR experiences", page); Assert.Contains("&lt;script&gt;fictional&lt;/script&gt;", page); Assert.Contains("&lt;b&gt;plain text&lt;/b&gt;", page);
        Assert.DoesNotContain("<script>fictional</script>", page); Assert.DoesNotContain("Other round secret", page);
        var wrong = await renderer.Dispatcher.InvokeAsync(async () => (await renderer.RenderComponentAsync<Scout.Web.Components.Pages.Interviews.PreparedQuestionEditor>(ParameterView.FromDictionary(new Dictionary<string, object?> { ["RoundId"] = first, ["Id"] = foreign.Id }))).ToHtmlString());
        Assert.Contains("Prepared question not found in this interview", wrong); Assert.DoesNotContain("Other round secret", wrong);
    }

    private sealed class RenderNavigation : NavigationManager
    {
        public RenderNavigation() => Initialize("http://localhost:5180/", "http://localhost:5180/");
        protected override void NavigateToCore(string uri, bool forceLoad) => throw new NotSupportedException("Static rendering does not navigate.");
    }
}
