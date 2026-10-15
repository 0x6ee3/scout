using System.Net;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.EntityFrameworkCore;
using Scout.Web.Data;
using Scout.Web.Domain;
using Scout.Web.Features.InterviewWorkspace;
using Scout.Web.Features.Cvs;
using Scout.Web.Features.Documents;
using Scout.Web.Features.Submissions;
using Scout.Web.Storage;
using Scout.Web.Tests.Data;
using Scout.Web.Tests.InterviewPreparation;
namespace Scout.Web.Tests.InterviewWorkspace;

public sealed class WorkspaceHttpTests
{
    [Fact]
    public async Task SelectedCvUsesExactEvidenceAndNeutralErrorsForWrongStaleAndCorruptRequests()
    {
        await using var f = await TemporaryDatabase.CreateAsync();
        var (first, second) = await PreparationTests.Rounds(f);
        var ops = new WorkspaceOperations(f.Factory);
        await using var db = await f.Factory.CreateDbContextAsync(TestContext.Current.CancellationToken);
        var application = await db.Interviews.Where(x => x.Id == first).Select(x => x.ApplicationId).SingleAsync(TestContext.Current.CancellationToken);
        var store = new DocumentFileStore(f.Paths);
        var documents = new DocumentOperations(f.Factory, store);
        var cvs = new CvOperations(f.Factory, documents);
        var bytes = "%PDF-1.4\nFictional submitted bytes"u8.ToArray();
        var cv = await cvs.ImportAsync("private-name.pdf", new MemoryStream(bytes), new()
        {
            DisplayName = "Private CV label"
        }, TestContext.Current.CancellationToken);
        var document = (await cvs.GetAsync(cv))!.Document;
        var submissions = new SubmissionOperations(f.Factory, store);
        var submission = await submissions.RecordAsync(new()
        {
            ApplicationId = application,
            PreparedCvId = cv
        });
        await ops.ReviewedApproveAsync(first, new()
        {
            CvKey = $"{submission}/{document.Id}"
        });
        await using var host = new WebApplicationFactory<Program>().WithWebHostBuilder(b => { b.UseEnvironment("Testing"); b.ConfigureServices(s => { s.Replace(ServiceDescriptor.Singleton(f.Paths)); s.Replace(ServiceDescriptor.Singleton(f.Factory)); }); });
        using var client = host.CreateClient(new()
        {
            BaseAddress = new Uri("http://localhost:5180"),
            AllowAutoRedirect = false
        });
        using var response = await client.GetAsync($"/interviews/{first}/workspace/cv/view?revision=1", TestContext.Current.CancellationToken);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal(bytes, await response.Content.ReadAsByteArrayAsync(TestContext.Current.CancellationToken));
        Assert.Equal("submitted-cv.pdf", response.Content.Headers.ContentDisposition!.FileName);
        Assert.True(response.Headers.CacheControl!.NoStore);
        foreach (var url in new[] { $"/interviews/{second}/workspace/cv/view?revision=1", $"/interviews/{first}/workspace/cv/view", $"/interviews/{first}/workspace/cv/view?revision=0", $"/interviews/{first}/workspace/cv/unknown?revision=1" })
        {
            using var invalid = await client.GetAsync(url, TestContext.Current.CancellationToken);
            var body = await invalid.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);
            Assert.Equal(HttpStatusCode.NotFound, invalid.StatusCode);
            Assert.DoesNotContain("/companies", body);
            Assert.DoesNotContain("private-name", body);
        }
        await File.WriteAllTextAsync(store.Resolve(document.StoredRelativePath), "changed", TestContext.Current.CancellationToken);
        using var corrupt = await client.GetAsync($"/interviews/{first}/workspace/cv/download?revision=1", TestContext.Current.CancellationToken);
        Assert.Equal(HttpStatusCode.Conflict, corrupt.StatusCode);
        Assert.DoesNotContain("/companies", await corrupt.Content.ReadAsStringAsync(TestContext.Current.CancellationToken));
        await ops.ReviewedApproveAsync(first, new()
        {
            ExpectedRevision = 1
        });
        Assert.Null(await ops.CvAsync(first, 1));
        Assert.Null(await ops.CvAsync(first, 2));
    }
    [Fact]
    public async Task ChangedVisiblePreviewCannotBeApprovedSilently()
    {
        await using var f = await TemporaryDatabase.CreateAsync();
        var (first, _) = await PreparationTests.Rounds(f);
        var ops = new WorkspaceOperations(f.Factory);
        var setup = (await ops.SetupAsync(first))!;
        await using (var db = await f.Factory.CreateDbContextAsync(TestContext.Current.CancellationToken))
        {
            await db.Interviews.Where(x => x.Id == first).ExecuteUpdateAsync(set => set.SetProperty(x => x.PreparationNotes, "New private content"), TestContext.Current.CancellationToken);
        }
        await Assert.ThrowsAsync<System.ComponentModel.DataAnnotations.ValidationException>(() => ops.ApproveAsync(first, new() { ExpectedReviewStamp = setup.ReviewStamp, PreparationNotes = true }));
        Assert.Null(await ops.GetAsync(first));
    }
}
