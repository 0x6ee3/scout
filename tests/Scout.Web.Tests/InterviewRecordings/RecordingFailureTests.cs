using System.Data.Common;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Scout.Web.Data;
using Scout.Web.Features.InterviewRecordings;
using Scout.Web.Features.InterviewTranscripts;
using Scout.Web.Storage;
using Scout.Web.Tests.Data;
using Scout.Web.Tests.InterviewPreparation;

namespace Scout.Web.Tests.InterviewRecordings;

public sealed class RecordingFailureTests
{
    [Fact]
    public async Task FailedMetadataCommitKeepsFinalizedOriginalAndRollsBackAllFacts()
    {
        var ct=TestContext.Current.CancellationToken;
        await using var f=await TemporaryDatabase.CreateAsync();var(round,_)=await PreparationTests.Rounds(f);var files=new DocumentFileStore(f.Paths);var ops=new RecordingOperations(f.Factory,files);
        await using var db=await f.Factory.CreateDbContextAsync(ct);
        await db.Database.ExecuteSqlRawAsync("CREATE TRIGGER RejectRecording BEFORE INSERT ON Recordings BEGIN SELECT RAISE(ABORT,'test failure'); END;",ct);
        await Assert.ThrowsAsync<IOException>(()=>ops.UploadAsync(round,"retained.wav",new MemoryStream("retained audio"u8.ToArray()),true,null,Guid.NewGuid(),ct));
        Assert.Empty(await db.Documents.ToListAsync(ct));Assert.Empty(await db.InterviewDocuments.ToListAsync(ct));Assert.Empty(await db.Recordings.ToListAsync(ct));Assert.Empty(await db.RecordingDecisions.ToListAsync(ct));
        Assert.Single(Directory.GetFiles(f.Paths.AssetDirectory,"original.wav",SearchOption.AllDirectories));
    }
    [Fact]
    public async Task AmbiguousCommittedAcknowledgmentsResolveSubmissionTokens()
    {
        var ct=TestContext.Current.CancellationToken;
        await using var f=await TemporaryDatabase.CreateAsync();var(round,_)=await PreparationTests.Rounds(f);
        var interceptor=new LostAcknowledgment();var options=new DbContextOptionsBuilder<ScoutDbContext>().UseSqlite(new SqliteConnectionStringBuilder(ScoutDatabase.ConnectionString(f.Paths)){Pooling=false}.ToString()).AddInterceptors(interceptor).Options;
        var factory=new TestFactory(options);var files=new DocumentFileStore(f.Paths);var ops=new RecordingOperations(factory,files);
        var token=Guid.NewGuid();interceptor.Armed=true;
        var id=await ops.UploadAsync(round,"committed.wav",new MemoryStream("committed"u8.ToArray()),true,null,token,ct);
        Assert.Equal(id,await ops.UploadAsync(round,"committed.wav",new MemoryStream("committed"u8.ToArray()),true,null,token,ct));Assert.Single(await ops.ListAsync(round));
        var transcripts=new TranscriptOperations(factory,files);interceptor.Armed=true;
        var transcript=await transcripts.ImportAsync(round,"committed.txt",new MemoryStream("imported"u8.ToArray()),null,Guid.NewGuid(),ct);
        interceptor.Armed=true;var correction=await transcripts.CorrectAsync(round,transcript,"corrected",Guid.NewGuid(),ct);
        Assert.Equal(transcript,(await transcripts.GetAsync(round,correction))!.ParentTranscriptId);Assert.Equal(2,(await transcripts.ListAsync(round)).Count);
    }
    [Fact]
    public async Task CanceledAndInterruptedUploadsNeverRegisterConsentOrDocuments()
    {
        var ct=TestContext.Current.CancellationToken;
        await using var f=await TemporaryDatabase.CreateAsync();var(round,_)=await PreparationTests.Rounds(f);var ops=new RecordingOperations(f.Factory,new(f.Paths));
        await Assert.ThrowsAsync<DocumentImportException>(()=>ops.UploadAsync(round,"failed.mp3",new FailingStream(),true,null,Guid.NewGuid(),ct));
        using var cancel=new CancellationTokenSource();cancel.Cancel();
        await Assert.ThrowsAnyAsync<OperationCanceledException>(()=>ops.UploadAsync(round,"canceled.wav",new MemoryStream([1,2]),true,null,Guid.NewGuid(),cancel.Token));
        await using var db=await f.Factory.CreateDbContextAsync(ct);Assert.Empty(await db.Recordings.ToListAsync(ct));Assert.Empty(await db.Documents.ToListAsync(ct));Assert.Empty(await db.RecordingDecisions.ToListAsync(ct));
    }
    [Fact]
    public async Task WorkingDirectoryJunctionIsRejected()
    {
        var ct=TestContext.Current.CancellationToken;await using var f=await TemporaryDatabase.CreateAsync();
        var links=new LinkedRecordingStore(f.Paths);var outside=Path.Combine(f.Paths.Root,"outside");Directory.CreateDirectory(outside);Directory.CreateDirectory(links.Root);var junction=Path.Combine(links.Root,"escape");
        if(OperatingSystem.IsWindows())
        {
            var start=new System.Diagnostics.ProcessStartInfo("cmd.exe"){UseShellExecute=false,CreateNoWindow=true,RedirectStandardOutput=true,RedirectStandardError=true};foreach(var arg in new[]{"/c","mklink","/J",junction,outside})start.ArgumentList.Add(arg);
            using var process=System.Diagnostics.Process.Start(start)!;await process.WaitForExitAsync(ct);Assert.Equal(0,process.ExitCode);
        }
        else Directory.CreateSymbolicLink(junction,outside);
        try{Assert.Throws<IOException>(()=>links.Resolve("escape/audio.wav"));}finally{Directory.Delete(junction);}
    }
    private sealed class FailingStream : MemoryStream
    {public override ValueTask<int> ReadAsync(Memory<byte> buffer,CancellationToken ct=default)=>throw new IOException("Injected upload interruption");}
    private sealed class TestFactory(DbContextOptions<ScoutDbContext> options) : IDbContextFactory<ScoutDbContext>
    {public ScoutDbContext CreateDbContext()=>new(options);}
    private sealed class LostAcknowledgment : DbTransactionInterceptor
    {
        public bool Armed {get;set;}
        public override Task TransactionCommittedAsync(DbTransaction transaction,TransactionEndEventData data,CancellationToken ct=default)
        {if(Armed){Armed=false;throw new IOException("Commit succeeded but acknowledgment was lost.");}return Task.CompletedTask;}
    }
}
