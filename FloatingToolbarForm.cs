using System.ComponentModel;
using System.Drawing.Drawing2D;
using System.Runtime.InteropServices;
using FloatingToolbar.Models;
using FloatingToolbar.Services;
using FloatingToolbar.UI;

namespace FloatingToolbar;

/// <summary>
/// The compact floating launcher bar: groups drop down, pinned entries launch on click,
/// the whole thing drags by its grip and stays inside the current screen.
/// </summary>
public partial class FloatingToolbarForm : Form
{
    private const int DwmWindowCornerPreference = 33;
    private const int DwmWindowCornerRound = 2;

    [DllImport("dwmapi.dll")]
    private static extern int DwmSetWindowAttribute(IntPtr window, int attribute, ref int value, int size);

    private const int HotkeyId = 0x4A21;
    private const int WmHotkey = 0x0312;
    private const uint ModAlt = 0x0001;
    private const uint ModControl = 0x0002;

    [DllImport("user32.dll", SetLastError = true)]
    private static extern bool RegisterHotKey(IntPtr window, int id, uint modifiers, uint virtualKey);

    [DllImport("user32.dll", SetLastError = true)]
    private static extern bool UnregisterHotKey(IntPtr window, int id);

    private readonly ToolbarSettings _settings;
    private readonly List<ToolbarItemButton> _groupButtons = new();
    private readonly List<ToolbarItemButton> _pinnedButtons = new();
    private readonly List<ContextMenuStrip> _ownedMenus = new();

    private NotifyIcon? _notifyIcon;
    private ContextMenuStrip? _trayMenu;
    private ContextMenuStrip? _toolbarMenu;
    private Icon? _appIcon;
    private bool _layoutRunning;
    private bool _windowDragging;
    private Point _windowDragCursor;
    private Point _windowDragOrigin;
    private bool _balloonShown;
    private System.Windows.Forms.Timer? _fullscreenTimer;
    private bool _hiddenByFullscreen;
    private bool _autoHideSuspended;
    private bool _hotkeyRegistered;

    private ToolbarItemButton? _dragSource;
    private FlowLayoutPanel? _dragFlow;
    private List<ToolbarItemButton>? _dragList;
    private Point _dragStart;
    private bool _dragActive;
    private int _dropIndex = -1;

    public FloatingToolbarForm()
    {
        _settings = SettingsStore.Load();
        _settings.StartWithWindows = StartupManager.IsEnabled();
        InitializeComponent();
        InitializeTray();
        InitializeTimers();
        WireEvents();
    }

    protected override CreateParams CreateParams
    {
        get
        {
            var parameters = base.CreateParams;
            parameters.ClassStyle |= 0x00020000; // CS_DROPSHADOW
            return parameters;
        }
    }

    private int S(int value) => (int)Math.Round(value * DeviceDpi / 96.0);

    // ---------------------------------------------------------------- setup

    private void InitializeTray()
    {
        _appIcon = IconFactory.Create();
        Icon = _appIcon;

        _trayMenu = MenuFactory.CreateMenu();
        _trayMenu.Opening += TrayMenu_Opening;

        _notifyIcon = new NotifyIcon(components)
        {
            Icon = _appIcon,
            Text = "Floating Toolbar",
            ContextMenuStrip = _trayMenu,
            Visible = true
        };
        _notifyIcon.DoubleClick += (_, _) => ShowToolbar();
    }

    private void WireEvents()
    {
        Load += FloatingToolbarForm_Load;
        FormClosing += FloatingToolbarForm_FormClosing;
        Activated += (_, _) => ClearBusyCursor();

        AllowDrop = true;
        DragEnter += OnFileDragEnter;
        DragDrop += OnFileDragDrop;

        MouseDown += BeginWindowDrag;
        MouseMove += ContinueWindowDrag;
        MouseUp += EndWindowDrag;

        titleLabel.MouseDown += BeginWindowDrag;
        titleLabel.MouseMove += ContinueWindowDrag;
        titleLabel.MouseUp += EndWindowDrag;

        dragGrip.MouseDown += BeginWindowDrag;
        dragGrip.MouseMove += ContinueWindowDrag;
        dragGrip.MouseUp += EndWindowDrag;

        pinButton.Activated += (_, _) => SetAlwaysOnTop(!_settings.AlwaysOnTop);
        hideButton.Activated += (_, _) => HideToolbar();
        closeButton.Activated += (_, _) => CloseToolbar();
        addGroupButton.Activated += (_, _) => CreateGroup();

        addGroupButton.AllowDrop = true;
        addGroupButton.DragEnter += OnFileDragEnter;
        addGroupButton.DragDrop += OnFilesDroppedOnAddButton;

        foreach (var panel in new Control[] { groupsFlow, itemsFlow })
        {
            panel.AllowDrop = true;
            panel.DragEnter += OnFileDragEnter;
            panel.DragDrop += OnFileDragDrop;
        }

        _toolbarMenu = MenuFactory.CreateMenu();
        _toolbarMenu.Opening += ToolbarMenu_Opening;
        ContextMenuStrip = _toolbarMenu;
        titleLabel.ContextMenuStrip = _toolbarMenu;
        dragGrip.ContextMenuStrip = _toolbarMenu;
        groupsFlow.ContextMenuStrip = _toolbarMenu;
        itemsFlow.ContextMenuStrip = _toolbarMenu;
    }

