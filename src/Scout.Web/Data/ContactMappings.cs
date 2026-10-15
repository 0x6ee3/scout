using Microsoft.EntityFrameworkCore;
using Scout.Web.Domain;
namespace Scout.Web.Data;

internal static class ContactMappings
{
    public static void Configure(ModelBuilder b)
    {
        var c = b.Entity<Contact>();
        c.HasOne(x => x.Company).WithMany().HasForeignKey(x => x.CompanyId).OnDelete(DeleteBehavior.Restrict);
        c.Property(x => x.Name).HasMaxLength(200).IsRequired();
        c.Property(x => x.Role).HasMaxLength(200);
        c.Property(x => x.Email).HasMaxLength(320);
        c.Property(x => x.Phone).HasMaxLength(100);
        c.Property(x => x.ProfileUrl).HasMaxLength(2048);
        c.Property(x => x.Notes).HasMaxLength(100000);
        var a = b.Entity<ApplicationContact>();
        a.HasKey(x => new { x.ApplicationId, x.ContactId });
        a.HasOne(x => x.Application).WithMany().HasForeignKey(x => x.ApplicationId).OnDelete(DeleteBehavior.Restrict);
        a.HasOne(x => x.Contact).WithMany().HasForeignKey(x => x.ContactId).OnDelete(DeleteBehavior.Restrict);
        var e = b.Entity<TimelineEventContact>();
        e.HasKey(x => new { x.EventId, x.ContactId });
        e.HasOne(x => x.Event).WithMany().HasForeignKey(x => x.EventId).OnDelete(DeleteBehavior.Restrict);
        e.HasOne(x => x.Contact).WithMany().HasForeignKey(x => x.ContactId).OnDelete(DeleteBehavior.Restrict);
    }
}
