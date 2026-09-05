---
name: mfr-implement-filter-editor
description: >-
  Implements Filter Configuration option editors for applied filters in this repo:
  ViewModel + AXAML under FilterEditors/<FilterGroup>, factory registration, live
  option replace, and VM/headless tests. Use when adding or changing a filter editor,
  Filter Options body, FilterOptionsEditorFactory, F5 applied-filter editors, or
  Filter Configuration UI for a filter type — not for creating the filter record itself
  (use mfr-implement-filter).
---

# MFR: implement a filter editor

Adds the **Filter Configuration** options body for one applied-filter type (or one intentional shared group). Does **not** create/change `Mfr.Filters` records — use `mfr-implement-filter` for that.

Canonical backlog / grouping rules: `docs/plans/applied-filter-editors.plan.md`.

## Workflow

```text
Filter editor:
- [ ] 1. Pick pass from plan (one filter, or one shared-options group)
- [ ] 2. MFR7 reference brief (mfr7-reference skill) — editor + help + defaults
- [ ] 3. Confirm options type / folder (FilterGroup) already exist in Mfr.Filters
- [ ] 4. Add ViewModel under ViewModels/FilterEditors/<Group>/
- [ ] 5. Add matching View under Views/FilterEditors/<Group>/
- [ ] 6. Register in FilterOptionsEditorFactory
- [ ] 7. VM tests + headless ToChain() tests (each filter type in the group)
- [ ] 8. Mark plan todo done; format/lint touched files
```

## Grouping

**Default:** one filter type per pass.

**Group only when it clearly saves work:** identical options type, near-identical UI (labels differ), or shared control surface + small value variant. Examples already shipped: Count L/R (`ICountOptionsFilter`); Space After + Around (`SpaceTriggerFilterEditorViewModel`); Capitalize After + Sentence End Characters (`CharacterListFilterEditorViewModel`).

Do **not** batch unrelated filters. File-list / audio / Formatter stay separate unless options truly share.

## Location and naming

- **VM** — `Mfr.App.Ui/ViewModels/FilterEditors/<FilterGroup>/YourFilterEditorViewModel.cs`

- **View** — `Mfr.App.Ui/Views/FilterEditors/<FilterGroup>/YourFilterEditorView.axaml` (+ `.cs`)

- **Factory** — `Mfr.App.Ui/ViewModels/FilterEditors/FilterOptionsEditorFactory.cs` (root)

- **ViewLocator** — `FilterEditorViewLocator` — prefix-replace; no edit if naming matches

- **Tests** — `Mfr.Tests/Ui/FilterEditors/FilterEditorViewModelTests.cs` + `FilterEditorViewTests.cs`

- Namespace / `x:Class` must match folder (`…FilterEditors.Trimming`, etc.).

- Shared editor for multiple filters: one VM/view name that describes the shared surface (e.g. `SpaceTrigger…`, `Count…`); factory maps each filter type to it.

- Root `FilterEditors/` holds base VMs, factory, host, ViewLocator only — not per-filter editors.

## ViewModel pattern

Copy the closest shipped editor (see list below). Required shape:

1. `internal sealed partial class … : FilterOptionsEditorViewModel`
1. `LoadWithoutApplying` around `_SyncFromFilter` so property setters do not re-apply during load
1. `[ObservableProperty]` + `partial void OnXChanged` → `_ApplyOptions()`
1. `_ApplyOptions`: early-return if `IsLoading` or wrong filter type; build options; `ApplyIfChanged(filter, filter with { Options = … })`
1. Clamp / normalize UI values to match filter option constraints (`ClampToInt` for NumericUpDown, empty char → `'\0'`, etc.)
1. Enum radios: bind `IsChecked` through `EnumToBooleanConverter.Instance` + `x:Static` (do not add per-value `IsXxx` properties)
1. **Do not** call `Setup()`; **no** Apply button — live replace only
1. Preview is already wired via `SetFilter` → chain change → Auto-Preview; do not re-wire `MainWindowViewModel`

