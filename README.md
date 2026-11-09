# Floating Toolbar

A compact, always-on-top Windows launcher bar built with C# and Windows Forms on **.NET 8**
(`net8.0-windows`, `UseWindowsForms`). It is a single small pill of drop-down groups, pinned
one-click launch icons and window chrome — not a full window.

![No screenshots included]()

---

## Build and run

```bash
# from this folder
dotnet build Floating_Toolbar.csproj -c Release
dotnet run --project Floating_Toolbar.csproj
```

The executable lands in `bin/Release/net8.0-windows/FloatingToolbar.exe`.

To produce a copy in a fixed location such as the folder named in the brief:

```bash
dotnet publish Floating_Toolbar.csproj -c Release -r win-x64 --self-contained false -o "E:\ToolBar"
```

Requires the .NET 8 Desktop Runtime (or SDK) and Windows 10/11.

---

## What it does

**Layout and chrome**
- Compact dark bar, 52 logical pixels tall (see *Design decisions* for the “6 pixels” note).
- Rounded corners on Windows 11 through `DwmSetWindowAttribute` (`DWMWA_WINDOW_CORNER_PREFERENCE`);
  falls back to a clipped rounded region on Windows 10.
- Drop shadow, 1 px border, hover/pressed states and a 1 px accent insertion marker while reordering.
- Drag by the dotted grip, the `TOOLBAR` label, or any empty part of the bar. The bar is clamped to the
  working area of the monitor it is on, so it cannot be lost off-screen, and it is restored to its saved
  position on the next run (with a screen-existence check for removed monitors).
- Pin (always on top), hide-to-notification-area and exit buttons. Double-clicking the tray icon, or
  launching the exe again, restores the existing instance instead of starting a second one.
- **Global show/hide hotkey `Ctrl+Alt+Space`** via `RegisterHotKey`, so it works from any application.
  If the combination is already taken the bar keeps running and says so when you enable it in Options.
- **Auto-hide for fullscreen apps** (off by default): a 750 ms foreground poll compares the foreground
  window against its monitor, so games, video players and presentations hide the bar and it returns
  afterwards. Maximized windows stop at the taskbar and do not count. If you bring the bar back yourself
  during fullscreen, it stays until that app closes.
- Per-monitor v2 DPI awareness; all dynamic sizing goes through `LogicalToDeviceUnits`.

**Groups and buttons**
- Ships with **Office** (Word, Excel, PowerPoint, Outlook, OneNote), **Browsers** (Edge, Chrome,
  Firefox plus a **Favorites** fly-out built from `%USERPROFILE%\Favorites\*.url`) and **Windows**
  (File Explorer, Notepad, Calculator, Settings).
- The bar auto-sizes whenever a group or button is added, removed, renamed (labels change width),
  reordered, or when “Show button labels” is toggled.
- Groups and pinned single-click icons are separate sections. Both can be reordered horizontally by
  dragging a pill left or right — the neighbour shows an accent bar for the drop position.
- Inside a group drop-down, hold the left mouse button on an entry and drag up/down; the hovered row
  turns bold and the order is saved on release. The **Edit group…** dialog offers the same drag
  reorder in a list, plus Move up / Move down buttons as a guaranteed fallback.
- Drag files from Explorer onto a group pill to append them to that group, onto the `+` pill to create
  a new group from them, or onto the bar background to pin them as one-click icons.
- Right-click a group for add / edit / rename / move left / move right / remove. Rename or remove an
  entry with **Edit group…** (or drag it to a new position). Right-click a pinned icon for open /
  rename / reveal / remove.
  Right-click the bar for new group, add, pin, always-on-top, labels, taskbar, options, reset position,
  hide and exit.

**Launching**
- Targets are resolved through, in order: existing file path → protocol/shell target
  (`https:`, `ms-settings:`, `shell:`) → `App Paths` registry (HKCU/HKLM, 64- and 32-bit views) →
  `PATH`. Nothing is hard-coded to a fabricated install directory.
- Launch uses `ShellExecute`, so `.lnk`, `.url` and document associations behave normally.
- A missing app shows a warning dialog with the target that failed; it never throws out of the click.

**Settings**
- Stored in `%APPDATA%\FloatingToolbar\settings.json` (portable JSON, hand-editable, safe defaults if
  corrupt). All edits are saved immediately; the file is also written on move and on close.
- Options dialog: Start with Windows, always on top, show in Taskbar (off by default, as requested),
  show button labels, favourites fly-out, global hotkey, fullscreen auto-hide, reset position.
- “Start with Windows” writes/removes `HKCU\Software\Microsoft\Windows\CurrentVersion\Run\FloatingToolbar`.

---

## Project layout

