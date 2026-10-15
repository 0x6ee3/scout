using Microsoft.Data.Sqlite;
using Scout.Web.Configuration;

namespace Scout.Web.Data;

public static class ScoutDatabase
{
    public static string ConnectionString(StoragePaths paths) => new SqliteConnectionStringBuilder
    {
        DataSource = paths.DatabasePath,
        ForeignKeys = true,
        DefaultTimeout = 5
    }.ToString();
}
