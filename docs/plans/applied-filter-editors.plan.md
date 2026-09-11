---
name: Applied Filter Editors
overview: "F1–F6 + F9a/b done on master. Remaining: F7 presets (next), F8 session chain, F9c help ?, F10 Filter Options polish."
todos:
  - id: f1-f5-complete
    content: "F1–F5 complete — Applied list, Filter Options host, folder reorg, all option editors + live preview"
    status: completed
  - id: f6-formatter-format-editor
    content: "F6 Formatter FormatEditor — complete on master"
    status: completed
  - id: f7-presets-ui
    content: "F7 Presets UI — enable Presets / Save Preset; load/save chain via PresetManager"
    status: pending
  - id: f8-session-chain
    content: "F8 Session — persist + restore working Applied Filters chain (current schema only)"
    status: pending
  - id: f9a-save-as-default
    content: "F9a Save as default (📌) — FilterDefaultsStore / filter-defaults.json"
    status: completed
  - id: f9b-reset-to-defaults
    content: "F9b Reset to defaults (↺) — FilterCatalog.CreateDefault"
    status: completed
  - id: f9c-help
    content: "F9c Help ? — per-filter help from Filter Configuration title bar / Filter Options"
    status: pending
  - id: f10-filter-options-polish
    content: "F10 Filter Options dialog — XAML/layout polish vs MFR7 (dialog already functional)"
    status: pending
isProject: false
---

# Applied Filters + Filter Configuration

Workspace plan (synced from Cursor `applied_filter_editors_c4a4260f`). Canonical for Applied Filters / Filter Configuration work.

**Status (2026-09-11):** **F1–F6 + F9a/b complete** on master. Every option-bearing catalog filter has a registered editor; optionless string filters stay title-only. Live option replace + Rename List Auto-Preview via `ToChain()` work. Shared `FormatEditor` is wired across format-capable filters. Pin **📌** and reset **↺** ship in Filter Configuration. **Presets / Save Preset remain disabled stubs.** Session has no Applied Filters chain fields yet. No help `?` button.

### Priority (what's left)

| Order | Item | Why next |
| ----- | ---- | -------- |
| **1** | **F7 Presets UI** | Highest product value; `PresetManager` + JSON shape already exist; menu/toolbar stubs only need enabling + dialogs + `ToChain()` replace/save |
| **2** | **F8 Session chain** | Same chain serialization lessons as F7; restores last working list on launch (`SessionState` — soft-load, current schema only) |
| **3** | **F9c Help `?`** | Remaining Filter Configuration chrome; needs help-host / MFR7 HTML mapping (no button yet) |
| **4** | **F10 Filter Options polish** | Dialog already works; cosmetic layout vs MFR7 only |

Do **not** mix F7–F10 into one pass. F7 before F8 so load/replace UX and error handling land once, then session reuses the chain DTO path.

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

**Optionless (title only, intentional):** Shrink/Remove/Strip Spaces, Separate Capitalized Text, Uppercase Initials.

### Implementation pattern (reference)

Agent checklist: [mfr-implement-filter-editor](../../.agents/skills/mfr-implement-filter-editor/SKILL.md) (+ `mfr7-reference`). Still the template if a **new** filter type needs an editor later:

1. VM + AXAML under matching `FilterGroup` folders; register in `FilterOptionsEditorFactory`.
1. Live-replace via `filter with { Options = … }` + `ApplyIfChanged` — no Apply button; do not call `Setup()`.
1. VM + headless tests under `Mfr.Tests/Ui/FilterEditors/<Group>/`.
1. Compact controls + `SharedSizeGroup="FilterEditorLabel"` for multi-row label+field forms.

Non-product cleanup (shared controls, clamp helpers, etc.): [f5-attributes-audio-editors-review-deeper-refactors.md](f5-attributes-audio-editors-review-deeper-refactors.md) — optional, not blocking F7+.

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

### F7 — Presets UI — **next**

