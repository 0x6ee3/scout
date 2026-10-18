using System.ComponentModel.DataAnnotations;
using System.Net;
using System.Text;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Scout.Web.Features.InterviewTranscripts;
using Scout.Web.Storage;
using Scout.Web.Tests.Data;
using Scout.Web.Tests.InterviewPreparation;

namespace Scout.Web.Tests.InterviewTranscripts;

public sealed class TranscriptTests
{
    [Fact]
    public async Task NotesOnlyImportsAndCorrectionsAreImmutableScopedAndRetrySafe()
    {
        var ct=TestContext.Current.CancellationToken;
        await using var f=await TemporaryDatabase.CreateAsync();var(round,other)=await PreparationTests.Rounds(f);
        var store=new DocumentFileStore(f.Paths);var ops=new TranscriptOperations(f.Factory,store);
        var text="Original # transcript\r\n**exact** source ü\n";var bytes=Encoding.UTF8.GetBytes(text);var token=Guid.NewGuid();
        var id=await ops.ImportAsync(round,"original.txt",new MemoryStream(bytes),null,token,ct);
        Assert.Equal(id,await ops.ImportAsync(round,"original.txt",new MemoryStream(bytes),null,token,ct));
        await Assert.ThrowsAsync<ValidationException>(()=>ops.ImportAsync(other,"original.txt",new MemoryStream(bytes),null,token,ct));
        var correctionToken=Guid.NewGuid();var corrected=await ops.CorrectAsync(round,id,"Corrected source",correctionToken,ct);
        Assert.Equal(corrected,await ops.CorrectAsync(round,id,"Corrected source",correctionToken,ct));
        await Assert.ThrowsAsync<ValidationException>(()=>ops.CorrectAsync(round,id,"changed retry",correctionToken,ct));
        await Assert.ThrowsAsync<ValidationException>(()=>ops.CorrectAsync(other,id,"cross round",Guid.NewGuid(),ct));
        Assert.Equal(text,(await ops.GetAsync(round,id))!.Text);Assert.Null(await ops.GetAsync(other,id));
        var copy=(await ops.GetAsync(round,corrected))!;Assert.Equal(id,copy.ParentTranscriptId);Assert.Equal((await ops.GetAsync(round,id))!.OriginalDocumentId,copy.OriginalDocumentId);
        await ops.ArchiveAsync(round,id,true);Assert.Single(await ops.ListAsync(round));Assert.Equal(2,(await ops.ListAsync(round,true)).Count);
        await using var db=await f.Factory.CreateDbContextAsync(ct);Assert.Empty(await db.Recordings.ToListAsync(ct));Assert.Empty(await db.RecordingDecisions.ToListAsync(ct));
        var original=await db.Documents.SingleAsync(ct);await using var stream=await store.OpenVerifiedAsync(original,ct);using var output=new MemoryStream();await stream.CopyToAsync(output,ct);Assert.Equal(bytes,output.ToArray());
        Assert.Equal(TranscriptOperations.TextHash(text),(await ops.GetAsync(round,id))!.Sha256);
    }
    [Fact]
    public async Task InvalidEncodingAndLimitsLeaveNoMetadataAndMaximumCorrectionIsAccepted()
    {
        var ct=TestContext.Current.CancellationToken;
        await using var f=await TemporaryDatabase.CreateAsync();var(round,_)=await PreparationTests.Rounds(f);var ops=new TranscriptOperations(f.Factory,new(f.Paths));
        await Assert.ThrowsAsync<ValidationException>(()=>ops.ImportAsync(round,"bad.txt",new MemoryStream([0xff,0xfe]),null,Guid.NewGuid(),ct));
        await Assert.ThrowsAsync<ValidationException>(()=>ops.ImportAsync(round,"large.txt",new MemoryStream(new byte[TranscriptOperations.MaximumBytes+1]),null,Guid.NewGuid(),ct));
        await Assert.ThrowsAsync<ValidationException>(()=>ops.ImportAsync(round,"characters.txt",new MemoryStream(Encoding.UTF8.GetBytes(new string('x',TranscriptOperations.MaximumCharacters+1))),null,Guid.NewGuid(),ct));
        Assert.Empty(await ops.ListAsync(round));
        var id=await ops.ImportAsync(round,"valid.txt",new MemoryStream("source"u8.ToArray()),null,Guid.NewGuid(),ct);
        var max=new string('x',TranscriptOperations.MaximumCharacters);var copy=await ops.CorrectAsync(round,id,max,Guid.NewGuid(),ct);Assert.Equal(max,(await ops.GetAsync(round,copy))!.Text);
        await Assert.ThrowsAsync<ValidationException>(()=>ops.CorrectAsync(round,id,max+"x",Guid.NewGuid(),ct));
    }
    [Fact]
    public async Task CorrectionHttpRequiresAntiforgery()
    {
        var ct=TestContext.Current.CancellationToken;
        await using var f=await TemporaryDatabase.CreateAsync();var(round,_)=await PreparationTests.Rounds(f);var ops=new TranscriptOperations(f.Factory,new(f.Paths));var id=await ops.ImportAsync(round,"source.txt",new MemoryStream("source"u8.ToArray()),null,Guid.NewGuid(),ct);
        await using var host=new WebApplicationFactory<Program>().WithWebHostBuilder(b=>{b.UseEnvironment("Testing");b.ConfigureServices(s=>{s.Replace(ServiceDescriptor.Singleton(f.Paths));s.Replace(ServiceDescriptor.Singleton(f.Factory));});});
        using var client=host.CreateClient(new(){BaseAddress=new Uri("http://localhost:5180"),AllowAutoRedirect=false});
        using var response=await client.PostAsync($"/interviews/{round}/transcripts/{id}/correct",new FormUrlEncodedContent(new Dictionary<string,string>{{"text","changed"},{"token",Guid.NewGuid().ToString()}}),ct);
        Assert.Equal(HttpStatusCode.BadRequest,response.StatusCode);Assert.Single(await ops.ListAsync(round));
    }
    [Fact]
    public async Task MillionCharacterCorrectionUsesBoundedHttpAndRetainsRetryIdentity()
    {
        var ct=TestContext.Current.CancellationToken;
        await using var f=await TemporaryDatabase.CreateAsync();var(round,_)=await PreparationTests.Rounds(f);var ops=new TranscriptOperations(f.Factory,new(f.Paths));var id=await ops.ImportAsync(round,"source.txt",new MemoryStream("source"u8.ToArray()),null,Guid.NewGuid(),ct);
        await using var host=new WebApplicationFactory<Program>().WithWebHostBuilder(b=>{b.UseEnvironment("Testing");b.ConfigureServices(s=>{s.Replace(ServiceDescriptor.Singleton(f.Paths));s.Replace(ServiceDescriptor.Singleton(f.Factory));});});
        using var client=host.CreateClient(new(){BaseAddress=new Uri("http://localhost:5180"),AllowAutoRedirect=false});
        using var scope=host.Services.CreateScope();var context=new DefaultHttpContext{RequestServices=scope.ServiceProvider};context.Request.Scheme="http";context.Request.Host=new HostString("localhost",5180);
        var tokens=scope.ServiceProvider.GetRequiredService<IAntiforgery>().GetAndStoreTokens(context);var cookie=context.Response.Headers.SetCookie.ToString().Split(';')[0];
        var token=Guid.NewGuid();var text=new string('ü',TranscriptOperations.MaximumCharacters);
        async Task<HttpResponseMessage> Send(string value)
        {
            using var request=new HttpRequestMessage(HttpMethod.Post,$"/interviews/{round}/transcripts/{id}/correct");request.Headers.Add("Cookie",cookie);
            var content=new MultipartFormDataContent();content.Add(new StringContent(value),"text");content.Add(new StringContent(token.ToString()),"token");content.Add(new StringContent(tokens.RequestToken!),tokens.FormFieldName);request.Content=content;
            return await client.SendAsync(request,ct);
        }
        using var result=await Send(text);Assert.Equal(HttpStatusCode.Redirect,result.StatusCode);
        using var retry=await Send(text);Assert.Equal(result.Headers.Location,retry.Headers.Location);
        using var altered=await Send("changed");Assert.Equal(HttpStatusCode.BadRequest,altered.StatusCode);
        Assert.Equal(2,(await ops.ListAsync(round)).Count);Assert.Equal(text,(await ops.ListAsync(round)).Last().Text);
    }
}
