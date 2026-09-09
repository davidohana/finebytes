---
name: Token editor fieldsets
overview: Add MFR7-style FieldsetGroup borders in FormatTokenEditorDialog to visually separate Options, Preview, and Resulting format string for every token editor at once.
todos:
  - id: wrap-dialog
    content: Wrap Options / Preview / Resulting format string in FieldsetGroups in FormatTokenEditorDialog.axaml; drop redundant Preview/Result labels
    status: completed
  - id: test-guard
    content: Assert three FieldsetGroups in FormatTokenEditorDialogPreviewTests
    status: completed
  - id: plan-doc
    content: Write docs/plans/token-editor-fieldsets.plan.md
    status: completed
isProject: false
---

# Token editor dialog group boxes

## Approach

All specialized token option UIs (`TokenFormatTokenEditorView`, `CounterFormatTokenEditorView`, etc.) are injected into [`FormatTokenEditorDialog.axaml`](../../Mfr.App.Ui/Views/FormatEditor/FormatTokenEditorDialog.axaml) via `FormatTokenEditorViewLocator`. Preview and resulting format string already live in that dialog. Wrap those three shared bands in the existing [`FieldsetGroup`](../../Mfr.App.Ui/Views/Controls/FieldsetGroup.cs) control (same MFR7/WinForms GroupBox look used by filter editors). **Do not** add fieldsets inside each `TokenEditors/*.axaml` — that would duplicate chrome and miss Preview/Result.

## Headers

Match the rest of the app’s fieldset wording (window title already names the token, e.g. “Token”):

- **Options** — token parameter fields
- **Preview** — sample + result + ▲/▼
- **Resulting format string** — read-only format editor

## Changes

In [`FormatTokenEditorDialog.axaml`](../../Mfr.App.Ui/Views/FormatEditor/FormatTokenEditorDialog.axaml):

1. Outer `StackPanel` spacing set to `8`.
1. Options `ContentControl` wrapped in `FieldsetGroup Header="Options"`.
1. `PreviewRow` wrapped in `FieldsetGroup Header="Preview"`; redundant `Preview:` label removed.
1. `FilterEditorLabeledRow` for resulting format string replaced with `FieldsetGroup Header="Resulting format string"`.

Named controls (`PreviewRow`, `PreviewSampleBox`, etc.) kept for existing headless tests.

## Tests

[`FormatTokenEditorDialogPreviewTests`](../../Mfr.Tests/Ui/FormatEditor/FormatTokenEditorDialogPreviewTests.cs) asserts the three fieldset headers.

## Out of scope

- Per-token AXAML redesigns, preview control repositioning to match MFR7’s right-side nav, or renaming dialog title to “Formatting Parameter Editor”.
