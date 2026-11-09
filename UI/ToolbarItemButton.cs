using System.Drawing.Drawing2D;

namespace FloatingToolbar.UI;

public enum InsertSide
{
    None,
    Before,
    After
}

/// <summary>
/// One compact pill on the bar: icon, optional label, optional drop-down chevron.
/// Supports hover feedback, drag-reorder hints and Explorer file drops.
/// </summary>
public sealed class ToolbarItemButton : Control
{
    private bool _hover;
    private bool _pressed;
    private bool _dropTarget;
    private string _caption = string.Empty;
    private Image? _icon;
    private bool _showLabel = true;

    public ToolbarItemButton()
    {
        SetStyle(
            ControlStyles.UserPaint |
            ControlStyles.AllPaintingInWmPaint |
            ControlStyles.OptimizedDoubleBuffer |
            ControlStyles.ResizeRedraw, true);

        BackColor = Theme.Background;
        Cursor = Cursors.Hand;
        Font = Theme.Ui(9.25f);
        Height = LogicalToDeviceUnits(40);
        AllowDrop = true;
    }

    public string Caption
    {
        get => _caption;
        set
        {
            _caption = value ?? string.Empty;
            UpdateSize(Height);
            Invalidate();
        }
    }

    public Image? Icon
    {
        get => _icon;
        set
        {
            _icon = value;
            Invalidate();
        }
    }

    public bool IsGroup { get; set; }

    public bool ShowChevron { get; set; }

    public bool ShowLabel
    {
        get => _showLabel;
        set
        {
            _showLabel = value;
            UpdateSize(Height);
            Invalidate();
        }
    }

    public InsertSide InsertSide { get; set; }

    public event EventHandler? Activated;

    public event EventHandler<string[]>? FilesDropped;

    public void UpdateSize(int height)
    {
        Height = Math.Max(LogicalToDeviceUnits(28), height);
        Width = PreferredWidth();
    }

    /// <summary>Space reserved to the right of the label, shared by measuring and painting.</summary>
    private int TextTrailing => ShowChevron ? LogicalToDeviceUnits(16) : LogicalToDeviceUnits(8);

    public int PreferredWidth()
    {
        var pad = LogicalToDeviceUnits(8);
        var iconSize = LogicalToDeviceUnits(20);
        var gap = LogicalToDeviceUnits(6);

        var width = pad + iconSize;
        if (ShowLabel && _caption.Length > 0)
        {
            var text = TextRenderer.MeasureText(
                _caption,
                Font,
                new Size(int.MaxValue, Height),
                TextFormatFlags.SingleLine);
            width += gap + text.Width + LogicalToDeviceUnits(2);
        }

        width += TextTrailing;
        return Math.Max(width, LogicalToDeviceUnits(38));
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        var graphics = e.Graphics;
        graphics.SmoothingMode = SmoothingMode.AntiAlias;
        graphics.InterpolationMode = InterpolationMode.HighQualityBicubic;

        var bounds = new Rectangle(0, 0, Width - 1, Height - 1);
        var radius = LogicalToDeviceUnits(8);

        if (_pressed)
        {
            Theme.FillRounded(graphics, bounds, radius, Theme.Pressed);
        }
        else if (_hover)
        {
            Theme.FillRounded(graphics, bounds, radius, Theme.Hover);
        }

        if (_dropTarget)
        {
            Theme.DrawRoundedBorder(graphics, bounds, radius, Theme.Accent, Math.Max(1, LogicalToDeviceUnits(1)));
        }

        var iconSize = LogicalToDeviceUnits(20);
        var x = LogicalToDeviceUnits(8);
        var iconTop = (Height - iconSize) / 2;

        if (_icon is not null)
        {
            graphics.DrawImage(_icon, new Rectangle(x, iconTop, iconSize, iconSize));
        }
        else
        {
            DrawLetterTile(graphics, new Rectangle(x, iconTop, iconSize, iconSize));
        }

        x += iconSize;

        if (ShowLabel && _caption.Length > 0)
        {
            x += LogicalToDeviceUnits(6);
            var textBounds = new Rectangle(x, 0, Math.Max(1, Width - x - TextTrailing), Height);
            TextRenderer.DrawText(
                graphics,
                _caption,
                Font,
                textBounds,
                Theme.Text,
                TextFormatFlags.VerticalCenter |
                TextFormatFlags.SingleLine |
                TextFormatFlags.EndEllipsis |
                TextFormatFlags.NoPrefix);
        }

        if (ShowChevron)
        {
            DrawChevron(graphics, Width - LogicalToDeviceUnits(11), Height / 2);
        }

        if (InsertSide != InsertSide.None)
        {
            using var pen = new Pen(Theme.Accent, Math.Max(2, LogicalToDeviceUnits(2)));
            var markerX = InsertSide == InsertSide.Before ? 0 : Width - Math.Max(1, LogicalToDeviceUnits(2));
            graphics.DrawLine(pen, markerX, LogicalToDeviceUnits(4), markerX, Height - LogicalToDeviceUnits(4));
        }
    }

