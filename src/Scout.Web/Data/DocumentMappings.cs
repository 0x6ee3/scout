using Microsoft.EntityFrameworkCore;
using Scout.Web.Domain;
namespace Scout.Web.Data;

internal static class DocumentMappings
{
    public static void Configure(ModelBuilder b)
    {
        var d = b.Entity<Document>();
        d.Property(x => x.OriginalName).HasMaxLength(255).IsRequired();
        d.Property(x => x.StoredRelativePath).HasMaxLength(200).IsRequired();
        d.HasIndex(x => x.StoredRelativePath).IsUnique();
        d.Property(x => x.ContentType).HasMaxLength(100).IsRequired();
        d.Property(x => x.Sha256).HasMaxLength(64).IsRequired();
        d.Property(x => x.Description).HasMaxLength(100000);
        d.Property(x => x.Category).HasMaxLength(200);
        var c = b.Entity<CompanyDocument>();
        c.HasKey(x => new { x.CompanyId, x.DocumentId });
        c.HasOne(x => x.Company).WithMany().HasForeignKey(x => x.CompanyId).OnDelete(DeleteBehavior.Restrict);
        c.HasOne(x => x.Document).WithMany().HasForeignKey(x => x.DocumentId).OnDelete(DeleteBehavior.Restrict);
        var p = b.Entity<PositionDocument>();
        p.HasKey(x => new { x.PositionId, x.DocumentId });
        p.HasOne(x => x.Position).WithMany().HasForeignKey(x => x.PositionId).OnDelete(DeleteBehavior.Restrict);
        p.HasOne(x => x.Document).WithMany().HasForeignKey(x => x.DocumentId).OnDelete(DeleteBehavior.Restrict);
        var a = b.Entity<ApplicationDocument>();
        a.HasKey(x => new { x.ApplicationId, x.DocumentId });
        a.HasOne(x => x.Application).WithMany().HasForeignKey(x => x.ApplicationId).OnDelete(DeleteBehavior.Restrict);
        a.HasOne(x => x.Document).WithMany().HasForeignKey(x => x.DocumentId).OnDelete(DeleteBehavior.Restrict);
        var e = b.Entity<TimelineEventDocument>();
        e.HasKey(x => new { x.EventId, x.DocumentId });
        e.HasOne(x => x.Event).WithMany().HasForeignKey(x => x.EventId).OnDelete(DeleteBehavior.Restrict);
        e.HasOne(x => x.Document).WithMany().HasForeignKey(x => x.DocumentId).OnDelete(DeleteBehavior.Restrict);
        var r = b.Entity<InterviewDocument>();
        r.HasKey(x => new { x.InterviewRoundId, x.DocumentId });
        r.HasOne(x => x.InterviewRound).WithMany().HasForeignKey(x => x.InterviewRoundId).OnDelete(DeleteBehavior.Restrict);
        r.HasOne(x => x.Document).WithMany().HasForeignKey(x => x.DocumentId).OnDelete(DeleteBehavior.Restrict);
    }
}
