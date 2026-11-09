using System.Drawing.Drawing2D;

namespace FloatingToolbar.UI;

/// <summary>
/// A borderless panel that hangs under the toolbar the way the group drop-downs do. It is shown
/// modeless, so the bar stays usable while it is open, and it closes itself once focus moves away —
/// unless one of its own child dialogs (a file picker or a nested prompt) is holding it open.
/// </summary>
public class DropDownForm : Form
{
    private const float EdgeWidth = 1.5f;
    private const int Gap = 6;
    private const int CornerRadius = 8;

    private readonly System.Windows.Forms.Timer _slideTimer = new() { Interval = 15 };
    private readonly System.Windows.Forms.Timer _armTimer = new() { Interval = 250 };
    private int _holdOpen;
    private int _slideFrom;
    private int _slideTo;
    private int _slideStep;
    private bool _armed;

    protected DropDownForm()
    {
        FormBorderStyle = FormBorderStyle.None;
        ShowInTaskbar = false;
        StartPosition = FormStartPosition.Manual;
        KeyPreview = true;
        DoubleBuffered = true;
        ForeColor = Theme.Text;
        Font = Theme.Ui(9f);

        _slideTimer.Tick += (_, _) => SlideStep();
        _armTimer.Tick += (_, _) => Arm();
    }

    /// <summary>Keeps the panel open while a modal child (file picker, nested prompt) is up.</summary>
    protected IDisposable HoldOpen()
    {
        _holdOpen++;
        return new Hold(this);
    }

    private sealed class Hold : IDisposable
    {
        private DropDownForm? _owner;

        public Hold(DropDownForm owner) => _owner = owner;

        public void Dispose()
        {
            var owner = _owner;
            _owner = null;
            if (owner is not null && owner._holdOpen > 0) owner._holdOpen--;
        }
    }

    /// <summary>Drops the panel out of the anchor's bottom edge and slides it into place.</summary>
    public void ShowUnder(Form anchor)
    {
        var screen = Screen.FromControl(anchor);
        var area = screen is null ? new Rectangle(0, 0, 1280, 720) : screen.WorkingArea;
        var gap = LogicalToDeviceUnits(Gap);

        var bounds = anchor.Visible ? anchor.Bounds : new Rectangle(area.Left + gap, area.Top + gap, 0, 0);
        var x = Math.Max(area.Left + gap, Math.Min(bounds.Left, area.Right - Width - gap));
        var y = bounds.Bottom + gap;

        if (y + Height > area.Bottom)
        {
            // No room underneath: hang from the top edge instead of running off the screen.
            y = bounds.Top - Height - gap;
            if (y < area.Top) y = area.Top;
        }

        TopMost = anchor.TopMost;
        Location = new Point(x, y);

        if (anchor is DropDownForm parent)
        {
            var hold = parent.HoldOpen();
            FormClosed += (_, _) => hold.Dispose();
        }

        Show(anchor);
        Activate();

        var slideFrom = Math.Max(area.Top, y - LogicalToDeviceUnits(10));
        if (slideFrom < y)
        {
            _slideFrom = slideFrom;
            _slideTo = y;
            _slideStep = 0;
            Location = new Point(x, slideFrom);
            _slideTimer.Start();
        }

        _armTimer.Start();
    }

    private void Arm()
    {
        _armTimer.Stop();
        _armed = true;

        if (IsDisposed || !Visible) return;
        if (ContainsFocus || ActiveForm == this) return;

        // The menu (or the bar's own startup) can hold on to focus for a moment after the panel is
        // shown. Take focus back once, and only close if that still leaves us in the background.
        Activate();

        var retry = new System.Windows.Forms.Timer { Interval = 300 };
        retry.Tick += (_, _) =>
        {
            retry.Stop();
            retry.Dispose();

            if (IsDisposed || !Visible || _holdOpen > 0) return;
            if (!ContainsFocus && ActiveForm != this) Close();
        };
        retry.Start();
    }

    private void SlideStep()
    {
        _slideStep++;
        var progress = Math.Min(1f, _slideStep / 4f);
        Location = new Point(Left, _slideFrom + (int)Math.Round((_slideTo - _slideFrom) * progress));
        if (progress >= 1f) _slideTimer.Stop();
    }

    protected override void OnDeactivate(EventArgs e)
    {
        base.OnDeactivate(e);

        if (!_armed || _holdOpen > 0 || !IsHandleCreated) return;

        BeginInvoke(new Action(() =>
        {
            if (IsDisposed || !Visible || _holdOpen > 0) return;
            Close();
        }));
    }

    protected override void OnKeyDown(KeyEventArgs e)
    {
        base.OnKeyDown(e);

        if (e.KeyCode != Keys.Escape) return;

        e.SuppressKeyPress = true;
        if (CancelButton is not null) CancelButton.PerformClick();
        else Close();
    }

    protected override void OnResize(EventArgs e)
    {
        base.OnResize(e);

        var radius = LogicalToDeviceUnits(CornerRadius);
        var previous = Region;
        Region = new Region(Theme.Rounded(new Rectangle(0, 0, Width, Height), radius));
        previous?.Dispose();
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        base.OnPaint(e);

        e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;

        var inset = (int)Math.Ceiling(EdgeWidth / 2f);
        Theme.DrawRoundedBorder(
            e.Graphics,
            new Rectangle(inset, inset, Width - inset * 2, Height - inset * 2),
            LogicalToDeviceUnits(CornerRadius),
            Theme.AccentEdge,
            EdgeWidth);
    }
}
