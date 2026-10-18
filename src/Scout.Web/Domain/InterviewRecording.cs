namespace Scout.Web.Domain;

public enum RecordingChoice
{
    NotesOnly, ExternalConsentAcknowledged
}
public enum RecordingStorage
{
    ManagedDocument, LinkedWorkingFile
}

public sealed class InterviewRecordingDecision
{
    public int Id
    {
        get; set;
    }
    public int InterviewRoundId
    {
        get; set;
    }
    public RecordingChoice Choice
    {
        get; set;
    }
    public DateTime AcknowledgedAtUtc
    {
        get; set;
    }
    public string? PrivateNote
    {
        get; set;
    }
    public DateTime CreatedAtUtc
    {
        get; set;
    }
}

public sealed class InterviewRecording
{
    public int Id
    {
        get; set;
    }
    public int InterviewRoundId
    {
        get; set;
    }
    public int ConsentDecisionId
    {
        get; set;
    }
    public InterviewRecordingDecision ConsentDecision { get; set; } = null!;
    public RecordingStorage StorageKind
    {
        get; set;
    }
    public int? DocumentId
    {
        get; set;
    }
    public Document? Document
    {
        get; set;
    }
    public string? WorkingRelativePath
    {
        get; set;
    }
    public string OriginalName { get; set; } = "";
    public string ContentType { get; set; } = "";
    public long Size
    {
        get; set;
    }
    public string Sha256 { get; set; } = "";
    public Guid RegistrationToken
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
