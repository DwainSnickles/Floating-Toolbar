using FloatingToolbar.UI;

namespace FloatingToolbar;

partial class GroupEditorForm
{
    private System.ComponentModel.IContainer components = null!;

    private Label nameLabel = null!;
    private Label hintLabel = null!;
    private ListBox itemsList = null!;
    private Button addButton = null!;
    private Button removeButton = null!;
    private Button renameButton = null!;
    private Button targetButton = null!;
    private Button moveUpButton = null!;
    private Button moveDownButton = null!;
    private Button doneButton = null!;
    private Label reorderHint = null!;

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
        nameLabel = new Label();
        hintLabel = new Label();
        itemsList = new ListBox();
        addButton = new Button();
        removeButton = new Button();
        renameButton = new Button();
        targetButton = new Button();
        moveUpButton = new Button();
        moveDownButton = new Button();
        doneButton = new Button();
        reorderHint = new Label();
        SuspendLayout();

        // nameLabel
        nameLabel.AutoSize = true;
        nameLabel.Font = Theme.Ui(13f, FontStyle.Bold);
        nameLabel.ForeColor = Theme.Text;
        nameLabel.Location = new Point(16, 12);
        nameLabel.Text = "Group";

        // hintLabel
        hintLabel.AutoSize = true;
        hintLabel.Font = Theme.Ui(8.5f);
        hintLabel.ForeColor = Theme.Muted;
        hintLabel.Location = new Point(16, 40);
        hintLabel.Text = "Entries appear in this order in the drop-down.";

        // itemsList
        itemsList.BackColor = Theme.MenuBackground;
        itemsList.BorderStyle = BorderStyle.FixedSingle;
        itemsList.DrawMode = DrawMode.OwnerDrawFixed;
        itemsList.Font = Theme.Ui(9.5f);
        itemsList.ForeColor = Theme.Text;
        itemsList.IntegralHeight = false;
        itemsList.ItemHeight = 32;
        itemsList.Location = new Point(16, 64);
        itemsList.Size = new Size(256, 296);

        // addButton
        addButton.Location = new Point(284, 64);
        addButton.Size = new Size(124, 32);
        addButton.Text = "Add\u2026";

        // removeButton
        removeButton.Location = new Point(284, 102);
        removeButton.Size = new Size(124, 32);
        removeButton.Text = "Remove";

        // renameButton
        renameButton.Location = new Point(284, 140);
        renameButton.Size = new Size(124, 32);
        renameButton.Text = "Rename\u2026";

        // targetButton
        targetButton.Location = new Point(284, 178);
        targetButton.Size = new Size(124, 32);
        targetButton.Text = "Change target\u2026";

        // moveUpButton
        moveUpButton.Location = new Point(284, 216);
        moveUpButton.Size = new Size(124, 32);
        moveUpButton.Text = "Move up";

        // moveDownButton
        moveDownButton.Location = new Point(284, 254);
        moveDownButton.Size = new Size(124, 32);
        moveDownButton.Text = "Move down";

        // reorderHint
        reorderHint.AutoSize = true;
        reorderHint.Font = Theme.Ui(8.5f);
        reorderHint.ForeColor = Theme.Muted;
        reorderHint.Location = new Point(16, 372);
        reorderHint.Text = "Tip: drag entries in the list to reorder them.";

        // doneButton
        doneButton.Location = new Point(300, 368);
        doneButton.Size = new Size(108, 30);
        doneButton.Text = "Done";

        // GroupEditorForm
        AutoScaleDimensions = new SizeF(96F, 96F);
        AutoScaleMode = AutoScaleMode.Dpi;
        BackColor = Theme.Background;
        ClientSize = new Size(424, 412);
        Controls.Add(doneButton);
        Controls.Add(reorderHint);
        Controls.Add(moveDownButton);
        Controls.Add(moveUpButton);
        Controls.Add(targetButton);
        Controls.Add(renameButton);
        Controls.Add(removeButton);
        Controls.Add(addButton);
        Controls.Add(itemsList);
        Controls.Add(hintLabel);
        Controls.Add(nameLabel);
        Font = Theme.Ui(9f);
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false;
        MinimizeBox = false;
        ShowInTaskbar = false;
        StartPosition = FormStartPosition.CenterParent;
        Text = "Edit group";

        ResumeLayout(false);
        PerformLayout();
    }
}
