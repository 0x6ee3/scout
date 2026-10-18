using Microsoft.EntityFrameworkCore;
using Scout.Web.Features.InterviewRecordings;
using Scout.Web.Features.InterviewTranscripts;
using Scout.Web.Storage;
using Scout.Web.Tests.Data;
using Scout.Web.Tests.InterviewPreparation;

namespace Scout.Web.Tests.InterviewRecordings;

public sealed class RecordingLimitTests
{
    [Fact]
    public async Task AudioAboveOldDocumentLimitCanBeImportedAndVerifiedWithoutWholeFileBuffering()
    {
        var ct=TestContext.Current.CancellationToken;
        await using var f=await TemporaryDatabase.CreateAsync();var(round,_)=await PreparationTests.Rounds(f);var store=new DocumentFileStore(f.Paths);var ops=new RecordingOperations(f.Factory,store);
        var size=DocumentFileStore.MaximumBytes+65536;await using var input=new RepeatingStream(size);
        await ops.UploadAsync(round,"large.wav",input,true,null,Guid.NewGuid(),ct);
        await using var db=await f.Factory.CreateDbContextAsync(ct);var doc=await db.Documents.SingleAsync(ct);
        Assert.Equal(size,doc.Size);await using var read=await store.OpenVerifiedAsync(doc,ct);Assert.Equal(size,read.Length);Assert.True(read.CanSeek);
        Assert.Equal(DocumentFileStore.MaximumBytes,DocumentFileStore.LimitFor("notes.txt"));Assert.Equal(DocumentFileStore.MaximumAudioBytes,DocumentFileStore.LimitFor("sample.M4A"));
    }
    [Fact]
    public async Task OversizeWorkingFileAndCrossRoundTranscriptAssociationAreRejected()
    {
        var ct=TestContext.Current.CancellationToken;await using var f=await TemporaryDatabase.CreateAsync();var(round,other)=await PreparationTests.Rounds(f);var links=new LinkedRecordingStore(f.Paths);Directory.CreateDirectory(links.Root);var path=Path.Combine(links.Root,"oversize.wav");
        await using(var file=File.Create(path))file.SetLength(DocumentFileStore.MaximumAudioBytes+1);
        await Assert.ThrowsAsync<IOException>(()=>links.InspectAsync("oversize.wav",ct));
        var ops=new RecordingOperations(f.Factory,new(f.Paths));var recording=await ops.UploadAsync(round,"small.wav",new MemoryStream("fictional"u8.ToArray()),true,null,Guid.NewGuid(),ct);
        var transcripts=new TranscriptOperations(f.Factory,new(f.Paths));
        await Assert.ThrowsAsync<System.ComponentModel.DataAnnotations.ValidationException>(()=>transcripts.ImportAsync(other,"wrong.txt",new MemoryStream("text"u8.ToArray()),recording,Guid.NewGuid(),ct));
        var id=await transcripts.ImportAsync(round,"right.txt",new MemoryStream("text"u8.ToArray()),recording,Guid.NewGuid(),ct);var copy=await transcripts.CorrectAsync(round,id,"corrected",Guid.NewGuid(),ct);Assert.Equal(recording,(await transcripts.GetAsync(round,copy))!.RecordingId);
    }
    private sealed class RepeatingStream(long remaining):Stream
    {
        public override ValueTask<int> ReadAsync(Memory<byte> buffer,CancellationToken ct=default){ct.ThrowIfCancellationRequested();var count=(int)Math.Min(buffer.Length,remaining);buffer.Span[..count].Fill(1);remaining-=count;return ValueTask.FromResult(count);}
        public override bool CanRead=>true;public override bool CanSeek=>false;public override bool CanWrite=>false;public override long Length=>throw new NotSupportedException();public override long Position{get=>throw new NotSupportedException();set=>throw new NotSupportedException();}
        public override int Read(byte[] b,int o,int c)=>throw new NotSupportedException();public override void Flush()=>throw new NotSupportedException();public override long Seek(long o,SeekOrigin s)=>throw new NotSupportedException();public override void SetLength(long v)=>throw new NotSupportedException();public override void Write(byte[] b,int o,int c)=>throw new NotSupportedException();
    }
}
