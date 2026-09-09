# Visual Trim Helper

Port MFR7’s Visual Trim Helper into Trim/Extract Left/Right and Trim Between Filter Configuration editors.

## Status

Implemented: shared helper control, Count + Trim Between wiring, Rename List sample init/drop, mapping/unit/headless tests.

## Behavior (MFR7 parity)

- Read-only sample text box labeled **Visual Trim Helper:**; placeholder `[ Drag Item Here ]`.
- **Init:** first Rename List item’s Apply Target string via `FilterTargetText` on **Original**.
- **Drop:** Rename List item only (resolve via Apply Target). File List / Explorer / plain text drops are rejected.
- **Selection → options** on pointer release:
  - Left Trim / Left Extract → force `[0, end)` → `Count`
  - Right Trim / Right Extract → force `[start, N)` → `Count`
  - Trim Between → 1-based inclusive **Left**-anchored `Position`s (no MFR7 “count from the right” checkbox)
- **Options → selection:** spinner/combo re-highlights the sample.

## Key files

- `Mfr.Models/Filters/FilterTargetText.cs`
- `Mfr.Filters/Trimming/TrimBetweenFilter.cs` (`TryGetSelectionRange` / `TryGetPositionsFromSelection`)
- `Mfr.App.Ui/ViewModels/FilterEditors/Trimming/VisualTrimHelperMapping.cs`
- `Mfr.App.Ui/ViewModels/FilterEditors/Trimming/VisualTrimHelperViewModel.cs`
- `Mfr.App.Ui/Views/Controls/VisualTrimHelperView.*`
- `Mfr.App.Ui/Views/RenameList/RenameListSampleDragPayload.cs`
- Count / Trim Between editor VM + AXAML
- `FilterEditorViewModel.SetSampleRenameItemSource` + `MainWindowViewModel` wiring

## Out of scope

- DateTime / Time Shifter pickers
- Apply Scope substring in the helper
- Shrink Duplicate / space trims
