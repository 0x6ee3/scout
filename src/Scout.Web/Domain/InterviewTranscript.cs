namespace Scout.Web.Domain;

public enum TranscriptOrigin
{
    Imported, EditedCopy
}

public sealed class InterviewTranscript
{
    public int Id
    {
        get; set;
    }
    public int InterviewRoundId
    {
        get; set;
    }
    public TranscriptOrigin Origin
    {
        get; set;
    }
    public string Text { get; set; } = "";
    public string Sha256 { get; set; } = "";
    public int? OriginalDocumentId
    {
        get; set;
    }
    public Document? OriginalDocument
    {
        get; set;
    }
    public int? RecordingId
    {
        get; set;
    }
    public int? ParentTranscriptId
    {
        get; set;
    }
    public Guid SubmissionToken
    {
        get; set;
    }
    public string PayloadHash { get; set; } = "";
    public DateTime CreatedAtUtc
    {
        get; set;
    }
    public DateTime? ArchivedAtUtc
    {
        get; set;
    }
}
