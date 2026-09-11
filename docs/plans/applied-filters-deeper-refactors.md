---
name: Applied Filters deeper refactors
overview: Ranked follow-ups after F7–F10 and post-session cleanup. Prefer high cost-to-value first. Done/obsolete items listed so they are not re-opened.
---

# Applied Filters / Filter Configuration — deeper refactors

Handover from the F7–F10 implement/review pass and later cleanup (Help convention, F8 removal, unified Save Preset, `PresetNameOrder`).

Canonical product backlog: [applied-filter-editors.plan.md](applied-filter-editors.plan.md) (empty). Older F5-only list: [f5-attributes-audio-editors-review-deeper-refactors.md](f5-attributes-audio-editors-review-deeper-refactors.md).

______________________________________________________________________

## Already done / obsolete (do not re-open)

| Item                                                                                                 | Status                                                                                                                               |
| ---------------------------------------------------------------------------------------------------- | ------------------------------------------------------------------------------------------------------------------------------------ |
| Shared ordered-preset sort (Manager + ▾)                                                             | **Done** — `PresetNameOrder.ByName`                                                                                                  |
| `FilterHelpMap` / HelpFileName attribute                                                             | **Done** — `{Type}.html` on `FilterCatalogEntry`                                                                                     |
| Release-gate Help roots / ship Help beside exe                                                       | **Done** — `AppContext.BaseDirectory/help` only                                                                                      |
| Share `SessionJsonOptions` ↔ `PresetJsonOptions`                                                     | **Obsolete** — F8 / session Applied Filters chain removed                                                                            |
| Mid-session session flush / sibling section capture                                                  | **Obsolete** — no Applied Filters write-through                                                                                      |
| Session debounce vs Auto-Preview helper                                                              | **Obsolete** — same                                                                                                                  |
| `CountFilterOptions.ClampToLength`                                                                   | **Done** — already on the options type                                                                                               |
| Collapse dual `IFileShellOpener` trees                                                               | **Done** — single `Services.Shell`                                                                                                   |
| Space Character empty-Other → `' '`                                                                  | **Superseded** — Other+empty → `'\0'`, rejected at setup                                                                             |
| `CanSavePreset` notify / `PresetRenameListColumns` / `TryLoadPresetAsync` / `Refresh(preferredName)` | **Done** during F7 reviews                                                                                                           |
| Rename `SessionStateRenameListColumn` → shared DTO                                                   | **Done** — `RenameListVisibleColumnSpec`                                                                                             |
| Multiline Entries control / line-iteration helper                                                    | **Done** — `MultilineEntriesFieldset` + `MultilineText.EnumerateLines`                                                               |
| Replace List / Replacer compile regex once in `_Setup`                                               | **Done** — `ReplacerMatching.CompileSearch` cached on both filters                                                                   |
| Inject column capture/apply into presets host                                                        | **Done** — `AppliedFiltersViewModel.SetRenameListColumnSource` from main window; deleted `PresetRenameListColumns` TopLevel reach-in |

______________________________________________________________________

## Still worth doing (best cost-to-value first)

### 5. Shared “position from the side” row control

- **Sites:** `FilterOptionsDialog` substring grid; `TrimBetweenFilterEditorView`
- **Target:** one small control/template for label + spinner + “from the” + anchor
- **Value:** closes wording/spacing drift
- **Cost:** medium AXAML + headless layout tests
- **Rank:** medium / later — wait for a third caller

### 6. Modal focus-and-select-all helper

- **Sites:** `FilterOptionsDialog.OnOpened`, `SavePresetDialog.OnOpened`, `TextInputDialog.OnOpened`
- **Target:** tiny attach helper (`FocusAndSelectAll(TextBox)`) or shared base
- **Value:** ~6–9 duplicated lines; one place for focus quirks
- **Cost:** low LOC; three dialogs + optional tests
- **Rank:** medium-low — only if another dialog needs the same pattern

### 7. AudioTagBlockKind UI display names
8
- **Sites:** `AudioTagBlockKindChoice` vs `AudioTagContainerPolicy` describe helpers
- **Target:** one label source when a second block UI appears
- **Value:** no label drift across block UIs
- **Cost:** low once a second consumer exists
- **Rank:** medium — **wait** for a second consumer
- **Also in:** F5 deeper-refactors doc

### 8. Attributes Setter On/Off/Keep radio-row control

- **Sites:** four near-identical stacks in `AttributesSetterFilterEditorView.axaml`
- **Target:** small UserControl with Label + GroupName + two-way `AttributeTriState`
- **Value:** ~80 AXAML lines → one template
- **Cost:** new control + headless names; **single caller today**
- **Rank:** medium-low — only if layout keeps churning or a second On/Off/Keep editor appears
- **Also in:** F5 deeper-refactors doc

______________________________________________________________________

## Low / skip unless already touching that code

| #   | Item                                                 | Why skip / defer                                     |
| --- | ---------------------------------------------------- | ---------------------------------------------------- |
| 9   | Promote `_CatalogEntryFor` → `FilterCatalog`         | One consumer                                         |
| 10  | Filter Configuration title-bar command property pack | Three stable buttons; churn for little gain          |
| 11  | Preset Manager dialog VM as mutation façade          | Code-behind thin enough                              |
| 12  | Soft-skip blank name in `ApplyFilterOptions`         | Dialog already gates OK; defense-in-depth            |
| 13  | `MainWindowViewModel` composition bag                | Only if ctor keeps growing                           |
| 14  | Date/Time Setter `_NudgeBoundText` redesign          | Fragile; keep unless a second consumer               |
| 15  | Structural equality for list-valued options          | Cross-cutting; skip unless touching options equality |

______________________________________________________________________

## Separate track (`docs/debts.md`)

- Nested `FormatEditor` in token `source=` fields + nested error/caret spans — high cost; soft Source text boxes are enough for now.