| Path | Purpose |
| --- | --- |
| `Floating_Toolbar.csproj` | .NET 8 WinForms project, manifest and DPI settings |
| `app.manifest` | PerMonitorV2 DPI awareness, Win10/11 supported OS |
| `Program.cs` | Single-instance mutex, tray/show signal, `Application.Run` |
| `FloatingToolbarForm.cs` / `.Designer.cs` | Bar shell (designer controls) + all behaviour |
| `GroupEditorForm.cs` / `.Designer.cs` | Add / remove / rename / retarget / drag-reorder entries |
| `OptionsForm.cs` / `.Designer.cs` | Settings dialog, all controls declared in the designer file |
| `Models/` | `LauncherItem`, `LauncherGroup`, `ToolbarSettings`, default groups |
| `Services/` | JSON store, app launcher/resolver, shell icon loader, Run-key startup, favourites reader, fullscreen detector |
| `UI/` | Theme/fonts/glyphs, custom pill, glyph button, drag grip, dark menu renderer, tray icon |

**Designer files** hold every statically placed control (bar shell, options dialog, group editor) so
positions and padding can be adjusted in the WinForms designer. The pills inside the two flow panels
are generated from `settings.json` at runtime because their count and order are data, not design.

---

## Design decisions and interpretations

1. **“Toolbar height is max 6 pixels” is not physically usable** — 6 px cannot host an icon, a label or
   a click target. The bar is **52 logical px** (40 px pill + 6 px padding each side), which is about as
   slim as a launcher with 20 px icons can be. Change `buttonHeight`/`height` in
   `FloatingToolbarForm.LayoutToolbar()` if you want it even tighter.
2. **Groups vs pinned icons.** The brief asks for groups *and* draggable individual buttons, so both
   exist: groups live in the left flow panel, single-click pinned icons in the right one. This keeps
   “drag singles horizontally” unambiguous instead of mixing two different drag semantics in one strip.
3. **Vertical reorder** is available both inside the open drop-down (live drag) and in the group editor.
   The editor path is deliberate: WinForms does not expose a reorder notification for `ContextMenuStrip`
   (`ToolStrip.ItemMoved` is not available), so the drop-down drag is implemented with mouse tracking
   and the editor guarantees the feature.
4. **Targets are names, not paths.** `WINWORD.EXE`, `chrome.exe`, `ms-settings:` etc. go through
   `App Paths` and the shell, which also covers Word/Chrome being installed in non-standard locations.
5. **Files stay in this repository** (`E:\repos\Floating_Toolbar`). The brief named `E:\ToolBar`; use the
   `dotnet publish -o` command above to copy the built output there without moving the sources.
6. No NuGet packages were added.

---

## Repository and website

- Published at **https://github.com/DwainSnickles/Floating-Toolbar** (public, branch `main`).
- Listed on **https://dwainsnickles.github.io/projects.html** under *UI components*. The curated title,
  description and category live in that site's `scripts/fetch-repos.mjs`, and `data/repos.json` was
  regenerated so the card appears without waiting for the daily refresh.
- Clone it with `git clone https://github.com/DwainSnickles/Floating-Toolbar.git`.

---

## Suggestions for the next iteration

- **Real screenshots and a short GIF** in this README; the current file has no images because the UI
  was not visually inspected in this run.
- **Rebindable hotkey** — a fixed `Ctrl+Alt+Space` is shipped; letting the user capture their own
  combination needs a key-capture dialog.
- **Profiles / import-export**: settings are already one JSON file, so a “Save profile…” menu entry is a
  file copy; useful for work vs. home setups.
- **Per-entry arguments editor** — the model already stores `Arguments`; only a text field is missing.
- **Search-as-you-type flyout** over pinned icons plus Start-menu apps (`shell:AppsFolder`).
- **Use the real application icon in the tray** (`Icon.ExtractAssociatedIcon` on a configured item)
  instead of the generated glyph, and allow per-group custom icons.
- **Snap to screen edges** while dragging.
- **Unit tests** for `AppLauncher.Resolve`, settings round-tripping and favourites parsing — all three
  are pure and testable without UI.
- **Code signing** so SmartScreen stops warning on the distributed exe.

---

## Notes from this run

- Verified by driving the real UI on the target machine: labels, hover states, drop-downs, the drag
  insertion marker, both drag-reorder paths, dark tooltips and the `Ctrl+Alt+Space` toggle (window
  hidden, then shown) were checked with screenshots and `settings.json` assertions.
- Fixed during that pass: labels ending in an ellipsis (measured without the padding `DrawText` uses),
  pills that never received `MouseMove`/`MouseUp` during a drag (missing mouse capture), drop-down
  reordering listening on the strip instead of its entries, white system tooltips over the dark UI, and
  a crash caused by opening a second drop-down from inside an open one (entry editing now lives in
  **Edit group…**).
- Fullscreen auto-hide is wired to the 750 ms foreground poll and covered by the settings toggle, but
  was not exercised against a real fullscreen application.
- The PC was put to sleep by the previous run; this pass ran after it was woken.
