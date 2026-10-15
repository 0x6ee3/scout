using Microsoft.EntityFrameworkCore;
using Scout.Web.Domain;
namespace Scout.Web.Data;

internal static class WorkMappings
{
    public static void Configure(ModelBuilder b)
    {
        var r = b.Entity<InterviewRound>();
        r.ToTable("Interviews");
        r.HasOne(x => x.Application).WithMany().HasForeignKey(x => x.ApplicationId).OnDelete(DeleteBehavior.Restrict);
        r.Property(x => x.Title).HasMaxLength(200).IsRequired();
        r.Property(x => x.Type).HasConversion<string>().HasMaxLength(30);
        r.Property(x => x.Status).HasConversion<string>().HasMaxLength(30);
        r.Property(x => x.LocationOrMeetingUrl).HasMaxLength(2048);
        r.Property(x => x.PreparationNotes).HasMaxLength(100000);
        r.Property(x => x.InterviewNotes).HasMaxLength(100000);
        var p = b.Entity<InterviewParticipant>();
        p.HasKey(x => new { x.InterviewRoundId, x.ContactId });
        p.HasOne(x => x.InterviewRound).WithMany().HasForeignKey(x => x.InterviewRoundId).OnDelete(DeleteBehavior.Restrict);
        p.HasOne(x => x.Contact).WithMany().HasForeignKey(x => x.ContactId).OnDelete(DeleteBehavior.Restrict);
        b.Entity<ApplicationTimelineEvent>().HasOne(x => x.InterviewRound).WithMany().HasForeignKey(x => x.InterviewRoundId).OnDelete(DeleteBehavior.Restrict);
        var a = b.Entity<NextAction>();
        a.ToTable("NextActions", t => { t.HasCheckConstraint("CK_NextAction_Owner", "(ApplicationId IS NOT NULL AND PositionId IS NULL) OR (ApplicationId IS NULL AND PositionId IS NOT NULL)"); t.HasCheckConstraint("CK_NextAction_Context", "(InterviewRoundId IS NULL OR TimelineEventId IS NULL) AND (PositionId IS NULL OR (InterviewRoundId IS NULL AND TimelineEventId IS NULL))"); });
        a.HasOne(x => x.Application).WithMany().HasForeignKey(x => x.ApplicationId).OnDelete(DeleteBehavior.Restrict);
        a.HasOne(x => x.Position).WithMany().HasForeignKey(x => x.PositionId).OnDelete(DeleteBehavior.Restrict);
        a.HasOne(x => x.InterviewRound).WithMany().HasForeignKey(x => x.InterviewRoundId).OnDelete(DeleteBehavior.Restrict);
        a.HasOne(x => x.TimelineEvent).WithMany().HasForeignKey(x => x.TimelineEventId).OnDelete(DeleteBehavior.Restrict);
        a.Property(x => x.Title).HasMaxLength(200).IsRequired();
        a.Property(x => x.Description).HasMaxLength(100000);
        a.Property(x => x.Status).HasConversion<string>().HasMaxLength(30);
        a.HasIndex(x => new { x.Status, x.DueOn });
    }
}
