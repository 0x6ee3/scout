namespace Scout.Web.Configuration;

public sealed class StoragePaths
{
    private StoragePaths(string root)
    {
        Root = root;
        DatabasePath = Path.Combine(root, "database", "scout.db");
        AssetDirectory = Path.Combine(root, "assets");
    }

    public string Root
    {
        get;
    }
    public string DatabasePath
    {
        get;
    }
    public string AssetDirectory
    {
        get;
    }

    public static StoragePaths ForCurrentUser()
    {
        var userProfile = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
        if (string.IsNullOrWhiteSpace(userProfile))
        {
            throw new InvalidOperationException("Scout could not find your Windows user profile.");
        }

        return FromRoot(Path.Combine(userProfile, "Documents", "Scout"));
    }

    // A supplied root also allows persistence tests to stay out of the user's real data.
    public static StoragePaths FromRoot(string root)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(root);
        if (!Path.IsPathFullyQualified(root) || root.StartsWith(@"\\", StringComparison.Ordinal))
        {
            throw new ArgumentException("Scout storage requires an absolute local directory.", nameof(root));
        }

        return new StoragePaths(Path.GetFullPath(root));
    }

    public void CreateDirectories()
    {
        Directory.CreateDirectory(Path.GetDirectoryName(DatabasePath)!);
        Directory.CreateDirectory(AssetDirectory);
    }
}
