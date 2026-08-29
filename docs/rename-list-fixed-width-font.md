# Rename List fixed-width font

Optional fixed-width font for the Rename List grid, persisted in `session.json` as a UI preference.

## Goal

- **Default:** proportional **Segoe UI** via existing `FileListFont` — no behavior change for existing users.
- **Optional:** user enables **fixed-width** (`Cascadia Mono, Consolas, monospace` at 12pt) for the **whole Rename List grid**.
- **Immediate:** checkbox toggles in the Rename List context menu and **Rename List** main menu apply and save instantly.
- **Persistent:** choice survives restarts via **`session.json`** (`SessionStateUi`), alongside add-policy prefs and restore toggles.

File List stays on `FileListFont`. The field shuttle dialog stays proportional (field picker, not the grid).

## Why session, not config

The `ui` object is UI-only. The CLI has its own `--files` / `--folders` / recurse switches and does not read add-policy, remember flags, or this font. `config.json` stays process-wide (`filters`, `log`).

Last-used layout (window, columns, sort) lives in the same `session.json` document under other keys.

## Persistence schema

```json
{
  "ui": {
    "renameListUseFixedWidthFont": false,
    "addMode": "files"
  }
}
```

| Field                         | CLR                           | Type | Default   |
| ----------------------------- | ----------------------------- | ---- | --------- |
| `renameListUseFixedWidthFont` | `RenameListUseFixedWidthFont` | bool | **false** |

Omitted keys use property initializer defaults. No legacy migration from `config.json`.

## SessionStore.SaveCurrentUi

`SessionStore.SaveCurrentUi()` writes `SessionStore.Current.Ui` into `session.json` and keeps other sections already on disk (`mainWindow`, `fileList`, `renameList`). Save failures are swallowed.

Toggle → update `SessionStore.Current.Ui.RenameListUseFixedWidthFont` → `SessionStore.SaveCurrentUi()`.

## UI

| Piece  | Detail                                                                           |
| ------ | -------------------------------------------------------------------------------- |
| Entry  | Rename List context menu → **Use Fixed-Width Font** (checkbox)                   |
| Entry  | Main menu **Rename List** → **Use Fixed-Width Font** (checkbox)                  |
| Toggle | `ToggleUseFixedWidthFontCommand` → VM + session + `SessionStore.SaveCurrentUi()` |

Same pattern as **Auto-Sort**: `ToggleType="CheckBox"`, `IsChecked` one-way from `UseFixedWidthFont`.

## Grid styling

`RenameListView` toggles the `fixed-width-font` class on the DataGrid when `UseFixedWidthFont` changes. Parallel style selectors apply `RenameListFixedWidthFont` to the grid, headers, cells, and glyph TextBlocks.

Column minimum widths are recomputed on toggle; user-resized widths above the new minimum are preserved.

## Out of scope

- Monospace on name columns only (whole grid first)
- Full Options dialog (Ctrl+, stub)
- Font family or size picker
- File List font change
