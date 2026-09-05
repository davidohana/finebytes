---
name: Applied Filter Editors
overview: "F1–F4 shipped. Reorg + Count L/R + Shrink Dup + Trim Between + Fix Leading 0's + Space After/Around + Capitalize After + Sentence End + Strip Parentheses + Cleaner + Counter + Inserter + Casing List + Replace List + Name List + Replacer + Token Mover + Mover + Date/Time Setter done. Audio Tag Remover done. Next: Time Shifter, then remaining F5 option UIs — group only when options/UI are shared or near-identical; otherwise one filter per pass."
todos:
  - id: f1-f4
    content: "F1–F4: ctors, Applied list, Filter Options/Apply To, Space Character + Letters Case"
    status: completed
  - id: f5-count-lr
    content: "F5 Count L/R — shared CountFilterOptions editor + factory for Trim/Extract Left/Right + tests for all four"
    status: completed
  - id: f5-shrink-dup
    content: "F5 Shrink Duplicate Characters — single-char editor"
    status: completed
  - id: f5-reorg-subfolders
    content: "F5 reorg — move editors into FilterGroup subfolders (mirror Mfr.Filters); update ViewLocator + usings"
    status: completed
  - id: f5-trim-between
    content: "F5 Trim Between — start/end Position editor"
    status: completed
  - id: f5-fix-leading-zeros
    content: "F5 Fix Leading 0's"
    status: completed
  - id: f5-space-after-around
    content: "F5 Space After + Space Around — shared chars+neighbor editor pattern + both factories/tests"
    status: completed
  - id: f5-capitalize-after
    content: "F5 Capitalize After"
    status: completed
  - id: f5-sentence-end
    content: "F5 Sentence End Characters"
    status: completed
  - id: f5-strip-parens
    content: "F5 Strip Parentheses"
    status: completed
  - id: f5-cleaner
    content: "F5 Cleaner"
    status: completed
  - id: f5-counter
    content: "F5 Counter"
    status: completed
  - id: f5-inserter
    content: "F5 Inserter"
    status: completed
  - id: f5-casing-list
    content: "F5 Casing List — space-separated words + sentence-initial"
    status: completed
  - id: f5-replace-list
    content: "F5 Replace List — embedded line pairs (`search => replacement`) + mode/options"
    status: completed
  - id: f5-name-list
    content: "F5 Name List — embedded one-name-per-line text + prefix/suffix"
    status: completed
  - id: f5-replacer
    content: "F5 Replacer"
    status: completed
  - id: f5-token-mover
    content: "F5 Token Mover"
    status: completed
  - id: f5-mover
    content: "F5 Mover — root folder + optional sub-folder template"
    status: completed
  - id: f5-date-time-setter
    content: "F5 Date/Time Setter — one filter with optional date + time checkboxes + timestamp-field picker"
    status: completed
  - id: f5-time-shifter
    content: "F5 Time Shifter"
    status: pending
  - id: f5-attributes-setter
    content: "F5 Attributes Setter"
    status: pending
  - id: f5-tag-remover
    content: "F5 Audio Tag Remover"
    status: completed
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

**Status (2026-09-05):** F5 Mover + Date/Time Setter + Audio Tag Remover done. **Next: F5 Time Shifter**. Rename List Phase 10–11 already consume `ToChain()` → live preview when Auto-Preview is on.

## Already shipped (F1–F4)

- Catalog `CreateDefault` + parameterless ctors; Applied list (add/remove/reorder/enable/DnD); Filters menu.
- Filter Options modal (name / Apply To / scope) — see [filter-options-dialog-ui-handover.md](../filter-options-dialog-ui-handover.md).
- Filter Configuration host: title + `FilterOptionsEditorFactory` + `FilterEditorViewLocator`.
- Option editors: **Space Character**, **Letters Case**.
- Optionless string filters: title only (no body) — Shrink/Remove/Strip Spaces, Separate Capitalized Text, Uppercase Initials.

______________________________________________________________________

## Folder layout (do before more F5 editors)

