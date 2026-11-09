using System.Drawing.Drawing2D;

namespace FloatingToolbar.UI;

/// <summary>Small square chrome button (pin, hide, close, add) drawn with a Segoe glyph.</summary>
public sealed class GlyphButton : Control
{
    private bool _hover;
    private bool _pressed;
    private bool _active;

    public GlyphButton()
    {
        SetStyle(
            ControlStyles.UserPaint |
            ControlStyles.AllPaintingInWmPaint |
            ControlStyles.OptimizedDoubleBuffer |
            ControlStyles.ResizeRedraw, true);

        BackColor = Theme.Background;
        Cursor = Cursors.Hand;
        Font = Theme.Icon(10.5f);
        Size = new Size(LogicalToDeviceUnits(30), LogicalToDeviceUnits(30));
    }

    public string Glyph { get; set; } = string.Empty;

    public Color GlyphColor { get; set; } = Theme.Muted;

    public Color ActiveGlyphColor { get; set; } = Theme.Accent;

    public Color HoverColor { get; set; } = Theme.Hover;

    public Color PressedColor { get; set; } = Theme.Pressed;

    public bool Active
    {
        get => _active;
        set
        {
            if (_active == value) return;
            _active = value;
            Invalidate();
        }
    }

    public event EventHandler? Activated;

    protected override void OnPaint(PaintEventArgs e)
    {
        var graphics = e.Graphics;
        graphics.SmoothingMode = SmoothingMode.AntiAlias;

        var bounds = new Rectangle(0, 0, Width - 1, Height - 1);
        var radius = LogicalToDeviceUnits(7);

        if (_pressed)
        {
            Theme.FillRounded(graphics, bounds, radius, PressedColor);
        }
        else if (_hover)
        {
            Theme.FillRounded(graphics, bounds, radius, HoverColor);
        }

        var color = _active ? ActiveGlyphColor : (_hover ? Theme.Text : GlyphColor);

        if (!string.IsNullOrEmpty(Glyph))
        {
            TextRenderer.DrawText(
                graphics,
                Glyph,
                Font,
                bounds,
                color,
                TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter | TextFormatFlags.NoPadding);
        }
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
}
