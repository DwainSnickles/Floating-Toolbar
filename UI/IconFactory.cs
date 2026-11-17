using System.Reflection;

namespace FloatingToolbar.UI;

/// <summary>
/// Supplies the app icon from the embedded Assets/app.ico, which is the same artwork the
/// .exe (and therefore any desktop shortcut) shows, so window, taskbar and tray all match.
/// </summary>
internal static class IconFactory
{
    private const string ResourceName = "FloatingToolbar.app.ico";

    /// <summary>Full multi-frame icon; Window picks whatever size a surface asks for.</summary>
    public static Icon Create() => FromResource(default);

    /// <summary>Icon pinned to one size, for surfaces that cannot pick a frame themselves (the tray).</summary>
    public static Icon Create(Size size) => FromResource(size);

    private static Icon FromResource(Size size)
    {
        using var stream = OpenIconStream();
        if (stream is not null)
        {
            try
            {
                return size.Width > 0 && size.Height > 0
                    ? new Icon(stream, size)
                    : new Icon(stream);
            }
            catch (ArgumentException)
            {
                // Malformed resource: fall back to the shell icon below.
            }
        }

        return Fallback(size);
    }

    private static Stream? OpenIconStream()
    {
        var assembly = typeof(IconFactory).Assembly;
        var stream = assembly.GetManifestResourceStream(ResourceName);
        if (stream is not null) return stream;

        // Tolerate a renamed resource rather than silently losing the artwork.
        var alias = Array.Find(
            assembly.GetManifestResourceNames(),
            name => name.EndsWith(".ico", StringComparison.OrdinalIgnoreCase));

        return alias is null ? null : assembly.GetManifestResourceStream(alias);
    }

    private static Icon Fallback(Size size)
    {
        var path = Environment.ProcessPath;
        if (!string.IsNullOrEmpty(path))
        {
            try
            {
                var fromExe = Icon.ExtractAssociatedIcon(path);
                if (fromExe is not null)
                {
                    return size.Width > 0 && size.Height > 0 ? new Icon(fromExe, size) : fromExe;
                }
            }
            catch (ArgumentException)
            {
                // No icon resource on the executable.
            }
        }

        return (Icon)SystemIcons.Application.Clone();
    }
}
