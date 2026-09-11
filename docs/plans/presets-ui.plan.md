---
name: Presets UI F7
overview: "F7 Presets UI in four phases: foundation, Save Preset dialog (Update + Save as new), Preset Manager (Load/Delete/Edit Description/Rename), then quick-pick + polish. Optional RL columns+widths; confirm-replace config flag."
todos:
  - id: p1-foundation
    content: "P1 Foundation — OpenDefault/CreateEmpty, FilterPreset.visibleColumns, confirm-replace config, ReplaceFromChain + inject + App composition"
    status: completed
  - id: p2-save-preset
    content: "P2 Save Preset dialog — Update + Save as new in one dialog; enable menu/toolbar stub"
    status: completed
  - id: p3-preset-manager
    content: "P3 Preset Manager — Load/Delete/Edit Description/Rename; confirm-replace gate; apply optional columns; enable Presets stubs"
    status: completed
  - id: p4-quickpick-polish
    content: "P4 Quick-pick + polish — toolbar ▾, AppTips, headless coverage, docs, mark F7 done in applied-filter-editors.plan.md"
    status: completed
isProject: false
---

# F7 Presets UI

Canonical product backlog: [applied-filter-editors.plan.md](applied-filter-editors.plan.md) (F7 done; F8 next). This file is the detailed implementation plan for that slice only. **Do not** fold in F8 session chain, F9c help, or F10 polish.

**Phased:** yes — four PR-sized phases below. Ship each phase with its tests before starting the next.

## Product decisions (locked)

| Topic                  | Choice                                                                                                                                                                                                                                                                                               |
| ---------------------- | ---------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------- |
| Load semantics         | **Always replace** Applied Filters (clear + rebuild). No merge.                                                                                                                                                                                                                                      |
| Confirm before replace | **Optional via config.** When `ConfirmReplaceAppliedFiltersOnLoad` is `true` and `Steps.Count > 0`, show `ConfirmMessageDialog` before replace (Cancel leaves chain unchanged). When `false` (default), replace immediately (MFR7 parity). Applies to Manager **Load** and toolbar **▾** quick-pick. |
| **Save Preset**        | One **Save Preset** dialog (name/description/columns; Name suggests existing presets). Single **Save** upserts by name and **warns before overwrite**. Prefill from last-loaded when Applied Filters is non-empty. |
| Save enabled           | **Save** enabled when the name is non-blank.                                                                                                                                                                        |
| Save payload           | **Chain always** (`ToChain()`). Description and columns come from the dialog (checkbox off → `null` columns).                                                                                                       |
| RL columns on load     | If preset has `visibleColumns`, apply via `ApplyVisibleColumnsFromSession`. If omitted/`null`, leave current columns unchanged.                                                                                     |
| Description            | Optional on Save dialog + read-only pane in Manager (edit via Save + overwrite). |
| Rename preset          | **In Manager** — keep `Id`; refuse if another preset already has that exact name; update last-loaded name if it matched.                                                                                            |
| Overwrite / delete     | Overwrite confirm when Save targets an existing name; **confirm delete**.                                                                                                                                           |
| Quick load             | Toolbar **Presets** + **▾**; Filters menu: Presets / Save Preset.                                                                                                                                                                                                                                    |
| First-run file         | `OpenDefault()` creates empty file when missing; corrupt hard-fails.                                                                                                                                                                                                                                 |
| Display names          | Catalog-synthesized on load; custom Filter Options names do not round-trip.                                                                                                                                                                                                                          |
| `.mps` / migration     | None.                                                                                                                                                                                                                                                                                                |
| Shortcuts              | None. `AppTips` only.                                                                                                                                                                                                                                                                                |

```mermaid
flowchart LR
  stubs[Menu_Toolbar_stubs] --> mgr[PresetManagerDialog]
  stubs --> saveDlg[SavePresetDialog]
  saveDlg -->|Update_or_SaveAs| pm[PresetManager]
  mgr --> confirmGate{ConfirmFlag_and_nonEmpty}
  stubs --> quickPick[Toolbar_dropdown]
  quickPick --> confirmGate
  confirmGate -->|yes_or_skip| applied[AppliedFiltersViewModel]
  confirmGate -->|cancel| stay[Leave_chain]
  applied -->|ReplaceFromChain| preview[RenameList_AutoPreview]
  confirmGate -->|optional_columns| rlCols[RenameList_VisibleColumns]
  mgr -->|Delete_EditDesc_Rename| pm
  pm --> json["AppData/presets.json"]
  cfg["config.json_ui.presets"] --> confirmGate
```

______________________________________________________________________

## Phases

### P1 — Foundation (no Presets UI yet)

Engine / models / Applied Filters plumbing. Menu/toolbar stay disabled.

1. **`PresetManager.OpenDefault()` / `CreateEmpty()`** — missing file → write empty container then load; corrupt → still throw. Tests for both.
1. **`FilterPreset.VisibleColumns`** — optional `IReadOnlyList<SessionStateRenameListColumn>?` (`key` + `width`). Docs in [Mfr.Filters/docs/README.md](../../Mfr.Filters/docs/README.md); JSON probe with columns present/absent.
1. **`MfrConfig.Ui.Presets.ConfirmReplaceAppliedFiltersOnLoad`** — default `false`; JSON string `"true"`/`"false"`; hand-edit only (Options UI stubbed). Binding coverage.
1. **`AppliedFiltersViewModel.ReplaceFromChain`** — one `ChainChanged`; catalog display names; `Enabled` from steps; select first step if any. Inject `PresetManager` (+ **`LastLoaded` / `CanSavePreset`**). Wire `PresetManager.OpenDefault()` in [`App.axaml.cs`](../../Mfr.App.Ui/App.axaml.cs) → `MainWindowViewModel`.

