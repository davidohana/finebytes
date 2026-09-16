# Dialog geometry persistence plan

## Decisions (locked)

- **Scope:** All **resizable** modal `Window`s (`CanResize="True"`). Fixed-size dialogs (Options, OK/Confirm/TextInput, Progress) stay `CenterOwner` with XAML defaults.
- **Gating:** `options.rememberWindowState` (Options checkbox). When false: do not restore or capture main-window or dialog geometry.
- **Coords:** Absolute screen pixels for `x`/`y` (same as main window), not owner-relative.
- **Flush:** Update in-memory `ConfigStore` on dialog **close**; disk flush stays with existing `ConfigStore.TrySave` paths (app close / Options OK). No per-dialog disk write.
- **Horizontal-only / content-height dialogs:** For `ModalDialogHorizontalResize` windows and `SizeToContent=Height` modals (`renameListRowError`), restore **width + position** only; keep height content-driven. Capture still stores height for a complete prefs entry; restore ignores it.
- **Maximize:** Do not persist dialog `WindowState`; modals stay normal.

## MFR7 reference brief

- Main form only: `LoadFormConfig` / `SaveFormConfig` (`Left`/`Top`/`Width`/`Height`/`Maximized`); gated by XML `LoadLastValues`.
- Field/sort selectors and other dialogs: `CenterParent`, fixed or tool-window sizes — **no** form-config save.
- **Parity gap:** finebytes adds dialog geometry (intentional improvement over MFR7).

## Approach

Root `dialogs` holds modal geometries; `options.rememberWindowState` gates main-window and dialog restore/capture:

```json
"options": {
  "suppressedConfirmations": [],
  "rememberWindowState": "true"
},
"mainWindow": {
  "x": …, "y": …, "width": …, "height": …, "state": "Normal",
  "splitters": { … }
},
"dialogs": {
  "fieldShuttle": { "x": 100, "y": 80, "width": 820, "height": 560 },
  "renameLog": { "x": …, "y": …, "width": …, "height": … }
}
```

- Soft-load: unknown/missing keys ignored; invalid off-screen/size → fall back to current `CenterOwner` + XAML size. Prior `ui` / `windows` / nested `mainWindow.dialogs` shapes are not read.
- On successful restore: set `WindowStartupLocation = Manual` before/at open so CenterOwner does not override.
- Capture on `Closing` when remember is on and size/position are valid.
- Root `dialogs` is independent of `WindowSession.Capture` / `SaveOnClose` main-window rewrite.

## Dialog IDs (live views)

| Id                    | Dialog                                                                                               | Mode      |
| --------------------- | ---------------------------------------------------------------------------------------------------- | --------- |
| `fieldShuttle`        | [RenameListFieldShuttleDialog](../../Mfr.App.Ui/Views/RenameList/RenameListFieldShuttleDialog.axaml) | size+pos  |
| `renameLog`           | [RenameLogDialog](../../Mfr.App.Ui/Views/LogDialog/RenameLogDialog.axaml)                            | size+pos  |
| `presetManager`       | [PresetManagerDialog](../../Mfr.App.Ui/Views/Presets/PresetManagerDialog.axaml)                      | size+pos  |
| `importSamplePresets` | [ImportSamplePresetsDialog](../../Mfr.App.Ui/Views/Presets/ImportSamplePresetsDialog.axaml)          | size+pos  |
| `savePreset`          | [SavePresetDialog](../../Mfr.App.Ui/Views/Presets/SavePresetDialog.axaml)                            | size+pos  |
| `filterOptions`       | [FilterOptionsDialog](../../Mfr.App.Ui/Views/FilterChainPane/FilterOptionsDialog.axaml)              | width+pos |
| `formatTokenEditor`   | [FormatTokenEditorDialog](../../Mfr.App.Ui/Views/FormatEditor/FormatTokenEditorDialog.axaml)         | width+pos |
| `excludeMasks`        | [ExcludeMasksDialog](../../Mfr.App.Ui/Views/FileList/ExcludeMasksDialog.axaml)                       | size+pos  |
| `renameListRowError`  | [RenameListRowErrorDialog](../../Mfr.App.Ui/Views/RenameList/RenameListRowErrorDialog.axaml)         | width+pos |
| `crash`               | [CrashDialog](../../Mfr.App.Ui/Views/Crash/CrashDialog.axaml)                                        | size+pos  |

## Non-goals

- Non-resizable dialogs
- Owner-relative positioning
- Separate Options toggle for dialogs
- Persisting maximized dialog state
- Migrating any legacy shapes (none exist)

## Phases

### P1 — Prefs + DialogSession

- Add `WindowGeometryPrefs` and root `ConfigStore.Dialogs` (was briefly nested under `MainWindowPrefs`; moved to root).
- Add [DialogSession.cs](../../Mfr.App.Ui/Services/Session/DialogSession.cs) + `DialogGeometryMode`.
- Tip: [AppTips.OptionsRememberWindowState](../../Mfr.App.Ui/Resources/AppTips.cs) mentions dialogs.
- Exit: prefs JSON round-trip; DialogSession restore/capture/skip-when-remember-off tests.
- Status: done (SHA `63e73916`, reviewed; root-map follow-up below)

### P2 — Wire all resizable dialogs

- `DialogSession.Attach` in each listed dialog ctor; width+pos before `ModalDialogHorizontalResize`.
- Exit: each listed dialog attaches; smoke construct test.
- Status: done (SHA `e015aa3c`, reviewed with P3; follow-up `057e4a92` width+pos for row-error)

### P3 — Plan doc + tip assertion

- Write this plan under `docs/plans/dialog-geometry-persistence.plan.md`.
- Assert Options remember-window tip mentions dialogs.
- Exit: plan on disk; tip/tests green.
- Status: done (SHA `e51a6f4a`, reviewed with P2)

### Follow-up — `options` + root `dialogs`

- Rename prefs section `ui` → `options`; put `rememberWindowState` there; keep modal geometries on root `dialogs`.
- Status: done (this change)
