using System.Drawing;
using System.Runtime.InteropServices;
using FloatingToolbar.Models;

namespace FloatingToolbar.Services;

/// <summary>
/// Loads real Windows shell icons for files and shortcuts and caches them by target.
/// Returns null when no icon exists so the button can draw a letter tile instead.
/// </summary>
internal static class IconLoader
{
    private const uint ShgfiIcon = 0x000000100;
    private const uint ShgfiLargeIcon = 0x000000000;

    private static readonly Dictionary<string, Bitmap?> Cache = new(StringComparer.OrdinalIgnoreCase);

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
    private struct ShFileInfo
    {
        public IntPtr hIcon;
        public int iIcon;
        public uint dwAttributes;

        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 260)]
        public string szDisplayName;

        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 80)]
        public string szTypeName;
    }

    [DllImport("shell32.dll", CharSet = CharSet.Unicode)]
    private static extern IntPtr SHGetFileInfo(string path, uint fileAttributes, ref ShFileInfo info, uint infoSize, uint flags);

    [DllImport("user32.dll", SetLastError = true)]
    private static extern bool DestroyIcon(IntPtr handle);

    public static Bitmap? Get(LauncherItem item)
    {
        var key = item.IconPath ?? item.Target;
        if (Cache.TryGetValue(key, out var cached)) return cached;

        Bitmap? bitmap = null;
        try
        {
            var path = item.IconPath;
            if (string.IsNullOrWhiteSpace(path))
            {
                path = AppLauncher.Resolve(item.Target) ??
                       (File.Exists(item.Target) ? item.Target : null);
            }

            if (!string.IsNullOrWhiteSpace(path) && File.Exists(path))
            {
                bitmap = LoadFromShell(path);
            }
        }
        catch (Exception)
        {
            bitmap = null;
        }

        Cache[key] = bitmap;
        return bitmap;
    }

    private static Bitmap? LoadFromShell(string path)
    {
        var info = new ShFileInfo();
        var result = SHGetFileInfo(
            path,
            0,
            ref info,
            (uint)Marshal.SizeOf<ShFileInfo>(),
            ShgfiIcon | ShgfiLargeIcon);

        if (result == IntPtr.Zero || info.hIcon == IntPtr.Zero)
        {
            return LoadAssociated(path);
        }

        try
        {
            using var icon = Icon.FromHandle(info.hIcon);
            return icon.ToBitmap();
        }
        finally
        {
            DestroyIcon(info.hIcon);
        }
    }

    private static Bitmap? LoadAssociated(string path)
    {
        try
        {
            using var icon = Icon.ExtractAssociatedIcon(path);
            return icon?.ToBitmap();
        }
        catch (Exception)
        {
            return null;
        }
    }
}
