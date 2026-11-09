namespace FloatingToolbar.Models;

/// <summary>
/// A single launchable entry: an executable, shortcut, document, URL or shell target.
/// </summary>
public sealed class LauncherItem
{
    public string Name { get; set; } = string.Empty;

    /// <summary>Executable name, App Paths key, full path, URL, or shell target (for example "ms-settings:").</summary>
    public string Target { get; set; } = string.Empty;

    /// <summary>Optional command line arguments.</summary>
    public string? Arguments { get; set; }

    /// <summary>Optional file whose icon should represent this entry.</summary>
    public string? IconPath { get; set; }

    public string DisplayTarget => string.IsNullOrWhiteSpace(Arguments) ? Target : $"{Target} {Arguments}";
}
