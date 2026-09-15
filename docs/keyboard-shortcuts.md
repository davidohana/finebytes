# Keyboard shortcuts

Keys below use **Ctrl** on Windows and Linux. macOS Command equivalents are not wired yet.

Gestures bound in the UI live in `Mfr.App.Ui/Input/AppShortcuts.cs`. Menu items show the same shortcuts on the right; toolbar and address-bar tooltip copy lives in `Mfr.App.Ui/Resources/AppTips.cs`.

## Working now

### Global

| Action               | Shortcut        |
| -------------------- | --------------- |
| GO                   | Ctrl+G          |
| Undo last            | Ctrl+Z          |
| Rename Log           | Ctrl+Shift+L    |
| Options              | Ctrl+,          |
| Exit                 | Alt+F4          |
| Refresh focused pane | F5              |
| Go to address bar    | Ctrl+L or Alt+D |

Undo last (Ctrl+Z) prepares an undo session from the last GO when a rename log exists: it replaces the Rename List with reverse OldValues, clears Filter Chain, and leaves disk unchanged until you press GO. Rename Log (Ctrl+Shift+L) opens the dialog (disk history + last operation); Undo there uses the same prepare path. Options (Ctrl+,) is live. GO previews the current list, warns before ignoring preview errors, applies valid renames, and clears manual field overrides on rows that were applied or hit a commit error (preview-error and skipped rows keep theirs).

Ctrl+L is the address bar (Explorer / Chrome). Rename Log is Ctrl+Shift+L so the two do not clash. Alt+F4 is shown on **MFR → Exit** and is handled by the window manager on Windows, not as an extra app binding.

### File List

| Action                | Shortcut                                           | Where it works                         |
| --------------------- | -------------------------------------------------- | -------------------------------------- |
| Go to address bar     | Ctrl+L, Alt+D                                      | Main window                            |
| Up (parent folder)    | Backspace                                          | File listing focused, not while typing |
| Refresh               | F5                                                 | Main window                            |
| Large Icons           | Ctrl+1                                             | Main window                            |
| Small Icons           | Ctrl+2                                             | Main window                            |
| Report                | Ctrl+3                                             | Main window                            |
| List                  | Ctrl+4                                             | Main window                            |
| Tiles                 | Ctrl+5                                             | Main window                            |
| Thumbnails            | Ctrl+6                                             | Main window                            |
| Zoom thumbnails in    | Ctrl++, Ctrl+Shift+=, Ctrl+numpad +, Ctrl+wheel up | Thumbnails view, not while typing      |
| Zoom thumbnails out   | Ctrl+-, Ctrl+numpad -, Ctrl+wheel down             | Thumbnails view, not while typing      |
| Reset thumbnail size  | Ctrl+0, Ctrl+numpad 0                              | Thumbnails view, not while typing      |
| First item            | Home                                               | File listing focused, not while typing |
| Last item             | End                                                | File listing focused, not while typing |
| Properties            | Alt+Enter                                          | File listing focused                   |
| Cut                   | Ctrl+X                                             | File listing focused                   |
| Copy                  | Ctrl+C                                             | File listing focused                   |
| Paste                 | Ctrl+V                                             | File listing focused                   |
| Delete to Recycle Bin | Del                                                | File listing focused                   |
| Permanent delete      | Shift+Del                                          | File listing focused                   |
| Commit typed path     | Enter                                              | Address bar edit                       |
| Cancel typed path     | Esc                                                | Address bar edit                       |

Ctrl+1 through Ctrl+6 follow **File List → File List Type** menu order.

**File List → Go Up** runs from the menu anywhere; Backspace is only handled when the listing has focus so it does not steal from text boxes.

### Rename List

