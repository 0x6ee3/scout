using Scout.Web.Configuration;
using Scout.Web.Tests.Data;

namespace Scout.Web.Tests.Configuration;

public sealed class StoragePathsTests
{
    [Fact]
    public void CurrentUserPathsResolveUnderDocumentsWithoutCreatingFiles()
    {
        var paths = StoragePaths.ForCurrentUser();
        var expected = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "Documents", "Scout");
        Assert.Equal(expected, paths.Root);
        Assert.Equal(Path.Combine(expected, "database", "scout.db"), paths.DatabasePath);
        Assert.Equal(Path.Combine(expected, "assets"), paths.AssetDirectory);
    }

    [Theory]
    [InlineData("")]
    [InlineData("relative")]
    [InlineData("..\\outside")]
    [InlineData("\\\\server\\share\\Scout")]
    public void RejectsMissingRelativeAndNetworkRoots(string root)
    {
        Assert.ThrowsAny<ArgumentException>(() => StoragePaths.FromRoot(root));
    }

    [Fact]
    public async Task CreatesOnlyExpectedDirectoriesAndDoesNotReplaceExistingFiles()
    {
        await using var database = await TemporaryDatabase.CreateAsync();
        var original = Path.Combine(database.Paths.AssetDirectory, "original.txt");
        await File.WriteAllTextAsync(original, "Keep this original.", TestContext.Current.CancellationToken);
        database.Paths.CreateDirectories();
        Assert.Equal("Keep this original.", await File.ReadAllTextAsync(original, TestContext.Current.CancellationToken));
        Assert.Equal(new[] { "assets", "database", "recordings" }, Directory.GetDirectories(database.Paths.Root)
            .Select(Path.GetFileName).OrderBy(name => name).ToArray());
    }

    [Fact]
    public async Task InvalidDirectoryFailsWithoutOverwritingOrFallingBack()
    {
        await using var database = await TemporaryDatabase.CreateAsync();
        var blocked = Path.Combine(database.Paths.Root, "blocked");
        await File.WriteAllTextAsync(blocked, "An existing file is not a directory.", TestContext.Current.CancellationToken);
        var paths = StoragePaths.FromRoot(blocked);
        Assert.ThrowsAny<IOException>(paths.CreateDirectories);
        Assert.Equal("An existing file is not a directory.", await File.ReadAllTextAsync(blocked, TestContext.Current.CancellationToken));
        Assert.False(File.Exists(paths.DatabasePath));
    }
}
