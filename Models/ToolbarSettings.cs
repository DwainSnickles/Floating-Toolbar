namespace FloatingToolbar.Models;

/// <summary>
/// Everything the toolbar persists between sessions.
/// </summary>
public sealed class ToolbarSettings
{
    public bool AlwaysOnTop { get; set; } = true;

    public bool ShowInTaskbar { get; set; }

    public bool ShowLabels { get; set; } = true;

    public bool StartWithWindows { get; set; }

    public bool ShowFavoritesFlyout { get; set; } = true;

    /// <summary>Ctrl+Alt+Space shows or hides the bar from any application.</summary>
    public bool ShowHideHotkey { get; set; } = true;

    /// <summary>Hide the bar while another application is fullscreen, restore it afterwards.</summary>
    public bool AutoHideFullscreen { get; set; }

    public int? LocationX { get; set; }

    public int? LocationY { get; set; }

    public List<LauncherGroup> Groups { get; set; } = new();

    /// <summary>Entries pinned directly to the bar (launch with a single click).</summary>
    public List<LauncherItem> PinnedItems { get; set; } = new();
}