| Action                  | Shortcut     | Where it works            |
| ----------------------- | ------------ | ------------------------- |
| Add selected            | Ctrl+Shift+S | Main window               |
| Add all                 | Ctrl+Shift+A | Main window               |
| Remove selected         | Ctrl+Shift+R | Main window               |
| Remove all but selected | Ctrl+Shift+B | Main window               |
| Remove selected rows    | Del          | Rename List grid focused  |
| Clear                   | Ctrl+Shift+C | Main window               |
| Manual override         | F2           | Rename List grid focused  |
| Locate in File List     | F4           | Rename List grid focused  |
| Properties              | Alt+Enter    | Rename List grid focused  |
| Refresh                 | F5           | Rename List grid focused  |
| Move selected up        | Ctrl+↑       | Rename List grid focused  |
| Move selected down      | Ctrl+↓       | Rename List grid focused  |
| Toggle Before           | Ctrl+\[      | Main window               |
| Toggle After            | Ctrl+\]      | Main window               |
| First item              | Home         | Rename List grid focused  |
| Last item               | End          | Rename List grid focused  |
| Add/toggle sort level   | Shift+click  | Rename List column header |

Selecting or focusing a Rename List cell shows the full cell value in the status bar.

Shift+click a column header to append a sort key, toggle its direction, or remove it from the sort list. A plain header click replaces the sort list with that column only. Any **original** (non-preview) catalog column supports header sort; preview columns do not.

Click the **Select Fields** toolbar button on the Rename List, or choose **Select Fields...** from the **Rename List** menu, the grid right-click menu, or a column-header right-click menu, to open the field shuttle (visible columns and Auto-Sort tabs). **Select Sort Fields...** on the **Rename List** menu opens the same dialog on the Sort tab. Right-click a column header and choose **Hide Field** to hide that column (persisted layout keys only — After-side preview headers map to the same stored field). **Export Rename List (csv)...** on the **Rename List** menu (or **Export → Export Visible Columns (csv)** on a column header) writes a CSV of the on-screen columns (preview values while the After side is showing).

**Before/After Mode** is available on the field shuttle Columns tab and the **Rename List** menu. When on, the stored layout is originals-only; use the Rename List toolbar **Before/After** toggle (◀ / ▶), matching **Rename List** menu radios (**Toggle Before** / **Toggle After**, icons ◀ / ▶), or **Ctrl+\[** / **Ctrl+\]** to flip between original and preview values for the same fields. The side control is hidden when Before/After Mode is off. There is no dedicated shortcut for enabling Before/After Mode itself.

**Add columns from filters** / **Set columns from filters** (Rename List toolbar after **Select Fields**, Rename List menu, or **Add** / **Set** on the field shuttle Columns tab) infer columns from the full applied filter chain (write targets and format tokens). In the shuttle they update the draft Selected fields until you press OK. Toolbar and menu apply immediately to the live column layout. There is no dedicated keyboard shortcut in v1.

### Filter Chain

| Action             | Shortcut | Where it works            |
| ------------------ | -------- | ------------------------- |
| Remove selected    | Del      | Filter Chain list focused |
| Move selected up   | Ctrl+↑   | Filter Chain list focused |
| Move selected down | Ctrl+↓   | Filter Chain list focused |

**Filters → Add Selected Filter** appends the selected Available Filters row (same as Enter on the palette or the Filter Chain add shuttle). **Remove Selected Filter**, **Remove All Filters**, and **Move Filter Up/Down** are on the **Filters** menu and the Filter Chain shuttle column.

### Field shuttle (Select Fields dialog)

| Action     | Shortcut | Where it works       |
| ---------- | -------- | -------------------- |
| First item | Home     | Focused shuttle list |
| Last item  | End      | Focused shuttle list |

## Shown in menus, not implemented yet

These shortcuts match MFR 7.4 and are already labeled on disabled menu items.

| Action | Shortcut | Notes                        |
| ------ | -------- | ---------------------------- |
| Help   | F1       | Help UI is not in this build |

F5 reloads the File List from the main window and address bar, or the Rename List when its grid has focus (MFR 7.4 shared the same key in both panes). Rename List refresh re-reads original fields from disk (and clears manual field overrides), then re-runs preview when Auto-Preview is on.
