using FloatingToolbar.Models;
using FloatingToolbar.UI;

namespace FloatingToolbar;

/// <summary>
/// Options dialog. The toolbar writes these values back to settings.json when OK is pressed.
/// </summary>
public partial class OptionsForm : Form
{
    private readonly ToolbarSettings _settings;

    public OptionsForm() : this(new ToolbarSettings())
    {
    }

    public OptionsForm(ToolbarSettings settings)
    {
        _settings = settings;
        InitializeComponent();

        startWithWindowsCheckBox.Checked = settings.StartWithWindows;
        alwaysOnTopCheckBox.Checked = settings.AlwaysOnTop;
        showInTaskbarCheckBox.Checked = settings.ShowInTaskbar;
        showLabelsCheckBox.Checked = settings.ShowLabels;
        showFavoritesCheckBox.Checked = settings.ShowFavoritesFlyout;
        hotkeyCheckBox.Checked = settings.ShowHideHotkey;
        autoHideFullscreenCheckBox.Checked = settings.AutoHideFullscreen;

        foreach (var checkBox in new[]
                 {
                     startWithWindowsCheckBox, alwaysOnTopCheckBox, showInTaskbarCheckBox,
                     showLabelsCheckBox, showFavoritesCheckBox, hotkeyCheckBox,
                     autoHideFullscreenCheckBox
                 })
        {
            checkBox.FlatStyle = FlatStyle.Flat;
            checkBox.BackColor = Theme.Background;
            checkBox.Font = Theme.Ui(9f);
        }

        foreach (var button in new[] { okButton, cancelButton, resetPositionButton })
        {
            button.FlatStyle = FlatStyle.Flat;
            button.BackColor = Theme.Surface;
            button.ForeColor = Theme.Text;
            button.UseVisualStyleBackColor = false;
            button.FlatAppearance.BorderColor = Theme.Border;
            button.FlatAppearance.MouseOverBackColor = Theme.Hover;
            button.FlatAppearance.MouseDownBackColor = Theme.Pressed;
        }

        okButton.Click += (_, _) => Confirm();
        cancelButton.Click += (_, _) =>
        {
            DialogResult = DialogResult.Cancel;
            Close();
        };
        resetPositionButton.Click += (_, _) =>
        {
            ResetPositionRequested = true;
            resetPositionButton.Text = "Reset when you press OK";
        };

        AcceptButton = okButton;
        CancelButton = cancelButton;
    }

    public bool ResetPositionRequested { get; private set; }

    private void Confirm()
    {
        _settings.StartWithWindows = startWithWindowsCheckBox.Checked;
        _settings.AlwaysOnTop = alwaysOnTopCheckBox.Checked;
        _settings.ShowInTaskbar = showInTaskbarCheckBox.Checked;
        _settings.ShowLabels = showLabelsCheckBox.Checked;
        _settings.ShowFavoritesFlyout = showFavoritesCheckBox.Checked;
        _settings.ShowHideHotkey = hotkeyCheckBox.Checked;
        _settings.AutoHideFullscreen = autoHideFullscreenCheckBox.Checked;

        DialogResult = DialogResult.OK;
        Close();
    }
}
