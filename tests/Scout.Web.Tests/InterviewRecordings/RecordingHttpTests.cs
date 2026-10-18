using System.Net;
using System.Net.Http.Headers;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Scout.Web.Features.InterviewRecordings;
using Scout.Web.Storage;
using Scout.Web.Tests.Data;
using Scout.Web.Tests.InterviewPreparation;

namespace Scout.Web.Tests.InterviewRecordings;

public sealed class RecordingHttpTests
{
    [Fact]
    public async Task ManagedAndLinkedAudioSupportRangesHeadAndRejectChangedOrForeignSources()
    {
        var ct=TestContext.Current.CancellationToken;
        await using var f=await TemporaryDatabase.CreateAsync();var(round,other)=await PreparationTests.Rounds(f);
        var ops=new RecordingOperations(f.Factory,new(f.Paths));var links=new LinkedRecordingStore(f.Paths);
        Directory.CreateDirectory(links.Root);
        var bytes="fictional-audio-bytes"u8.ToArray();
        await File.WriteAllBytesAsync(Path.Combine(links.Root,"sample.wav"),bytes,ct);
        var linked=await ops.RegisterLinkAsync(round,"sample.wav",links,true,null,Guid.NewGuid(),ct);
        var managed=await ops.UploadAsync(round,"sample.mp3",new MemoryStream(bytes),true,null,Guid.NewGuid(),ct);
        await using var host=new WebApplicationFactory<Program>().WithWebHostBuilder(b=>{b.UseEnvironment("Testing");b.ConfigureServices(s=>{s.Replace(ServiceDescriptor.Singleton(f.Paths));s.Replace(ServiceDescriptor.Singleton(f.Factory));});});
        using var client=host.CreateClient(new(){BaseAddress=new Uri("http://localhost:5180"),AllowAutoRedirect=false});
        foreach(var id in new[]{linked,managed})
        {
            var url=$"/interviews/{round}/recordings/{id}/media";
            using var full=await client.GetAsync(url,ct);Assert.Equal(HttpStatusCode.OK,full.StatusCode);Assert.Equal(bytes,await full.Content.ReadAsByteArrayAsync(ct));Assert.True(full.Headers.CacheControl!.NoStore);Assert.Equal("nosniff",full.Headers.GetValues("X-Content-Type-Options").Single());
            using var request=new HttpRequestMessage(HttpMethod.Get,url);request.Headers.Range=new RangeHeaderValue(2,6);
            using var partial=await client.SendAsync(request,ct);Assert.Equal(HttpStatusCode.PartialContent,partial.StatusCode);Assert.Equal(bytes[2..7],await partial.Content.ReadAsByteArrayAsync(ct));
            using var invalid=new HttpRequestMessage(HttpMethod.Get,url);invalid.Headers.Range=new RangeHeaderValue(1000,null);
            using var unsatisfied=await client.SendAsync(invalid,ct);Assert.Equal(HttpStatusCode.RequestedRangeNotSatisfiable,unsatisfied.StatusCode);
            using var head=await client.SendAsync(new(HttpMethod.Head,url),ct);Assert.Equal(HttpStatusCode.OK,head.StatusCode);Assert.Empty(await head.Content.ReadAsByteArrayAsync(ct));Assert.Equal(bytes.Length,head.Content.Headers.ContentLength);
            using var wrong=await client.GetAsync($"/interviews/{other}/recordings/{id}/media",ct);Assert.Equal(HttpStatusCode.NotFound,wrong.StatusCode);
            using var download=await client.GetAsync($"/interviews/{round}/recordings/{id}/download",ct);Assert.Equal("attachment",download.Content.Headers.ContentDisposition!.DispositionType);
        }
        // Responses own/dispose their opened stream, allowing replacement after completion.
        await File.WriteAllBytesAsync(Path.Combine(links.Root,"sample.wav"),new byte[bytes.Length],ct);
        using var changed=await client.GetAsync($"/interviews/{round}/recordings/{linked}/download",ct);Assert.Equal(HttpStatusCode.Conflict,changed.StatusCode);
        File.Delete(Path.Combine(links.Root,"sample.wav"));
        using var missing=await client.GetAsync($"/interviews/{round}/recordings/{linked}/media",ct);Assert.Equal(HttpStatusCode.Conflict,missing.StatusCode);
    }
    [Theory]
    [InlineData("../sample.wav")][InlineData("/sample.wav")][InlineData("C:/sample.wav")][InlineData("//server/share/sample.wav")][InlineData("file.wav:stream")][InlineData("sub/../sample.wav")][InlineData("sub\\sample.wav")][InlineData("sample.txt")]
    public async Task WorkingPathsAreConfined(string path)
    {
        await using var f=await TemporaryDatabase.CreateAsync();var links=new LinkedRecordingStore(f.Paths);
        Assert.Throws<IOException>(()=>links.Resolve(path));
    }
    [Fact]
    public async Task CanceledVerificationReleasesHandle()
    {
        var ct=TestContext.Current.CancellationToken;
        await using var f=await TemporaryDatabase.CreateAsync();var(round,_)=await PreparationTests.Rounds(f);
        var links=new LinkedRecordingStore(f.Paths);Directory.CreateDirectory(links.Root);var path=Path.Combine(links.Root,"cancel.wav");await File.WriteAllBytesAsync(path,new byte[65536],ct);
        var ops=new RecordingOperations(f.Factory,new(f.Paths));await ops.RegisterLinkAsync(round,"cancel.wav",links,true,null,Guid.NewGuid(),ct);
        using var cancel=new CancellationTokenSource();cancel.Cancel();
        var recording=(await ops.ListAsync(round)).Single();
        await Assert.ThrowsAnyAsync<OperationCanceledException>(()=>links.OpenVerifiedAsync(recording,cancel.Token));
        File.Delete(path);
    }
}
