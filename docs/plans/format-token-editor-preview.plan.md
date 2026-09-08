---
name: Format token editor Preview
overview: "Add an MFR7-style Preview band to FormatTokenEditorDialog: cycle Rename List items, show sample full name, and live-evaluate the resulting token string against that item."
todos:
  - id: eval-api
    content: Add FormatStringSyntax.TryEvaluate + unit tests
    status: completed
  - id: preview-vm
    content: FormatTokenPreviewViewModel (cycle + refresh from ResultingFormatString)
    status: completed
  - id: dialog-ui
    content: Preview band on FormatTokenEditorDialog; wire PropertyChanged refresh
    status: completed
  - id: open-path
    content: FormatEditor._EditSpanAsync passes Rename List items from MainWindowViewModel
    status: completed
  - id: tests-docs
    content: VM + headless dialog tests; write docs/plans/format-token-editor-preview.plan.md
    status: completed
isProject: false
---

# Format token editor Preview (MFR7 parity)

## Goal

Add the missing **Preview** section to [`FormatTokenEditorDialog`](../../Mfr.App.Ui/Views/FormatEditor/FormatTokenEditorDialog.axaml) so every parameterized token editor (all 11 registry types) shows sample input → evaluated output while options change — matching MFR7 [`FpEditor`](D:/Devl/mfr7/Core/FiltersBase/Format/FpEditor.cs).

## Behavior

- **Shared** for every token that opens this dialog.
- Sample = rename-list item **full file name** (`Prefix + Extension`), read-only.
- ▲ / ▼ + **1-based** index label; start at index 0.
- Output = evaluate **only** the token under edit (`ResultingFormatString`).
- Empty list: `"<Rename list is empty>"` / `"<Preview N/A>"`, buttons disabled.
- Eval failure: `"ERROR: " + message`.
- Fresh compile per refresh → counter shows **start** value.

## Shape

- [`FormatStringSyntax.TryEvaluate`](../../Mfr.Filters/Formatting/FormatString/FormatStringSyntax.cs) — public compile+invoke wrapper (compiler stays internal).
- [`FormatTokenPreviewViewModel`](../../Mfr.App.Ui/ViewModels/FormatEditor/FormatTokenPreviewViewModel.cs) — sample/result/index + Previous/Next.
- Dialog order: config → Preview → resulting format string → OK/Cancel.
- [`FormatEditor._EditSpanAsync`](../../Mfr.App.Ui/Views/FormatEditor/FormatEditor.axaml.cs) passes Rename List items from `MainWindowViewModel` when available.

## Tests

- [`FormatStringSyntaxTests`](../../Mfr.Tests/Models/Filters/Formatting/FormatString/FormatStringSyntaxTests.cs) — `TryEvaluate` happy/error paths.
- [`FormatTokenPreviewViewModelTests`](../../Mfr.Tests/Ui/FormatEditor/FormatTokenPreviewViewModelTests.cs) — empty list, cycle, refresh, ERROR prefix.
- [`FormatTokenEditorDialogPreviewTests`](../../Mfr.Tests/Ui/FormatEditor/FormatTokenEditorDialogPreviewTests.cs) — headless Preview chrome + live eval/cycle.

## Out of scope

- Editable sample text / free-typed paths.
- Evaluating the full parent format string.
- Global ApplyContext / mutating live rename-list selection.
- Changing Rename List Auto-Preview outside the dialog.
