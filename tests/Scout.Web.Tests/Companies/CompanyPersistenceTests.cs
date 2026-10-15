using System.ComponentModel.DataAnnotations;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Data.Sqlite;
using Scout.Web.Data;
using Scout.Web.Features.Companies;
using Scout.Web.Tests.Data;

namespace Scout.Web.Tests.Companies;

public sealed class CompanyPersistenceTests
{
    [Fact]
    public async Task CreatePersistsTrimmedUnicodeNameAcrossIndependentContexts()
    {
        await using var database = await TemporaryDatabase.CreateAsync();
        var before = DateTime.UtcNow;
        var id = await database.Companies.CreateAsync(new CompanyEditModel { Name = "  München & Söhne  " });
        var after = DateTime.UtcNow;

        await using var reopened = await database.Factory.CreateDbContextAsync(TestContext.Current.CancellationToken);
        var company = await reopened.Companies.SingleAsync(TestContext.Current.CancellationToken);
        Assert.Equal(id, company.Id);
        Assert.Equal("München & Söhne", company.Name);
        Assert.False(company.IsArchived);
        Assert.InRange(company.CreatedAtUtc, before, after);
        Assert.Equal(DateTimeKind.Utc, company.CreatedAtUtc.Kind);
        Assert.Equal(company.CreatedAtUtc, company.UpdatedAtUtc);
        Assert.Equal(DateTimeKind.Utc, company.UpdatedAtUtc.Kind);

        var details = await database.Companies.GetAsync(id);
        Assert.NotNull(details);
        Assert.Equal(company.Name, details.Name);
        Assert.Equal(company.CreatedAtUtc, details.CreatedAtUtc);
        var item = Assert.Single(await database.Companies.ListAsync());
        Assert.Equal(id, item.Id);
        Assert.Equal(company.Name, item.Name);
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData("\t\r\n")]
    [InlineData(null)]
    public async Task MissingNameIsRejectedWithoutInsertingARow(string? name)
    {
        await using var database = await TemporaryDatabase.CreateAsync();
        await Assert.ThrowsAsync<ValidationException>(() =>
            database.Companies.CreateAsync(new CompanyEditModel { Name = name! }));
        Assert.Empty(await database.Companies.ListAsync());
    }

    [Fact]
    public async Task NameLimitIsEnforcedAtTheOperationBoundary()
    {
        await using var database = await TemporaryDatabase.CreateAsync();
        await Assert.ThrowsAsync<ValidationException>(() =>
            database.Companies.CreateAsync(new CompanyEditModel { Name = new string('a', 201) }));
        var id = await database.Companies.CreateAsync(new CompanyEditModel { Name = new string('a', 200) });
        Assert.Equal(id, Assert.Single(await database.Companies.ListAsync()).Id);
    }

    [Fact]
    public async Task MissingCompanyReturnsNullWithoutCreatingARecord()
    {
        await using var database = await TemporaryDatabase.CreateAsync();
        Assert.Null(await database.Companies.GetAsync(123));
        Assert.Empty(await database.Companies.ListAsync());
    }

    [Fact]
    public async Task ReapplyingMigrationsPreservesSavedDataAndDoesNotSeed()
    {
        await using var database = await TemporaryDatabase.CreateAsync();
        Assert.Empty(await database.Companies.ListAsync());
        var id = await database.Companies.CreateAsync(new CompanyEditModel { Name = "Migration persistence check" });

        await using var context = await database.Factory.CreateDbContextAsync(TestContext.Current.CancellationToken);
        await context.Database.MigrateAsync(TestContext.Current.CancellationToken);
        Assert.Equal(context.Database.GetMigrations(), await context.Database.GetAppliedMigrationsAsync(TestContext.Current.CancellationToken));
        Assert.Empty(await context.Database.GetPendingMigrationsAsync(TestContext.Current.CancellationToken));
        Assert.Equal(id, Assert.Single(await database.Companies.ListAsync()).Id);
    }

    [Fact]
    public async Task DuplicateCompanyNamesRemainSeparateRecords()
    {
        await using var database = await TemporaryDatabase.CreateAsync();
        var first = await database.Companies.CreateAsync(new CompanyEditModel { Name = "Example" });
        var second = await database.Companies.CreateAsync(new CompanyEditModel { Name = "Example" });
        Assert.NotEqual(first, second);
        Assert.Equal(2, (await database.Companies.ListAsync()).Count);
    }

