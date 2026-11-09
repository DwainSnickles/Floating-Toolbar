using System.Drawing.Drawing2D;

namespace FloatingToolbar.UI;

/// <summary>
/// Central palette, fonts and rounded-rectangle helpers so every surface matches.
/// </summary>
internal static class Theme
{
    public static readonly Color Background = Color.FromArgb(22, 23, 27);
    public static readonly Color Surface = Color.FromArgb(30, 32, 37);
    public static readonly Color Hover = Color.FromArgb(45, 48, 57);
    public static readonly Color Pressed = Color.FromArgb(58, 62, 72);
    public static readonly Color Border = Color.FromArgb(50, 54, 63);
    public static readonly Color Text = Color.FromArgb(238, 240, 245);
    public static readonly Color Muted = Color.FromArgb(148, 154, 167);
    public static readonly Color Accent = Color.FromArgb(94, 140, 255);
    public static readonly Color AccentEdge = Color.FromArgb(140, 94, 140, 255);
    public static readonly Color Danger = Color.FromArgb(223, 74, 60);
    public static readonly Color MenuBackground = Color.FromArgb(27, 29, 34);
    public static readonly Color MenuHover = Color.FromArgb(47, 51, 61);

    private static readonly string UiFamilyName = ResolveFamily("Segoe UI Variable Text", "Segoe UI", "Tahoma");
    private static readonly string IconFamilyName = ResolveFamily("Segoe Fluent Icons", "Segoe MDL2 Assets", "Segoe UI Symbol");

    private static readonly Dictionary<(float Size, FontStyle Style), Font> UiFonts = new();
    private static readonly Dictionary<float, Font> IconFonts = new();

    public static Font Ui(float size, FontStyle style = FontStyle.Regular)
    {
        var key = (size, style);
        if (!UiFonts.TryGetValue(key, out var font))
        {
            font = new Font(UiFamilyName, size, style, GraphicsUnit.Point);
            UiFonts[key] = font;
        }

        return font;
    }

    public static Font Icon(float size)
    {
        if (!IconFonts.TryGetValue(size, out var font))
        {
            font = new Font(IconFamilyName, size, FontStyle.Regular, GraphicsUnit.Point);
            IconFonts[size] = font;
        }

        return font;
    }

    public static GraphicsPath Rounded(Rectangle bounds, int radius)
    {
        var path = new GraphicsPath();
        var diameter = Math.Max(2, radius * 2);

        if (bounds.Width <= diameter || bounds.Height <= diameter)
        {
            path.AddRectangle(bounds);
            return path;
        }

        path.AddArc(bounds.X, bounds.Y, diameter, diameter, 180, 90);
        path.AddArc(bounds.Right - diameter, bounds.Y, diameter, diameter, 270, 90);
        path.AddArc(bounds.Right - diameter, bounds.Bottom - diameter, diameter, diameter, 0, 90);
        path.AddArc(bounds.X, bounds.Bottom - diameter, diameter, diameter, 90, 90);
        path.CloseFigure();
        return path;
    }

    public static void FillRounded(Graphics graphics, Rectangle bounds, int radius, Color color)
    {
        using var path = Rounded(bounds, radius);
        using var brush = new SolidBrush(color);
        graphics.FillPath(brush, path);
    }

    public static void DrawRoundedBorder(Graphics graphics, Rectangle bounds, int radius, Color color, float width = 1f)
    {
        using var path = Rounded(bounds, radius);
        using var pen = new Pen(color, width);
        graphics.DrawPath(pen, path);
    }

    private static string ResolveFamily(params string[] candidates)
    {
        HashSet<string> installed;
        try
        {
            installed = new HashSet<string>(FontFamily.Families.Select(f => f.Name), StringComparer.OrdinalIgnoreCase);
        }
        catch (Exception)
        {
            installed = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        }

        foreach (var candidate in candidates)
        {
            if (installed.Contains(candidate)) return candidate;
        }

        return SystemFonts.MessageBoxFont?.FontFamily.Name ?? "Microsoft Sans Serif";
    }
}

/// <summary>Segoe Fluent Icons / Segoe MDL2 Assets glyphs used by the chrome buttons.</summary>
internal static class Glyphs
{
    public const string Close = "\uE8BB";
    public const string Minimize = "\uE921";
    public const string Pin = "\uE718";
    public const string Unpin = "\uE77A";
    /// <summary>Folder with a plus: the group-add pill, so it never reads as "add a button".</summary>
    public const string NewGroup = "\uE8F4";

    /// <summary>Plain plus: the single-button-add pill.</summary>
    public const string NewButton = "\uE710";
}
