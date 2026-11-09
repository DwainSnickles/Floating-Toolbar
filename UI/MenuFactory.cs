using System.Drawing.Drawing2D;

namespace FloatingToolbar.UI;

/// <summary>A dark professional renderer for menus so they match the toolbar.</summary>
internal sealed class DarkMenuRenderer : ToolStripProfessionalRenderer
{
    public DarkMenuRenderer() : base(new DarkMenuColorTable())
    {
        RoundedEdges = false;
    }

    protected override void OnRenderToolStripBackground(ToolStripRenderEventArgs e)
    {
        using var brush = new SolidBrush(Theme.MenuBackground);
        e.Graphics.FillRectangle(brush, e.AffectedBounds);
    }

    protected override void OnRenderImageMargin(ToolStripRenderEventArgs e)
    {
        using var brush = new SolidBrush(Theme.MenuBackground);
        e.Graphics.FillRectangle(brush, e.AffectedBounds);
    }

    protected override void OnRenderToolStripBorder(ToolStripRenderEventArgs e)
    {
        using var pen = new Pen(Theme.Border);
        e.Graphics.DrawRectangle(pen, 0, 0, e.ToolStrip.Width - 1, e.ToolStrip.Height - 1);
    }

    protected override void OnRenderItemText(ToolStripItemTextRenderEventArgs e)
    {
        e.TextColor = e.Item.Enabled ? Theme.Text : Theme.Muted;
        base.OnRenderItemText(e);
    }

    protected override void OnRenderArrow(ToolStripArrowRenderEventArgs e)
    {
        e.ArrowColor = Theme.Muted;
        base.OnRenderArrow(e);
    }

    protected override void OnRenderItemImage(ToolStripItemImageRenderEventArgs e)
    {
        if (e.Image is null)
        {
            base.OnRenderItemImage(e);
            return;
        }

        e.Graphics.InterpolationMode = InterpolationMode.HighQualityBicubic;
        e.Graphics.DrawImage(e.Image, e.ImageRectangle);
    }

    protected override void OnRenderMenuItemBackground(ToolStripItemRenderEventArgs e)
    {
        if (!e.Item.Selected) return;

        if (e.ToolStrip is null) return;

        using var brush = new SolidBrush(Theme.MenuHover);
        var bounds = new Rectangle(2, e.Item.ContentRectangle.Top - 1, e.ToolStrip.Width - 4, e.Item.Height);
        e.Graphics.FillRectangle(brush, bounds);
    }

    protected override void OnRenderSeparator(ToolStripSeparatorRenderEventArgs e)
    {
        if (e.ToolStrip is null) return;

        using var pen = new Pen(Theme.Border);
        var y = e.Item.Height / 2;
        e.Graphics.DrawLine(pen, 8, y, e.ToolStrip.Width - 8, y);
    }
}

internal sealed class DarkMenuColorTable : ProfessionalColorTable
{
    public override Color ToolStripDropDownBackground => Theme.MenuBackground;

    public override Color ImageMarginGradientBegin => Theme.MenuBackground;

    public override Color ImageMarginGradientMiddle => Theme.MenuBackground;

    public override Color ImageMarginGradientEnd => Theme.MenuBackground;

    public override Color MenuBorder => Theme.Border;

    public override Color MenuItemBorder => Theme.MenuHover;

    public override Color MenuItemSelected => Theme.MenuHover;

    public override Color MenuItemSelectedGradientBegin => Theme.MenuHover;

    public override Color MenuItemSelectedGradientEnd => Theme.MenuHover;

    public override Color MenuItemPressedGradientBegin => Theme.MenuHover;

    public override Color MenuItemPressedGradientEnd => Theme.MenuHover;

    public override Color SeparatorDark => Theme.Border;

    public override Color SeparatorLight => Theme.Border;

    public override Color CheckBackground => Theme.MenuHover;

    public override Color CheckSelectedBackground => Theme.MenuHover;

    public override Color CheckPressedBackground => Theme.MenuHover;
}

/// <summary>Builds consistently styled dark context menus.</summary>
internal static class MenuFactory
{
    public static ContextMenuStrip CreateMenu()
    {
        var menu = new ContextMenuStrip();
        Style(menu);
        return menu;
    }

    public static void Style(ContextMenuStrip menu)
    {
        menu.Renderer = new DarkMenuRenderer();
        menu.BackColor = Theme.MenuBackground;
        menu.ForeColor = Theme.Text;
        menu.Font = Theme.Ui(9f);
        menu.ShowImageMargin = true;
        menu.ImageScalingSize = new Size(18, 18);
        menu.Padding = new Padding(0, 4, 0, 4);
        menu.DropShadowEnabled = true;
    }

    public static ToolStripMenuItem Item(string text, Image? image, EventHandler? onClick)
    {
        var item = new ToolStripMenuItem(text)
        {
            ForeColor = Theme.Text,
            Font = Theme.Ui(9f),
            Padding = new Padding(4, 3, 8, 3)
        };

        if (image is not null) item.Image = image;
        if (onClick is not null) item.Click += onClick;
        return item;
    }

    public static void StyleSubMenus(ContextMenuStrip menu)
    {
        foreach (ToolStripItem item in menu.Items)
        {
            if (item is ToolStripMenuItem menuItem) StyleDropDown(menuItem);
        }
    }

    public static void StyleDropDown(ToolStripMenuItem item)
    {
        var dropDown = item.DropDown;
        dropDown.Renderer = new DarkMenuRenderer();
        dropDown.BackColor = Theme.MenuBackground;
        dropDown.ForeColor = Theme.Text;
        dropDown.Font = Theme.Ui(9f);
        dropDown.ImageScalingSize = new Size(18, 18);
        dropDown.DropShadowEnabled = true;

        if (dropDown is ToolStripDropDownMenu dropDownMenu)
        {
            dropDownMenu.ShowImageMargin = true;
        }

        foreach (ToolStripItem child in item.DropDownItems)
        {
            child.ForeColor = Theme.Text;
            child.Font = Theme.Ui(9f);
            child.Padding = new Padding(4, 3, 8, 3);
            if (child is ToolStripMenuItem childMenu) StyleDropDown(childMenu);
        }
    }
}
