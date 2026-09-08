---
name: Applied Filter Editors
overview: "F1–F6 complete on master: Applied list + Filter Configuration + FormatEditor (reuse, highlight, token picker pane). Next: F7 presets, F8 session chain, F9 filter chrome, F10 Filter Options polish."
todos:
  - id: f1-f5-complete
    content: "F1–F5 complete — Applied list, Filter Options host, folder reorg, all option editors + live preview"
    status: completed
  - id: f6-formatter-format-editor
    content: "F6 Formatter FormatEditor — complete on master; see formatter-formateditor-ux.plan.md"
    status: completed
  - id: f7-presets-ui
    content: "F7 Presets UI — enable Presets / Save Preset; load/save chain via PresetManager"
    status: pending
  - id: f8-session-chain
    content: "F8 Session — persist + restore working Applied Filters chain (current schema only)"
    status: pending
  - id: f9-filter-chrome
    content: "F9 Filter chrome — instance heart/favorite, reset-to-defaults, per-filter help ?"
    status: pending
  - id: f10-filter-options-polish
    content: "F10 Filter Options dialog — XAML/layout polish vs MFR7 (dialog already functional)"
    status: pending
isProject: false
---

# Applied Filters + Filter Configuration

Workspace plan (synced from Cursor `applied_filter_editors_c4a4260f`). Canonical for Applied Filters / Filter Configuration work.

**Status (2026-09-08):** **F1–F6 complete** on master. Every option-bearing catalog filter has a registered editor; optionless string filters stay title-only. Live option replace + Rename List Auto-Preview via `ToChain()` work. Shared `FormatEditor` (catalog, param dialogs, syntax highlight, token picker pane) is wired across format-capable filters. **Next:** F7 presets → F8 session chain → F9 chrome → F10 Filter Options polish.

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

Non-product cleanup (shared controls, clamp helpers, etc.): [f5-attributes-audio-editors-review-deeper-refactors.md](f5-attributes-audio-editors-review-deeper-refactors.md) — optional, not blocking F6+.

______________________________________________________________________

## Remaining backlog (F7+)

Ordered for product value. Do **not** mix these into a single “editor” pass — each is its own feature slice.

### F6 — Formatter FormatEditor UX — **done**

Detailed plan: [formatter-formateditor-ux.plan.md](formatter-formateditor-ux.plan.md). Deep follow-ups: [f6-formateditor-deep-refactors.md](f6-formateditor-deep-refactors.md).

**Shipped on master:**

1. Public `FormatTokenCatalog` + `FormatStringSyntax.TryValidate` (engine).
1. Shared `FormatEditor` (searchable insert, caret insert, inline error + jump) wired to Formatter.
1. Param dialogs for all arg-bearing tokens (`FormatTokenEditorRegistry` + Edit/right-click).

**Reuse done:** PathMover Sub-folder, Inserter, Name List Prefix/Suffix, Audio Tag Setter fields, ID3v2 Field Setter text — all use shared `FormatEditor` (`WhenLikelyTokens` where the filter gates compile). Genre stays an editable ComboBox. Syntax highlight: [`formateditor-syntax-highlight.plan.md`](formateditor-syntax-highlight.plan.md). Token picker pane (collapsible Insert+Edit, last-focused field): [`format-token-picker-pane.plan.md`](format-token-picker-pane.plan.md).

**Landed as:** [A #35](https://github.com/davidohana/finebytes/pull/35) (merged) · [B #36](https://github.com/davidohana/finebytes/pull/36) (closed; work on master) · [C #37](https://github.com/davidohana/finebytes/pull/37) (merged). Optional polish still open: FormatEditor auto-grow / expand ([#38](https://github.com/davidohana/finebytes/pull/38)).

### F7 — Presets UI

Engine already has `PresetManager` + JSON preset shape (`Mfr.Filters` docs). UI stubs exist (`Presets` / `Save Preset` menu items, disabled).

1. Enable **Presets** menu: list named presets → replace (or confirm-replace) Applied Filters chain from preset steps.
1. Enable **Save Preset**: name prompt → serialize current `ToChain()` into presets store.
1. Edge cases: empty chain, overwrite same name, load errors (bad JSON / unknown filter type → clear message, no silent remap).
1. Tests: VM + headless for load/save round-trip of a small chain.

### F8 — Session persist of working chain

Rename List / other session fields already persist; Applied Filters chain does not yet.

1. Add current-schema fields on `SessionState` for the working chain (steps: type, options, enabled, display name, Apply To / scope as already modeled).
1. Save on change (debounced) / shutdown; restore on launch.
1. Missing/unrecognized → defaults (first launch), **no** legacy converters (`AGENTS.md` persistence policy).
1. Tests: serialize/deserialize round-trip; unknown type drops to empty or skips that step with documented behavior.

### F9 — Filter chrome (heart / reset / help)

MFR7 Applied / Filter Configuration chrome still missing:

1. **Instance heart / favorite** — mark an applied-step instance name (or options snapshot) as favorite; clarify product meaning vs presets before coding (MFR7 heart vs preset overlap).
1. **Reset to defaults** — **done** (single selection; options / Apply To / scope via `FilterCatalog.CreateDefault`; keeps display name + enabled; title-bar `↺`).
1. **Help `?`** — open per-filter help (ported help pages or MFR7 `Help/*.html` mapping). Wire from Filter Configuration title bar and/or Filter Options.

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

## References

- Agent skill: [mfr-implement-filter-editor](../../.agents/skills/mfr-implement-filter-editor/SKILL.md)
- MFR7: `FilterEdit.cs`, per-filter `*FilterEditor.cs`, FormatEditor help / images; help under `mfr7/Site/finebytes/mfr/Help/`
- Formatter tokens: [formatter-tokens.md](../../.agents/skills/mfr7-reference/formatter-tokens.md)
- Optional cleanup: [f5-attributes-audio-editors-review-deeper-refactors.md](f5-attributes-audio-editors-review-deeper-refactors.md)
- Rename List preview: [rename-list-ui.plan.md](rename-list-ui.plan.md) Phase 10–11
- Prior slice history: Cursor plan `applied_filter_editors_c4a4260f.plan.md` (F1–F4)
