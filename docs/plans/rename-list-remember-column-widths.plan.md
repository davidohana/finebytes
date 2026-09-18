# Remember Rename List column widths

## Goal

When a column is resized, store that width in `config.json`. Next time the same field key is added (shuttle, Add/Set from filters, preview companions), reuse the stored width instead of the catalog default — gated by an Options checkbox (default **on**).

**Decision:** Keep both behaviors — session preserve on Set (already shipped via `WithPreservedWidths`) **and** durable remembered widths. Remembered widths cover hide/re-add and cross-launch; session preserve still covers mid-session Set when the option is off.

## Current code state (baseline)

Config was split since the first draft of this plan:

- **Options policy** lives in [`OptionsConfig`](Mfr.Models/Config/AppConfigSections.cs) under root `options` (string leaves via `ConfigJsonApplier`: `"true"`/`"false"`). Includes `rememberLastFolder`, `addMode`, `addFolderContents`, etc. Bound by [`OptionsDialogViewModel`](Mfr.App.Ui/ViewModels/Options/OptionsDialogViewModel.cs) → `ConfigStore.Options`.
- **Rename List UI session** lives in [`RenameListPrefs`](Mfr.Models/Config/UiSessionSections.cs) under root `renameList` (STJ). Sort, `visibleColumns`, font, preview, A/B — **not** add policy.
- [`UiSessionPersistence.SaveOnClose`](Mfr.App.Ui/Services/Session/UiSessionPersistence.cs) **replaces** `ConfigStore.RenameList` with `CaptureSession()` (no field-level merge). Options fields are untouched by close-save.
- Session-local width preserve already exists:
  - [`RenameListVisibleColumn.WithPreservedWidths`](Mfr.App.Ui/ViewModels/RenameList/RenameListVisibleColumn.cs)
  - Used by [`_BuildDefaultsThenRelevantColumns`](Mfr.App.Ui/ViewModels/RenameList/RenameListViewModel.Columns.cs) (Set from filters / undo columns) and shuttle [`SetColumnsFromFilters`](Mfr.App.Ui/ViewModels/RenameList/RenameListFieldShuttleDialogViewModel.cs)
- Resize writes only the live visible list today: [`UpdateVisibleColumnWidth`](Mfr.App.Ui/ViewModels/RenameList/RenameListViewModel.Columns.cs) ← [`RenameListView.Columns.cs`](Mfr.App.Ui/Views/RenameList/RenameListView.Columns.cs)

## Prefs shape

**Toggle (Options-owned):** add to `OptionsConfig`:

- `RememberColumnWidths` (`bool`, default `true`) → persisted as `options.rememberColumnWidths` (`"true"`/`"false"`)
- Load/Commit only via Options dialog → `ConfigStore.Options` (same as `AddFolderContents`)
- Do **not** put this on `RenameListPrefs` / `CaptureSession`

**Width map (session-owned):** add to `RenameListPrefs`:

- `ColumnWidths` (`List<RenameListVisibleColumnSpec>?`) → `renameList.columnWidths`
- Soft-load via existing STJ session deserialize; skip unknown keys / non-positive widths when applying into the VM map
- Only store positive absolute pixel widths (never `-1` / null)

Example document:

```json
"options": {
  "rememberColumnWidths": "true",
  "addMode": "files",
  "addFolderContents": "true"
},
"renameList": {
  "visibleColumns": [ … ],
  "columnWidths": [
    { "key": { "group": "basic", "property": "Name", "preview": false }, "width": 180 }
  ]
}
```

Reuse [`RenameListVisibleColumnSpec`](Mfr.Models/RenameList/RenameListVisibleColumnSpec.cs).

## Write path

1. VM holds an in-memory `Dictionary<RenameListFieldKey, int>` (or list) loaded in `ApplySessionSection` from `renameList.columnWidths`.
1. `UpdateVisibleColumnWidth`: when `ConfigStore.Options.RememberColumnWidths` is true, upsert the stored key’s width into that map (same `_StoredColumnKey` as today for A/B).
1. `CaptureSession`: include `ColumnWidths` from the map (and when remembering is on, upsert all current visible absolute widths so session columns seed the map without another resize).
1. `SaveOnClose`: unchanged pattern — whole `CaptureSession()` object assigned to `ConfigStore.RenameList`, so the map must be on the captured prefs.

## Read / apply path

Add `WithRememberedWidths(columns, keyToWidth)` on `RenameListVisibleColumn`: for each column still at `UseCatalogDefaultWidth`, replace with remembered width when present. Do **not** override an explicit width (visible list / `WithPreservedWidths` wins).

Apply after session preserve (or when creating catalog-default columns):

- `_BuildDefaultsThenRelevantColumns` — after `WithPreservedWidths`
- `_AppendMissingRelevantColumns` / Add from filters — create with remembered width when available
- Shuttle: pass remembered map into ctor; use in `_ColumnsForRelevantKeys`, manual Selected-field add, and Set from filters (after `WithPreservedWidths`)
- `WithPreviewCompanions` insertions when remembering is on

When the option is **off**: do not update the map; do not apply remembered widths (catalog + session preserve only). Do not clear the stored map.

Live flag: read `ConfigStore.Options.RememberColumnWidths` (Options OK mutates live `ConfigStore` before save) — no restart required.

## Options UI

In Options → Rename List ([`OptionsDialog.axaml`](Mfr.App.Ui/Views/Options/OptionsDialog.axaml)):

- Checkbox: **Remember column widths** (next to Add folder contents)
- Tip in [`AppTips`](Mfr.App.Ui/Resources/AppTips.cs)
- [`OptionsDialogViewModel`](Mfr.App.Ui/ViewModels/Options/OptionsDialogViewModel.cs): draft property; ctor from `ConfigStore.Options`; `Commit` writes `options.RememberColumnWidths`

## Tests

- `OptionsConfig` / ConfigStore round-trip for `rememberColumnWidths` string leaf ([`PrefsBindingTests`](Mfr.Tests/Models/PrefsBindingTests.cs) / EnsureDefaultFile if defaults dump includes new leaf)
- `RenameListPrefs` STJ round-trip for `columnWidths` ([`RenameListPrefsTests`](Mfr.Tests/Models/RenameListPrefsTests.cs))
- Resize updates map when on; no update when off
- Hide column then Add/Set from filters reuses remembered width
- Shuttle add uses remembered width when map provided
- Options VM load/commit for the new checkbox ([`OptionsDialogViewModelTests`](Mfr.Tests/Ui/Options/OptionsDialogViewModelTests.cs))
- `CaptureSession` / `SaveOnClose` persist `columnWidths` without clobbering `options.*`

## Out of scope

- Preset `visibleColumns` (presets keep their own widths; do not write remembered map into presets)
- Migrating catalog defaults into the map on first launch without a user resize (capture sync of current absolute widths is enough)
- Changing the already-shipped `WithPreservedWidths` Set-from-filters behavior
