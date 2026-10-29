namespace Scout.Scribe.Tests;

public sealed class ScribePreferencesTests
{
    [Fact]
    public void MissingSettingsUseDefaultAndSavedPathSurvivesReload()
    {
        var root = Path.Combine(Path.GetTempPath(), "scribe-preferences-" + Guid.NewGuid().ToString("N"));
        var file = Path.Combine(root, "settings.json");
        try
        {
            Assert.Equal(ScribePreferences.DefaultRecordingPath, ScribePreferences.Load(file).RecordingPath);
            var expected = new ScribePreferences(Path.Combine(root, "Recordings with spaces"));
            expected.Save(file);
            Assert.Equal(expected, ScribePreferences.Load(file));
            var replacement = new ScribePreferences(Path.Combine(root, "New recordings"));
            replacement.Save(file);
            Assert.Equal(replacement, ScribePreferences.Load(file));
            Assert.False(File.Exists(file + ".tmp"));
            Assert.False(Directory.Exists(expected.RecordingPath));
        }
        finally { if (Directory.Exists(root)) Directory.Delete(root, recursive: true); }
    }

    [Theory]
    [InlineData("")]
    [InlineData("relative-folder")]
    [InlineData(@"C:relative-folder")]
    public void RejectsPathsThatDependOnWorkingDirectory(string path) =>
        Assert.Throws<ArgumentException>(() => ScribePreferences.ValidatePath(path));

    [Fact]
    public void InvalidSettingsAreReportedWithoutOverwritingTheFile()
    {
        var root = Path.Combine(Path.GetTempPath(), "scribe-preferences-" + Guid.NewGuid().ToString("N"));
        var file = Path.Combine(root, "settings.json");
        Directory.CreateDirectory(root);
        try
        {
            File.WriteAllText(file, "invalid JSON");
            Assert.Throws<System.Text.Json.JsonException>(() => ScribePreferences.Load(file));
            Assert.Equal("invalid JSON", File.ReadAllText(file));
        }
        finally { Directory.Delete(root, recursive: true); }
    }
}
