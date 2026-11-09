using FloatingToolbar.UI;

namespace FloatingToolbar;

partial class FloatingToolbarForm
{
    private System.ComponentModel.IContainer components = null!;

    private DragGrip dragGrip = null!;
    private Label titleLabel = null!;
    private FlowLayoutPanel groupsFlow = null!;
    private FlowLayoutPanel itemsFlow = null!;
    private GlyphButton addGroupButton = null!;
    private GlyphButton addItemButton = null!;
    private GlyphButton pinButton = null!;
    private GlyphButton hideButton = null!;
    private GlyphButton closeButton = null!;
    private Panel promptPanel = null!;
    private Label promptTitleLabel = null!;
    private Label promptLabel = null!;
    private TextBox promptTextBox = null!;
    private Button promptOkButton = null!;
    private Button promptCancelButton = null!;
    private Button promptIconButton = null!;
    private PictureBox promptIconPreview = null!;
    private ToolTip toolTip = null!;

    protected override void Dispose(bool disposing)
    {
        if (disposing && components is not null)
        {
            components.Dispose();
        }

        base.Dispose(disposing);
    }

    private void InitializeComponent()
    {
        components = new System.ComponentModel.Container();
        dragGrip = new DragGrip();
        titleLabel = new Label();
        groupsFlow = new FlowLayoutPanel();
        itemsFlow = new FlowLayoutPanel();
        addGroupButton = new GlyphButton();
        addItemButton = new GlyphButton();
        pinButton = new GlyphButton();
        hideButton = new GlyphButton();
        closeButton = new GlyphButton();
        promptPanel = new Panel();
        promptTitleLabel = new Label();
        promptLabel = new Label();
        promptTextBox = new TextBox();
        promptOkButton = new Button();
        promptCancelButton = new Button();
        promptIconButton = new Button();
        promptIconPreview = new PictureBox();
        toolTip = new ToolTip(components)
        {
            OwnerDraw = true,
            BackColor = Theme.Surface,
            ForeColor = Theme.Text
        };
        toolTip.Draw += ToolTip_Draw;
        SuspendLayout();

        // dragGrip
        dragGrip.Cursor = Cursors.SizeAll;
        dragGrip.Location = new Point(4, 6);
        dragGrip.Size = new Size(14, 40);
        toolTip.SetToolTip(dragGrip, "Drag to move the toolbar");

        // titleLabel
        titleLabel.AutoSize = false;
        titleLabel.BackColor = Theme.Background;
        titleLabel.Font = Theme.Ui(8.75f, FontStyle.Bold);
        titleLabel.ForeColor = Theme.Muted;
        titleLabel.Location = new Point(20, 6);
        titleLabel.Size = new Size(58, 40);
        titleLabel.Text = "TOOLBAR";
        titleLabel.TextAlign = ContentAlignment.MiddleCenter;

        // groupsFlow
        groupsFlow.AutoSize = false;
        groupsFlow.BackColor = Theme.Background;
        groupsFlow.FlowDirection = FlowDirection.LeftToRight;
        groupsFlow.Location = new Point(82, 6);
        groupsFlow.Margin = Padding.Empty;
        groupsFlow.Padding = Padding.Empty;
        groupsFlow.Size = new Size(160, 40);
        groupsFlow.WrapContents = false;

        // itemsFlow
        itemsFlow.AutoSize = false;
        itemsFlow.BackColor = Theme.Background;
        itemsFlow.FlowDirection = FlowDirection.LeftToRight;
        itemsFlow.Location = new Point(246, 6);
        itemsFlow.Margin = Padding.Empty;
        itemsFlow.Padding = Padding.Empty;
        itemsFlow.Size = new Size(120, 40);
        itemsFlow.WrapContents = false;
        itemsFlow.Visible = false;

        // addGroupButton
        addGroupButton.ActiveGlyphColor = Theme.Accent;
        addGroupButton.Glyph = Glyphs.NewGroup;
        addGroupButton.GlyphColor = Theme.Muted;
        addGroupButton.Size = new Size(28, 40);
        toolTip.SetToolTip(addGroupButton, "New group");

        // addItemButton
        addItemButton.ActiveGlyphColor = Theme.Accent;
        addItemButton.Glyph = Glyphs.NewButton;
        addItemButton.GlyphColor = Theme.Accent;
        addItemButton.Size = new Size(28, 40);
        toolTip.SetToolTip(addItemButton, "New button");

        // pinButton
        pinButton.ActiveGlyphColor = Theme.Accent;
        pinButton.Glyph = Glyphs.Pin;
        pinButton.GlyphColor = Theme.Muted;
        pinButton.Location = new Point(408, 11);
        pinButton.Size = new Size(30, 30);
        toolTip.SetToolTip(pinButton, "Always on top");

        // hideButton
        hideButton.Glyph = Glyphs.Minimize;
        hideButton.GlyphColor = Theme.Muted;
        hideButton.Location = new Point(442, 11);
        hideButton.Size = new Size(30, 30);
        toolTip.SetToolTip(hideButton, "Hide to the notification area");

        // closeButton
        closeButton.Glyph = Glyphs.Close;
        closeButton.GlyphColor = Theme.Muted;
        closeButton.HoverColor = Theme.Danger;
        closeButton.Location = new Point(476, 11);
        closeButton.Size = new Size(30, 30);
        toolTip.SetToolTip(closeButton, "Exit the toolbar");

        // promptPanel (slides out of the bar itself, so it can never sit behind the toolbar)
        promptPanel.BackColor = Theme.Background;
        promptPanel.Margin = Padding.Empty;
        promptPanel.Padding = Padding.Empty;
        promptPanel.Visible = false;
        promptPanel.Controls.Add(promptTitleLabel);
        promptPanel.Controls.Add(promptLabel);
        promptPanel.Controls.Add(promptTextBox);
        promptPanel.Controls.Add(promptOkButton);
        promptPanel.Controls.Add(promptCancelButton);
        promptPanel.Controls.Add(promptIconButton);
        promptPanel.Controls.Add(promptIconPreview);

        // promptTitleLabel
        promptTitleLabel.AutoSize = false;
        promptTitleLabel.Font = Theme.Ui(9.5f, FontStyle.Bold);
        promptTitleLabel.ForeColor = Theme.Text;
        promptTitleLabel.Text = "New group";

        // promptLabel
        promptLabel.AutoSize = false;
        promptLabel.Font = Theme.Ui(9f);
        promptLabel.ForeColor = Theme.Muted;
        promptLabel.Text = "Group name:";

        // promptTextBox
        promptTextBox.BackColor = Theme.Surface;
        promptTextBox.BorderStyle = BorderStyle.FixedSingle;
        promptTextBox.Font = Theme.Ui(9f);
        promptTextBox.ForeColor = Theme.Text;

        // promptOkButton
        promptOkButton.BackColor = Theme.Surface;
        promptOkButton.FlatStyle = FlatStyle.Flat;
        promptOkButton.FlatAppearance.BorderColor = Theme.Border;
        promptOkButton.FlatAppearance.MouseDownBackColor = Theme.Pressed;
        promptOkButton.FlatAppearance.MouseOverBackColor = Theme.Hover;
        promptOkButton.ForeColor = Theme.Text;
        promptOkButton.Text = "OK";
        promptOkButton.UseVisualStyleBackColor = false;

        // promptCancelButton
        promptCancelButton.BackColor = Theme.Surface;
        promptCancelButton.FlatStyle = FlatStyle.Flat;
        promptCancelButton.FlatAppearance.BorderColor = Theme.Border;
        promptCancelButton.FlatAppearance.MouseDownBackColor = Theme.Pressed;
        promptCancelButton.FlatAppearance.MouseOverBackColor = Theme.Hover;
        promptCancelButton.ForeColor = Theme.Text;
        promptCancelButton.Text = "Cancel";
        promptCancelButton.UseVisualStyleBackColor = false;

        // promptIconButton
        promptIconButton.BackColor = Theme.Surface;
        promptIconButton.FlatStyle = FlatStyle.Flat;
        promptIconButton.FlatAppearance.BorderColor = Theme.Border;
        promptIconButton.FlatAppearance.MouseDownBackColor = Theme.Pressed;
        promptIconButton.FlatAppearance.MouseOverBackColor = Theme.Hover;
        promptIconButton.ForeColor = Theme.Text;
        promptIconButton.Text = "Icon\u2026";
        promptIconButton.UseVisualStyleBackColor = false;
        promptIconButton.Visible = false;

        // promptIconPreview
        promptIconPreview.BackColor = Theme.Surface;
        promptIconPreview.BorderStyle = BorderStyle.FixedSingle;
        promptIconPreview.SizeMode = PictureBoxSizeMode.Zoom;
        promptIconPreview.Visible = false;

        // FloatingToolbarForm
        AllowDrop = true;
        AutoScaleDimensions = new SizeF(96F, 96F);
        AutoScaleMode = AutoScaleMode.Dpi;
        BackColor = Theme.Background;
        ClientSize = new Size(560, 52);
        Controls.Add(addGroupButton);
        Controls.Add(addItemButton);
        Controls.Add(closeButton);
        Controls.Add(hideButton);
        Controls.Add(pinButton);
        Controls.Add(itemsFlow);
        Controls.Add(promptPanel);
        Controls.Add(groupsFlow);
        Controls.Add(titleLabel);
        Controls.Add(dragGrip);
        DoubleBuffered = true;
        Font = Theme.Ui(9f);
        FormBorderStyle = FormBorderStyle.None;
        Name = "FloatingToolbarForm";
        ShowInTaskbar = false;
        StartPosition = FormStartPosition.Manual;
        Text = "Floating Toolbar";
        TopMost = true;

        ResumeLayout(false);
    }
}
