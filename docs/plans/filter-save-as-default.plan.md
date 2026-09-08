---
name: Filter save as default
overview: "F9a: Filter Configuration pin (📌) saves the selected step’s options as the per-type add default in filter-defaults.json — not favorites, not presets, not session."
todos:
  - id: update-applied-plan
    content: Rewrite F9 heart bullet in applied-filter-editors.plan.md to “save as default” (not favorite)
    status: completed
  - id: defaults-store
    content: Add FilterDefaultsStore + filter-defaults.json (PresetJsonOptions polymorphism)
    status: completed
  - id: add-path
    content: Wire _CreateStep to user default; keep Reset on factory CreateDefault
    status: completed
  - id: title-bar-ui
    content: Pin (📌) title-bar button + SaveSelectedAsDefaultCommand + AppTips + confirmation
    status: completed
  - id: tests-docs
    content: Round-trip / add / reset / overwrite / bad-entry tests; save this plan under docs/plans/
    status: completed
isProject: false
---

# Filter save as default (F9a)

## Product meaning

MFR7’s title-bar “heart” is **save current filter options as the per-type add default**, not a favorites list and not presets.

| Concept                  | Meaning                                                                                              | Store                  | When used                          |
| ------------------------ | ---------------------------------------------------------------------------------------------------- | ---------------------- | ---------------------------------- |
| **Save as default (📌)** | Save **current** applied filter options (+ Apply To / scope) as the **default for that filter type** | `filter-defaults.json` | Next palette **add**               |
| **Presets (F7)**         | Named **multi-step** pipeline                                                                        | `presets.json`         | Load/replace Applied Filters chain |
| **Session chain (F8)**   | Last working Applied Filters list                                                                    | `session.json`         | Restore on launch                  |
| **Reset ↺**              | Restore **factory** (parameterless ctor) on the **current** instance                                 | none                   | Title-bar reset                    |

Chrome glyph is **📌** (not heart). Tip: “Save current settings for this filter as default.”

## Behavior

1. One snapshot **per** `BaseFilter.Type`; overwrite on re-save.
1. Payload = full `BaseFilter` JSON (same shape as a preset step’s `filter`). No display name.
1. Defaults apply only on palette add (`_CreateStep`). Not on reset, preset load, or session restore.
1. Reset stays factory via `FilterCatalog.CreateDefault`.
1. Confirmation dialog after save (catalog display name).
1. Not session; not `presets.json`; not hand-edited `config.json`.

## Implementation

- [`FilterDefaultsStore`](../../Mfr.Engine/Presets/FilterDefaultsStore.cs) + AppData `filter-defaults.json`
- [`AppliedFiltersViewModel`](../../Mfr.App.Ui/ViewModels/AppliedFilters/AppliedFiltersViewModel.cs): `SaveSelectedAsDefaultCommand`, `_ResolveAddDefault`
- Title bar: [`FilterEditorView.axaml`](../../Mfr.App.Ui/Views/FilterEditors/FilterEditorView.axaml)
- App opens store via `FilterDefaultsStore.OpenDefault()` in [`App.axaml.cs`](../../Mfr.App.Ui/App.axaml.cs)