    private void FloatingToolbarForm_Load(object? sender, EventArgs e)
    {
        ApplySettings();
        BuildToolbar();
        LayoutToolbar();
        RestorePosition();
        UpdateHotkey();
        ClearBusyCursor();
    }

    /// <summary>
    /// WinForms keeps the app-starting (busy) cursor until something replaces it. Clear it once the
    /// bar is up, and again whenever the bar is activated, so the pointer never stays busy.
    /// </summary>
    private void ClearBusyCursor()
    {
        UseWaitCursor = false;
        Application.UseWaitCursor = false;
        Cursor.Current = Cursors.Default;
    }

    /// <summary>Keeps tooltips dark instead of the default white system hint.</summary>
    private void ToolTip_Draw(object? sender, DrawToolTipEventArgs e)
    {
        e.DrawBackground();
        e.DrawText();
    }

    private void InitializeTimers()
    {
        _fullscreenTimer = new System.Windows.Forms.Timer(components) { Interval = 750 };
        _fullscreenTimer.Tick += FullscreenTimer_Tick;
    }

    private void ApplySettings()
    {
        TopMost = _settings.AlwaysOnTop;
        ShowInTaskbar = _settings.ShowInTaskbar;
        pinButton.Active = _settings.AlwaysOnTop;
        pinButton.Glyph = _settings.AlwaysOnTop ? Glyphs.Pin : Glyphs.Unpin;

        if (_fullscreenTimer is not null)
        {
            _fullscreenTimer.Enabled = _settings.AutoHideFullscreen;
        }
    }

    private void FullscreenTimer_Tick(object? sender, EventArgs e)
    {
        if (!_settings.AutoHideFullscreen) return;

        var fullscreen = FullscreenDetector.IsFullscreenInForeground(Handle);
        if (!fullscreen)
        {
            _autoHideSuspended = false;

            if (_hiddenByFullscreen)
            {
                _hiddenByFullscreen = false;
                ShowToolbar(activate: false);
            }

            return;
        }

        // Do not fight the user: if they just brought the bar back, leave it alone until
        // the fullscreen application goes away.
        if (_autoHideSuspended) return;

        if (Visible)
        {
            _hiddenByFullscreen = true;
            HideToolbar(automatic: true);
        }
    }

    private void UpdateHotkey()
    {
        if (!IsHandleCreated) return;

        if (_hotkeyRegistered)
        {
            UnregisterHotKey(Handle, HotkeyId);
            _hotkeyRegistered = false;
        }

        if (!_settings.ShowHideHotkey) return;

        _hotkeyRegistered = RegisterHotKey(Handle, HotkeyId, ModControl | ModAlt, (uint)Keys.Space);
    }

    protected override void WndProc(ref Message message)
    {
        if (message.Msg == WmHotkey && message.WParam.ToInt32() == HotkeyId)
        {
            ToggleVisibilityFromHotkey();
            return;
        }

        base.WndProc(ref message);
    }

    private void ToggleVisibilityFromHotkey()
    {
        if (Visible)
        {
            _autoHideSuspended = false;
            HideToolbar();
            return;
        }

        ShowToolbar();
    }

    private void FloatingToolbarForm_FormClosing(object? sender, FormClosingEventArgs e)
    {
        if (_hotkeyRegistered)
        {
            UnregisterHotKey(Handle, HotkeyId);
            _hotkeyRegistered = false;
        }

        SaveSettings();

        if (_notifyIcon is not null)
        {
            _notifyIcon.Visible = false;
            _notifyIcon.Dispose();
            _notifyIcon = null;
        }

        foreach (var menu in _ownedMenus) menu.Dispose();
        _ownedMenus.Clear();

        _trayMenu?.Dispose();
        _appIcon?.Dispose();
    }

    protected override void OnDpiChanged(DpiChangedEventArgs e)
    {
        base.OnDpiChanged(e);
        BuildToolbar();
        LayoutToolbar();
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        base.OnPaint(e);
        e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
        Theme.DrawRoundedBorder(
            e.Graphics,
            new Rectangle(0, 0, Width - 1, Height - 1),
            S(10),
            Theme.Border);
    }

    // ---------------------------------------------------------------- layout

