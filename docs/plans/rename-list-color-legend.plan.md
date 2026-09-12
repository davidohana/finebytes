# Rename List color legend (Phase 16) plan

Parent: [docs/plans/rename-list-ui.plan.md](rename-list-ui.plan.md) § Phase 16 — color legend.

## Decisions (locked)

- **Toolbar only** — `ToggleButton.rename-list-action` on the left rail after Refresh (last item, MFR7 “last after Auto-Preview” intent with finebytes Refresh already present). No View-menu item, no keyboard shortcut.
- **Default off, not session-persisted** — MFR7 never saved legend visibility; skip `SessionStateRenameList` field (YAGNI vs fixed-width / Auto-Preview prefs).
- **Layout** — Inside [`RenameListView.axaml`](../../Mfr.App.Ui/Views/RenameList/RenameListView.axaml): change root to `ColumnDefinitions="Auto,*,Auto"`; legend in column 2 (~132px after polish, `IsVisible` bound). Star column already shrinks — no manual width math.
- **Swatches document shipped brushes** — Use `DynamicResource` keys from [`Themes/RenameList.axaml`](../../Mfr.App.Ui/Themes/RenameList.axaml) / chrome (so light/dark match the grid). Do **not** retarget row/cell colors to raw MFR7 Plum. Omit focused-cell amber (MFR7 legend omits focus).
- **Labels** — MFR7 strings except blue swatch: **`Manual Override`** (not MFR7 `Forced Value`) so legend matches finebytes 14d menus/commands. Footer hint unchanged.
- **Icon** — Add `Assets/RenameList/Legend.png` (24×24), ported from MFR7 toolbar image (`btnLegendEnabled.Image` / `design_interface_toolbar.legend.png` in `RenameList.resx`).
- **debts.md** — No legend bullet exists; do not invent one. Parent “drop the legend bullet” note is stale.

## MFR7 reference brief

### Sources

- Help: `renamelist.html` `#highlighting` / Highlighting (+ `Help/Images/colorlegendbtn.gif`)
- Code: `D:\Devl\mfr7\Core\MFRGui\Forms\RenameList\Legend.cs`, host toggle in `RenameList.cs` (`btnLegendEnabled`, `LegendButtonClicked`)
- finebytes: Phase 16 shipped — toolbar toggle + right-dock legend documenting shipped brushes

### Behavior

- Purpose: toggleable right panel explaining Rename List highlight colors.
- Default: hidden; CheckOnClick toolbar; show shrinks grid by legend width + gap; hide restores.
- Not persisted; no shortcut.
- Status hints: `"Show or Hide Rename List Color Legend Panel"` / `"Rename List Color Legend Panel"`.

### Legend UI (exact labels)

| Order  | Label                                                           | Meaning                                            | finebytes brush / class                                                    |
| ------ | --------------------------------------------------------------- | -------------------------------------------------- | -------------------------------------------------------------------------- |
| Title  | **Color Legend**                                                | —                                                  | —                                                                          |
| 1      | `Original Value`                                                | Unchanged default fg                               | Theme foreground on pane bg                                                |
| 2      | `Value Changed`                                                 | Preview will change                                | `RenameListPreviewChangedForegroundBrush` / `rename-list-preview-changed`  |
| 3      | `Manual Override`                                               | Forced / manual cell override (MFR7: Forced Value) | `RenameListManualOverrideForegroundBrush` / `rename-list-manual-override`  |
| 4      | `—  Value Error` (italic gray + em dash sample)                 | Load / missing gray                                | `RenameListMissingOnDiskForegroundBrush` (load-error cells use same brush) |
| 5      | `Preview Error`                                                 | Preview-error row                                  | `RenameListPreviewErrorRowBrush` / `rename-list-preview-error`             |
| 6      | `Rename Error`                                                  | Commit/apply error row                             | `RenameListCommitErrorRowBrush` / `rename-list-commit-error`               |
| Footer | `Right-Click on a cell or row with error to see error details.` | —                                                  | —                                                                          |

### Parity gaps (intentional)

- Blue label is **Manual Override**, not MFR7 **Forced Value**.
- Commit-error swatch uses shipped pale plum (`#F2D9F2` / dark `#6B5068`), not MFR7 `#DDA0DD`.
- Avalonia star column instead of WinForms absolute width math.
- Dark-theme swatches via existing ThemeDictionaries.

## Non-goals

- Changing any highlight rules, precedence, or brush values.
- Menu item, shortcut, session persist, help HTML page, marked-row (Salmon) swatch, focused-cell swatch.
- Phase 14f drag-out (cut from parent plan).

## Phases

### P1 — Toggle + right panel

- [x] **Status:** done

**Scope / files**

- [`RenameListViewModel.cs`](../../Mfr.App.Ui/ViewModels/RenameList/RenameListViewModel.cs) (or small partial): `IsLegendVisible` (default `false`), `ToggleLegendCommand` / `ToggleLegend()`.
- [`RenameListView.axaml`](../../Mfr.App.Ui/Views/RenameList/RenameListView.axaml): third column panel (title + six bordered swatch `TextBlock`s + footer); toolbar `ToggleButton` with `IsChecked` OneWay + command (same as Auto-Sort / Auto-Preview).
- Optional thin styles for swatch borders in the same UserControl.Styles block.
- [`AppTips.cs`](../../Mfr.App.Ui/Resources/AppTips.cs): tip string matching MFR7 button hint (wire on toggle; panel can use a second tip or omit).

**Exit:** Toggle shows/hides ~112–120px right panel; grid column shrinks; labels/brushes match table above; default hidden.

### P2 — Icon + tests + parent bookkeeping

- [x] **Status:** done

**Scope / files**

- `Mfr.App.Ui/Assets/RenameList/Legend.png` (+ csproj resource if not globbed).
- VM test: `ToggleLegend` flips `IsLegendVisible`.
- Headless [`Mfr.Tests/Ui/RenameList/`](../../Mfr.Tests/Ui/RenameList/): show `RenameListView`, click legend toggle, assert panel `IsVisible` and VM (per `mfr-ui-headless-tests`).
- Update parent [`rename-list-ui.plan.md`](rename-list-ui.plan.md): Phase 16 → done + link child plan; fix stale overview / “blue+plum missing” reusable notes; clear “What to implement next” legend line when done.

**Exit:** Icon loads without throw; headless + VM tests green; parent Phase 16 marked complete.

## Suggested execution

Use `mfr-plan-phase` for P1 → review → commit → P2, or implement P1+P2 in one pass if preferred.