    [Fact]
    public async Task OptionalDetailsRoundTripWithoutChangingMultilineText()
    {
        await using var database = await TemporaryDatabase.CreateAsync();
        var model = new CompanyEditModel
        {
            Name = "  München & Söhne  ",
            WebsiteUrl = " https://example.com/unternehmen?lang=de&view=jobs ",
            Location = " München / Remote ",
            Description = "  Referenz: Entwicklung & Forschung\r\nEnglish second line.  ",
            PersonalNotes = "<script>alert('plain text')</script>\nMeine Einschätzung: schön."
        };
        var id = await database.Companies.CreateAsync(model);
        var saved = Assert.IsType<CompanyDetails>(await database.Companies.GetAsync(id));

        Assert.Equal("https://example.com/unternehmen?lang=de&view=jobs", saved.WebsiteUrl);
        Assert.Equal("München / Remote", saved.Location);
        Assert.Equal(model.Description, saved.Description);
        Assert.Equal(model.PersonalNotes, saved.PersonalNotes);
        Assert.Equal("  München & Söhne  ", model.Name);
    }

    [Fact]
    public async Task UpdateKeepsIdentityCreationTimeAndArchiveStateAndCanClearOptionalValues()
    {
        await using var database = await TemporaryDatabase.CreateAsync();
        var id = await database.Companies.CreateAsync(new CompanyEditModel
        {
            Name = "Original",
            WebsiteUrl = "http://example.com",
            Location = "Berlin",
            Description = "Reference",
            PersonalNotes = "Private"
        });
        var original = Assert.IsType<CompanyDetails>(await database.Companies.GetAsync(id));
        await using (var context = await database.Factory.CreateDbContextAsync(TestContext.Current.CancellationToken))
        {
            var company = await context.Companies.SingleAsync(TestContext.Current.CancellationToken);
            company.IsArchived = true;
            company.UpdatedAtUtc = original.CreatedAtUtc.AddDays(-1);
            await context.SaveChangesAsync(TestContext.Current.CancellationToken);
        }
        var before = DateTime.UtcNow;
        Assert.True(await database.Companies.UpdateAsync(id, new CompanyEditModel
        {
            Name = " Geändert ",
            WebsiteUrl = "  ",
            Location = "\t",
            Description = "",
            PersonalNotes = null
        }));
        var saved = Assert.IsType<CompanyDetails>(await database.Companies.GetAsync(id));
        Assert.Equal(id, saved.Id);
        Assert.Equal("Geändert", saved.Name);
        Assert.True(saved.IsArchived);
        Assert.Equal(original.CreatedAtUtc, saved.CreatedAtUtc);
        Assert.InRange(saved.UpdatedAtUtc, before, DateTime.UtcNow);
        Assert.Equal(DateTimeKind.Utc, saved.UpdatedAtUtc.Kind);
        Assert.Null(saved.WebsiteUrl);
        Assert.Null(saved.Location);
        Assert.Null(saved.Description);
        Assert.Null(saved.PersonalNotes);
    }

    public static TheoryData<string, string> InvalidDetails => new()
    {
        { nameof(CompanyEditModel.Name), "   " },
        { nameof(CompanyEditModel.Name), new string('n', 201) },
        { nameof(CompanyEditModel.WebsiteUrl), "javascript:alert(1)" },
        { nameof(CompanyEditModel.WebsiteUrl), "file:///C:/notes.txt" },
        { nameof(CompanyEditModel.WebsiteUrl), "ftp://example.com" },
        { nameof(CompanyEditModel.WebsiteUrl), "example.com/jobs" },
        { nameof(CompanyEditModel.WebsiteUrl), "/relative" },
        { nameof(CompanyEditModel.WebsiteUrl), "https://" },
        { nameof(CompanyEditModel.WebsiteUrl), "https://example.com/" + new string('x', 2048) },
        { nameof(CompanyEditModel.Location), new string('l', 301) },
        { nameof(CompanyEditModel.Description), new string('d', 100_001) },
        { nameof(CompanyEditModel.PersonalNotes), new string('p', 100_001) }
    };

    [Theory]
    [MemberData(nameof(InvalidDetails))]
    public async Task InvalidCreateOrUpdateDoesNotChangeStoredValuesOrTimestamps(string field, string value)
    {
        await using var database = await TemporaryDatabase.CreateAsync();
        var id = await database.Companies.CreateAsync(new CompanyEditModel { Name = "Keep me", PersonalNotes = "Saved notes" });
        var original = await database.Companies.GetAsync(id);
        var draft = new CompanyEditModel { Name = "Unsaved replacement", PersonalNotes = "Unsaved notes" };
        typeof(CompanyEditModel).GetProperty(field)!.SetValue(draft, value);

        await Assert.ThrowsAsync<ValidationException>(() => database.Companies.UpdateAsync(id, draft));
        await Assert.ThrowsAsync<ValidationException>(() => database.Companies.CreateAsync(draft));
        Assert.Equal(original, await database.Companies.GetAsync(id));
        Assert.Single(await database.Companies.ListAsync());
        Assert.Equal(value, typeof(CompanyEditModel).GetProperty(field)!.GetValue(draft));
    }

