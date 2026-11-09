namespace FloatingToolbar.Models;

/// <summary>
/// A dropdown group of launcher entries shown as one button on the toolbar.
/// </summary>
public sealed class LauncherGroup
{
    public string Name { get; set; } = string.Empty;

    /// <summary>When true the drop-down also lists the user's Internet Explorer favourites.</summary>
    public bool ShowFavorites { get; set; }

    public List<LauncherItem> Items { get; set; } = new();
}
