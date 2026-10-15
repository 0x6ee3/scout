using Microsoft.EntityFrameworkCore;
using Scout.Web.Domain;
using Scout.Web.Storage;

namespace Scout.Web.Data;

public static class DevelopmentSamples
{
    public static async Task<bool> SeedAsync(ScoutDbContext context, bool isDevelopment, bool enabled, DocumentFileStore files)
    {
        if (!isDevelopment || !enabled)
            return false;

        // SQLite's write transaction keeps the emptiness check and insert together.
        await using var transaction = await context.Database.BeginTransactionAsync();
        // Include every business table, including standalone preparation entries.
        foreach (var table in context.Model.GetEntityTypes().Select(e => e.GetTableName()).Distinct())
        {
            var quoted = table!.Replace("\"", "\"\"");
            var emptinessSql = $"SELECT EXISTS(SELECT 1 FROM \"{quoted}\") AS Value";
            if (await context.Database.SqlQueryRaw<int>(emptinessSql).SingleAsync() != 0)
                return false;
        }

        var captured = new DateTime(2026, 10, 1, 9, 0, 0, DateTimeKind.Utc);
        var northern = new Company
        {
            Name = "[Sample] Nordlicht Software",
            WebsiteUrl = "https://nordlicht.example",
            Location = "München",
            Description = "Fictional software company.\nEntwickelt Werkzeuge für kleine Teams.",
            PersonalNotes = "Sample only. Ask about pairing and team routines.",
            CreatedAtUtc = captured,
            UpdatedAtUtc = captured
        };
        var river = new Company
        {
            Name = "[Sample] Riverstone Labs",
            WebsiteUrl = "https://riverstone.example",
            Location = "Berlin / Remote",
            Description = "Fictional research tools company.",
            IsArchived = true,
            CreatedAtUtc = captured,
            UpdatedAtUtc = captured
        };
        context.Companies.AddRange(northern, river);
        var backend = Vacancy(northern, "Backend developer", PositionStatus.Interested, new(2026, 10, 1));
        var platform = Vacancy(northern, "Platform engineer", PositionStatus.Discovered, new(2026, 10, 3));
        context.Positions.AddRange(backend, platform,
            Vacancy(northern, "Technical lead", PositionStatus.RejectedByUser, new(2026, 9, 28), archived: true),
            Vacancy(river, "Research software engineer", PositionStatus.Expired, new(2026, 9, 20)));
        await context.SaveChangesAsync();
        var application = new JobApplication { Position = backend, AttemptLabel = "[Sample] Initial attempt", Status = ApplicationStatus.Screening, AppliedOn = new(2026, 10, 2), Notes = "Fictional sample process.\nFragen und Beobachtungen hier festhalten.", CreatedAtUtc = captured, UpdatedAtUtc = captured };
        var recruiter = new Contact { Company = northern, Name = "[Sample] Alex Beispiel", Role = "Recruiter", Email = "alex@nordlicht.example", CreatedAtUtc = captured, UpdatedAtUtc = captured };
        var interviewer = new Contact { Company = northern, Name = "[Sample] Sam Muster", Role = "Engineering", CreatedAtUtc = captured, UpdatedAtUtc = captured };
        var round = new InterviewRound { Application = application, Title = "[Sample] Engineering conversation", Type = InterviewType.Engineering, Status = InterviewStatus.Planned, PreparationNotes = "Discuss maintenance, pairing, and delivery.", CreatedAtUtc = captured, UpdatedAtUtc = captured };
        var completed = new InterviewRound { Application = application, Title = "[Sample] Recruiter call", Type = InterviewType.HR, Status = InterviewStatus.Completed, InterviewNotes = "Notes-only historical example.\nKeine Aufnahme vorhanden.", CreatedAtUtc = captured, UpdatedAtUtc = captured };
        context.Applications.Add(application);
        context.Contacts.AddRange(recruiter, interviewer);
        context.Interviews.AddRange(round, completed);
        context.ApplicationContacts.Add(new()
        {
            Application = application,
            Contact = recruiter
        });
        context.InterviewParticipants.Add(new()
        {
            InterviewRound = round,
            Contact = interviewer
        });
        context.NextActions.AddRange(new NextAction { Application = application, Title = "[Sample] Prepare engineering questions", InterviewRound = round, DueOn = new(2026, 10, 16), CreatedAtUtc = captured, UpdatedAtUtc = captured }, new NextAction { Position = platform, Title = "[Sample] Review platform vacancy", CreatedAtUtc = captured, UpdatedAtUtc = captured });
        await using var pdf = typeof(DevelopmentSamples).Assembly.GetManifestResourceStream("Scout.Web.Data.SampleCv.pdf")!;
        var stored = await files.ImportAsync("Fictional sample CV.pdf", pdf);
        var document = new Document { OriginalName = "Fictional sample CV.pdf", StoredRelativePath = stored.RelativePath, ContentType = stored.ContentType, Size = stored.Size, Sha256 = stored.Sha256, ImportedAtUtc = captured, CreatedAtUtc = captured, UpdatedAtUtc = captured, Category = "CV", Description = "Fictional development fixture only." };
        var cv = new PreparedCv { Document = document, DisplayName = "[Sample] Backend CV", Language = "en", TechnologyFocus = ".NET", CreatedAtUtc = captured, UpdatedAtUtc = captured };
        var submission = new ApplicationSubmission { Application = application, SubmittedOn = new(2026, 10, 2), Notes = "Fictional example of a manually recorded submission.", RecordedAtUtc = captured, RecordToken = Guid.NewGuid(), PayloadHash = Convert.ToHexString(System.Security.Cryptography.SHA256.HashData("fictional seed"u8)) };
        context.Documents.Add(document);
        context.PreparedCvs.Add(cv);
        context.Submissions.Add(submission);
        context.SubmissionFiles.Add(new()
        {
            Submission = submission,
            Document = document,
            Role = SubmissionFileRole.CV,
            LabelSnapshot = cv.DisplayName
        });
        context.ApplicationDocuments.Add(new()
        {
            Application = application,
            Document = document
        });
        context.CompanyDocuments.Add(new()
        {
            Company = northern,
            Document = document
        });
        context.TimelineEvents.AddRange(new ApplicationTimelineEvent { Application = application, EventDate = new(2026, 10, 1), OccurredAtUtc = captured, Kind = TimelineKind.ApplicationCreated, IsSystem = true, Description = "Fictional sample application created.", CreatedAtUtc = captured, UpdatedAtUtc = captured }, new ApplicationTimelineEvent { Application = application, Submission = submission, EventDate = submission.SubmittedOn, Kind = TimelineKind.ApplicationSubmitted, IsSystem = true, Description = "Fictional sample submission already sent outside Scout.", CreatedAtUtc = captured, UpdatedAtUtc = captured });
        var conversation = new ApplicationTimelineEvent { Application = application, EventDate = new(2026, 10, 5), Kind = TimelineKind.RecruiterCall, Description = "Fictional historical call; time not recorded.", CreatedAtUtc = captured, UpdatedAtUtc = captured };
        context.TimelineEvents.Add(conversation);
        context.TimelineEventContacts.Add(new()
        {
            Event = conversation,
            Contact = recruiter
        });
        var question = new EmployerQuestion
        {
            Text = "[Sample] How are technical decisions made?",
            Intent = "Understand the team's decision process.",
            FollowUpQuestions = "How are alternatives documented?",
            Notes = "Fictional private library note; not copied to a round.",
            CreatedAtUtc = captured, UpdatedAtUtc = captured,
            Tags = [new() { Tag = "technical-decisions" }, new() { Tag = "teamwork" }]
        };
        var office = new EmployerQuestion { Text = "[Sample] What is the office attendance policy?", CreatedAtUtc = captured, UpdatedAtUtc = captured, Tags = [new() { Tag = "working-arrangements" }] };
        var story = new ProfessionalExperience
        {
            Title = "[Sample] Fictional design disagreement", MemoryCue = "Compare two options with a small experiment.",
            Situation = "Fictional example: a team had two design options.", Task = "Help compare maintenance costs.",
            Action = "List tradeoffs and try a small prototype together.", Result = "The fictional team agreed on one option.",
            Reflection = "Make evaluation criteria explicit earlier.", UsefulFor = "Describe a technical disagreement.",
            Notes = "Fictional fixture, not the user's professional history.", CreatedAtUtc = captured, UpdatedAtUtc = captured,
            Tags = [new() { Tag = "teamwork" }, new() { Tag = "team-conflict" }]
        };
        var partial = new ProfessionalExperience { Title = "[Sample] Fictional mentoring cue", MemoryCue = "An intentionally incomplete STAR example.", CreatedAtUtc = captured, UpdatedAtUtc = captured, Tags = [new() { Tag = "mentoring" }] };
        context.EmployerQuestions.AddRange(question, office);
        context.ProfessionalExperiences.AddRange(story, partial);
        context.InterviewQuestions.Add(new() { InterviewRound = round, EmployerQuestion = question, Text = question.Text, Intent = question.Intent, FollowUpQuestions = question.FollowUpQuestions, Answer = "[Sample] Fictional answer: compare alternatives and document the decision.", DisplayOrder = 0, CreatedAtUtc = captured, UpdatedAtUtc = captured });
        context.InterviewExperiences.Add(new() { InterviewRound = round, ProfessionalExperience = story, PreparationNotes = "Fictional rehearsal cue for this round only.", DisplayOrder = 0, CreatedAtUtc = captured, UpdatedAtUtc = captured });
        await context.SaveChangesAsync();
        await transaction.CommitAsync();
        return true;

        Position Vacancy(Company company, string title, PositionStatus status, DateOnly discovered, bool archived = false) => new()
        {
            Company = company,
            Title = title,
            Status = status,
            DiscoveredOn = discovered,
            ClosingOn = new(2026, 10, 31),
            Location = company.Location,
            EmploymentType = "Full-time",
            OriginalVacancyUrl = company.WebsiteUrl + "/careers",
            JobDescriptionSnapshot = "Fictional sample vacancy.\nWir suchen Menschen mit Freude an wartbarer Software.\nWork with a small team on local tools.",
            DescriptionCapturedAtUtc = captured,
            PersonalNotes = "Sample observation: clarify responsibilities.",
            RemoteWorkNotes = "Sample arrangement: discuss office and remote days.",
            IsArchived = archived,
            CreatedAtUtc = captured,
            UpdatedAtUtc = captured
        };
    }
}

