using System.ComponentModel.DataAnnotations;
using Microsoft.EntityFrameworkCore;
using Scout.Web.Domain;
using Scout.Web.Features.Documents;
using Scout.Web.Features.InterviewRecordings;
using Scout.Web.Features.InterviewWorkspace;
using Scout.Web.Storage;
using Scout.Web.Tests.Data;
using Scout.Web.Tests.InterviewPreparation;
using Scout.Web.Tests.InterviewWorkspace;

namespace Scout.Web.Tests.InterviewRecordings;

public sealed class RecordingTests
{
    [Fact]
    public async Task ChoicesRegistrationRetriesAndArchivesPreserveOriginals()
    {
        await using var f = await TemporaryDatabase.CreateAsync();
        var (round, other) = await PreparationTests.Rounds(f);
        var store = new DocumentFileStore(f.Paths);
        var ops = new RecordingOperations(f.Factory, store);
        Assert.Empty(await ops.HistoryAsync(round));
        await ops.ChooseAsync(round, RecordingChoice.NotesOnly);
        var bytes = "fictional audio"u8.ToArray();
        var token = Guid.NewGuid();
        var id = await ops.UploadAsync(round,"sample.mp3",new MemoryStream(bytes),true,"Historical recording acknowledged now",token,TestContext.Current.CancellationToken);
        Assert.Equal(id, await ops.UploadAsync(round,"sample.mp3",new MemoryStream(bytes),true,"Historical recording acknowledged now",token,TestContext.Current.CancellationToken));
        await Assert.ThrowsAsync<ValidationException>(()=>ops.UploadAsync(round,"sample.mp3",new MemoryStream("changed"u8.ToArray()),true,"Historical recording acknowledged now",token,TestContext.Current.CancellationToken));
        var row = Assert.Single(await ops.ListAsync(round));
        await Assert.ThrowsAsync<ValidationException>(()=>ops.RegisterDocumentAsync(other,row.DocumentId!.Value,true,null,Guid.NewGuid(),TestContext.Current.CancellationToken));
        await Assert.ThrowsAsync<ValidationException>(()=>ops.RegisterDocumentAsync(round,row.DocumentId!.Value,false,null,Guid.NewGuid(),TestContext.Current.CancellationToken));
        await ops.RegisterDocumentAsync(round,row.DocumentId!.Value,true,null,Guid.NewGuid(),TestContext.Current.CancellationToken);
        await ops.ChooseAsync(round,RecordingChoice.NotesOnly);
        Assert.Equal(4,(await ops.HistoryAsync(round)).Count);
        await ops.ArchiveAsync(round,id,true);
        Assert.Single(await ops.ListAsync(round));
        Assert.Equal(2,(await ops.ListAsync(round,true)).Count);
        await using var db=await f.Factory.CreateDbContextAsync(TestContext.Current.CancellationToken);
        Assert.Single(await db.Documents.ToListAsync(TestContext.Current.CancellationToken));
        var document=await db.Documents.SingleAsync(TestContext.Current.CancellationToken);
        await using var stream=await store.OpenVerifiedAsync(document,TestContext.Current.CancellationToken);
        using var copy=new MemoryStream();await stream.CopyToAsync(copy,TestContext.Current.CancellationToken);Assert.Equal(bytes,copy.ToArray());
        (await db.Interviews.FindAsync([round],TestContext.Current.CancellationToken))!.IsArchived=true;await db.SaveChangesAsync(TestContext.Current.CancellationToken);
        await Assert.ThrowsAsync<ValidationException>(()=>ops.UploadAsync(round,"new.wav",new MemoryStream(bytes),true,null,Guid.NewGuid(),TestContext.Current.CancellationToken));
    }
    [Fact]
    public async Task WorkspaceChoiceRequiresFreshExplicitApprovalAndProjectsOnlyLabel()
    {
        await using var f=await TemporaryDatabase.CreateAsync();var(round,_)=await PreparationTests.Rounds(f);
        var workspace=new WorkspaceOperations(f.Factory);var recordings=new RecordingOperations(f.Factory,new(f.Paths));
        await workspace.ReviewedApproveAsync(round,new());
        Assert.Null((await workspace.GetAsync(round))!.RecordingChoiceStatus);
        await Assert.ThrowsAsync<ValidationException>(()=>workspace.ChooseRecordingAsync(round,1,RecordingChoice.NotesOnly));
        var stale=(await workspace.SetupAsync(round))!;
        await recordings.ChooseAsync(round,RecordingChoice.ExternalConsentAcknowledged,"private consent note");
        await Assert.ThrowsAsync<ValidationException>(()=>workspace.ApproveAsync(round,new(){ExpectedRevision=1,ExpectedReviewStamp=stale.ReviewStamp,RecordingChoice=true}));
        await workspace.ReviewedApproveAsync(round,new(){ExpectedRevision=1,RecordingChoice=true});
        Assert.Equal("Consent received — use external recorder",(await workspace.GetAsync(round))!.RecordingChoiceStatus);
        await workspace.ChooseRecordingAsync(round,2,RecordingChoice.NotesOnly);
        Assert.Equal("Continue without recording",(await workspace.GetAsync(round))!.RecordingChoiceStatus);
        await Assert.ThrowsAsync<ValidationException>(()=>workspace.ChooseRecordingAsync(round,1,RecordingChoice.NotesOnly));
    }
}
