namespace FloatingToolbar.UI;

/// <summary>Minimal dark text prompt used for group and button names.</summary>
internal sealed class TextPromptForm : Form
{
    private readonly TextBox _input;

    private TextPromptForm(string title, string label, string initialValue)
    {
        Text = title;
        FormBorderStyle = FormBorderStyle.FixedDialog;
        StartPosition = FormStartPosition.CenterParent;
        MaximizeBox = false;
        MinimizeBox = false;
        ShowInTaskbar = false;
        BackColor = Theme.Background;
        ForeColor = Theme.Text;
        Font = Theme.Ui(9f);
        ClientSize = new Size(360, 138);

        var promptLabel = new Label
        {
            Text = label,
            AutoSize = true,
            Location = new Point(16, 16),
            ForeColor = Theme.Muted
        };

        _input = new TextBox
        {
            Text = initialValue,
            Location = new Point(16, 42),
            Width = 328,
            BackColor = Theme.Surface,
            ForeColor = Theme.Text,
            BorderStyle = BorderStyle.FixedSingle
        };

        var okButton = CreateButton("OK", 166);
        okButton.Click += (_, _) => Confirm();

        var cancelButton = CreateButton("Cancel", 254);
        cancelButton.Click += (_, _) =>
        {
            DialogResult = DialogResult.Cancel;
            Close();
        };

        Controls.AddRange(new Control[] { promptLabel, _input, okButton, cancelButton });
        AcceptButton = okButton;
        CancelButton = cancelButton;
        Shown += (_, _) =>
        {
            _input.Focus();
            _input.SelectAll();
        };
    }

    public string Value => _input.Text.Trim();

    public static string? Show(IWin32Window owner, string title, string label, string initialValue = "")
    {
        using var prompt = new TextPromptForm(title, label, initialValue);
        return prompt.ShowDialog(owner) == DialogResult.OK ? prompt.Value : null;
    }

    private void Confirm()
    {
        if (Value.Length == 0) return;

        DialogResult = DialogResult.OK;
        Close();
    }

    private Button CreateButton(string text, int xPosition)
    {
        var button = new Button
        {
            Text = text,
            Location = new Point(xPosition, 90),
            Size = new Size(88, 30),
            FlatStyle = FlatStyle.Flat,
            BackColor = Theme.Surface,
            ForeColor = Theme.Text,
            UseVisualStyleBackColor = false
        };

        button.FlatAppearance.BorderColor = Theme.Border;
        button.FlatAppearance.MouseOverBackColor = Theme.Hover;
        button.FlatAppearance.MouseDownBackColor = Theme.Pressed;
        return button;
    }
}
