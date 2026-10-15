using System.Net;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Scout.Web.Configuration;
using Scout.Web.Data;
using Scout.Web.Features.Documents;
using Scout.Web.Storage;
using Scout.Web.Tests.Data;
namespace Scout.Web.Tests.Documents;

public sealed class DocumentHttpTests
{
    [Fact]
    public async Task ControlledReadsUseIsolatedStorageAndSafeHeaders()
    {
        await using var db = await TemporaryDatabase.CreateAsync();
        await using var host = new WebApplicationFactory<Program>().WithWebHostBuilder(b => { b.UseEnvironment("Testing"); b.ConfigureServices(s => s.Replace(ServiceDescriptor.Singleton(db.Paths))); });
        using var client = host.CreateClient(new()
        {
            BaseAddress = new Uri("http://localhost:5180"),
            AllowAutoRedirect = false
        });
        using var scope = host.Services.CreateScope();
        Assert.Equal(db.Paths.Root, scope.ServiceProvider.GetRequiredService<StoragePaths>().Root);
        var docs = scope.ServiceProvider.GetRequiredService<DocumentOperations>();
        var store = scope.ServiceProvider.GetRequiredService<DocumentFileStore>();
        var bytes = "%PDF-1.4\nFictional test PDF\n%%EOF"u8.ToArray();
        var id = await docs.ImportAsync("Müller\".pdf", new MemoryStream(bytes), new(), token: TestContext.Current.CancellationToken);
        using var response = await client.GetAsync($"/documents/{id}/download", TestContext.Current.CancellationToken);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal(bytes, await response.Content.ReadAsByteArrayAsync(TestContext.Current.CancellationToken));
        Assert.Equal("application/pdf", response.Content.Headers.ContentType!.MediaType);
        Assert.Equal("attachment", response.Content.Headers.ContentDisposition!.DispositionType);
        Assert.True(response.Headers.CacheControl!.NoStore);
        Assert.Equal("nosniff", response.Headers.GetValues("X-Content-Type-Options").Single());
        using var preview = await client.GetAsync($"/documents/{id}/view", TestContext.Current.CancellationToken);
        Assert.Equal("inline", preview.Content.Headers.ContentDisposition!.DispositionType);
        Assert.Contains("sandbox", preview.Headers.GetValues("Content-Security-Policy").Single());
        var text = await docs.ImportAsync("notes.md", new MemoryStream("<script>alert('x')</script>\nMünchen"u8.ToArray()), new(), token: TestContext.Current.CancellationToken);
        using var textResponse = await client.GetAsync($"/documents/{text}/view", TestContext.Current.CancellationToken);
        var html = await textResponse.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);
        Assert.Contains("&lt;script&gt;", html);
        Assert.DoesNotContain("<script>", html);
        var mail = await docs.ImportAsync("message.eml", new MemoryStream("Subject: Fictional"u8.ToArray()), new(), token: TestContext.Current.CancellationToken);
        var largeText = new byte[DocumentFileStore.PreviewBytes + 32];
        Array.Fill(largeText, (byte)'a');
        var longId = await docs.ImportAsync("long.txt", new MemoryStream(largeText), new(), token: TestContext.Current.CancellationToken);
        using var truncated = await client.GetAsync($"/documents/{longId}/view", TestContext.Current.CancellationToken);
        var longHtml = await truncated.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);
        Assert.Contains("Preview truncated", longHtml);
        var previewStart = longHtml.IndexOf("<pre>", StringComparison.Ordinal) + 5;
        var previewEnd = longHtml.IndexOf("</pre>", StringComparison.Ordinal);
        Assert.Equal(DocumentFileStore.PreviewBytes, previewEnd - previewStart);
        using var opaque = await client.GetAsync($"/documents/{mail}/view", TestContext.Current.CancellationToken);
        Assert.Equal(HttpStatusCode.BadRequest, opaque.StatusCode);
        await docs.SetArchivedAsync(id, true);
        using var archived = await client.GetAsync($"/documents/{id}/download", TestContext.Current.CancellationToken);
        Assert.Equal(HttpStatusCode.OK, archived.StatusCode);
        using var unknown = await client.GetAsync("/documents/99999/download", TestContext.Current.CancellationToken);
        Assert.Equal(HttpStatusCode.NotFound, unknown.StatusCode);
        using var hostile = new HttpRequestMessage(HttpMethod.Get, $"/documents/{id}/download");
        hostile.Headers.Host = "evil.example:5180";
        using var badHost = await client.SendAsync(hostile, TestContext.Current.CancellationToken);
        Assert.Equal(HttpStatusCode.BadRequest, badHost.StatusCode);
        using var origin = new HttpRequestMessage(HttpMethod.Get, $"/documents/{id}/download");
        origin.Headers.Add("Origin", "https://evil.example");
        using var badOrigin = await client.SendAsync(origin, TestContext.Current.CancellationToken);
        Assert.Equal(HttpStatusCode.Forbidden, badOrigin.StatusCode);
        var d = (await docs.GetAsync(id))!;
        await File.WriteAllTextAsync(store.Resolve(d.StoredRelativePath), "tampered", TestContext.Current.CancellationToken);
        using var tampered = await client.GetAsync($"/documents/{id}/download", TestContext.Current.CancellationToken);
        Assert.Equal(HttpStatusCode.Conflict, tampered.StatusCode);
        Microsoft.Data.Sqlite.SqliteConnection.ClearAllPools();
    }
}