Engine already has `PresetManager` + JSON preset shape (`Mfr.Filters` docs). UI stubs exist (`Presets` / `Save Preset` in Filters menu + Applied Filters toolbar) — all `IsEnabled="False"`.

1. Enable **Presets**: list named presets → replace (or confirm-replace) Applied Filters chain from preset steps.
1. Enable **Save Preset**: name prompt → serialize current `ToChain()` into presets store.
1. Edge cases: empty chain, overwrite same name, load errors (bad JSON / unknown filter type → clear message, no silent remap). Hard-fail dialect of `PresetManager` stays.
1. Tests: VM + headless for load/save round-trip of a small chain.

### F8 — Session persist of working chain

Rename List / other session fields already persist; Applied Filters chain does **not** (`SessionState` has no chain fields).

1. Add current-schema fields on `SessionState` for the working chain (steps: type, options, enabled, display name, Apply To / scope as already modeled). Prefer reuse of the same step shape F7 saves into presets.
1. Save on change (debounced) / shutdown; restore on launch.
1. Missing/unrecognized → defaults (first launch), **no** legacy converters (`AGENTS.md` persistence policy). Soft-load like other session fields.
1. Tests: serialize/deserialize round-trip; unknown type drops to empty or skips that step with documented behavior.

### F9 — Filter chrome

| Sub | Status | Notes |
| --- | ------ | ----- |
| **F9a Save as default (📌)** | **done** | Title-bar pin; `FilterDefaultsStore` / `filter-defaults.json`; applies on palette add only; reset stays factory. See [filter-save-as-default.plan.md](filter-save-as-default.plan.md). |
| **F9b Reset to defaults (↺)** | **done** | Single selection; options / Apply To / scope via `FilterCatalog.CreateDefault`; keeps display name + enabled. |
| **F9c Help `?`** | **todo** | No button yet. Open per-filter help (ported pages or MFR7 `Help/*.html` mapping). Wire from Filter Configuration title bar and/or Filter Options. |

### F10 — Filter Options dialog polish

Dialog already edits name, Apply To, and scope. Polish only:

1. Layout / spacing / control sizing vs MFR7 Filter Options.
1. Shared label column alignment (same `SharedSizeGroup` pattern as filter editors where it still drifts).
1. Any remaining Apply To / scope edge cases discovered in use — fix with tests, not a full rewrite.

______________________________________________________________________

## Layering / files

- Defaults: parameterless ctor; `FilterCatalog.CreateDefault`
- UI editors: `Views/FilterEditors/<FilterGroup>/…`, `ViewModels/FilterEditors/<FilterGroup>/…`
- Host: `FilterEditorView` / factory / base VMs / ViewLocator at `FilterEditors/` root
- Filter Options: `Views/AppliedFilters/FilterOptionsDialog*`
- Wiring: factory + ViewLocator; `MainWindowViewModel` already selects the editor
- Preview: already hooked — do not re-wire for F7+ product chrome
- Presets engine: `Mfr.Engine/Presets/PresetManager` (UI still stubbed)
- Per-type add defaults: `FilterDefaultsStore` (F9a done)

## References

- Agent skill: [mfr-implement-filter-editor](../../.agents/skills/mfr-implement-filter-editor/SKILL.md)
- MFR7: `FilterEdit.cs`, per-filter `*FilterEditor.cs`, FormatEditor help / images; help under `mfr7/Site/finebytes/mfr/Help/`
- Formatter tokens: [formatter-tokens.md](../../.agents/skills/mfr7-reference/formatter-tokens.md)
- Optional cleanup: [f5-attributes-audio-editors-review-deeper-refactors.md](f5-attributes-audio-editors-review-deeper-refactors.md)
- Rename List preview: [rename-list-ui.plan.md](rename-list-ui.plan.md) Phase 10–11
- F9a detail: [filter-save-as-default.plan.md](filter-save-as-default.plan.md)
- Prior slice history: Cursor plan `applied_filter_editors_c4a4260f.plan.md` (F1–F4)
