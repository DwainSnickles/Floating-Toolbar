using FloatingToolbar.Models;
using FloatingToolbar.Services;
using FloatingToolbar.UI;

namespace FloatingToolbar;

/// <summary>
/// Edits one group: add, remove, rename, retarget and reorder by dragging (or with the buttons).
/// Changes are applied to the live group when the dialog is closed.
/// </summary>
public partial class GroupEditorForm : Form
{
    private readonly LauncherGroup _group;
    private readonly List<LauncherItem> _items;
    private int _dragFrom = -1;

    public GroupEditorForm() : this(new LauncherGroup { Name = "Group" })
    {
    }

    public GroupEditorForm(LauncherGroup group)
    {
        _group = group;
        _items = group.Items.ToList();
        InitializeComponent();

        Text = $"Edit group \u2014 {group.Name}";
        nameLabel.Text = group.Name;
        hintLabel.Text = $"Entries appear in this order in the \u201c{group.Name}\u201d drop-down.";
        itemsList.ItemHeight = LogicalToDeviceUnits(32);

        foreach (var button in new[]
                 {
                     addButton, removeButton, renameButton, targetButton,
                     moveUpButton, moveDownButton, doneButton
                 })
        {
            StyleButton(button);
        }

        addButton.Click += (_, _) => AddEntry();
        removeButton.Click += (_, _) => RemoveEntry();
        renameButton.Click += (_, _) => RenameEntry();
        targetButton.Click += (_, _) => ChangeTarget();
        moveUpButton.Click += (_, _) => MoveEntry(-1);
        moveDownButton.Click += (_, _) => MoveEntry(1);
        doneButton.Click += (_, _) => Confirm();

        itemsList.DrawItem += ItemsList_DrawItem;
        itemsList.MouseDown += ItemsList_MouseDown;
        itemsList.MouseMove += ItemsList_MouseMove;
        itemsList.MouseUp += (_, _) => _dragFrom = -1;
        itemsList.DoubleClick += (_, _) => ChangeTarget();
        itemsList.SelectedIndexChanged += (_, _) => UpdateButtons();

        AcceptButton = doneButton;
        CancelButton = doneButton;

        RefreshList();
    }

    private static void StyleButton(Button button)
    {
        button.FlatStyle = FlatStyle.Flat;
        button.BackColor = Theme.Surface;
        button.ForeColor = Theme.Text;
        button.Font = Theme.Ui(9f);
        button.UseVisualStyleBackColor = false;
        button.FlatAppearance.BorderColor = Theme.Border;
        button.FlatAppearance.MouseOverBackColor = Theme.Hover;
        button.FlatAppearance.MouseDownBackColor = Theme.Pressed;
    }

    private LauncherItem? Selected =>
        itemsList.SelectedIndex >= 0 && itemsList.SelectedIndex < _items.Count
            ? _items[itemsList.SelectedIndex]
            : null;

    private void UpdateButtons()
    {
        var index = itemsList.SelectedIndex;
        var hasSelection = index >= 0 && index < _items.Count;

        removeButton.Enabled = hasSelection;
        renameButton.Enabled = hasSelection;
        targetButton.Enabled = hasSelection;
        moveUpButton.Enabled = hasSelection && index > 0;
        moveDownButton.Enabled = hasSelection && index < _items.Count - 1;
    }

    private void RefreshList()
    {
        itemsList.BeginUpdate();
        itemsList.Items.Clear();
        foreach (var item in _items) itemsList.Items.Add(item.Name);
        itemsList.EndUpdate();
        UpdateButtons();
    }

