using System.ComponentModel.DataAnnotations;
using Microsoft.EntityFrameworkCore;
using Scout.Web.Data;
using Scout.Web.Domain;

namespace Scout.Web.Features.Contacts;

public sealed class ContactOperations(IDbContextFactory<ScoutDbContext> factory)
{
    public async Task<IReadOnlyList<ContactListItem>> ListAsync(int? companyId = null, ArchiveFilter archive = ArchiveFilter.Active, string? search = null)
    {
        await using var db = await factory.CreateDbContextAsync();
        var query = db.Contacts.AsNoTracking();
        if (companyId.HasValue)
            query = query.Where(c => c.CompanyId == companyId);
        if (archive != ArchiveFilter.All)
            query = query.Where(c => c.IsArchived == (archive == ArchiveFilter.Archived));
        var rows = await query.Select(c => new ContactListItem(c.Id, c.CompanyId, c.Company.Name, c.Name, c.Role, c.Email, c.IsArchived)).ToListAsync();
        return rows.Where(c => string.IsNullOrWhiteSpace(search) || $"{c.Name} {c.Role} {c.Email} {c.CompanyName}".Contains(search.Trim(), StringComparison.OrdinalIgnoreCase))
            .OrderBy(c => c.Name, StringComparer.OrdinalIgnoreCase).ThenBy(c => c.Id).ToList();
    }

    public async Task<Contact?> GetAsync(int id)
    {
        await using var db = await factory.CreateDbContextAsync();
        return await db.Contacts.AsNoTracking().Include(c => c.Company).SingleOrDefaultAsync(c => c.Id == id);
    }

    public async Task<int> SaveAsync(int? id, ContactEditModel input)
    {
        var m = InputRules.Validate(new ContactEditModel { CompanyId = input.CompanyId, Name = input.Name.Trim(), Role = InputRules.Trim(input.Role), Email = InputRules.Trim(input.Email), Phone = InputRules.Trim(input.Phone), ProfileUrl = InputRules.Trim(input.ProfileUrl), Notes = InputRules.Text(input.Notes) });
        await using var db = await factory.CreateDbContextAsync();
        var c = id.HasValue ? await db.Contacts.FindAsync(id.Value) ?? throw new ValidationException("Contact no longer exists.") : new Contact { CompanyId = m.CompanyId, CreatedAtUtc = DateTime.UtcNow };
        if (c.CompanyId != m.CompanyId)
            throw new ValidationException("A contact's company cannot be changed.");
        if (!id.HasValue && !await db.Companies.AnyAsync(x => x.Id == m.CompanyId && !x.IsArchived))
            throw new ValidationException("Choose an active company.");
        c.Name = m.Name;
        c.Role = m.Role;
        c.Email = m.Email;
        c.Phone = m.Phone;
        c.ProfileUrl = m.ProfileUrl;
        c.Notes = m.Notes;
        c.UpdatedAtUtc = DateTime.UtcNow;
        if (!id.HasValue)
            db.Contacts.Add(c);
        await db.SaveChangesAsync();
        return c.Id;
    }

    public async Task<bool> SetArchivedAsync(int id, bool archived)
    {
        await using var db = await factory.CreateDbContextAsync();
        var c = await db.Contacts.FindAsync(id);
        if (c is null)
            return false;
        if (c.IsArchived != archived)
        {
            c.IsArchived = archived;
            c.UpdatedAtUtc = DateTime.UtcNow;
            await db.SaveChangesAsync();
        }
        return true;
    }

    internal static async Task ValidateSelectionAsync(ScoutDbContext db, int companyId, IEnumerable<int> selected, IEnumerable<int>? existing = null)
    {
        var ids = selected.Distinct().ToArray();
        var old = (existing ?? []).ToHashSet();
        var contacts = await db.Contacts.Where(c => ids.Contains(c.Id)).ToListAsync();
        if (contacts.Count != ids.Length || contacts.Any(c => c.CompanyId != companyId || c.IsArchived && !old.Contains(c.Id)))
            throw new ValidationException("Choose contacts from this company. Restore archived contacts before adding new links.");
    }

    public async Task SaveApplicationContactsAsync(int applicationId, IEnumerable<int> selected)
    {
        await using var db = await factory.CreateDbContextAsync();
        var app = await db.Applications.Include(a => a.Position).SingleOrDefaultAsync(a => a.Id == applicationId) ?? throw new ValidationException("Application no longer exists.");
        var links = await db.ApplicationContacts.Where(l => l.ApplicationId == applicationId).ToListAsync();
        var ids = selected.Distinct().ToHashSet();
        if (app.IsArchived && ids.Except(links.Select(l => l.ContactId)).Any())
            throw new ValidationException("Restore the application before adding contacts.");
        await ValidateSelectionAsync(db, app.Position.CompanyId, ids, links.Select(l => l.ContactId));
        db.ApplicationContacts.RemoveRange(links.Where(l => !ids.Contains(l.ContactId)));
        db.ApplicationContacts.AddRange(ids.Except(links.Select(l => l.ContactId)).Select(id => new ApplicationContact { ApplicationId = applicationId, ContactId = id }));
        await db.SaveChangesAsync();
    }

    public async Task<IReadOnlyList<RelatedContact>> RelatedAsync(int applicationId)
    {
        await using var db = await factory.CreateDbContextAsync();
        var direct = await db.ApplicationContacts.Where(l => l.ApplicationId == applicationId).Select(l => new RelatedContact(l.ContactId, l.Contact.Name, l.Contact.IsArchived, "Application")).ToListAsync();
        var events = await db.TimelineEventContacts.Where(l => l.Event.ApplicationId == applicationId).Select(l => new RelatedContact(l.ContactId, l.Contact.Name, l.Contact.IsArchived, "Timeline")).ToListAsync();
        var rounds = await db.InterviewParticipants.Where(l => l.InterviewRound.ApplicationId == applicationId).Select(l => new RelatedContact(l.ContactId, l.Contact.Name, l.Contact.IsArchived, "Interview")).ToListAsync();
        return direct.Concat(events).Concat(rounds).GroupBy(c => c.Id).Select(g => g.First() with { Origin = string.Join(", ", g.Select(c => c.Origin).Distinct()) }).OrderBy(c => c.Name, StringComparer.OrdinalIgnoreCase).ToList();
    }

    public async Task<HashSet<int>> ApplicationSelectionAsync(int id)
    {
        await using var db = await factory.CreateDbContextAsync();
        return (await db.ApplicationContacts.Where(l => l.ApplicationId == id).Select(l => l.ContactId).ToListAsync()).ToHashSet();
    }
}

public sealed record ContactListItem(int Id, int CompanyId, string CompanyName, string Name, string? Role, string? Email, bool IsArchived);
public sealed record RelatedContact(int Id, string Name, bool IsArchived, string Origin);