    private void BuildToolbar()
    {
        foreach (var button in _groupButtons) button.Dispose();
        _groupButtons.Clear();

        foreach (var button in _pinnedButtons) button.Dispose();
        _pinnedButtons.Clear();

        groupsFlow.Controls.Clear();
        itemsFlow.Controls.Clear();

        foreach (var menu in _ownedMenus) menu.Dispose();
        _ownedMenus.Clear();

        foreach (var group in _settings.Groups)
        {
            var captured = group;
            var button = CreateToolbarButton(group.Name, FirstIcon(group), isGroup: true);
            button.Activated += (_, _) =>
            {
                if (_dragActive) return;
                ShowGroupMenu(button, captured);
            };

            var groupMenu = BuildGroupContextMenu(captured);
            _ownedMenus.Add(groupMenu);
            button.ContextMenuStrip = groupMenu;
            button.FilesDropped += (_, files) => AddFilesToGroup(captured, files);

            HookItemDrag(button, groupsFlow, _groupButtons);
            groupsFlow.Controls.Add(button);
            _groupButtons.Add(button);
        }

        addGroupButton.Size = new Size(S(28), S(40));
        addGroupButton.Glyph = Glyphs.Add;
        addGroupButton.GlyphColor = Theme.Muted;
        addGroupButton.Margin = new Padding(S(2), 0, S(2), 0);
        addGroupButton.Font = Theme.Icon(10.5f);
        groupsFlow.Controls.Add(addGroupButton);

        foreach (var pinned in _settings.PinnedItems)
        {
            var captured = pinned;
            var button = CreateToolbarButton(pinned.Name, IconLoader.Get(pinned), isGroup: false);
            button.Activated += (_, _) =>
            {
                if (_dragActive) return;
                Launch(captured);
            };

            var pinnedMenu = BuildPinnedContextMenu(captured);
            _ownedMenus.Add(pinnedMenu);
            button.ContextMenuStrip = pinnedMenu;

            HookItemDrag(button, itemsFlow, _pinnedButtons);
            itemsFlow.Controls.Add(button);
            _pinnedButtons.Add(button);
        }
    }

    private void LayoutToolbar()
    {
        if (_layoutRunning) return;

        _layoutRunning = true;
        try
        {
            var pad = S(4);
            var gap = S(4);
            var buttonHeight = S(40);
            var height = buttonHeight + S(12);
            var top = (height - buttonHeight) / 2;

            var x = pad;

            var gripWidth = S(12);
            dragGrip.SetBounds(x, top, gripWidth, buttonHeight);
            x += gripWidth + S(2);

            var titleWidth = string.IsNullOrEmpty(titleLabel.Text)
                ? 0
                : TextRenderer.MeasureText(titleLabel.Text, titleLabel.Font).Width + S(6);
            titleLabel.Visible = titleWidth > 0;
            titleLabel.SetBounds(x, top, titleWidth, buttonHeight);
            x += titleWidth + gap;

            var groupsWidth = MeasureFlow(groupsFlow, buttonHeight);
            groupsFlow.SetBounds(x, top, groupsWidth, buttonHeight);
            x += groupsWidth + gap;

            var itemsWidth = MeasureFlow(itemsFlow, buttonHeight);
            itemsFlow.SetBounds(x, top, Math.Max(itemsWidth, 1), buttonHeight);
            itemsFlow.Visible = _pinnedButtons.Count > 0;
            if (itemsFlow.Visible) x += itemsWidth + gap;

            var glyphSize = S(30);
            var glyphGap = S(2);
            var chromeWidth = glyphSize * 3 + glyphGap * 2;
            var totalWidth = x + chromeWidth + pad;

            ClientSize = new Size(totalWidth, height);

            var glyphTop = (height - glyphSize) / 2;
            var right = totalWidth - pad - glyphSize;
            closeButton.SetBounds(right, glyphTop, glyphSize, glyphSize);
            right -= glyphSize + glyphGap;
            hideButton.SetBounds(right, glyphTop, glyphSize, glyphSize);
            right -= glyphSize + glyphGap;
            pinButton.SetBounds(right, glyphTop, glyphSize, glyphSize);

            ApplyRoundedCorners();
        }
        finally
        {
            _layoutRunning = false;
        }
    }

    private static int MeasureFlow(FlowLayoutPanel flow, int height)
    {
        var width = 0;
        foreach (Control control in flow.Controls)
        {
            if (!control.Visible) continue;
            if (control.Height != height) control.Height = height;
            width += control.Width + control.Margin.Horizontal;
        }

        return width;
    }

    private void ApplyRoundedCorners()
    {
        if (!OperatingSystem.IsWindowsVersionAtLeast(10, 0, 22000))
        {
            ApplyRoundedRegion();
            return;
        }

        var preference = DwmWindowCornerRound;
        try
        {
            var result = DwmSetWindowAttribute(Handle, DwmWindowCornerPreference, ref preference, sizeof(int));
            if (result != 0) ApplyRoundedRegion();
        }
        catch (Exception)
        {
            ApplyRoundedRegion();
        }
    }

    private void ApplyRoundedRegion()
    {
        using var path = Theme.Rounded(new Rectangle(0, 0, Width, Height), S(10));
        Region = new Region(path);
    }

    private ToolbarItemButton CreateToolbarButton(string caption, Image? icon, bool isGroup)
    {
        var button = new ToolbarItemButton
        {
            Caption = caption,
            Icon = icon,
            IsGroup = isGroup,
            ShowChevron = isGroup,
            ShowLabel = _settings.ShowLabels,
            Margin = new Padding(0, 0, S(4), 0),
            AllowDrop = true
        };

        button.UpdateSize(S(40));
        return button;
    }

    private static Image? FirstIcon(LauncherGroup group)
    {
        foreach (var item in group.Items)
        {
            var icon = IconLoader.Get(item);
            if (icon is not null) return icon;
        }

        return null;
    }

