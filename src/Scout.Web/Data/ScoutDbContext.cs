using Microsoft.EntityFrameworkCore;
using Scout.Web.Domain;

namespace Scout.Web.Data;

public sealed class ScoutDbContext(DbContextOptions<ScoutDbContext> options) : DbContext(options)
{
    public DbSet<InterviewRecordingDecision> RecordingDecisions => Set<InterviewRecordingDecision>();
    public DbSet<InterviewRecording> Recordings => Set<InterviewRecording>();
    public DbSet<InterviewTranscript> Transcripts => Set<InterviewTranscript>();
    public DbSet<EmployerQuestion> EmployerQuestions => Set<EmployerQuestion>();
    public DbSet<EmployerQuestionTag> EmployerQuestionTags => Set<EmployerQuestionTag>();
    public DbSet<ProfessionalExperience> ProfessionalExperiences => Set<ProfessionalExperience>();
    public DbSet<ExperienceTag> ExperienceTags => Set<ExperienceTag>();
    public DbSet<InterviewQuestion> InterviewQuestions => Set<InterviewQuestion>();
    public DbSet<InterviewExperience> InterviewExperiences => Set<InterviewExperience>();
    public DbSet<Company> Companies => Set<Company>();
    public DbSet<Position> Positions => Set<Position>();
    public DbSet<JobApplication> Applications => Set<JobApplication>();
    public DbSet<ApplicationTimelineEvent> TimelineEvents => Set<ApplicationTimelineEvent>();
    public DbSet<Contact> Contacts => Set<Contact>();
    public DbSet<InterviewRound> Interviews => Set<InterviewRound>();
    public DbSet<InterviewParticipant> InterviewParticipants => Set<InterviewParticipant>();
    public DbSet<NextAction> NextActions => Set<NextAction>();
    public DbSet<Document> Documents => Set<Document>();
    public DbSet<PreparedCv> PreparedCvs => Set<PreparedCv>();
    public DbSet<ApplicationSubmission> Submissions => Set<ApplicationSubmission>();
    public DbSet<SubmissionFile> SubmissionFiles => Set<SubmissionFile>();
    public DbSet<CompanyDocument> CompanyDocuments => Set<CompanyDocument>();
    public DbSet<PositionDocument> PositionDocuments => Set<PositionDocument>();
    public DbSet<ApplicationDocument> ApplicationDocuments => Set<ApplicationDocument>();
    public DbSet<TimelineEventDocument> TimelineEventDocuments => Set<TimelineEventDocument>();
    public DbSet<InterviewDocument> InterviewDocuments => Set<InterviewDocument>();
    public DbSet<ApplicationContact> ApplicationContacts => Set<ApplicationContact>();
    public DbSet<TimelineEventContact> TimelineEventContacts => Set<TimelineEventContact>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        ApplicationMappings.Configure(modelBuilder);
        ContactMappings.Configure(modelBuilder);
        WorkMappings.Configure(modelBuilder);
        DocumentMappings.Configure(modelBuilder);
        SubmissionMappings.Configure(modelBuilder);
        PreparationMappings.Configure(modelBuilder);
        WorkspaceMappings.Configure(modelBuilder);
        InterviewReviewMappings.Configure(modelBuilder);
        var company = modelBuilder.Entity<Company>();
        company.Property(c => c.Name).HasMaxLength(200).IsRequired();
        company.Property(c => c.WebsiteUrl).HasMaxLength(2048);
        company.Property(c => c.Location).HasMaxLength(300);
        company.Property(c => c.Description).HasMaxLength(100_000);
        company.Property(c => c.PersonalNotes).HasMaxLength(100_000);
        company.Property(c => c.CreatedAtUtc).HasConversion(
            value => value,
            value => DateTime.SpecifyKind(value, DateTimeKind.Utc));
        company.Property(c => c.UpdatedAtUtc).HasConversion(
            value => value,
            value => DateTime.SpecifyKind(value, DateTimeKind.Utc));

        var position = modelBuilder.Entity<Position>();
        position.HasOne(p => p.Company).WithMany().HasForeignKey(p => p.CompanyId).OnDelete(DeleteBehavior.Restrict);
        position.Property(p => p.Title).HasMaxLength(200).IsRequired();
        position.Property(p => p.OriginalVacancyUrl).HasMaxLength(2048);
        position.Property(p => p.Location).HasMaxLength(300);
        position.Property(p => p.EmploymentType).HasMaxLength(100);
        position.Property(p => p.RemoteWorkNotes).HasMaxLength(100_000);
        position.Property(p => p.PersonalNotes).HasMaxLength(100_000);
        position.Property(p => p.JobDescriptionSnapshot).HasMaxLength(100_000);
        position.Property(p => p.Status).HasConversion<string>().HasMaxLength(30);
        position.Property(p => p.CreatedAtUtc).HasConversion(v => v, v => DateTime.SpecifyKind(v, DateTimeKind.Utc));
        position.Property(p => p.UpdatedAtUtc).HasConversion(v => v, v => DateTime.SpecifyKind(v, DateTimeKind.Utc));
        position.Property(p => p.DescriptionCapturedAtUtc).HasConversion(v => v,
            v => v.HasValue ? DateTime.SpecifyKind(v.Value, DateTimeKind.Utc) : (DateTime?)null);
        foreach (var entity in modelBuilder.Model.GetEntityTypes())
            foreach (var property in entity.GetProperties())
            {
                if (property.ClrType == typeof(DateTime))
                    property.SetValueConverter(new Microsoft.EntityFrameworkCore.Storage.ValueConversion.ValueConverter<DateTime, DateTime>(v => v, v => DateTime.SpecifyKind(v, DateTimeKind.Utc)));
                if (property.ClrType == typeof(DateTime?))
                    property.SetValueConverter(new Microsoft.EntityFrameworkCore.Storage.ValueConversion.ValueConverter<DateTime?, DateTime?>(v => v, v => v.HasValue ? DateTime.SpecifyKind(v.Value, DateTimeKind.Utc) : null));
            }
    }
}
