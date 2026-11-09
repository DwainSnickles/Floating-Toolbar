namespace FloatingToolbar.Models;

/// <summary>
/// A dropdown group of launcher entries shown as one button on the toolbar.
/// </summary>
public sealed class LauncherGroup
{
    public string Name { get; set; } = string.Empty;

    /// <summary>Optional picture for the group's own button, used instead of the first entry's icon.</summary>
    public string? IconPath { get; set; }

    /// <summary>When true the drop-down also lists the user's Internet Explorer favourites.</summary>
    public bool ShowFavorites { get; set; }

    public List<LauncherItem> Items { get; set; } = new();
}