    // ---------------------------------------------------------------- dragging items

    private void HookItemDrag(
        ToolbarItemButton button,
        FlowLayoutPanel flow,
        List<ToolbarItemButton> list)
    {
        button.MouseDown += (_, e) =>
        {
            if (e.Button != MouseButtons.Left) return;
            _dragSource = button;
            _dragFlow = flow;
            _dragList = list;
            _dragStart = Cursor.Position;

            // Without capture the pill stops receiving MouseMove/MouseUp as soon as the
            // pointer leaves it, which left the bar drag and the click guard stuck.
            button.Capture = true;
        };

        button.MouseMove += (_, e) =>
        {
            if (_dragSource != button || _dragList is null || _dragFlow is null) return;
            if (e.Button != MouseButtons.Left) return;

            var delta = new Size(Cursor.Position.X - _dragStart.X, Cursor.Position.Y - _dragStart.Y);
            if (!_dragActive && (Math.Abs(delta.Width) > S(4) || Math.Abs(delta.Height) > S(4)))
            {
                _dragActive = true;
                button.Cursor = Cursors.SizeAll;
            }

            if (_dragActive) UpdateDropIndicator(_dragFlow, _dragList);
        };

        button.MouseUp += (_, _) =>
        {
            if (_dragSource != button) return;
            if (_dragActive && _dragList is not null) CompleteItemDrag(_dragList, button);
            ResetItemDrag();
        };

        button.MouseCaptureChanged += (_, _) =>
        {
            if (_dragSource == button && !button.Capture) ResetItemDrag();
        };
    }

    private void UpdateDropIndicator(FlowLayoutPanel flow, List<ToolbarItemButton> list)
    {
        if (list.Count == 0) return;

        var point = flow.PointToClient(Cursor.Position);
        var index = list.Count;
        for (var i = 0; i < list.Count; i++)
        {
            var candidate = list[i];
            if (point.X < candidate.Left + candidate.Width / 2)
            {
                index = i;
                break;
            }
        }

        if (index == _dropIndex) return;
        _dropIndex = index;

        foreach (var candidate in list)
        {
            candidate.InsertSide = InsertSide.None;
        }

        if (index >= list.Count)
        {
            list[^1].InsertSide = InsertSide.After;
        }
        else
        {
            list[index].InsertSide = InsertSide.Before;
        }

        foreach (var candidate in list) candidate.Invalidate();
    }

    private void CompleteItemDrag(List<ToolbarItemButton> list, ToolbarItemButton source)
    {
        var from = list.IndexOf(source);
        var to = _dropIndex;
        if (from < 0 || to < 0) return;

        if (to > from) to--;
        to = Math.Clamp(to, 0, list.Count - 1);
        if (from == to) return;

        if (ReferenceEquals(list, _groupButtons))
        {
            var group = _settings.Groups[from];
            _settings.Groups.RemoveAt(from);
            _settings.Groups.Insert(to, group);
        }
        else if (from < _settings.PinnedItems.Count && to < _settings.PinnedItems.Count)
        {
            var pinned = _settings.PinnedItems[from];
            _settings.PinnedItems.RemoveAt(from);
            _settings.PinnedItems.Insert(to, pinned);
        }

        SaveSettings();
        BuildToolbar();
        LayoutToolbar();
    }

    private void ResetItemDrag()
    {
        if (_dragList is not null)
        {
            foreach (var button in _dragList)
            {
                button.InsertSide = InsertSide.None;
                button.Cursor = Cursors.Hand;
                button.Invalidate();
            }
        }

        var source = _dragSource;
        _dragSource = null;
        _dragFlow = null;
        _dragList = null;
        _dragActive = false;
        _dropIndex = -1;

        // Only take the capture back if the pill still owns it: a drop-down opened by the
        // click already holds it, and releasing it there would close the menu again.
        if (source is not null && source.Capture)
        {
            source.Capture = false;
        }
    }

    // ---------------------------------------------------------------- window dragging

    private void BeginWindowDrag(object? sender, MouseEventArgs e)
    {
        if (e.Button != MouseButtons.Left) return;

        _windowDragging = true;
        _windowDragCursor = Cursor.Position;
        _windowDragOrigin = Location;

        if (sender is Control control) control.Capture = true;
    }

    private void ContinueWindowDrag(object? sender, MouseEventArgs e)
    {
        if (!_windowDragging || e.Button != MouseButtons.Left) return;

        var delta = new Point(
            Cursor.Position.X - _windowDragCursor.X,
            Cursor.Position.Y - _windowDragCursor.Y);

        Location = new Point(_windowDragOrigin.X + delta.X, _windowDragOrigin.Y + delta.Y);
    }

    private void EndWindowDrag(object? sender, MouseEventArgs e)
    {
        if (!_windowDragging) return;

        _windowDragging = false;
        if (sender is Control control) control.Capture = false;
        ClampToScreen();
        SaveSettings();
    }

    private void ClampToScreen()
    {
        var screen = Screen.FromControl(this) ?? Screen.PrimaryScreen;
        if (screen is null) return;

        var area = screen.WorkingArea;
        var x = Math.Max(area.Left, Math.Min(Left, area.Right - Width));
        var y = Math.Max(area.Top, Math.Min(Top, area.Bottom - Height));
        Location = new Point(x, y);
    }