Flat `FilterEditors/` will not scale (~25 option editors). Mirror [`FilterGroup`](../../Mfr.Filters/FilterGroup.cs) / `Mfr.Filters` category folders under **both** VM and view trees.

### Target tree

```text
ViewModels/FilterEditors/          Views/FilterEditors/
  FilterEditorViewModel.cs           FilterEditorView.axaml(+.cs)
  FilterOptionsEditorViewModel.cs    FilterEditorViewLocator.cs
  FilterOptionsEditorFactory.cs
    Space/                             Space/
      SpaceCharacter…                  SpaceCharacter…
      SpaceTrigger… (After + Around)   SpaceTrigger…
    Case/                              Case/
    LettersCase…                     …
    CharacterList… (Capitalize After + Sentence End)
    CasingList…
  Trimming/                          Trimming/
    Count…                           …
    ShrinkDuplicateCharacters…
    TrimBetween…
  Replace/                           Replace/
    Cleaner…, ReplaceList…, Replacer…
  Formatting/                        Formatting/
    Counter…, Inserter…, NameList…
    TokenMover…, Formatter…
  Attributes/                        Attributes/
    DateTimeSetter…
    TimeShifter…, AttributesSetter…
  Audio/                             Audio/
    TagRemover…, AudioTagSetter…
    Id3v2FieldSetter…
  Misc/                              Misc/
    FixLeadingZeros…
    StripParentheses…
    Mover…
```

Namespaces follow folders, e.g. `Mfr.App.Ui.ViewModels.FilterEditors.Trimming` ↔ `Mfr.App.Ui.Views.FilterEditors.Trimming`.

### Shared vs per-filter

- **Root** `FilterEditors/` — base VMs, factory, ViewLocator; any helper used by ≥2 categories
- **Category subfolder** — that group's `*FilterEditorViewModel` / `*FilterEditorView` (+ helpers used only there, e.g. `SpaceCharacterDefinition` → `Space/`)

### ViewLocator change (required for reorg)

Today the locator requires an **exact** flat namespace (`ViewModels.FilterEditors` → `Views.FilterEditors`). After reorg, resolve by **prefix replace**:

- VM namespace must start with `Mfr.App.Ui.ViewModels.FilterEditors`
- View type = same relative suffix under `Mfr.App.Ui.Views.FilterEditors`, with `ViewModel` → `View` on the type name

Example: `…ViewModels.FilterEditors.Trimming.CountFilterEditorViewModel` → `…Views.FilterEditors.Trimming.CountFilterEditorView`.

Keep types in the same category folder on both sides; do not cross-map categories.

### Reorg pass checklist

1. Create the eight category folders under VM + Views.
1. Move existing editors: Space Character → `Space/`; Letters Case → `Case/`; Count + Shrink Duplicate → `Trimming/`; move `SpaceCharacterDefinition` with Space.
1. Update namespaces + factory/test usings.
1. Teach `FilterEditorViewLocator` the prefix-replace rule; keep `Match` on `FilterOptionsEditorViewModel`.
1. Build + existing Filter Editor VM/headless tests green.
1. No behavior changes — move only.

Do this **before Trim Between** so new editors land in the right folder from the start.

______________________________________________________________________

## Pattern to copy (every F5 pass)

Agent checklist: [mfr-implement-filter-editor](../../.agents/skills/mfr-implement-filter-editor/SKILL.md) (plus `mfr7-reference`).

Same as F4b/F4c — **one pass = one filter, or one intentional group** (see grouping rule below):

1. Read MFR7 `*FilterEditor` + help for each filter in the pass (`mfr7-reference` skill).
1. Add `…FilterEditorViewModel` under `ViewModels/FilterEditors/<FilterGroup>/` (or one shared editor when options type / UI is identical).
1. Add matching `…FilterEditorView.axaml` (+ code-behind) under `Views/FilterEditors/<same group>/`. ViewLocator resolves by naming + folder convention.
1. Register in [`FilterOptionsEditorFactory`](../../Mfr.App.Ui/ViewModels/FilterEditors/FilterOptionsEditorFactory.cs) (stays at root; add category `using`s as needed).
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
- Shared control surface plus a small value variant (historical example was Date + Time Setter; those are now one `DateTimeSetter` filter).

