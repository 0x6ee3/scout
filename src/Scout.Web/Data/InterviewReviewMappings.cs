using Microsoft.EntityFrameworkCore;
using Scout.Web.Domain;

namespace Scout.Web.Data;

internal static class InterviewReviewMappings
{
    internal static void Configure(ModelBuilder model)
    {
        var decision = model.Entity<InterviewRecordingDecision>();
        decision.HasOne<InterviewRound>().WithMany().HasForeignKey(x => x.InterviewRoundId).OnDelete(DeleteBehavior.Restrict);
        decision.HasAlternateKey(x => new { x.Id, x.InterviewRoundId });
        decision.Property(x => x.PrivateNote).HasMaxLength(1000);
        var recording = model.Entity<InterviewRecording>();
        recording.HasOne<InterviewRound>().WithMany().HasForeignKey(x => x.InterviewRoundId).OnDelete(DeleteBehavior.Restrict);
        recording.HasOne(x => x.ConsentDecision).WithMany().HasForeignKey(x => new { x.ConsentDecisionId, x.InterviewRoundId }).HasPrincipalKey(x => new { x.Id, x.InterviewRoundId }).OnDelete(DeleteBehavior.Restrict);
        recording.HasOne(x => x.Document).WithMany().HasForeignKey(x => x.DocumentId).OnDelete(DeleteBehavior.Restrict);
        recording.HasIndex(x => x.RegistrationToken).IsUnique();
        recording.HasAlternateKey(x => new { x.Id, x.InterviewRoundId });
        recording.ToTable(t => t.HasCheckConstraint("CK_Recording_Source", "(StorageKind = 0 AND DocumentId IS NOT NULL AND WorkingRelativePath IS NULL) OR (StorageKind = 1 AND DocumentId IS NULL AND WorkingRelativePath IS NOT NULL)"));
        var transcript = model.Entity<InterviewTranscript>();
        transcript.HasOne<InterviewRound>().WithMany().HasForeignKey(x => x.InterviewRoundId).OnDelete(DeleteBehavior.Restrict);
        transcript.HasAlternateKey(x => new { x.Id, x.InterviewRoundId });
        transcript.HasOne<InterviewRecording>().WithMany().HasForeignKey(x => new { x.RecordingId, x.InterviewRoundId }).HasPrincipalKey(x => new { x.Id, x.InterviewRoundId }).OnDelete(DeleteBehavior.Restrict);
        transcript.HasOne<InterviewTranscript>().WithMany().HasForeignKey(x => new { x.ParentTranscriptId, x.InterviewRoundId }).HasPrincipalKey(x => new { x.Id, x.InterviewRoundId }).OnDelete(DeleteBehavior.Restrict);
        transcript.HasOne(x => x.OriginalDocument).WithMany().HasForeignKey(x => x.OriginalDocumentId).OnDelete(DeleteBehavior.Restrict);
        transcript.HasIndex(x => x.SubmissionToken).IsUnique();
        transcript.Property(x => x.Text).HasMaxLength(1_000_000);
        transcript.ToTable(t => t.HasCheckConstraint("CK_Transcript_Origin", "(Origin = 0 AND OriginalDocumentId IS NOT NULL AND ParentTranscriptId IS NULL) OR (Origin = 1 AND ParentTranscriptId IS NOT NULL)"));
    }
}