    private void RestorePosition()
    {
        if (_settings.LocationX is int x && _settings.LocationY is int y)
        {
            var target = new Point(x, y);
            if (Screen.AllScreens.Any(screen => screen.WorkingArea.Contains(target)))
            {
                Location = target;
                ClampToScreen();
                return;
            }
        }

        MoveToDefaultPosition();
    }

    private void MoveToDefaultPosition()
    {
        var area = Screen.PrimaryScreen?.WorkingArea ?? new Rectangle(0, 0, 1280, 720);
        Location = new Point(area.Left + (area.Width - Width) / 2, area.Top + S(8));
    }

    // ---------------------------------------------------------------- group menus

    private void ShowGroupMenu(ToolbarItemButton button, LauncherGroup group)
    {
        var menu = MenuFactory.CreateMenu();
        var entries = new List<ToolStripMenuItem>();
        var suppressLaunch = false;

        foreach (var item in group.Items)
        {
            var captured = item;
            var menuItem = MenuFactory.Item(item.Name, IconLoader.Get(item), null);
            menuItem.Tag = item;
            menuItem.ToolTipText = item.Target;
            menuItem.Click += (_, _) =>
            {
                if (suppressLaunch) return;
                Launch(captured);
            };
            menu.Items.Add(menuItem);
            entries.Add(menuItem);
        }

        HookMenuReorder(menu, group, entries, () => suppressLaunch = true);

        if (group.ShowFavorites && _settings.ShowFavoritesFlyout)
        {
            AddFavoritesSubmenu(menu);
        }

        menu.Items.Add(new ToolStripSeparator());
        menu.Items.Add(MenuFactory.Item("Add application\u2026", null, (_, _) => AddApplicationToGroup(group)));
        menu.Items.Add(MenuFactory.Item("Pin an application\u2026", null, (_, _) => PinApplication()));
        menu.Items.Add(MenuFactory.Item("Edit group\u2026", null, (_, _) => EditGroup(group)));
        menu.Items.Add(new ToolStripSeparator());
        menu.Items.Add(MenuFactory.Item("Rename group\u2026", null, (_, _) => RenameGroup(group)));
        menu.Items.Add(MenuFactory.Item("Remove group", null, (_, _) => RemoveGroup(group)));

        menu.Closed += (_, _) => menu.Dispose();

        MenuFactory.StyleSubMenus(menu);
        menu.Show(button, new Point(0, button.Height + S(4)));
    }

    // A second drop-down opened from inside an entry (right-click actions) made the framework
    // dispose the group menu while it was still unwinding the same message, so entry editing lives
    // in the group editor (Edit group...) instead of a nested menu.

    /// <summary>
    /// Drag an entry up or down inside the open drop-down. The hovered entry turns bold and
    /// the order is committed when the mouse is released.
    /// </summary>
    private void HookMenuReorder(
        ContextMenuStrip menu,
        LauncherGroup group,
        List<ToolStripMenuItem> entries,
        Action onDragStarted)
    {
        if (entries.Count == 0) return;

        var dragFrom = -1;
        var targetIndex = -1;
        var dragging = false;
        var dragStart = Point.Empty;

        void ClearTargets()
        {
            foreach (var entry in entries) entry.Font = Theme.Ui(9f);
        }

        int IndexUnderCursor()
        {
            var hit = menu.GetItemAt(menu.PointToClient(Cursor.Position)) as ToolStripMenuItem;
            return hit is null ? -1 : entries.IndexOf(hit);
        }

        // The drop-down itself only sees mouse events on its margins, so every entry gets its
        // own handlers; whichever entry receives the release commits the shared drag state.
        foreach (var entry in entries)
        {
            var index = entries.IndexOf(entry);

            entry.MouseDown += (_, e) =>
            {
                if (e.Button != MouseButtons.Left) return;

                dragFrom = index;
                targetIndex = index;
                dragStart = Cursor.Position;
            };

            entry.MouseMove += (_, e) =>
            {
                if (dragFrom < 0 || e.Button != MouseButtons.Left) return;
                if (!dragging && Math.Abs(Cursor.Position.Y - dragStart.Y) < S(5)) return;

                if (!dragging)
                {
                    dragging = true;
                    onDragStarted();
                    menu.AutoClose = false;
                }

                var target = IndexUnderCursor();
                if (target < 0 || target == targetIndex) return;

                targetIndex = target;
                ClearTargets();
                entries[target].Font = Theme.Ui(9f, FontStyle.Bold);
            };

            entry.MouseUp += (_, _) =>
            {
                if (dragFrom < 0) return;

                var from = dragFrom;
                var to = targetIndex;
                var moved = dragging;

                dragFrom = -1;
                targetIndex = -1;
                dragging = false;
                ClearTargets();
                menu.AutoClose = true;

                if (!moved || from == to || from < 0 || to < 0 ||
                    from >= group.Items.Count || to >= group.Items.Count)
                {
                    menu.Close();
                    return;
                }

                var item = group.Items[from];
                group.Items.RemoveAt(from);
                group.Items.Insert(to, item);

                SaveSettings();
                menu.Close();
                BuildToolbar();
                LayoutToolbar();
            };
        }
    }

