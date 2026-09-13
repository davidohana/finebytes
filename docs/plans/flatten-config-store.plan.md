---
name: Flatten config store
overview: Remove `MfrConfig` / `SessionState` wrappers; expose log/ui/session sections as siblings on `ConfigStore`; flatten `config.json` the same way; move double-click pref onto File List.
todos:
  - id: store-flatten
    content: Flatten ConfigStore API + JSON load/save; private PrefsRoot; delete MfrConfig/SessionState wrappers; DoubleClick on FileList
    status: completed
  - id: ui-wire
    content: Retarget Options, UiSessionPersistence, App/MainWindow, FileList double-click, ConfirmationPolicy, CLI log reads
    status: completed
  - id: tests-docs
    content: Update tests for root keys + new props; AGENTS/README/remarks; save docs/plans/flatten-config-store.plan.md
    status: completed
isProject: false
---

# Flatten ConfigStore + config.json

## Decisions (locked)

- **C# API:** Drop `MfrConfig` and `SessionState`. `ConfigStore` exposes section props directly:

```csharp
ConfigStore.Log                 // LogConfig
ConfigStore.Ui                  // UiConfig (ConfirmationPrompts only)
ConfigStore.MainWindow          // MainWindowPrefs?
ConfigStore.FileList            // FileListPrefs? (+ DoubleClickAddsToRenameList)
ConfigStore.RenameList          // RenameListPrefs?
ConfigStore.FilterEditor        // FilterEditorPrefs?
ConfigStore.FilterDefaultsJson  // JsonObject (unchanged)
```

- **JSON shape** (one current schema; no migration — nested `"session"` soft-loads as missing → defaults):

```json
{
  "log": { "...": "string leaves" },
  "ui": { "confirmationPrompts": "..." },
  "mainWindow": { },
  "fileList": { "doubleClickAddsToRenameList": false, "...": "..." },
  "renameList": { },
  "filterEditor": { },
  "filterDefaults": { }
}
```

- **Double-click:** move from `UiConfig` → `FileListPrefs.DoubleClickAddsToRenameList` (default `false`). Options still edits it; File List reads `ConfigStore.FileList`.
- **ConfirmationPrompts:** stays on `UiConfig` / `ui.confirmationPrompts`.
- **Applier constraint:** keep a **private** prefs root inside `ConfigStore` with `[ConfigSection] Log` + `Ui` for `ConfigJsonApplier` / `ConfigJsonWriter` / CLI `--set`. Public surface is `ConfigStore.Log` / `ConfigStore.Ui` aliases (no public `MfrConfig`).
- **Section DTOs:** `MainWindowPrefs` / `FileListPrefs` / `RenameListPrefs` / `FilterEditorPrefs` / `MainWindowSplitters` in [`SessionPrefs.cs`](../../Mfr.Models/Config/SessionPrefs.cs); `Ensure*` helpers on `ConfigStore`.
- **Omit empty sections on write:** null/empty UI sections omitted; empty `filterDefaults` omitted.

## Status

Implemented.