## View pattern

1. `UserControl` with `x:DataType` = the editor VM; code-behind empty aside from `InitializeComponent`
1. Prefer `FieldsetGroup`, `CompactNumericUpDown`, `CompactCheckBox`, `CompactRadioButton`, `filter-editor-label`
1. **Label/control columns:** when several rows are label + control, put them in a parent with `Grid.IsSharedSizeScope="True"` and give each row a `Grid` whose first `ColumnDefinition` uses `SharedSizeGroup="FilterEditorLabel"` (same group name across those rows) so control left edges line up. Mirror `FilterOptionsDialog.axaml` / `Formatting/CounterFilterEditorView.axaml`. Do not use stacked `Orientation="Horizontal"` StackPanels for multi-row label+field forms — labels of different lengths will stagger the inputs. Full-width rows (checkbox-only, label-above multiline text) stay outside that pattern.
1. Give interactive controls `x:Name` for headless `FindControl`
1. Labels / `ToolTip.Tip` from MFR7 editor + help (parity over invention). For longer tips, use `controls:RichToolTip` (title / body / bullets) or `RichToolTipConverter` for bound strings
1. Theme styles live in `Mfr.App.Ui/Themes/FilterEditor.axaml` — reuse classes; do not invent one-off chrome

## Factory

Add a `switch` arm in `FilterOptionsEditorFactory.Create`. Until registered, selecting the filter shows title only (Apply To stays in Filter Options dialog). Unchanged for optionless string filters (Shrink/Remove/Strip Spaces, Separate Capitalized Text, Uppercase Initials).

## Reference editors by shape

- **Shared numeric count** — `Trimming/CountFilterEditor*`
- **Label+control form (aligned columns)** — `Formatting/CounterFilterEditor*` (`SharedSizeGroup=FilterEditorLabel`)
- **Single char (`MaxLength=1`, empty semantics)** — `Trimming/ShrinkDuplicateCharactersFilterEditor*`
- **Position start/end** — `Trimming/TrimBetweenFilterEditor*`
- **Multi checkbox + numeric** — `Misc/FixLeadingZerosFilterEditor*`
- **Shared two-filter labels differ** — `Space/SpaceTriggerFilterEditor*` or `Case/CharacterListFilterEditor*`
- **Enum / radio case mode** — `Case/LettersCaseFilterEditor*`
- **Find/replace + Literal/Wildcard/Regex mode** — `Replace/ReplacerFilterEditor*` (shared: `Controls/ReplacerModeFieldset`, `Controls/ReplacerMatchOptionsFieldset`; list pairs: `ReplaceListFilterEditor*`)
- **Shared timestamp field + date/time value** — `Attributes/DateTimeSetterFilterEditor*` (Date Setter + Time Setter)
- **Space char catalog** — `Space/SpaceCharacterFilterEditor*`

## Tests

For **each** filter type wired in the pass:

1. **VM** (`FilterEditorViewModelTests`): construct step + editor; assert defaults; mutate properties; assert `step.Filter` options
1. **Headless** (`FilterEditorViewTests`, `[AvaloniaFact]`): append via `AppliedFiltersTestUi.Entry("Type")`; find named controls; set values; assert `ToChain().Steps[0].Filter` options

Follow `mfr-ui-headless-tests` for gesture/control rules. Prefer extending the existing FilterEditors test files over new one-off suites.

## Out of scope (do not do in an editor pass)

- Preset load/save UI; session persist of the working chain
- Filter Options dialog XAML polish / Apply To / instance rename
- Filter help `?` button
- Creating the underlying `BaseFilter` / options record (separate pass + `mfr-implement-filter`)
- Formatter rich token builder (own sub-project when that plan item is current)

## Related skills

- `mfr7-reference` — MFR7 editor + help before coding
- `mfr-implement-filter` — filter record / JSON / docs only
- `mfr-ui-headless-tests` — Avalonia headless coverage rules
