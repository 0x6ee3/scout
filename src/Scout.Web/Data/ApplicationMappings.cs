using Microsoft.EntityFrameworkCore;
using Scout.Web.Domain;

namespace Scout.Web.Data;

internal static class ApplicationMappings
{
    public static void Configure(ModelBuilder model)
    {
        var app = model.Entity<JobApplication>();
        app.ToTable("Applications");
        app.HasOne(a => a.Position).WithMany().HasForeignKey(a => a.PositionId).OnDelete(DeleteBehavior.Restrict);
        app.Property(a => a.AttemptLabel).HasMaxLength(200);
        app.Property(a => a.Notes).HasMaxLength(100000);
        app.Property(a => a.OutcomeNotes).HasMaxLength(100000);
        app.Property(a => a.Status).HasConversion<string>().HasMaxLength(30);
        var ev = model.Entity<ApplicationTimelineEvent>();
        ev.ToTable("TimelineEvents");
        ev.HasOne(e => e.Application).WithMany().HasForeignKey(e => e.ApplicationId).OnDelete(DeleteBehavior.Restrict);
        ev.Property(e => e.Kind).HasConversion<string>().HasMaxLength(40);
        ev.Property(e => e.Description).HasMaxLength(100000).IsRequired();
        ev.Property(e => e.PreviousStatus).HasConversion<string>().HasMaxLength(30);
        ev.Property(e => e.NewStatus).HasConversion<string>().HasMaxLength(30);
        ev.HasIndex(e => new { e.ApplicationId, e.EventDate });
    }
}
