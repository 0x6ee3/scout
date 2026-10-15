using Microsoft.EntityFrameworkCore;
using Scout.Web.Domain;
namespace Scout.Web.Data;

internal static class WorkspaceMappings
{
    public static void Configure(ModelBuilder b)
    {
        var question = b.Entity<InterviewQuestion>();
        question.Property(x => x.ContentRevision).IsConcurrencyToken();
        question.Property(x => x.Origin).HasConversion<string>().HasMaxLength(20).HasDefaultValue(InterviewQuestionOrigin.Library);
        question.HasIndex(x => new { x.InterviewRoundId, x.CreationToken }).IsUnique().HasFilter("CreationToken IS NOT NULL");
        question.ToTable(t => t.HasCheckConstraint("CK_Question_Origin", "Origin IN ('Library','AdHoc') AND (Origin != 'AdHoc' OR EmployerQuestionId IS NULL)"));
        b.Entity<InterviewRound>().Property(x => x.NotesRevision).IsConcurrencyToken();
        var c = b.Entity<InterviewWorkspaceConfiguration>();
        c.HasKey(x => x.InterviewRoundId);
        c.HasOne(x => x.InterviewRound).WithOne().HasForeignKey<InterviewWorkspaceConfiguration>(x => x.InterviewRoundId).OnDelete(DeleteBehavior.Restrict);
        c.HasIndex(x => x.WorkspaceId).IsUnique();
        c.Property(x => x.ConfigurationRevision).IsConcurrencyToken();
        c.HasOne(x => x.Cv).WithMany().HasForeignKey(x => new { x.SubmissionId, x.DocumentId }).OnDelete(DeleteBehavior.Restrict);
        c.ToTable(t => t.HasCheckConstraint("CK_Workspace_Cv", "(SubmissionId IS NULL AND DocumentId IS NULL) OR (SubmissionId IS NOT NULL AND DocumentId IS NOT NULL)"));
        foreach (var name in new[] { "RoundTitle", "CompanyName", "PositionTitle" })
            c.Property<string?>(name).HasMaxLength(200);
        c.Property(x => x.JobDescription).HasMaxLength(100000);
        c.Property(x => x.PreparationNotes).HasMaxLength(100000);
        b.Entity<InterviewQuestion>().HasAlternateKey(x => new { x.InterviewRoundId, x.Id });
        b.Entity<InterviewExperience>().HasAlternateKey(x => new { x.InterviewRoundId, x.Id });
        var q = b.Entity<WorkspaceQuestion>();
        q.HasKey(x => new { x.InterviewRoundId, x.InterviewQuestionId });
        q.HasOne(x => x.Configuration).WithMany().HasForeignKey(x => x.InterviewRoundId).OnDelete(DeleteBehavior.Restrict);
        q.HasOne(x => x.Question).WithMany().HasForeignKey(x => new { x.InterviewRoundId, x.InterviewQuestionId }).HasPrincipalKey(x => new { x.InterviewRoundId, x.Id }).OnDelete(DeleteBehavior.Restrict);
        q.Property(x => x.Text).HasMaxLength(4000);
        var e = b.Entity<WorkspaceExperience>();
        e.HasKey(x => new { x.InterviewRoundId, x.InterviewExperienceId });
        e.HasOne(x => x.Configuration).WithMany().HasForeignKey(x => x.InterviewRoundId).OnDelete(DeleteBehavior.Restrict);
        e.HasOne(x => x.Experience).WithMany().HasForeignKey(x => new { x.InterviewRoundId, x.InterviewExperienceId }).HasPrincipalKey(x => new { x.InterviewRoundId, x.Id }).OnDelete(DeleteBehavior.Restrict);
        e.Property(x => x.Title).HasMaxLength(200);
        e.Property(x => x.MemoryCue).HasMaxLength(1000);
        var p = b.Entity<WorkspacePriorAnswer>();
        p.HasOne(x => x.Configuration).WithMany().HasForeignKey(x => x.InterviewRoundId).OnDelete(DeleteBehavior.Restrict);
        p.HasOne(x => x.SourceQuestion).WithMany().HasForeignKey(x => x.SourceQuestionId).OnDelete(DeleteBehavior.Restrict);
        p.Property(x => x.Text).HasMaxLength(4000);
        p.Property(x => x.RoundTitle).HasMaxLength(200);
        p.Property(x => x.Answer).HasMaxLength(100000);
    }
}
