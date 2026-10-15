using Microsoft.EntityFrameworkCore;
using Scout.Web.Domain;
namespace Scout.Web.Data;

internal static class SubmissionMappings
{
    public static void Configure(ModelBuilder b)
    {
        var c = b.Entity<PreparedCv>();
        c.HasOne(x => x.Document).WithMany().HasForeignKey(x => x.DocumentId).OnDelete(DeleteBehavior.Restrict);
        c.HasIndex(x => x.DocumentId).IsUnique();
        c.Property(x => x.DisplayName).HasMaxLength(200).IsRequired();
        c.Property(x => x.Language).HasMaxLength(20);
        c.Property(x => x.TechnologyFocus).HasMaxLength(200);
        var s = b.Entity<ApplicationSubmission>();
        s.ToTable("Submissions");
        s.HasOne(x => x.Application).WithMany().HasForeignKey(x => x.ApplicationId).OnDelete(DeleteBehavior.Restrict);
        s.HasIndex(x => x.RecordToken).IsUnique();
        s.Property(x => x.Notes).HasMaxLength(100000);
        s.Property(x => x.CorrectionReason).HasMaxLength(100000);
        s.Property(x => x.PayloadHash).HasMaxLength(64).IsRequired();
        var f = b.Entity<SubmissionFile>();
        f.HasKey(x => new { x.SubmissionId, x.DocumentId });
        f.HasOne(x => x.Submission).WithMany().HasForeignKey(x => x.SubmissionId).OnDelete(DeleteBehavior.Restrict);
        f.HasOne(x => x.Document).WithMany().HasForeignKey(x => x.DocumentId).OnDelete(DeleteBehavior.Restrict);
        f.Property(x => x.Role).HasConversion<string>().HasMaxLength(30);
        f.Property(x => x.LabelSnapshot).HasMaxLength(255).IsRequired();
        f.HasIndex(x => x.SubmissionId).IsUnique().HasFilter("Role = 'CV'");
        b.Entity<ApplicationTimelineEvent>().HasOne(x => x.Submission).WithMany().HasForeignKey(x => x.SubmissionId).OnDelete(DeleteBehavior.Restrict);
        b.Entity<ApplicationTimelineEvent>().HasIndex(x => x.SubmissionId).IsUnique();
    }
}