    private void AddFavoritesSubmenu(ContextMenuStrip menu)
    {
        var favorites = FavoritesProvider.GetFavorites(25);
        if (favorites.Count == 0) return;

        menu.Items.Add(new ToolStripSeparator());
        var root = MenuFactory.Item("Favorites", null, null);
        foreach (var favorite in favorites)
        {
            var captured = favorite;
            var item = MenuFactory.Item(
                captured.Name,
                null,
                (_, _) => Launch(new LauncherItem { Name = captured.Name, Target = captured.Url }));
            item.ToolTipText = captured.Url;
            root.DropDownItems.Add(item);
        }

        menu.Items.Add(root);
        MenuFactory.StyleDropDown(root);
    }

    private ContextMenuStrip BuildGroupContextMenu(LauncherGroup group)
    {
        var menu = MenuFactory.CreateMenu();
        menu.Items.Add(MenuFactory.Item("Add application\u2026", null, (_, _) => AddApplicationToGroup(group)));
        menu.Items.Add(MenuFactory.Item("Edit group\u2026", null, (_, _) => EditGroup(group)));
        menu.Items.Add(MenuFactory.Item("Rename group\u2026", null, (_, _) => RenameGroup(group)));
        menu.Items.Add(new ToolStripSeparator());
        menu.Items.Add(MenuFactory.Item("Move left", null, (_, _) => MoveGroup(group, -1)));
        menu.Items.Add(MenuFactory.Item("Move right", null, (_, _) => MoveGroup(group, 1)));
        menu.Items.Add(new ToolStripSeparator());
        menu.Items.Add(MenuFactory.Item("Remove group", null, (_, _) => RemoveGroup(group)));
        return menu;
    }

    private ContextMenuStrip BuildPinnedContextMenu(LauncherItem item)
    {
        var menu = MenuFactory.CreateMenu();
        menu.Items.Add(MenuFactory.Item("Open", null, (_, _) => Launch(item)));
        menu.Items.Add(new ToolStripSeparator());
        menu.Items.Add(MenuFactory.Item("Rename\u2026", null, (_, _) => RenameItem(item)));
        menu.Items.Add(MenuFactory.Item("Open file location", null, (_, _) => AppLauncher.TryReveal(item)));
        menu.Items.Add(new ToolStripSeparator());
        menu.Items.Add(MenuFactory.Item("Remove from toolbar", null, (_, _) => UnpinItem(item)));
        return menu;
    }

    private void ToolbarMenu_Opening(object? sender, CancelEventArgs e)
    {
        if (_toolbarMenu is null) return;

        _toolbarMenu.Items.Clear();
        _toolbarMenu.Items.Add(MenuFactory.Item("New group\u2026", null, (_, _) => CreateGroup()));
        _toolbarMenu.Items.Add(MenuFactory.Item("Add application\u2026", null, (_, _) =>
        {
            if (_settings.Groups.Count > 0) AddApplicationToGroup(_settings.Groups[0]);
            else CreateGroup();
        }));
        _toolbarMenu.Items.Add(MenuFactory.Item("Pin an application\u2026", null, (_, _) => PinApplication()));
        _toolbarMenu.Items.Add(new ToolStripSeparator());

        var alwaysOnTop = MenuFactory.Item("Always on top", null, (_, _) => SetAlwaysOnTop(!_settings.AlwaysOnTop));
        alwaysOnTop.Checked = _settings.AlwaysOnTop;
        _toolbarMenu.Items.Add(alwaysOnTop);

        var showLabels = MenuFactory.Item("Show button labels", null, (_, _) => ToggleLabels());
        showLabels.Checked = _settings.ShowLabels;
        _toolbarMenu.Items.Add(showLabels);

        var showTaskbar = MenuFactory.Item("Show in taskbar", null, (_, _) => ToggleTaskbar());
        showTaskbar.Checked = _settings.ShowInTaskbar;
        _toolbarMenu.Items.Add(showTaskbar);

        _toolbarMenu.Items.Add(new ToolStripSeparator());
        _toolbarMenu.Items.Add(MenuFactory.Item("Options\u2026", null, (_, _) => ShowOptions()));
        _toolbarMenu.Items.Add(MenuFactory.Item("Reset position", null, (_, _) =>
        {
            MoveToDefaultPosition();
            SaveSettings();
        }));
        _toolbarMenu.Items.Add(new ToolStripSeparator());
        _toolbarMenu.Items.Add(MenuFactory.Item("Hide toolbar", null, (_, _) => HideToolbar()));
        _toolbarMenu.Items.Add(MenuFactory.Item("Exit", null, (_, _) => CloseToolbar()));

        MenuFactory.StyleSubMenus(_toolbarMenu);
    }

