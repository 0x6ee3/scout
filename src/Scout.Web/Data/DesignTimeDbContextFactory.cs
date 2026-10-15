using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Scout.Web.Configuration;

namespace Scout.Web.Data;

public sealed class DesignTimeDbContextFactory : IDesignTimeDbContextFactory<ScoutDbContext>
{
    public ScoutDbContext CreateDbContext(string[] args)
    {
        // Model/migration commands do not create directories, run migrations, or insert data.
        var paths = StoragePaths.ForCurrentUser();
        var options = new DbContextOptionsBuilder<ScoutDbContext>()
            .UseSqlite(ScoutDatabase.ConnectionString(paths))
            .Options;
        return new ScoutDbContext(options);
    }
}
