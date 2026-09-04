---
name: Applied Filter Editors
overview: "F1–F4 shipped. F5 remaining option UIs — one filter per pass. Next: Trim Left (shared CountFilterOptions editor)."
todos:
  - id: f1-f4
    content: "F1–F4: ctors, Applied list, Filter Options/Apply To, Space Character + Letters Case"
    status: completed
  - id: f5-trim-left
    content: "F5 Trim Left — shared CountFilterOptions editor + factory + tests"
    status: pending
  - id: f5-trim-right
    content: "F5 Trim Right — factory case + tests (reuse count editor)"
    status: pending
  - id: f5-extract-left
    content: "F5 Extract Left — factory case + tests"
    status: pending
  - id: f5-extract-right
    content: "F5 Extract Right — factory case + tests"
    status: pending
  - id: f5-shrink-dup
    content: "F5 Shrink Duplicate Characters — single-char editor"
    status: pending
  - id: f5-fix-between
    content: "F5 Trim Between — start/end Position editor"
    status: pending
  - id: f5-fix-leading-zeros
    content: "F5 Fix Leading 0's"
    status: pending
  - id: f5-space-after
    content: "F5 Space After"
    status: pending
  - id: f5-space-around
    content: "F5 Space Around"
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
  - id: f5-date-setter
    content: "F5 Date Setter"
    status: pending
  - id: f5-time-setter
    content: "F5 Time Setter"
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

**Status (2026-09-04):** F1–F4 done. **Next: F5 Trim Left** (first remaining option editor). Rename List Phase 10–11 already consume `ToChain()` → live preview when Auto-Preview is on.

### Already shipped (F1–F4)

- Catalog `CreateDefault` + parameterless ctors; Applied list (add/remove/reorder/enable/DnD); Filters menu.
- Filter Options modal (name / Apply To / scope) — see [filter-options-dialog-ui-handover.md](../filter-options-dialog-ui-handover.md).
- Filter Configuration host: title + `FilterOptionsEditorFactory` + `FilterEditorViewLocator`.
- Option editors: **Space Character**, **Letters Case**.
- Optionless string filters: title only (no body) — Shrink/Remove/Strip Spaces, Separate Capitalized Text, Uppercase Initials.

### Pattern to copy (every F5 pass)

Same as F4b/F4c — **one filter type per pass**:

1. Read MFR7 `*FilterEditor` + help for that filter (`mfr7-reference` skill).
2. Add `…FilterEditorViewModel` under `ViewModels/FilterEditors/` (or extend a shared editor when options type is identical).
3. Add matching `…FilterEditorView.axaml` (+ code-behind). ViewLocator resolves by naming convention.
4. Register in [`FilterOptionsEditorFactory`](../../Mfr.App.Ui/ViewModels/FilterEditors/FilterOptionsEditorFactory.cs).
5. Live-replace via `filter with { Options = … }` + `ApplyIfChanged` — no Apply button; do not call `Setup()`.
6. VM unit tests + headless gesture → `ToChain()` options match.
7. Use compact controls (`CompactNumericUpDown`, `CompactCheckBox`, `CompactRadioButton`, `FieldsetGroup`).

Until a type is registered, selecting it still shows title only (Apply To stays in Filter Options).

______________________________________________________________________

## Phase F5 — Remaining editors (one filter per pass)

**Rule:** implement only the **next pending** todo below in a single agent pass. Do not batch multiple filter types.

### Shared count editor (Trim/Extract L/R)

`TrimLeft` / `TrimRight` / `ExtractLeft` / `ExtractRight` all use [`CountFilterOptions`](../../Mfr.Filters/CountFilterOptions.cs) (`Count` int).

- **First of these (Trim Left):** introduce one shared `CountFilterOptionsEditorViewModel` + view (label + `CompactNumericUpDown`). Factory switches on all four filter types to the same VM.
- **Later three:** factory already covers them if wired in the first pass — still ship **one filter’s tests + headless coverage per pass** so each type is verified alone. Prefer wiring all four factory arms in the Trim Left pass (same options shape) and dedicating Trim Right / Extract L/R passes to tests + any label/tooltip parity; if that feels like empty passes, fold factory+tests for all four into Trim Left and cancel the three follow-ups.

**Recommendation:** Trim Left pass = shared editor + factory for all four Count filters + tests for Trim Left; next three passes = headless/VM tests only for that type (no new UI). Or one pass for all four if the user asks to collapse.

### Ordered backlog

| Order | Filter | Options shape | MFR7 editor cue |
| --- | --- | --- | --- |
| **1 (next)** | Trim Left | `CountFilterOptions` | `LeftTrimFilterEditor` — NumericUpDown |
| 2 | Trim Right | `CountFilterOptions` | `RightTrimFilterEditor` |
| 3 | Extract Left | `CountFilterOptions` | Extract Left editor |
| 4 | Extract Right | `CountFilterOptions` | Extract Right editor |
| 5 | Shrink Duplicate Characters | `char` | single-character field (not count) |
| 6 | Trim Between | `Position` start/end + side | `TrimBetweenFilterEditor` |
| 7 | Fix Leading 0's | width / remove extras / max / whole-word | Fix Leading 0's editor |
| 8 | Space After | chars + neighbor checkbox | Space After editor |
| 9 | Space Around | chars + neighbor checkbox | Space Around editor |
| 10 | Capitalize After | trigger chars string | Capitalize After editor |
| 11 | Sentence End Characters | char list | Sentence End editor |
| 12 | Strip Parentheses | pair type + remove contents | Strip Parentheses editor |
| 13 | Cleaner | illegal + custom + replacement | Cleaner editor |
| 14 | Counter | start / step / format | Counter editor |
| 15 | Inserter | text + position | Inserter editor |
| 16 | Casing List | file path + sentence-initial | file picker + checkbox |
| 17 | Replace List | file path + options | file picker |
| 18 | Name List | file path + prefix/suffix | file picker + format fields |
| 19 | Replacer | find/replace / regex / scope | Replacer editor |
| 20 | Token Mover | token indices / destination | Token Mover editor |
| 21 | Mover | substring move | Mover editor |
| 22 | Date Setter | timestamp field + value | Date Setter editor |
| 23 | Time Setter | timestamp field + value | Time Setter editor |
| 24 | Time Shifter | field + amount + unit | Time Shifter editor |
| 25 | Attributes Setter | attribute flags | Attributes Setter editor |
| 26 | Audio Tag Remover | all / block types | Tag Remover editor |
| 27 | Audio Tag Setter | per-field format specs | larger pane |
| 28 | ID3v2 Field Setter | frame + value | ID3v2 editor |
| **last** | Formatter | format string + token UI | own sub-project |

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
