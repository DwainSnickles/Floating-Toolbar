using System.Text.Json;
using FloatingToolbar.Models;

namespace FloatingToolbar.Services;

/// <summary>
/// Reads and writes %APPDATA%\FloatingToolbar\settings.json. Failures never break the toolbar.
/// </summary>
public static class SettingsStore
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        PropertyNameCaseInsensitive = true
    };

    public static string SettingsDirectory { get; } = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
        "FloatingToolbar");

    public static string SettingsPath { get; } = Path.Combine(SettingsDirectory, "settings.json");

    public static ToolbarSettings Load()
    {
        try
        {
            if (File.Exists(SettingsPath))
            {
                var json = File.ReadAllText(SettingsPath);
                var settings = JsonSerializer.Deserialize<ToolbarSettings>(json, JsonOptions);
                if (settings is not null)
                {
                    settings.Groups ??= new List<LauncherGroup>();
                    settings.PinnedItems ??= new List<LauncherItem>();
                    if (settings.Groups.Count == 0) settings.Groups = DefaultSettingsFactory.CreateGroups();
                    return settings;
                }
            }
        }
        catch (Exception)
        {
            // Corrupt or unreadable settings fall back to defaults.
        }

        return DefaultSettingsFactory.CreateSettings();
    }

    public static void Save(ToolbarSettings settings)
    {
        try
        {
            Directory.CreateDirectory(SettingsDirectory);
            File.WriteAllText(SettingsPath, JsonSerializer.Serialize(settings, JsonOptions));
        }
        catch (Exception)
        {
            // A read-only profile should not take the toolbar down.
        }
    }
}