Do **not** batch unrelated filters just to shrink the todo list. File-list filters (Casing / Replace / Name List) stay separate — they share a path-picker *idea* but different option shapes. Audio tag editors stay separate. Time Shifter stays separate from Date/Time Setter.

When grouping: ship the shared editor once, wire every factory arm in that pass, and cover **each** type with VM + headless tests in the same pass.

### Ordered backlog

- **0 (done)** — **Reorg subfolders** (all) — move existing editors; ViewLocator prefix-replace; no behavior change
- **1 (done)** — **Count L/R** (Trimming) — Trim Left, Trim Right, Extract Left, Extract Right; shared `CountFilterOptions` editor + four factory arms + tests for all four
- **2 (done)** — **Shrink Duplicate Characters** (Trimming) — `char` — not count-style
- **3 (done)** — **Trim Between** (Trimming) — `Position` start/end + side
- **4 (done)** — **Fix Leading 0's** (Misc) — width / remove extras / max / whole-word
- **5 (done)** — **Space After + Around** (Space) — Space After, Space Around; shared `SpaceTrigger` chars+neighbor editor; two factory arms + tests
- **6 (done)** — Capitalize After (Case) — trigger chars string; later shared `CharacterList` editor with Sentence End
- **7 (done)** — Sentence End Characters (Case) — char list; later shared `CharacterList` editor with Capitalize After
- **8 (done)** — Strip Parentheses (Misc) — pair type + remove contents
- **9 (done)** — Cleaner (Replace) — illegal + custom + replacement
- **10 (done)** — Counter (Formatting) — start / step / leading-zeros mode (None/Automatic/Custom) / position / separator / reset-per-folder
- **11 (done)** — Inserter (Formatting) — text + position
- **12 (done)** — Casing List (Case) — space-separated words + sentence-initial
- **13 (done)** — Replace List (Replace) — embedded line pairs (`search => replacement`) + mode/options
- **14 (done)** — Name List (Formatting) — embedded one-name-per-line text + prefix/suffix
- **15 (done)** — Replacer (Replace) — find/replace / regex / scope
- **16 (done)** — Token Mover (Formatting) — token indices / destination
- **17 (done)** — **Mover** (Misc) — root folder + optional sub-folder template (formatter tokens / `\`)
- **18 (done)** — **Date/Time Setter** (Attributes) — one `DateTimeSetter` filter; optional date + time checkboxes; timestamp-field picker
- **19 (next)** — Time Shifter (Attributes) — field + amount + unit (not grouped with setters)
- **20** — Attributes Setter (Attributes) — attribute flags
- **21 (done)** — Audio Tag Remover (Audio) — all / block types
- **22** — Audio Tag Setter (Audio) — per-field format specs
- **23** — ID3v2 Field Setter (Audio) — frame + value
- **last** — Formatter (Formatting) — format string + token UI — own sub-project

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
- UI: `Views/FilterEditors/<FilterGroup>/…`, `ViewModels/FilterEditors/<FilterGroup>/…` (host: `FilterEditorView` / factory / base VMs / ViewLocator at `FilterEditors/` root)
- Wiring: factory + ViewLocator only; `MainWindowViewModel` already selects the editor
- Preview: already hooked via `ChainChanged` → `ToChain()` → `Preview()` — do not re-wire; just ensure `SetFilter` keeps raising chain changes

## References

- Agent skill: [mfr-implement-filter-editor](../../.agents/skills/mfr-implement-filter-editor/SKILL.md)
- MFR7: `FilterEdit.cs`, per-filter `*FilterEditor.cs`, help under `mfr7/Site/finebytes/mfr/Help/`
- Prior slice: Cursor plan `applied_filter_editors_c4a4260f.plan.md` (F1–F4 history)
- Rename List preview: [rename-list-ui.plan.md](rename-list-ui.plan.md) Phase 10–11
