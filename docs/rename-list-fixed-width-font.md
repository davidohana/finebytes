# Rename List fixed-width font

Optional fixed-width font for the Rename List grid, persisted in `session.json` on the Rename List section.

## Goal

- **Default:** **fixed-width** (`Cascadia Mono, Consolas, monospace` at 12pt) for the **whole Rename List grid**.
- **Optional:** user can disable fixed-width to use proportional **Segoe UI** via existing `FileListFont`.
- **Immediate:** checkbox toggles in the Rename List context menu and **Rename List** main menu apply at once.
- **Persistent:** choice is stored in **`session.json`** (`renameList.useFixedWidthFont`) on close, with the rest of the Rename List section.

File List stays on `FileListFont`. The field shuttle dialog stays proportional (field picker, not the grid).

## Why session, not config

This is UI-only. The CLI does not read Rename List display prefs. `config.json` stays process-wide (`filters`, `log`).

Add-policy (`addMode`, `addFolderContents`) lives on the same `renameList` object. Window restore (`rememberWindowState`) is on `mainWindow`; last folder (`rememberLastFolder`) is on `fileList`.

## Persistence schema

```json
{
  "renameList": {
    "useFixedWidthFont": true,
    "addMode": "files"
  }
}
```

| Field               | CLR                 | Type | Default  |
| ------------------- | ------------------- | ---- | -------- |
| `useFixedWidthFont` | `UseFixedWidthFont` | bool | **true** |

Omitted keys use property initializer defaults. No legacy migration from `config.json` or a `ui` session object.

## Persistence

Font toggle updates `RenameListViewModel.UseFixedWidthFont`. The grid reacts immediately. `CaptureSession()` includes the flag; `UiSessionPersistence.SaveOnClose` writes it with sort, columns, and add-policy.

Toggle → view-model property → save-on-close `CaptureSession()`.

## UI

| Piece  | Detail                                                                      |
| ------ | --------------------------------------------------------------------------- |
| Entry  | Rename List context menu → **Use Fixed-Width Font** (checkbox)              |
| Entry  | Main menu **Rename List** → **Use Fixed-Width Font** (checkbox)             |
| Toggle | `ToggleUseFixedWidthFontCommand` → VM; save-on-close via `CaptureSession()` |

Same pattern as **Auto-Sort**: `ToggleType="CheckBox"`, `IsChecked` one-way from `UseFixedWidthFont`.

## Grid styling

Family names and sizes live in `GridFonts`. `App.Initialize` registers the `FileListFont`, `RenameListFixedWidthFont`, `FileListFontSize`, and `FileListSortGlyphFontSize` theme keys from that type so styles and column measurement share one source.

`RenameListView` toggles the `fixed-width-font` class on the DataGrid when `UseFixedWidthFont` changes. Parallel style selectors apply `RenameListFixedWidthFont` to the grid, headers, and cells. Sort and preview glyphs stay on `FileListFont` (same as header glyph-reserve measurement).

Column minimum widths are recomputed on toggle; user-resized widths above the new minimum are preserved.

## Out of scope

- Monospace on name columns only (whole grid first)
- Full Options dialog (Ctrl+, stub)
- Font family or size picker
- File List font change