**Exit:** unit/engine tests green; UI still stubs.

### P2 — Save Preset dialog

Replace the stub with one **Save Preset** action (menu + toolbar) that opens a combined dialog.

1. **`SavePresetDialog`** + VM (`Views/Presets/` ↔ `ViewModels/Presets/`): **Update** last-loaded (when `CanSavePreset`) and **Save as new** — name, description, checkbox **Save Rename List columns**, overwrite confirm when name exists.
1. Prefill Save as new from last-loaded when present (name/description/checkbox); default mode Update when available else Save as new.
1. Upsert on Save as new: keep `Id` on overwrite else `Guid.NewGuid()`; `Chain = ToChain()`; columns from capture when checked else `null`; set last-loaded to the saved preset.
1. **Update**: resolve last-loaded in `NameToPreset`; write `existing with { Chain = ToChain(), VisibleColumns = prior was null ? null : Capture… }` (same `Id`/`Name`/`Description`); `SavePresets()`.
1. Enable **Save Preset** on Filters menu + Applied Filters toolbar (after Presets / ▾); always opens the dialog.

**Exit:** can create (Save as new) and update (Update) presets from one dialog; load still Manager/▾ (P3/P4) or tests.

### P3 — Preset Manager (Load / Delete / Edit Description / Rename)

1. **`PresetManagerDialog`** + VM: sorted list, read-only description, **Load**, **Delete**, **Edit Description**, **Rename**, **Close**.
1. **Load** (double-click / Enter / button): confirm-replace gate → `ReplaceFromChain` → optional columns apply → set last-loaded (enables Save) → close OK.
1. **Delete**: confirm → remove → `SavePresets` → if deleted was last-loaded, clear last-loaded (`CanSavePreset` false) → refresh.
1. **Edit Description**: multiline prompt → save.
1. **Rename**: name prompt; blank invalid; same name no-op; name taken by another → error (no overwrite); success keeps `Id`/chain/description/columns, updates key + last-loaded name if needed.
1. Enable **Presets** menu + toolbar → open Manager.
1. Corrupt open → `OkMessageDialog`; leave chain unchanged.

**Exit:** full named-preset lifecycle except ▾ quick-pick.

### P4 — Quick-pick + polish

1. Toolbar **▾** next to Presets: sorted names; disabled “No presets”; same load path as Manager Load (sets last-loaded).
1. `AppTips` for Presets / Save Preset / ▾; bind tooltips.
1. Headless tests; mark F7 done in [applied-filter-editors.plan.md](applied-filter-editors.plan.md).

**Exit:** F7 complete.

______________________________________________________________________

## Shared design detail (all phases)

### Preset schema: optional RL columns

```csharp
[JsonPropertyName("visibleColumns")]
public IReadOnlyList<SessionStateRenameListColumn>? VisibleColumns { get; init; }
```

Reuse [`SessionStateRenameListColumn`](../../Mfr.Models/Config/SessionState.cs). No `sortFields` on presets. Invalid keys: reuse existing `ApplyVisibleColumns` filtering.

### Config flag

```json
{ "ui": { "presets": { "confirmReplaceAppliedFiltersOnLoad": "false" } } }
```

Shared host helper `TryLoadPresetAsync` (confirm → `LoadPreset` → `ApplyIfPresent`) for Manager Load and ▾.

### Engine

No Delete/Rename/Save APIs on `PresetManager` — UI mutates `NameToPreset` then `SavePresets()`.

### Dialogs / chrome

Reuse `ConfirmMessageDialog`, `OkMessageDialog`, `TextInputDialog`, `ModalDialogKeyboard`, `Themes/MessageDialog.axaml`. Save Preset uses one dedicated dialog for Update and Save as new.

### Host wiring

Open Manager / Save Preset from Applied Filters code-behind + MainWindow Filters menu → pane (Filter Options pattern). Empty chain save allowed for both Update and Save as new.

## Tests by phase

| Phase | Coverage                                                                                                                                                                                   |
| ----- | ------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------ |
| P1    | `OpenDefault` missing vs corrupt; `visibleColumns` JSON; config bool bind; `ReplaceFromChain` + `ChainChanged` + display names; `CanSavePreset` false initially                            |
| P2    | Save as new upsert / overwrite keeps Id; columns checkbox; **Update** keeps Id/name/description; Update radio disabled without last-loaded; Save as new sets last-loaded so Update enables |
| P3    | Load sets last-loaded; delete clears it; rename updates it; confirm-replace cancel; edit description                                                                                       |
| P4    | ▾ load sets last-loaded; tips smoke; mark F7 done                                                                                                                                          |

Do **not** retest JSON polymorphism (`PresetJsonPolymorphismTests`).

## Out of scope (explicit)

- F8 session persistence of working Applied Filters chain
- Shipping default seed presets
- Import MFR7 `.mps`
- Packing Rename List **sort** (or other session fields) into presets
- Soft-load of corrupt `presets.json`
- Options dialog checkbox for the confirm flag (config.json only until Options UI exists)
- Changing description via **Update** (use Edit Description or Save as new)
