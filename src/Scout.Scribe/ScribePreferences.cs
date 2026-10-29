using System.Text.Json;
using Microsoft.Win32;

namespace Scout.Scribe;

internal sealed record ScribePreferences(string RecordingPath)
{
    internal static string DefaultRecordingPath => Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "Scout.Scribe", "recordings");
    internal static string FilePath => Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Scout.Scribe", "settings.json");

    internal static ScribePreferences Load(string? file = null)
    {
        var path = file ?? FilePath;
        if (!File.Exists(path)) return new(DefaultRecordingPath);
        var preferences = JsonSerializer.Deserialize<ScribePreferences>(File.ReadAllText(path))
            ?? throw new InvalidDataException("The Scribe settings file is empty.");
        return new(ValidatePath(preferences.RecordingPath));
    }

    internal static string ValidatePath(string path)
    {
        if (string.IsNullOrWhiteSpace(path) || !Path.IsPathFullyQualified(path))
            throw new ArgumentException("Choose an absolute recording folder path.");
        return Path.GetFullPath(path.Trim());
    }

    internal void Save(string? file = null)
    {
        var path = file ?? FilePath;
        var validated = new ScribePreferences(ValidatePath(RecordingPath));
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        var temporary = path + ".tmp";
        try
        {
            File.WriteAllText(temporary, JsonSerializer.Serialize(validated, new JsonSerializerOptions { WriteIndented = true }));
            File.Move(temporary, path, overwrite: true);
        }
        finally { if (File.Exists(temporary)) File.Delete(temporary); }
    }
}

internal static class LoginStartup
{
    private const string KeyPath = @"Software\Microsoft\Windows\CurrentVersion\Run";
    private const string ValueName = "Scout.Scribe";
    internal static bool Enabled
    {
        get { using var key = Registry.CurrentUser.OpenSubKey(KeyPath); return key?.GetValue(ValueName) is string; }
    }

    internal static void SetEnabled(bool enabled)
    {
        using var key = Registry.CurrentUser.CreateSubKey(KeyPath, writable: true);
        if (enabled)
        {
            var executable = Environment.ProcessPath ?? throw new InvalidOperationException("Cannot locate the Scribe executable.");
            if (!string.Equals(Path.GetFileName(executable), "Scout.Scribe.exe", StringComparison.OrdinalIgnoreCase))
                throw new InvalidOperationException("Launch Scout.Scribe.exe directly before enabling launch at sign-in.");
            key.SetValue(ValueName, $"\"{executable}\"", RegistryValueKind.String);
        }
        else key.DeleteValue(ValueName, throwOnMissingValue: false);
    }
}
