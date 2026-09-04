---
name: Applied Filter Editors
overview: "F1–F4 shipped. F5 remaining option UIs — group only when options/UI are shared or near-identical; otherwise one filter per pass. Next: Count L/R (Trim/Extract)."
todos:
  - id: f1-f4
    content: "F1–F4: ctors, Applied list, Filter Options/Apply To, Space Character + Letters Case"
    status: completed
  - id: f5-count-lr
    content: "F5 Count L/R — shared CountFilterOptions editor + factory for Trim/Extract Left/Right + tests for all four"
    status: completed
  - id: f5-shrink-dup
    content: "F5 Shrink Duplicate Characters — single-char editor"
    status: pending
  - id: f5-trim-between
    content: "F5 Trim Between — start/end Position editor"
    status: pending
  - id: f5-fix-leading-zeros
    content: "F5 Fix Leading 0's"
    status: pending
  - id: f5-space-after-around
    content: "F5 Space After + Space Around — shared chars+neighbor editor pattern + both factories/tests"
    status: pending
  - id: f5-capitalize-after
    content: "F5 Capitalize After"
    status: pending
  - id: f5-sentence-end
    content: "F5 Sentence End Characters"
    status: pending
  - id: f5-strip-parens
    content: "F5 Strip Parentheses"
    status: pending
  - id: f5-cleaner
    content: "F5 Cleaner"
    status: pending
  - id: f5-counter
    content: "F5 Counter"
    status: pending
  - id: f5-inserter
    content: "F5 Inserter"
    status: pending
  - id: f5-casing-list
    content: "F5 Casing List"
    status: pending
  - id: f5-replace-list
    content: "F5 Replace List"
    status: pending
  - id: f5-name-list
    content: "F5 Name List"
    status: pending
  - id: f5-replacer
    content: "F5 Replacer"
    status: pending
  - id: f5-token-mover
    content: "F5 Token Mover"
    status: pending
  - id: f5-mover
    content: "F5 Mover"
    status: pending
  - id: f5-date-time-setter
    content: "F5 Date Setter + Time Setter — shared timestamp-field picker + date/time value editors"
    status: pending
  - id: f5-time-shifter
    content: "F5 Time Shifter"
    status: pending
  - id: f5-attributes-setter
    content: "F5 Attributes Setter"
    status: pending
  - id: f5-tag-remover
    content: "F5 Audio Tag Remover"
    status: pending
  - id: f5-audio-tag-setter
    content: "F5 Audio Tag Setter"
    status: pending
  - id: f5-id3v2-field-setter
    content: "F5 ID3v2 Field Setter"
    status: pending
  - id: f5-formatter
    content: "F5 Formatter — token builder (own sub-project; last)"
    status: pending
isProject: false
---

# Applied Filters + Filter Configuration

Workspace plan (synced from Cursor `applied_filter_editors_c4a4260f`). Canonical for F5 onward.

**Status (2026-09-04):** F5 Count L/R done. **Next: F5 Shrink Duplicate Characters** (single filter). Rename List Phase 10–11 already consume `ToChain()` → live preview when Auto-Preview is on.

### Already shipped (F1–F4)

- Catalog `CreateDefault` + parameterless ctors; Applied list (add/remove/reorder/enable/DnD); Filters menu.
- Filter Options modal (name / Apply To / scope) — see [filter-options-dialog-ui-handover.md](../filter-options-dialog-ui-handover.md).
- Filter Configuration host: title + `FilterOptionsEditorFactory` + `FilterEditorViewLocator`.
- Option editors: **Space Character**, **Letters Case**.
- Optionless string filters: title only (no body) — Shrink/Remove/Strip Spaces, Separate Capitalized Text, Uppercase Initials.

### Pattern to copy (every F5 pass)

Same as F4b/F4c — **one pass = one filter, or one intentional group** (see grouping rule below):

1. Read MFR7 `*FilterEditor` + help for each filter in the pass (`mfr7-reference` skill).
1. Add `…FilterEditorViewModel` under `ViewModels/FilterEditors/` (or one shared editor when options type / UI is identical).
1. Add matching `…FilterEditorView.axaml` (+ code-behind). ViewLocator resolves by naming convention.
1. Register in [`FilterOptionsEditorFactory`](../../Mfr.App.Ui/ViewModels/FilterEditors/FilterOptionsEditorFactory.cs).
1. Live-replace via `filter with { Options = … }` + `ApplyIfChanged` — no Apply button; do not call `Setup()`.
1. VM unit tests + headless gesture → `ToChain()` options match (**each** filter type in the group).
1. Use compact controls (`CompactNumericUpDown`, `CompactCheckBox`, `CompactRadioButton`, `FieldsetGroup`).

