using Microsoft.EntityFrameworkCore;
using Scout.Web.Domain;

namespace Scout.Web.Data;

internal static class PreparationMappings
{
    public static void Configure(ModelBuilder model)
    {
        var question = model.Entity<EmployerQuestion>();
        question.Property(x => x.Text).HasMaxLength(4000).IsRequired();
        question.Property(x => x.Intent).HasMaxLength(100000);
        question.Property(x => x.FollowUpQuestions).HasMaxLength(100000);
        question.Property(x => x.Notes).HasMaxLength(100000);
        question.HasIndex(x => x.IsArchived);
        var tag = model.Entity<EmployerQuestionTag>();
        tag.HasKey(x => new { x.EmployerQuestionId, x.Tag });
        tag.Property(x => x.Tag).HasMaxLength(50);
        tag.HasIndex(x => x.Tag);
        tag.HasOne(x => x.EmployerQuestion).WithMany(x => x.Tags)
            .HasForeignKey(x => x.EmployerQuestionId).OnDelete(DeleteBehavior.Restrict);
        var experience = model.Entity<ProfessionalExperience>();
        experience.Property(x => x.Title).HasMaxLength(200).IsRequired();
        experience.Property(x => x.MemoryCue).HasMaxLength(1000);
        foreach (var name in new[] { "Situation", "Task", "Action", "Result", "Reflection", "UsefulFor", "Notes" })
            experience.Property<string?>(name).HasMaxLength(100000);
        experience.HasIndex(x => x.IsArchived);
        var experienceTag = model.Entity<ExperienceTag>();
        experienceTag.HasKey(x => new { x.ProfessionalExperienceId, x.Tag });
        experienceTag.Property(x => x.Tag).HasMaxLength(50);
        experienceTag.HasIndex(x => x.Tag);
        experienceTag.HasOne(x => x.ProfessionalExperience).WithMany(x => x.Tags)
            .HasForeignKey(x => x.ProfessionalExperienceId).OnDelete(DeleteBehavior.Restrict);
        var prepared = model.Entity<InterviewQuestion>();
        prepared.Property(x => x.Text).HasMaxLength(4000).IsRequired();
        foreach (var name in new[] { "Intent", "FollowUpQuestions", "PreparationNotes", "Answer" })
            prepared.Property<string?>(name).HasMaxLength(100000);
        prepared.HasOne(x => x.InterviewRound).WithMany().HasForeignKey(x => x.InterviewRoundId).OnDelete(DeleteBehavior.Restrict);
        prepared.HasOne(x => x.EmployerQuestion).WithMany().HasForeignKey(x => x.EmployerQuestionId).OnDelete(DeleteBehavior.SetNull);
        prepared.HasIndex(x => new { x.InterviewRoundId, x.EmployerQuestionId }).IsUnique().HasFilter("\"EmployerQuestionId\" IS NOT NULL");
        prepared.HasIndex(x => new { x.InterviewRoundId, x.IsArchived, x.DisplayOrder });
        var selectedExperience = model.Entity<InterviewExperience>();
        selectedExperience.Property(x => x.PreparationNotes).HasMaxLength(100000);
        selectedExperience.HasOne(x => x.InterviewRound).WithMany().HasForeignKey(x => x.InterviewRoundId).OnDelete(DeleteBehavior.Restrict);
        selectedExperience.HasOne(x => x.ProfessionalExperience).WithMany().HasForeignKey(x => x.ProfessionalExperienceId).OnDelete(DeleteBehavior.Restrict);
        selectedExperience.HasIndex(x => new { x.InterviewRoundId, x.ProfessionalExperienceId }).IsUnique();
        selectedExperience.HasIndex(x => new { x.InterviewRoundId, x.IsArchived, x.DisplayOrder });
    }
}