    private void ItemsList_DrawItem(object? sender, DrawItemEventArgs e)
    {
        if (e.Index < 0 || e.Index >= _items.Count) return;

        var item = _items[e.Index];
        var selected = (e.State & DrawItemState.Selected) != 0;

        using (var background = new SolidBrush(selected ? Theme.MenuHover : Theme.MenuBackground))
        {
            e.Graphics.FillRectangle(background, e.Bounds);
        }

        var iconSize = LogicalToDeviceUnits(20);
        var icon = IconLoader.Get(item);
        var iconBounds = new Rectangle(
            e.Bounds.Left + LogicalToDeviceUnits(8),
            e.Bounds.Top + (e.Bounds.Height - iconSize) / 2,
            iconSize,
            iconSize);

        if (icon is not null)
        {
            e.Graphics.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.HighQualityBicubic;
            e.Graphics.DrawImage(icon, iconBounds);
        }
        else
        {
            using var tile = Theme.Rounded(iconBounds, LogicalToDeviceUnits(5));
            using var brush = new SolidBrush(Theme.Hover);
            e.Graphics.FillPath(brush, tile);
        }

        var textBounds = new Rectangle(
            iconBounds.Right + LogicalToDeviceUnits(8),
            e.Bounds.Top,
            Math.Max(1, e.Bounds.Width - iconBounds.Right - LogicalToDeviceUnits(12)),
            e.Bounds.Height);

        TextRenderer.DrawText(
            e.Graphics,
            item.Name,
            Theme.Ui(9.5f),
            textBounds,
            Theme.Text,
            TextFormatFlags.VerticalCenter | TextFormatFlags.SingleLine |
            TextFormatFlags.EndEllipsis | TextFormatFlags.NoPrefix);
    }

    private void ItemsList_MouseDown(object? sender, MouseEventArgs e)
    {
        if (e.Button != MouseButtons.Left) return;
        _dragFrom = itemsList.IndexFromPoint(e.Location);
    }

    private void ItemsList_MouseMove(object? sender, MouseEventArgs e)
    {
        if (_dragFrom < 0 || _dragFrom >= _items.Count) return;
        if (e.Button != MouseButtons.Left) return;

        var target = itemsList.IndexFromPoint(e.Location);
        if (target < 0) target = _items.Count - 1;
        if (target == _dragFrom || target >= _items.Count) return;

        var item = _items[_dragFrom];
        _items.RemoveAt(_dragFrom);
        _items.Insert(target, item);
        _dragFrom = target;

        RefreshList();
        itemsList.SelectedIndex = target;
    }

    private void AddEntry()
    {
        using var dialog = new OpenFileDialog
        {
            Title = "Add an entry",
            Filter = "Applications and shortcuts (*.exe;*.lnk;*.url;*.bat;*.cmd)|*.exe;*.lnk;*.url;*.bat;*.cmd|All files (*.*)|*.*",
            CheckFileExists = true,
            Multiselect = true
        };

        if (dialog.ShowDialog(this) != DialogResult.OK) return;

        foreach (var file in dialog.FileNames)
        {
            _items.Add(new LauncherItem
            {
                Name = Path.GetFileNameWithoutExtension(file),
                Target = file,
                IconPath = file
            });
        }

        RefreshList();
        itemsList.SelectedIndex = _items.Count - 1;
    }

    private void RemoveEntry()
    {
        var index = itemsList.SelectedIndex;
        if (index < 0 || index >= _items.Count) return;

        _items.RemoveAt(index);
        RefreshList();
        itemsList.SelectedIndex = Math.Min(index, _items.Count - 1);
    }

    private void RenameEntry()
    {
        if (Selected is not { } item) return;

        var name = TextPromptForm.Show(this, "Rename entry", "Entry name:", item.Name);
        if (string.IsNullOrWhiteSpace(name)) return;

        item.Name = name.Trim();
        RefreshList();
    }

    private void ChangeTarget()
    {
        if (Selected is not { } item) return;

        var target = TextPromptForm.Show(
            this,
            "Change target",
            "Executable, shortcut, URL or shell target:",
            item.Target);

        if (string.IsNullOrWhiteSpace(target)) return;

        item.Target = target.Trim();
        item.IconPath = File.Exists(item.Target) ? item.Target : null;
        RefreshList();
    }

    private void MoveEntry(int offset)
    {
        var index = itemsList.SelectedIndex;
        if (index < 0 || index >= _items.Count) return;

        var target = index + offset;
        if (target < 0 || target >= _items.Count) return;

        var item = _items[index];
        _items.RemoveAt(index);
        _items.Insert(target, item);

        RefreshList();
        itemsList.SelectedIndex = target;
    }

    private void Confirm()
    {
        _group.Items.Clear();
        _group.Items.AddRange(_items);
        DialogResult = DialogResult.OK;
        Close();
    }
}
