---
name: Applied Filter Editors
overview: "F1–F7 and F9–F10 complete on master. F8 session chain persist removed. Applied Filters / Filter Configuration backlog empty."
todos:
  - id: f1-f5-complete
    content: "F1–F5 complete — Applied list, Filter Options host, folder reorg, all option editors + live preview"
    status: completed
  - id: f6-formatter-format-editor
    content: "F6 Formatter FormatEditor — complete on master"
    status: completed
  - id: f7-presets-ui
    content: "F7 Presets UI — enable Presets / Save Preset; load/save chain via PresetManager"
    status: completed
  - id: f8-session-chain
    content: "F8 Session — persist + restore working Applied Filters chain — removed (not wanted)"
    status: cancelled
  - id: f9a-save-as-default
    content: "F9a Save as default (📌) — FilterDefaultsStore / filter-defaults.json"
    status: completed
  - id: f9b-reset-to-defaults
    content: "F9b Reset to defaults (↺) — FilterCatalog.CreateDefault"
    status: completed
  - id: f9c-help
    content: "F9c Help ? — per-filter help from Filter Configuration title bar (not Filter Options)"
    status: completed
  - id: f10-filter-options-polish
    content: "F10 Filter Options dialog — XAML/layout polish vs MFR7 (dialog already functional)"
    status: completed
isProject: false
---

# Applied Filters + Filter Configuration

Workspace plan (synced from Cursor `applied_filter_editors_c4a4260f`). Canonical for Applied Filters / Filter Configuration work.

**Status (2026-09-12):** **F1–F7 and F9–F10 complete** on master. Every option-bearing catalog filter has a registered editor; optionless string filters stay title-only. Live option replace + Rename List Auto-Preview via `ToChain()` work. Shared `FormatEditor` is wired across format-capable filters. Pin **📌**, reset **↺**, and help **?** ship in Filter Configuration. **F7 Presets UI** ships Preset Manager, a single Save Preset dialog (upsert + overwrite warn), toolbar ▾ quick-pick, and confirm-replace. **F8** (session persist of the working Applied Filters chain) was **removed**. **F10** polishes Filter Options layout (shared label rows, MFR7 copy/spacing, blank-name OK gate).

## Priority (what's left)

| Order | Item                     | Why next |
| ----- | ------------------------ | -------- |
| —     | *(none — backlog empty)* | —        |

Do **not** mix remaining polish into unrelated passes.

______________________________________________________________________

## Shipped (F1–F5)

### Host + Applied list (F1–F4)

- Catalog `CreateDefault` + parameterless ctors; Applied list (add/remove/reorder/enable/DnD); Filters menu.
- Filter Options modal (name / Apply To / scope) — `FilterOptionsDialog` (+ VM/tests); functional, polish deferred to F10.
- Filter Configuration host: title + `FilterOptionsEditorFactory` + `FilterEditorViewLocator` (prefix-replace by `FilterGroup` folder).
- Live preview: `SetFilter` → `ChainChanged` → `ToChain()` → Rename List `Preview()` when Auto-Preview is on (Rename List Phase 10–11).

### Folder layout (F5 reorg — done)

Editors live under `ViewModels/FilterEditors/<FilterGroup>/` ↔ `Views/FilterEditors/<same>/`. Root holds base VMs, factory, host, ViewLocator only. Namespaces match folders.

### Option editors by group

| Group          | Editors (shared where noted)                                                                    |
| -------------- | ----------------------------------------------------------------------------------------------- |
| **Space**      | Space Character; Space After + Around → shared `SpaceTrigger`                                   |
| **Case**       | Letters Case; Capitalize After + Sentence End → shared `CharacterList`; Casing List             |
| **Trimming**   | Count L/R (Trim/Extract Left/Right) → shared `Count`; Shrink Duplicate Characters; Trim Between |
| **Replace**    | Cleaner; Replacer; Replace List                                                                 |
| **Formatting** | Counter; Inserter; Name List; Token Mover; **Formatter** (shared `FormatEditor`)                |
| **Attributes** | Date/Time Setter; Time Shifter; Attributes Setter                                               |
| **Audio**      | Tag Remover; Audio Tag Setter; ID3v2 Field Setter                                               |
| **Misc**       | Fix Leading 0's; Strip Parentheses; Mover (`PathMover`)                                         |

**Optionless (title only, intentional):** Shrink/Remove/Strip Spaces, Separate Capitalized Words, Uppercase Initials.

### Implementation pattern (reference)

Agent checklist: [mfr-implement-filter-editor](../../.agents/skills/mfr-implement-filter-editor/SKILL.md) (+ `mfr7-reference`). Still the template if a **new** filter type needs an editor later:

1. VM + AXAML under matching `FilterGroup` folders; register in `FilterOptionsEditorFactory`.
1. Live-replace via `filter with { Options = … }` + `ApplyIfChanged` — no Apply button; do not call `Setup()`.
1. VM + headless tests under `Mfr.Tests/Ui/FilterEditors/<Group>/`.
1. Compact controls + `SharedSizeGroup="FilterEditorLabel"` for multi-row label+field forms.

Non-product cleanup: [applied-filters-deeper-refactors.md](applied-filters-deeper-refactors.md) (current ranked list); older F5-only notes in [f5-attributes-audio-editors-review-deeper-refactors.md](f5-attributes-audio-editors-review-deeper-refactors.md).

______________________________________________________________________

## Remaining backlog (F7+)

### F6 — Formatter FormatEditor UX — **done**

**Shipped on master:**

