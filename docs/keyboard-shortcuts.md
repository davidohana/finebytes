# Keyboard shortcuts

Keys below use **Ctrl** on Windows and Linux. macOS Command equivalents are not wired yet.

Gestures bound in the UI live in `Mfr.App.Ui/Input/AppShortcuts.cs`. Menu items show the same shortcuts on the right; toolbar and address-bar tooltip copy lives in `Mfr.App.Ui/Resources/AppTips.cs`.

## Working now

### Global

| Action            | Shortcut        |
| ----------------- | --------------- |
| GO                | Ctrl+G          |
| Undo last         | Ctrl+Z          |
| Log               | Ctrl+Shift+L    |
| Options           | Ctrl+,          |
| Exit              | Alt+F4          |
| Refresh File List | F5              |
| Go to address bar | Ctrl+L or Alt+D |

GO, Undo, Log, and Options appear in the menu and toolbar now; the commands themselves are still stubs.

Ctrl+L is the address bar (Explorer / Chrome). Log is Ctrl+Shift+L so the two do not clash. Alt+F4 is shown on **MFR → Exit** and is handled by the window manager on Windows, not as an extra app binding.

### File List

| Action               | Shortcut                                           | Where it works                         |
| -------------------- | -------------------------------------------------- | -------------------------------------- |
| Go to address bar    | Ctrl+L, Alt+D                                      | Main window                            |
| Up (parent folder)   | Backspace                                          | File listing focused, not while typing |
| Refresh              | F5                                                 | Main window                            |
| Large Icons          | Ctrl+1                                             | Main window                            |
| Small Icons          | Ctrl+2                                             | Main window                            |
| Report               | Ctrl+3                                             | Main window                            |
| List                 | Ctrl+4                                             | Main window                            |
| Tiles                | Ctrl+5                                             | Main window                            |
| Thumbnails           | Ctrl+6                                             | Main window                            |
| Zoom thumbnails in   | Ctrl++, Ctrl+Shift+=, Ctrl+numpad +, Ctrl+wheel up | Thumbnails view, not while typing      |
| Zoom thumbnails out  | Ctrl+-, Ctrl+numpad -, Ctrl+wheel down             | Thumbnails view, not while typing      |
| Reset thumbnail size | Ctrl+0, Ctrl+numpad 0                              | Thumbnails view, not while typing      |
| Commit typed path    | Enter                                              | Address bar edit                       |
| Cancel typed path    | Esc                                                | Address bar edit                       |

Ctrl+1 through Ctrl+6 follow **File List → File List Type** menu order.

**File List → Go Up** runs from the menu anywhere; Backspace is only handled when the listing has focus so it does not steal from text boxes.

### Rename List

| Action                  | Shortcut     | Where it works           |
| ----------------------- | ------------ | ------------------------ |
| Add selected            | Ctrl+Shift+S | Main window              |
| Add all                 | Ctrl+Shift+A | Main window              |
| Remove selected         | Ctrl+Shift+R | Main window              |
| Remove all but selected | Ctrl+Shift+B | Main window              |
| Remove selected rows    | Del          | Rename List grid focused |
| Clear                   | Ctrl+Shift+C | Main window              |
| Locate in File List     | F4           | Rename List grid focused |
| Move selected up        | Ctrl+↑       | Rename List grid focused |
| Move selected down      | Ctrl+↓       | Rename List grid focused |

Selecting or focusing a Rename List cell shows the full cell value in the status bar.

## Shown in menus, not implemented yet

These shortcuts match MFR 7.4 and are already labeled on disabled menu items.

| Action              | Shortcut |
| ------------------- | -------- |
| Rename List refresh | F5       |

Rename List refresh will share F5 with the File List later (MFR 7.4 used F5 in both). Until then, F5 only reloads the File List.

## Later (not on menus yet)

| Action        | Shortcut | Notes                        |
| ------------- | -------- | ---------------------------- |
| Help          | F1       | Help UI is not in this build |
| Manual rename | F2       | Rename List                  |