    private void DrawLetterTile(Graphics graphics, Rectangle bounds)
    {
        var radius = Math.Max(2, LogicalToDeviceUnits(5));
        using (var path = Theme.Rounded(bounds, radius))
        using (var brush = new LinearGradientBrush(bounds, Color.FromArgb(74, 84, 112), Color.FromArgb(42, 47, 62), 45f))
        {
            graphics.FillPath(brush, path);
        }

        var text = _caption.Trim();
        var letter = text.Length == 0 ? "?" : text[..1].ToUpperInvariant();
        TextRenderer.DrawText(
            graphics,
            letter,
            Theme.Ui(9f, FontStyle.Bold),
            bounds,
            Theme.Text,
            TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter | TextFormatFlags.NoPadding);
    }

    private void DrawChevron(Graphics graphics, int centerX, int centerY)
    {
        var half = LogicalToDeviceUnits(3);
        using var pen = new Pen(Theme.Muted, Math.Max(1f, LogicalToDeviceUnits(2) * 0.8f))
        {
            StartCap = LineCap.Round,
            EndCap = LineCap.Round
        };

        graphics.DrawLines(pen, new[]
        {
            new Point(centerX - half, centerY - half / 2),
            new Point(centerX, centerY + half),
            new Point(centerX + half, centerY - half / 2)
        });
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
        _pressed = false;
        Invalidate();
        base.OnMouseLeave(e);
    }

    protected override void OnMouseDown(MouseEventArgs e)
    {
        if (e.Button == MouseButtons.Left)
        {
            _pressed = true;
            Invalidate();
        }

        base.OnMouseDown(e);
    }

    protected override void OnMouseUp(MouseEventArgs e)
    {
        var activate = _pressed && e.Button == MouseButtons.Left && ClientRectangle.Contains(e.Location);
        _pressed = false;
        Invalidate();

        if (activate)
        {
            Activated?.Invoke(this, EventArgs.Empty);
        }

        base.OnMouseUp(e);
    }

    protected override void OnDragEnter(DragEventArgs drgevent)
    {
        if (drgevent.Data?.GetDataPresent(DataFormats.FileDrop) == true)
        {
            drgevent.Effect = DragDropEffects.Copy;
            _dropTarget = true;
            Invalidate();
        }
        else
        {
            drgevent.Effect = DragDropEffects.None;
        }

        base.OnDragEnter(drgevent);
    }

    protected override void OnDragLeave(EventArgs e)
    {
        _dropTarget = false;
        Invalidate();
        base.OnDragLeave(e);
    }

    protected override void OnDragDrop(DragEventArgs drgevent)
    {
        _dropTarget = false;
        Invalidate();

        if (drgevent.Data?.GetData(DataFormats.FileDrop) is string[] files && files.Length > 0)
        {
            FilesDropped?.Invoke(this, files);
        }

        base.OnDragDrop(drgevent);
    }
}
