using System.ComponentModel.DataAnnotations;
using Microsoft.EntityFrameworkCore;
using Scout.Web.Data;
using Scout.Web.Domain;

namespace Scout.Web.Features.InterviewWorkspace;

public sealed partial class WorkspaceOperations
{
    private static async Task<string> ChoiceStatusAsync(ScoutDbContext db, int id)
    {
        var choice = await db.RecordingDecisions.Where(x => x.InterviewRoundId == id).OrderByDescending(x => x.Id).Select(x => (RecordingChoice?)x.Choice).FirstOrDefaultAsync();
        return choice switch
        {
            RecordingChoice.NotesOnly => "Continue without recording",
            RecordingChoice.ExternalConsentAcknowledged => "Consent received — use external recorder",
            _ => "No recording choice recorded"
        };
    }
    public async Task ChooseRecordingAsync(int id, int revision, RecordingChoice choice)
    {
        if (!Enum.IsDefined(choice))
            throw new ValidationException("Choose a recording option.");
        await using var db = await factory.CreateDbContextAsync();
        await using var tx = await db.Database.BeginTransactionAsync();
        if (!await AllowedAsync(db, id, revision) || !await db.Set<InterviewWorkspaceConfiguration>().AnyAsync(x => x.InterviewRoundId == id && x.RecordingChoiceEnabled && !x.InterviewRound.IsArchived))
            throw new ValidationException("Recording choice is unavailable. Review the Workspace privately.");
        db.RecordingDecisions.Add(new()
        {
            InterviewRoundId = id,
            Choice = choice,
            AcknowledgedAtUtc = DateTime.UtcNow,
            CreatedAtUtc = DateTime.UtcNow
        });
        await db.SaveChangesAsync();
        await tx.CommitAsync();
    }
}
