using System.ComponentModel.DataAnnotations;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Scout.Web.Data;
using Scout.Web.Domain;
using Scout.Web.Features.Applications;
using Scout.Web.Storage;
namespace Scout.Web.Features.Submissions;

public sealed class SubmissionEditModel
{
    public int ApplicationId
    {
        get; set;
    }
    public DateOnly SubmittedOn { get; set; } = DateOnly.FromDateTime(DateTime.Today);
    [StringLength(100000)]
    public string? Notes
    {
        get; set;
    }
    public int? PreparedCvId
    {
        get; set;
    }
    [StringLength(200)]
    public string? ReviewedCvLabel
    {
        get; set;
    }
    public HashSet<int> CoverLetterIds { get; set; } = [];
    public HashSet<int> OtherDocumentIds { get; set; } = [];
    public bool SetAppliedStatus
    {
        get; set;
    }
    public bool SetAppliedOnIfEmpty
    {
        get; set;
    }
    public Guid RecordToken { get; set; } = Guid.NewGuid();
}
public sealed class SubmissionOperations(IDbContextFactory<ScoutDbContext> factory, DocumentFileStore files)
{
    public async Task<IReadOnlyList<ApplicationSubmission>> ListAsync(int applicationId)
    {
        await using var db = await factory.CreateDbContextAsync();
        return await db.Submissions.AsNoTracking().Where(s => s.ApplicationId == applicationId).OrderByDescending(s => s.SubmittedOn).ThenByDescending(s => s.Id).ToListAsync();
    }
    public async Task<ApplicationSubmission?> GetAsync(int id)
    {
        await using var db = await factory.CreateDbContextAsync();
        return await db.Submissions.AsNoTracking().SingleOrDefaultAsync(s => s.Id == id);
    }
    public async Task<IReadOnlyList<SubmissionFile>> FilesAsync(int id)
    {
        await using var db = await factory.CreateDbContextAsync();
        return await db.SubmissionFiles.AsNoTracking().Include(f => f.Document).Where(f => f.SubmissionId == id).OrderBy(f => f.Role).ThenBy(f => f.DocumentId).ToListAsync();
    }
    private static string Fingerprint(SubmissionEditModel m) => Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(JsonSerializer.Serialize(new { m.ApplicationId, m.SubmittedOn, Notes = InputRules.Text(m.Notes), m.PreparedCvId, m.ReviewedCvLabel, Cover = m.CoverLetterIds.Order().ToArray(), Other = m.OtherDocumentIds.Order().ToArray(), m.SetAppliedStatus, m.SetAppliedOnIfEmpty }))));
    public async Task<int> RecordAsync(SubmissionEditModel m)
    {
        InputRules.Validate(m);
        if (m.RecordToken == Guid.Empty || m.SubmittedOn == default)
            throw new ValidationException("A submission date and confirmation identity are required.");
        var fingerprint = Fingerprint(m);
        await using var db = await factory.CreateDbContextAsync();
        var prior = await db.Submissions.SingleOrDefaultAsync(s => s.RecordToken == m.RecordToken);
        if (prior is not null)
            return SameAttempt(prior, fingerprint);
        var a = await db.Applications.SingleOrDefaultAsync(a => a.Id == m.ApplicationId) ?? throw new ValidationException("Application no longer exists.");
        if (a.IsArchived)
            throw new ValidationException("Restore the application before recording a submission.");
        var selected = new List<(Document Document, SubmissionFileRole Role, string Label)>();
        if (m.PreparedCvId.HasValue)
        {
            var cv = await db.PreparedCvs.Include(c => c.Document).SingleOrDefaultAsync(c => c.Id == m.PreparedCvId);
            if (cv is null || cv.IsArchived || cv.Document.IsArchived)
                throw new ValidationException("Choose an active prepared CV and document.");
            selected.Add((cv.Document, SubmissionFileRole.CV, m.ReviewedCvLabel ?? cv.DisplayName));
        }
        foreach (var selection in new[] { (Ids: m.CoverLetterIds, Role: SubmissionFileRole.CoverLetter), (Ids: m.OtherDocumentIds, Role: SubmissionFileRole.Other) })
            foreach (var id in selection.Ids)
            {
                var d = await db.Documents.FindAsync(id);
                if (d is null || d.IsArchived)
                    throw new ValidationException("Choose active documents.");
                selected.Add((d, selection.Role, d.OriginalName));
            }
        if (selected.Select(x => x.Document.Id).Distinct().Count() != selected.Count)
            throw new ValidationException("Select each file once, with one role.");
        foreach (var d in selected)
        {
            await using var verified = await files.OpenVerifiedAsync(d.Document);
        }
        var now = DateTime.UtcNow;
        var submission = new ApplicationSubmission { ApplicationId = a.Id, SubmittedOn = m.SubmittedOn, Notes = InputRules.Text(m.Notes), RecordedAtUtc = now, RecordToken = m.RecordToken, PayloadHash = fingerprint };
        db.Submissions.Add(submission);
        db.SubmissionFiles.AddRange(selected.Select(x => new SubmissionFile { Submission = submission, DocumentId = x.Document.Id, Role = x.Role, LabelSnapshot = x.Label }));
        db.TimelineEvents.Add(new()
        {
            ApplicationId = a.Id,
            Submission = submission,
            EventDate = m.SubmittedOn,
            Kind = TimelineKind.ApplicationSubmitted,
            Description = $"Submission recorded as already sent outside Scout on {m.SubmittedOn:yyyy-MM-dd}. {(selected.Count == 0 ? "No files recorded." : $"{selected.Count} file(s) preserved.")}",
            IsSystem = true,
            CreatedAtUtc = now,
            UpdatedAtUtc = now
        });
        if (m.SetAppliedStatus && a.Status != ApplicationStatus.Applied)
        {
            var old = a.Status;
            a.Status = ApplicationStatus.Applied;
            db.TimelineEvents.Add(ApplicationOperations.SystemEvent(a, TimelineKind.StatusChanged, $"Status changed from {old} to Applied while recording a submission.", now, old, ApplicationStatus.Applied));
        }
        if (m.SetAppliedOnIfEmpty && a.AppliedOn is null)
            a.AppliedOn = m.SubmittedOn;
        if (m.SetAppliedStatus || m.SetAppliedOnIfEmpty)
            a.UpdatedAtUtc = now;
        try
        {
            await db.SaveChangesAsync();
            return submission.Id;
        }
        catch (DbUpdateException) { await using var check = await factory.CreateDbContextAsync(); prior = await check.Submissions.AsNoTracking().SingleOrDefaultAsync(s => s.RecordToken == m.RecordToken); if (prior is not null) return SameAttempt(prior, fingerprint); throw; }
    }
    private static int SameAttempt(ApplicationSubmission prior, string fingerprint)
    {
        if (prior.PayloadHash != fingerprint)
            throw new ValidationException("This confirmation identity was already used with different details. Open a new submission form to record a separate submission.");
        return prior.Id;
    }
    public async Task<bool> MarkIncorrectAsync(int id, string reason)
    {
        if (string.IsNullOrWhiteSpace(reason) || reason.Length > 100000)
            throw new ValidationException("Enter a correction reason of at most 100,000 characters.");
        await using var db = await factory.CreateDbContextAsync();
        var s = await db.Submissions.FindAsync(id);
        if (s is null)
            return false;
        if (!s.IsIncorrect)
        {
            s.IsIncorrect = true;
            s.CorrectionReason = reason;
            await db.SaveChangesAsync();
        }
        return true;
    }
}
