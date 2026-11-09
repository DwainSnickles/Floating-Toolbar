namespace FloatingToolbar.Models;

/// <summary>
/// First-run content. Targets are registered names or shell targets, never invented paths:
/// they are resolved through the Windows App Paths registry or the shell when launched.
/// </summary>
public static class DefaultSettingsFactory
{
    public static List<LauncherGroup> CreateGroups() => new()
    {
        new LauncherGroup
        {
            Name = "Office",
            Items =
            {
                Item("Microsoft Word", "WINWORD.EXE"),
                Item("Microsoft Excel", "EXCEL.EXE"),
                Item("Microsoft PowerPoint", "POWERPNT.EXE"),
                Item("Microsoft Outlook", "OUTLOOK.EXE"),
                Item("Microsoft OneNote", "ONENOTE.EXE")
            }
        },
        new LauncherGroup
        {
            Name = "Browsers",
            ShowFavorites = true,
            Items =
            {
                Item("Microsoft Edge", "msedge.exe"),
                Item("Google Chrome", "chrome.exe"),
                Item("Mozilla Firefox", "firefox.exe")
            }
        },
        new LauncherGroup
        {
            Name = "Windows",
            Items =
            {
                Item("File Explorer", "explorer.exe"),
                Item("Notepad", "notepad.exe"),
                Item("Calculator", "calc.exe"),
                Item("Windows Settings", "ms-settings:")
            }
        }
    };

    public static ToolbarSettings CreateSettings() => new() { Groups = CreateGroups() };

    private static LauncherItem Item(string name, string target, string? arguments = null) => new()
    {
        Name = name,
        Target = target,
        Arguments = arguments
    };
}
