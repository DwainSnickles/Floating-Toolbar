using System.Drawing.Drawing2D;

namespace FloatingToolbar.UI;

/// <summary>The dotted drag handle at the left edge of the bar.</summary>
public sealed class DragGrip : Control
{
    private bool _hover;

    public DragGrip()
    {
        SetStyle(
            ControlStyles.UserPaint |
            ControlStyles.AllPaintingInWmPaint |
            ControlStyles.OptimizedDoubleBuffer |
            ControlStyles.ResizeRedraw, true);

        BackColor = Theme.Background;
        Cursor = Cursors.SizeAll;
        Size = new Size(LogicalToDeviceUnits(14), LogicalToDeviceUnits(40));
    }

    protected override void OnMouseEnter(EventArgs e)
    {
        _hover = true;
        Invalidate();
        base.OnMouseEnter(e);
    }

    protected override void OnMouseLeave(EventArgs e)
    {
        _hover = false;
        Invalidate();
        base.OnMouseLeave(e);
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        var graphics = e.Graphics;
        graphics.SmoothingMode = SmoothingMode.AntiAlias;

        var color = _hover ? Theme.Muted : Color.FromArgb(96, 102, 114);
        var dot = Math.Max(2, LogicalToDeviceUnits(2));
        var gap = Math.Max(3, LogicalToDeviceUnits(4));
        const int columns = 2;
        const int rows = 3;

        var totalWidth = columns * dot + (columns - 1) * gap;
        var totalHeight = rows * dot + (rows - 1) * gap;
        var startX = (Width - totalWidth) / 2;
        var startY = (Height - totalHeight) / 2;

        using var brush = new SolidBrush(color);
        for (var row = 0; row < rows; row++)
        {
            for (var column = 0; column < columns; column++)
            {
                graphics.FillEllipse(
                    brush,
                    startX + column * (dot + gap),
                    startY + row * (dot + gap),
                    dot,
                    dot);
            }
        }
    }
}