    [Fact]
    public async Task ExactOptionalLimitsAndHttpUrlAreAcceptedOnUpdate()
    {
        await using var database = await TemporaryDatabase.CreateAsync();
        var id = await database.Companies.CreateAsync(new CompanyEditModel { Name = "Original" });
        var draft = new CompanyEditModel
        {
            Name = new string('n', 200),
            WebsiteUrl = "http://example.com/".PadRight(2048, 'x'),
            Location = new string('l', 300),
            Description = new string('d', 100_000),
            PersonalNotes = new string('p', 100_000)
        };
        Assert.True(await database.Companies.UpdateAsync(id, draft));
        var saved = Assert.IsType<CompanyDetails>(await database.Companies.GetAsync(id));
        Assert.Equal(draft.Name, saved.Name);
        Assert.Equal(draft.WebsiteUrl, saved.WebsiteUrl);
        Assert.Equal(draft.Location, saved.Location);
        Assert.Equal(draft.Description, saved.Description);
        Assert.Equal(draft.PersonalNotes, saved.PersonalNotes);
    }

    [Fact]
    public async Task UpdatingMissingCompanyReturnsFalseWithoutInserting()
    {
        await using var database = await TemporaryDatabase.CreateAsync();
        Assert.False(await database.Companies.UpdateAsync(123, new CompanyEditModel { Name = "No insert" }));
        Assert.Empty(await database.Companies.ListAsync());
    }

    [Fact]
    public async Task FailedWriteLeavesDraftAndSavedCompanyIntact()
    {
        await using var database = await TemporaryDatabase.CreateAsync();
        var id = await database.Companies.CreateAsync(new CompanyEditModel { Name = "Original", Description = "Saved reference" });
        var original = await database.Companies.GetAsync(id);
        var connection = new SqliteConnectionStringBuilder(ScoutDatabase.ConnectionString(database.Paths))
        {
            Mode = SqliteOpenMode.ReadOnly,
            Pooling = false
        };
        var readOnlyFactory = new PooledDbContextFactory<ScoutDbContext>(new DbContextOptionsBuilder<ScoutDbContext>()
            .UseSqlite(connection.ToString()).Options);
        var operations = new CompanyOperations(readOnlyFactory);
        var draft = new CompanyEditModel { Name = " Unsaved name ", Description = "Unsaved\nreference", PersonalNotes = "My draft" };
        await Assert.ThrowsAsync<DbUpdateException>(() => operations.UpdateAsync(id, draft));
        await Assert.ThrowsAsync<DbUpdateException>(() => operations.CreateAsync(draft));
        Assert.Equal(original, await database.Companies.GetAsync(id));
        Assert.Equal(" Unsaved name ", draft.Name);
        Assert.Equal("Unsaved\nreference", draft.Description);
        Assert.Equal("My draft", draft.PersonalNotes);
        Assert.Single(await database.Companies.ListAsync());
    }

    [Fact]
    public async Task DetailsMigrationPreservesAnExistingM11Company()
    {
        await using var database = await TemporaryDatabase.CreateAsync("20261015153333_InitialCompanies");
        await using (var context = await database.Factory.CreateDbContextAsync(TestContext.Current.CancellationToken))
        {
            await context.Database.ExecuteSqlRawAsync("""
                INSERT INTO Companies (Id, Name, IsArchived, CreatedAtUtc, UpdatedAtUtc)
                VALUES (42, 'München before migration', 1, '2026-10-01 10:00:00', '2026-10-02 11:00:00');
                """, TestContext.Current.CancellationToken);
            await context.Database.MigrateAsync(TestContext.Current.CancellationToken);
        }
        var company = Assert.IsType<CompanyDetails>(await database.Companies.GetAsync(42));
        Assert.Equal("München before migration", company.Name);
        Assert.True(company.IsArchived);
        Assert.Equal(new DateTime(2026, 10, 1, 10, 0, 0, DateTimeKind.Utc), company.CreatedAtUtc);
        Assert.Equal(new DateTime(2026, 10, 2, 11, 0, 0, DateTimeKind.Utc), company.UpdatedAtUtc);
        Assert.Null(company.WebsiteUrl);
        Assert.Null(company.Location);
        Assert.Null(company.Description);
        Assert.Null(company.PersonalNotes);
    }
}

