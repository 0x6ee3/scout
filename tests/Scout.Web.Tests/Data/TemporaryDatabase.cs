using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.Extensions.DependencyInjection;
using Scout.Web.Configuration;
using Scout.Web.Data;
using Scout.Web.Features.Companies;
using Scout.Web.Features.Positions;

namespace Scout.Web.Tests.Data;

internal sealed class TemporaryDatabase : IAsyncDisposable
{
    private readonly ServiceProvider services;
    private static readonly string TestDirectory = Path.GetFullPath(Path.Combine(Path.GetTempPath(), "Scout.Tests"));

    private TemporaryDatabase()
    {
        Paths = StoragePaths.FromRoot(Path.Combine(TestDirectory, Guid.NewGuid().ToString("N")));
        Paths.CreateDirectories();
        var connection = new SqliteConnectionStringBuilder(ScoutDatabase.ConnectionString(Paths)) { Pooling = false };
        var registrations = new ServiceCollection();
        registrations.AddDbContextFactory<ScoutDbContext>(options => options.UseSqlite(connection.ToString()));
        services = registrations.BuildServiceProvider();
        Factory = services.GetRequiredService<IDbContextFactory<ScoutDbContext>>();
        Companies = new CompanyOperations(Factory);
        Positions = new PositionOperations(Factory);
    }

    public StoragePaths Paths
    {
        get;
    }
    public IDbContextFactory<ScoutDbContext> Factory
    {
        get;
    }
    public CompanyOperations Companies
    {
        get;
    }
    public PositionOperations Positions
    {
        get;
    }

    public static async Task<TemporaryDatabase> CreateAsync(string? targetMigration = null)
    {
        var database = new TemporaryDatabase();
        try
        {
            await using var context = await database.Factory.CreateDbContextAsync();
            await context.GetService<IMigrator>().MigrateAsync(targetMigration);
            return database;
        }
        catch
        {
            await database.DisposeAsync();
            throw;
        }
    }

    public async ValueTask DisposeAsync()
    {
        await services.DisposeAsync();
        // Delete only the unique test directory created above, never user data.
        var target = Path.GetFullPath(Paths.Root);
        if (!target.StartsWith(TestDirectory + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException("Refusing to clean up outside Scout's temporary test directory.");
        }
        Directory.Delete(target, recursive: true);
    }
}