Until a type is registered, selecting it still shows title only (Apply To stays in Filter Options).

______________________________________________________________________

## Phase F5 — Remaining editors

### Grouping rule

**Default:** one filter type per agent pass.

**Group in one pass only when it clearly saves work**, for example:

- Identical options type / same shared editor (e.g. all four Count L/R filters).
- Near-identical UI with only labels/property names differing (e.g. Space After + Space Around).
- Shared control surface plus a small value variant (e.g. Date Setter + Time Setter: same timestamp-field picker, date vs time value).

Do **not** batch unrelated filters just to shrink the todo list. File-list filters (Casing / Replace / Name List) stay separate — they share a path-picker *idea* but different option shapes. Audio tag editors stay separate. Time Shifter stays separate from Date/Time Setter.

When grouping: ship the shared editor once, wire every factory arm in that pass, and cover **each** type with VM + headless tests in the same pass.

### Ordered backlog

| Order        | Pass                        | Filters                                            | Options / notes                                                             |
| ------------ | --------------------------- | -------------------------------------------------- | --------------------------------------------------------------------------- |
| **1 (done)** | **Count L/R**               | Trim Left, Trim Right, Extract Left, Extract Right | Shared `CountFilterOptions` editor + four factory arms + tests for all four |
| **2 (next)** | Shrink Duplicate Characters | single                                             | `char` — not count-style                                                    |
| 3            | Trim Between                | single                                             | `Position` start/end + side                                                 |
| 4            | Fix Leading 0's             | single                                             | width / remove extras / max / whole-word                                    |
| 5            | **Space After + Around**    | Space After, Space Around                          | Chars string + neighbor checkbox (shared pattern; two option records)       |
| 6            | Capitalize After            | single                                             | trigger chars string                                                        |
| 7            | Sentence End Characters     | single                                             | char list                                                                   |
| 8            | Strip Parentheses           | single                                             | pair type + remove contents                                                 |
| 9            | Cleaner                     | single                                             | illegal + custom + replacement                                              |
| 10           | Counter                     | single                                             | start / step / format                                                       |
| 11           | Inserter                    | single                                             | text + position                                                             |
| 12           | Casing List                 | single                                             | file path + sentence-initial                                                |
| 13           | Replace List                | single                                             | file path + mode/options                                                    |
| 14           | Name List                   | single                                             | file path + prefix/suffix                                                   |
| 15           | Replacer                    | single                                             | find/replace / regex / scope                                                |
| 16           | Token Mover                 | single                                             | token indices / destination                                                 |
| 17           | Mover                       | single                                             | substring move                                                              |
| 18           | **Date + Time Setter**      | Date Setter, Time Setter                           | Shared timestamp-field picker; date vs time value                           |
| 19           | Time Shifter                | single                                             | field + amount + unit (not grouped with setters)                            |
| 20           | Attributes Setter           | single                                             | attribute flags                                                             |
| 21           | Audio Tag Remover           | single                                             | all / block types                                                           |
| 22           | Audio Tag Setter            | single                                             | per-field format specs                                                      |
| 23           | ID3v2 Field Setter          | single                                             | frame + value                                                               |
| **last**     | Formatter                   | single                                             | format string + token UI — own sub-project                                  |

**Corrections vs older F5a–f batches:** Shrink Duplicate is **not** count-style; **Trim Between** was missing from the batch list; Fix Leading 0's is its own richer editor.

### User-facing (each pass)

Select that filter in Applied → Filter Configuration shows its options; edits update the step and (with Auto-Preview) Rename List preview. Other unfinished types stay title-only.

### Still out of scope for F5

- Preset load/save UI; session persist of the working chain
- Instance rename heart/reset; filter help `?`
- Filter Options XAML polish ([handover](../filter-options-dialog-ui-handover.md))
- Formatter token catalog UX beyond a minimal format-string box (defer rich builder to the Formatter pass)

______________________________________________________________________

## Layering / files

- Defaults: parameterless ctor; `FilterCatalog.CreateDefault`
- UI: `Views/FilterEditors/*`, `ViewModels/FilterEditors/*`
- Wiring: factory + ViewLocator only; `MainWindowViewModel` already selects the editor
- Preview: already hooked via `ChainChanged` → `ToChain()` → `Preview()` — do not re-wire; just ensure `SetFilter` keeps raising chain changes

## References

- MFR7: `FilterEdit.cs`, per-filter `*FilterEditor.cs`, help under `mfr7/Site/finebytes/mfr/Help/`
- Prior slice: Cursor plan `applied_filter_editors_c4a4260f.plan.md` (F1–F4 history)
- Rename List preview: [rename-list-ui.plan.md](rename-list-ui.plan.md) Phase 10–11