    private void TrayMenu_Opening(object? sender, CancelEventArgs e)
    {
        if (_trayMenu is null) return;

        _trayMenu.Items.Clear();
        _trayMenu.Items.Add(MenuFactory.Item("Show toolbar", null, (_, _) => ShowToolbar()));

        var alwaysOnTop = MenuFactory.Item("Always on top", null, (_, _) => SetAlwaysOnTop(!_settings.AlwaysOnTop));
        alwaysOnTop.Checked = _settings.AlwaysOnTop;
        _trayMenu.Items.Add(alwaysOnTop);

        _trayMenu.Items.Add(new ToolStripSeparator());
        _trayMenu.Items.Add(MenuFactory.Item("Options\u2026", null, (_, _) => ShowOptions()));
        _trayMenu.Items.Add(new ToolStripSeparator());
        _trayMenu.Items.Add(MenuFactory.Item("Exit", null, (_, _) => CloseToolbar()));

        MenuFactory.StyleSubMenus(_trayMenu);
    }

    // ---------------------------------------------------------------- editing

    private void CreateGroup()
    {
        var name = TextPromptForm.Show(this, "New group", "Group name:", "New group");
        if (string.IsNullOrWhiteSpace(name)) return;

        _settings.Groups.Add(new LauncherGroup { Name = name.Trim() });
        SaveSettings();
        BuildToolbar();
        LayoutToolbar();
    }

    private void RenameGroup(LauncherGroup group)
    {
        var name = TextPromptForm.Show(this, "Rename group", "Group name:", group.Name);
        if (string.IsNullOrWhiteSpace(name)) return;

        group.Name = name.Trim();
        SaveSettings();
        BuildToolbar();
        LayoutToolbar();
    }

    private void RemoveGroup(LauncherGroup group)
    {
        var answer = MessageBox.Show(
            this,
            $"Remove the group \u201c{group.Name}\u201d and its {group.Items.Count} entr{(group.Items.Count == 1 ? "y" : "ies")}?",
            "Floating Toolbar",
            MessageBoxButtons.YesNo,
            MessageBoxIcon.Question);

        if (answer != DialogResult.Yes) return;

        _settings.Groups.Remove(group);
        SaveSettings();
        BuildToolbar();
        LayoutToolbar();
    }

    private void MoveGroup(LauncherGroup group, int offset)
    {
        var index = _settings.Groups.IndexOf(group);
        if (index < 0) return;

        var target = index + offset;
        if (target < 0 || target >= _settings.Groups.Count) return;

        _settings.Groups.RemoveAt(index);
        _settings.Groups.Insert(target, group);
        SaveSettings();
        BuildToolbar();
        LayoutToolbar();
    }

    private void AddApplicationToGroup(LauncherGroup group)
    {
        using var dialog = new OpenFileDialog
        {
            Title = $"Add an application to \u201c{group.Name}\u201d",
            Filter = "Applications and shortcuts (*.exe;*.lnk;*.url;*.bat;*.cmd)|*.exe;*.lnk;*.url;*.bat;*.cmd|All files (*.*)|*.*",
            CheckFileExists = true,
            Multiselect = true
        };

        if (dialog.ShowDialog(this) != DialogResult.OK) return;
        AddFilesToGroup(group, dialog.FileNames);
    }

    private void PinApplication()
    {
        using var dialog = new OpenFileDialog
        {
            Title = "Pin an application to the toolbar",
            Filter = "Applications and shortcuts (*.exe;*.lnk;*.url;*.bat;*.cmd)|*.exe;*.lnk;*.url;*.bat;*.cmd|All files (*.*)|*.*",
            CheckFileExists = true,
            Multiselect = true
        };

        if (dialog.ShowDialog(this) != DialogResult.OK) return;

        foreach (var file in dialog.FileNames)
        {
            _settings.PinnedItems.Add(new LauncherItem
            {
                Name = Path.GetFileNameWithoutExtension(file),
                Target = file,
                IconPath = file
            });
        }

        SaveSettings();
        BuildToolbar();
        LayoutToolbar();
    }

    private void AddFilesToGroup(LauncherGroup group, IEnumerable<string> paths)
    {
        var added = 0;
        foreach (var path in paths)
        {
            if (string.IsNullOrWhiteSpace(path) || !File.Exists(path)) continue;

            group.Items.Add(new LauncherItem
            {
                Name = Path.GetFileNameWithoutExtension(path),
                Target = path,
                IconPath = path
            });
            added++;
        }

        if (added == 0) return;

        SaveSettings();
        BuildToolbar();
        LayoutToolbar();
    }

    private void AddPinnedFiles(IEnumerable<string> paths)
    {
        var added = 0;
        foreach (var path in paths)
        {
            if (string.IsNullOrWhiteSpace(path) || !File.Exists(path)) continue;

            _settings.PinnedItems.Add(new LauncherItem
            {
                Name = Path.GetFileNameWithoutExtension(path),
                Target = path,
                IconPath = path
            });
            added++;
        }

        if (added == 0) return;

        SaveSettings();
        BuildToolbar();
        LayoutToolbar();
    }

