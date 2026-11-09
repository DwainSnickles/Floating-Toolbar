using System.Drawing.Drawing2D;
using System.Runtime.InteropServices;

namespace FloatingToolbar.UI;

/// <summary>Draws the tray/window icon at runtime so the app needs no embedded .ico.</summary>
internal static class IconFactory
{
    [DllImport("user32.dll", SetLastError = true)]
    private static extern bool DestroyIcon(IntPtr handle);

    public static Icon Create()
    {
        using var bitmap = new Bitmap(32, 32);
        using (var graphics = Graphics.FromImage(bitmap))
        {
            graphics.SmoothingMode = SmoothingMode.AntiAlias;
            var bounds = new Rectangle(1, 1, 30, 30);

            using (var path = Theme.Rounded(bounds, 8))
            using (var background = new LinearGradientBrush(bounds, Color.FromArgb(44, 48, 58), Color.FromArgb(20, 21, 25), 90f))
            {
                graphics.FillPath(background, path);
            }

            using var pen = new Pen(Theme.Accent, 2f) { StartCap = LineCap.Round, EndCap = LineCap.Round };
            graphics.DrawLine(pen, 9, 12, 23, 12);
            graphics.DrawLine(pen, 9, 16, 23, 16);
            graphics.DrawLine(pen, 9, 20, 17, 20);
        }

        var handle = bitmap.GetHicon();
        try
        {
            using var icon = Icon.FromHandle(handle);
            return (Icon)icon.Clone();
        }
        finally
        {
            DestroyIcon(handle);
        }
    }
}
