# Filter Options dialog — UI handover

Handoff for fixing remaining layout/styling in the Filter Options modal. View-model and filter logic are done; this is **XAML-only** work (unless a worthwhile headless UI test is added).

## What is already shipped (do not revert)

- Filter Options modal moved out of Filter Configuration (MFR7 model).
- VM supports: name, Apply To (group + property), Whole/Substring/Token scope, ancestor folder level.
- Opened from Applied Filters toolbar, double-click row, and **Filters → Filter Options**.
- Tests: `Mfr.Tests/Ui/AppliedFilters/FilterOptionsDialogViewModelTests.cs` (VM only; no view layout tests).

## Primary files

| Area        | Path                                                                   |
| ----------- | ---------------------------------------------------------------------- |
| Dialog XAML | `Mfr.App.Ui/Views/AppliedFilters/FilterOptionsDialog.axaml`            |
| Code-behind | `Mfr.App.Ui/Views/AppliedFilters/FilterOptionsDialog.axaml.cs`         |
| View model  | `Mfr.App.Ui/ViewModels/AppliedFilters/FilterOptionsDialogViewModel.cs` |

## Known UI bugs (latest screenshot)

1. **Substring `NumericUpDown` spinners broken** — numeric value not visible; only up/down chevrons and a thin vertical strip. Affects Start/End position rows (possibly Level and Token number too).
1. **Substring row layout cramped/misaligned** — labels, spinners, anchor combos, and `side (incl.)` do not line up cleanly.
1. **Label column inconsistency** — top rows use an `88px` label column; Substring/Token groups use `104px`, causing horizontal misalignment.
1. **Missing MFR7 copy** — legacy dialog has `from the` between spinner and anchor combo; current layout skips it.

## Failed attempts (avoid repeating)

| Attempt                                                                       | Result                                                   |
| ----------------------------------------------------------------------------- | -------------------------------------------------------- |
| Custom `ButtonSpinner` template overrides (18px spinner, 12px button heights) | Completely broke rendering (dashed lines, chevrons only) |
| Removed template overrides                                                    | Partially better, but values still not visible at 72px   |
| `HorizontalAlignment="Stretch"` on Apply To combos + `CanResize="True"`       | Absurdly wide fields and huge empty middle when resized  |
| `MaxHeight="520"`                                                             | User said no max — already removed                       |
| `CanResize="False"`, `Width="400"`, `SizeToContent="Height"`                  | Resize issue fixed; spinner/alignment still bad          |

## MFR7 reference (layout parity)

Source: `D:\Devl\mfr7\Core\MFRGui\Forms\Filters\FilterOptions.cs`

Key sizes:

- **Dialog:** 402×312, non-resizable (`FormBorderStyle.FixedToolWindow`).
- **Spinners:** 56×20 (`spinSubstringFrom/To`, `spinTokenNum`).
- **Anchor combos:** 88×21.
- **Substring row layout (absolute):** `[Label 88px right-aligned] [Spinner 56px] [from the 48px] [Combo 88px] [side (incl.) 56px]`.
- **Group box content width:** ~368px inside margins.

## Likely root cause (spinners)

`NumericUpDown` at **72px** with **26px height** plus Avalonia’s default right-side `ButtonSpinner` leaves almost no room for the text area. WinForms fits 56px at 20px height; Avalonia needs different sizing or a different approach.

**Try in order:**

1. Widen spinners to **88–96px** (or match MFR7 proportions scaled for 26px height).
1. If still broken: `ShowButtonSpinner="False"` on position fields only (keyboard entry) — user wanted spinners, so treat as last resort.
1. If spinners are required: minimal template tweak — only constrain `ButtonSpinner#PART_Spinner` **width** (~20px). Do **not** set RepeatButton `Height`/`MinHeight` (that broke layout before).
1. Verify `Value` binding — VM uses `decimal` with `FormatString="0"`; binding is fine in tests.

## Layout recommendations

1. **Unify label column** to one width (88 or 104) across all rows.

1. **Restructure Substring rows** to match MFR7:

   ```text
   Start at position:  [spinner]  from the  [left/right ▼]  side (incl.)
   End at position:    [spinner]  from the  [left/right ▼]  side (incl.)
   ```

1. **Dialog width:** consider **402px** to match MFR7 (content ~368px inside group).

1. Keep **`CanResize="False"`** and **`SizeToContent="Height"`** so height grows when Substring/Token sections appear.

1. **Apply To row** — `Grid ColumnDefinitions="88,*,6,*"` with `HorizontalAlignment="Stretch"` on combos is correct; re-verify after other fixes.

## Current style classes

| Class                    | Purpose                                      |
| ------------------------ | -------------------------------------------- |
| `filter-options-field`   | Base 26px height, FileList font/colors       |
| `filter-options-spinner` | Fixed 72px width, `HorizontalAlignment=Left` |
| `filter-options-anchor`  | Fixed 76px anchor combos                     |
| `filter-options-group`   | Substring/Token bordered sections            |

## How to verify

```powershell
cd d:\Devl\finebytes
just build
just run-ui
```

1. Select a string-target filter (e.g. Extract Right) in Applied Filters.
1. Open Filter Options (toolbar or double-click).
1. Select **Substring** — spinners show values (default 1 and 5), arrows work, anchors show full `left`/`right`.
1. Select **Token** — separator and token number spinner.
1. Select **Path → Ancestor Folder** — Level spinner.
1. Confirm dialog does not resize awkwardly; height grows when Substring/Token sections appear.

Optional: add a headless UI test only if control bounds/visibility can be asserted meaningfully (see `.cursor/rules/ui-headless-tests.mdc`).

## Out of scope

- VM / filter engine changes.
- ID3v2 frame pickers, defaults reset, help `?`.
- Filter Configuration pane changes.

## Suggested first edit

In `FilterOptionsDialog.axaml`:

1. Bump `filter-options-spinner` to **88px** (or **96px**).
1. Add `from the` `TextBlock` between spinner and anchor in the Substring grid.
1. Align label columns to **88px** everywhere.
1. If spinners are still broken, try a minimal `ButtonSpinner` width-only template override — not button height overrides.