    private void OnFilesDroppedOnAddButton(object? sender, DragEventArgs e)
    {
        if (e.Data?.GetData(DataFormats.FileDrop) is not string[] files) return;

        var valid = files.Where(File.Exists).ToArray();
        if (valid.Length == 0) return;

        var group = new LauncherGroup { Name = Path.GetFileNameWithoutExtension(valid[0]) };
        foreach (var file in valid)
        {
            group.Items.Add(new LauncherItem
            {
                Name = Path.GetFileNameWithoutExtension(file),
                Target = file,
                IconPath = file
            });
        }

        _settings.Groups.Add(group);
        SaveSettings();
        BuildToolbar();
        LayoutToolbar();
    }

    private void RenameItem(LauncherItem item)
    {
        var name = TextPromptForm.Show(this, "Rename button", "Button name:", item.Name);
        if (string.IsNullOrWhiteSpace(name)) return;

        item.Name = name.Trim();
        SaveSettings();
        BuildToolbar();
        LayoutToolbar();
    }

    private void RemoveItemFromGroup(LauncherGroup group, LauncherItem item)
    {
        group.Items.Remove(item);
        SaveSettings();
        BuildToolbar();
        LayoutToolbar();
    }

    private void UnpinItem(LauncherItem item)
    {
        _settings.PinnedItems.Remove(item);
        SaveSettings();
        BuildToolbar();
        LayoutToolbar();
    }

    private void EditGroup(LauncherGroup group)
    {
        using var editor = new GroupEditorForm(group);
        editor.ShowDialog(this);

        SaveSettings();
        BuildToolbar();
        LayoutToolbar();
    }

    private void ShowOptions()
    {
        using var options = new OptionsForm(_settings);
        if (options.ShowDialog(this) != DialogResult.OK) return;

        StartupManager.Apply(_settings.StartWithWindows);
        _settings.StartWithWindows = StartupManager.IsEnabled();
        SaveSettings();
        ApplySettings();
        BuildToolbar();
        LayoutToolbar();

        UpdateHotkey();
        if (_settings.ShowHideHotkey && !_hotkeyRegistered)
        {
            MessageBox.Show(
                this,
                "Ctrl+Alt+Space is already reserved by another application, so the global hotkey could not be registered.",
                "Floating Toolbar",
                MessageBoxButtons.OK,
                MessageBoxIcon.Warning);
        }

        if (!_settings.AutoHideFullscreen && _hiddenByFullscreen)
        {
            _hiddenByFullscreen = false;
            ShowToolbar(activate: false);
        }

        if (options.ResetPositionRequested)
        {
            MoveToDefaultPosition();
            SaveSettings();
        }
    }

    // ---------------------------------------------------------------- behaviour toggles

    private void SetAlwaysOnTop(bool enabled)
    {
        _settings.AlwaysOnTop = enabled;
        ApplySettings();
        SaveSettings();
    }

    private void ToggleLabels()
    {
        _settings.ShowLabels = !_settings.ShowLabels;
        SaveSettings();
        BuildToolbar();
        LayoutToolbar();
    }

    private void ToggleTaskbar()
    {
        _settings.ShowInTaskbar = !_settings.ShowInTaskbar;
        ApplySettings();
        SaveSettings();
    }

    public void ShowToolbar()
    {
        _autoHideSuspended = true;
        ShowToolbar(activate: true);
    }

    private void ShowToolbar(bool activate)
    {
        if (!Visible) Show();

        if (WindowState == FormWindowState.Minimized) WindowState = FormWindowState.Normal;

        TopMost = false;
        TopMost = _settings.AlwaysOnTop;
        ClampToScreen();
        BringToFront();

        if (activate) Activate();
    }

    private void HideToolbar(bool automatic = false)
    {
        if (!Visible) return;

        SaveSettings();
        Hide();

        if (!automatic) _hiddenByFullscreen = false;

        if (!automatic && _notifyIcon is not null && !_balloonShown)
        {
            _balloonShown = true;
            _notifyIcon.ShowBalloonTip(
                3000,
                "Floating Toolbar",
                "The toolbar is still running. Double-click the notification area icon to bring it back.",
                ToolTipIcon.Info);
        }
    }

    private void CloseToolbar()
    {
        SaveSettings();
        Close();
    }

    // ---------------------------------------------------------------- launching

    private void Launch(LauncherItem item)
    {
        var result = AppLauncher.Launch(item);
        if (result.Success) return;

        MessageBox.Show(
            this,
            result.Error ?? $"Windows could not start \u201c{item.Name}\u201d.",
            "Floating Toolbar",
            MessageBoxButtons.OK,
            MessageBoxIcon.Warning);
    }

    // ---------------------------------------------------------------- drag & drop from Explorer

    private void OnFileDragEnter(object? sender, DragEventArgs e)
    {
        e.Effect = e.Data?.GetDataPresent(DataFormats.FileDrop) == true
            ? DragDropEffects.Copy
            : DragDropEffects.None;
    }

    private void OnFileDragDrop(object? sender, DragEventArgs e)
    {
        if (e.Data?.GetData(DataFormats.FileDrop) is string[] files)
        {
            AddPinnedFiles(files);
        }
    }

    // ---------------------------------------------------------------- persistence

    private void SaveSettings()
    {
        _settings.LocationX = Location.X;
        _settings.LocationY = Location.Y;
        SettingsStore.Save(_settings);
    }
}
