# Rename List fixed-width font

Optional fixed-width font for the Rename List grid, persisted in `config.json` as a user preference.

## Goal

- **Default:** proportional **Segoe UI** via existing `FileListFont` — no behavior change for existing users.
- **Optional:** user enables **fixed-width** (`Cascadia Mono, Consolas, monospace` at 12pt) for the **whole Rename List grid**.
- **Immediate:** checkbox toggles in the Rename List context menu and **Rename List** main menu apply and save instantly.
- **Persistent:** choice survives restarts via **`config.json`** (`UiConfig`), alongside `RememberWindowState` and add-policy prefs.

File List stays on `FileListFont`. The field shuttle dialog stays proportional (field picker, not the grid).

## Why config, not session

This is a **user preference** (like `RememberWindowState`, `AddMode`), not transient Rename List layout state. Session holds per-workspace grid state (sort keys, column order/widths); font choice should follow the user across sessions and machines (when config is shared).

`UiConfig` documents that Options will expose these settings later; this toggle is the first UI-driven write to config.

## Persistence schema

```json
{
  "ui": {
    "renameListUseFixedWidthFont": "false",
    "addMode": "files"
  }
}
```

| Field                         | CLR                           | Type         | Default   |
| ----------------------------- | ----------------------------- | ------------ | --------- |
| `renameListUseFixedWidthFont` | `RenameListUseFixedWidthFont` | `bool` field | **false** |

Leaf values are JSON **strings** (`"true"` / `"false"`). Omitted keys use field initializer defaults. No legacy migration.

CLI override: `ui.renameListUseFixedWidthFont=true`.

## ConfigStore.Save

`ConfigStore.Save()` merge-writes the in-memory `ConfigStore.Config` to `config.json`:

- Default path: `AppDataPaths.RoamingRoot()/config.json`.
- Preserves unrelated sections and keys (`filters`, `log`, custom `ui` keys).
- Serializes via `ConfigJsonWriter` (mirrors `ConfigJsonApplier` field walk).
- Save failures are swallowed (same spirit as session save).

Toggle → update `ConfigStore.Config.Ui.RenameListUseFixedWidthFont` → `ConfigStore.Save()`.

## UI

| Piece  | Detail                                                                |
| ------ | --------------------------------------------------------------------- |
| Entry  | Rename List context menu → **Use Fixed-Width Font** (checkbox)        |
| Entry  | Main menu **Rename List** → **Use Fixed-Width Font** (checkbox)       |
| Toggle | `ToggleUseFixedWidthFontCommand` → VM + config + `ConfigStore.Save()` |

Same pattern as **Auto-Sort**: `ToggleType="CheckBox"`, `IsChecked` one-way from `UseFixedWidthFont`.

## Grid styling

`RenameListView` toggles the `fixed-width-font` class on the DataGrid when `UseFixedWidthFont` changes. Parallel style selectors apply `RenameListFixedWidthFont` to the grid, headers, cells, and glyph TextBlocks.

Column minimum widths are recomputed on toggle; user-resized widths above the new minimum are preserved.

## Out of scope

- Monospace on name columns only (whole grid first)
- Full Options dialog (Ctrl+, stub)
- Font family or size picker
- File List font change
