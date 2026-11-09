using FloatingToolbar.UI;

namespace FloatingToolbar;

partial class OptionsForm
{
    private System.ComponentModel.IContainer components = null!;

    private Label titleLabel = null!;
    private Label subtitleLabel = null!;
    private CheckBox startWithWindowsCheckBox = null!;
    private CheckBox alwaysOnTopCheckBox = null!;
    private CheckBox showInTaskbarCheckBox = null!;
    private CheckBox showLabelsCheckBox = null!;
    private CheckBox showFavoritesCheckBox = null!;
    private CheckBox hotkeyCheckBox = null!;
    private CheckBox autoHideFullscreenCheckBox = null!;
    private Button resetPositionButton = null!;
    private Label settingsPathLabel = null!;
    private Button okButton = null!;
    private Button cancelButton = null!;

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
        titleLabel = new Label();
        subtitleLabel = new Label();
        startWithWindowsCheckBox = new CheckBox();
        alwaysOnTopCheckBox = new CheckBox();
        showInTaskbarCheckBox = new CheckBox();
        showLabelsCheckBox = new CheckBox();
        showFavoritesCheckBox = new CheckBox();
        hotkeyCheckBox = new CheckBox();
        autoHideFullscreenCheckBox = new CheckBox();
        resetPositionButton = new Button();
        settingsPathLabel = new Label();
        okButton = new Button();
        cancelButton = new Button();
        SuspendLayout();

        // titleLabel
        titleLabel.AutoSize = true;
        titleLabel.Font = Theme.Ui(13f, FontStyle.Bold);
        titleLabel.ForeColor = Theme.Text;
        titleLabel.Location = new Point(18, 16);
        titleLabel.Text = "Options";

        // subtitleLabel
        subtitleLabel.AutoSize = true;
        subtitleLabel.Font = Theme.Ui(8.5f);
        subtitleLabel.ForeColor = Theme.Muted;
        subtitleLabel.Location = new Point(18, 44);
        subtitleLabel.Text = "Toolbar behaviour";

        // startWithWindowsCheckBox
        startWithWindowsCheckBox.AutoSize = true;
        startWithWindowsCheckBox.ForeColor = Theme.Text;
        startWithWindowsCheckBox.Location = new Point(18, 76);
        startWithWindowsCheckBox.Text = "Start with Windows";

        // alwaysOnTopCheckBox
        alwaysOnTopCheckBox.AutoSize = true;
        alwaysOnTopCheckBox.ForeColor = Theme.Text;
        alwaysOnTopCheckBox.Location = new Point(18, 104);
        alwaysOnTopCheckBox.Text = "Always on top of other windows";

        // showInTaskbarCheckBox
        showInTaskbarCheckBox.AutoSize = true;
        showInTaskbarCheckBox.ForeColor = Theme.Text;
        showInTaskbarCheckBox.Location = new Point(18, 132);
        showInTaskbarCheckBox.Text = "Show in Taskbar";

        // showLabelsCheckBox
        showLabelsCheckBox.AutoSize = true;
        showLabelsCheckBox.ForeColor = Theme.Text;
        showLabelsCheckBox.Location = new Point(18, 160);
        showLabelsCheckBox.Text = "Show button labels";

        // showFavoritesCheckBox
        showFavoritesCheckBox.AutoSize = true;
        showFavoritesCheckBox.ForeColor = Theme.Text;
        showFavoritesCheckBox.Location = new Point(18, 188);
        showFavoritesCheckBox.Text = "Show browser favourites fly-out";

        // hotkeyCheckBox
        hotkeyCheckBox.AutoSize = true;
        hotkeyCheckBox.ForeColor = Theme.Text;
        hotkeyCheckBox.Location = new Point(18, 216);
        hotkeyCheckBox.Text = "Global show/hide hotkey (Ctrl+Alt+Space)";

        // autoHideFullscreenCheckBox
        autoHideFullscreenCheckBox.AutoSize = true;
        autoHideFullscreenCheckBox.ForeColor = Theme.Text;
        autoHideFullscreenCheckBox.Location = new Point(18, 244);
        autoHideFullscreenCheckBox.Text = "Auto-hide while a fullscreen app is focused";

        // resetPositionButton
        resetPositionButton.Location = new Point(18, 284);
        resetPositionButton.Size = new Size(168, 30);
        resetPositionButton.Text = "Reset toolbar position";

        // settingsPathLabel
        settingsPathLabel.AutoSize = true;
        settingsPathLabel.Font = Theme.Ui(8.25f);
        settingsPathLabel.ForeColor = Theme.Muted;
        settingsPathLabel.Location = new Point(18, 330);
        settingsPathLabel.MaximumSize = new Size(340, 40);
        settingsPathLabel.Text = "Settings are saved to %APPDATA%\\FloatingToolbar\\settings.json";

        // okButton
        okButton.Location = new Point(186, 362);
        okButton.Size = new Size(84, 32);
        okButton.Text = "OK";

        // cancelButton
        cancelButton.Location = new Point(278, 362);
        cancelButton.Size = new Size(84, 32);
        cancelButton.Text = "Cancel";

        // OptionsForm
        AutoScaleDimensions = new SizeF(96F, 96F);
        AutoScaleMode = AutoScaleMode.Dpi;
        BackColor = Theme.Background;
        ClientSize = new Size(380, 412);
        Controls.Add(cancelButton);
        Controls.Add(okButton);
        Controls.Add(settingsPathLabel);
        Controls.Add(resetPositionButton);
        Controls.Add(autoHideFullscreenCheckBox);
        Controls.Add(hotkeyCheckBox);
        Controls.Add(showFavoritesCheckBox);
        Controls.Add(showLabelsCheckBox);
        Controls.Add(showInTaskbarCheckBox);
        Controls.Add(alwaysOnTopCheckBox);
        Controls.Add(startWithWindowsCheckBox);
        Controls.Add(subtitleLabel);
        Controls.Add(titleLabel);
        Font = Theme.Ui(9f);
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false;
        MinimizeBox = false;
        ShowInTaskbar = false;
        StartPosition = FormStartPosition.CenterParent;
        Text = "Floating Toolbar options";

        ResumeLayout(false);
        PerformLayout();
    }
}