1. Public `FormatTokenCatalog` + `FormatStringSyntax.TryValidate` (engine).
1. Shared `FormatEditor` (searchable insert, caret insert, inline error + jump) wired to Formatter.
1. Param dialogs for all arg-bearing tokens (`FormatTokenEditorRegistry` + Edit/right-click).
1. Syntax highlight, capped auto-grow, token picker pane, token-editor Preview band + fieldsets.

**Reuse done:** PathMover Sub-folder, Inserter, Name List Prefix/Suffix, Audio Tag Setter fields, ID3v2 Field Setter text — all use shared `FormatEditor` (`WhenLikelyTokens` where the filter gates compile). Genre stays an editable ComboBox.

**Landed as:** [A #35](https://github.com/davidohana/finebytes/pull/35) (merged) · [B #36](https://github.com/davidohana/finebytes/pull/36) (closed; work on master) · [C #37](https://github.com/davidohana/finebytes/pull/37) (merged).

### F7 — Presets UI — **done**

Detail: [presets-ui.plan.md](presets-ui.plan.md) (P1–P4). Engine `PresetManager` + JSON preset shape; UI ships:

1. **Presets** Manager — Load / Delete / Rename; confirm-replace when configured; optional Rename List columns on load.
1. **Save Preset** — dialog with name/description/columns; single Save upserts and warns on overwrite.
1. Toolbar **▾** quick-pick (sorted names; disabled “No presets” when empty) sharing the host load path with Manager Load.
1. Hard-fail corrupt `presets.json` with a clear dialog; no silent remap. Tests cover VM + headless load/save and ▾ last-loaded.

### F8 — Session persist of working chain — **removed**

Not a product feature. The working Applied Filters chain is not written to `session.json`. Rename List / window / File List session fields still persist. Named presets remain the way to save and restore a chain.

Removed with this cleanup: `SessionState.AppliedFilters`, `SessionJsonOptions`, `SoftLoadFilterChainJsonConverter`, write-through on `ChainChanged`, launch `ReplaceFromChain` restore, and the F8 tests. Leftover `appliedFilters` in existing `session.json` files is ignored (unknown property) and dropped on the next save.

### F9 — Filter chrome

| Sub                           | Status   | Notes                                                                                                                                                                                                                                                           |
| ----------------------------- | -------- | --------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------- |
| **F9a Save as default (📌)**  | **done** | Title-bar pin; `FilterDefaultsStore` / `filter-defaults.json`; applies on palette add only; reset stays factory. See [filter-save-as-default.plan.md](filter-save-as-default.plan.md).                                                                          |
| **F9b Reset to defaults (↺)** | **done** | Single selection; options / Apply To / scope via `FilterCatalog.CreateDefault`; keeps display name + enabled.                                                                                                                                                   |
| **F9c Help `?`**              | **done** | Title-bar **?** (MFR7 `FilterTitle`); Help basename `{Type}.html` by convention; `FilterHelpHost` opens shipped `help/` beside the exe (or missing-help dialog). See [filter-html-help.plan.md](filter-html-help.plan.md). Not on Filter Options (MFR7 parity). |

### F10 — Filter Options dialog polish — **done**

Dialog already edited name, Apply To, and scope. Polish shipped:

1. Layout / spacing / control sizing vs MFR7 Filter Options (outer frame, denser footer gap, shared dialog footer chrome).
1. Shared label column alignment via `FilterEditorLabeledRow` / `FilterEditorLabel` (nested scopes inside Token / Substring fieldsets).
1. Blank-name OK gate (`Name required`) with tooltip on disabled OK; Name focuses on open; MFR7 label copy (`Apply To:`, `Token separator string:`, colonized substring positions, right-aligned substring labels).

______________________________________________________________________

## Layering / files

- Defaults: parameterless ctor; `FilterCatalog.CreateDefault`
- Help: `{Type}.html` convention on `FilterCatalogEntry.HelpFileName`; `FilterHelpHost` opens shipped app `help/` (see [filter-html-help.plan.md](filter-html-help.plan.md))
- UI editors: `Views/FilterEditors/<FilterGroup>/…`, `ViewModels/FilterEditors/<FilterGroup>/…`
- Host: `FilterEditorView` / factory / base VMs / ViewLocator at `FilterEditors/` root
- Filter Options: `Views/AppliedFilters/FilterOptionsDialog*`
- Wiring: factory + ViewLocator; `MainWindowViewModel` already selects the editor
- Preview: already hooked — do not re-wire for F7+ product chrome
- Presets engine: `Mfr.Engine/Presets/PresetManager` (F7 UI done)
- Per-type add defaults: `FilterDefaultsStore` (F9a done)

## References

- Agent skill: [mfr-implement-filter-editor](../../.agents/skills/mfr-implement-filter-editor/SKILL.md)
- MFR7: `FilterEdit.cs`, per-filter `*FilterEditor.cs`, FormatEditor help / images; help under `mfr7/Site/finebytes/mfr/Help/`
- Formatter tokens: [formatter-tokens.md](../../.agents/skills/mfr7-reference/formatter-tokens.md)
- Optional cleanup: [applied-filters-deeper-refactors.md](applied-filters-deeper-refactors.md) (F5 notes: [f5-attributes-audio-editors-review-deeper-refactors.md](f5-attributes-audio-editors-review-deeper-refactors.md))
- Rename List preview: [rename-list-ui.plan.md](rename-list-ui.plan.md) Phase 10–11
- F9a detail: [filter-save-as-default.plan.md](filter-save-as-default.plan.md)
- F7 detail: [presets-ui.plan.md](presets-ui.plan.md)
- Prior slice history: Cursor plan `applied_filter_editors_c4a4260f.plan.md` (F1–F4)
