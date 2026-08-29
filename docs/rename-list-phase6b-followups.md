---
title: Rename List Phase 6b follow-ups
description: Leftovers after original field-load errors. Do 6c–6e before Phase 8a Refresh.
---

# Rename List Phase 6b follow-ups

Handover from the Phase 6b review. **6b is shipped:** row-level TagLib + image exceptions,
gray `"Error"` cells, Show Load Errors dialog, status-bar `[Field value error]` hint,
`ErrorsLast` sort.

These phases are **6b leftovers**, not preview work. **8a Refresh** should wait until **6d**
so Refresh does not inherit the text-sentinel gray path.

Do **not** go back to per-field stored exceptions or format-specific user-message parsers.
Keep two slots on `RenameItem` (`TagLibMetadataLoadError`, `ImagePropertiesLoadError`).

## Suggested order

1. **6c** TagLib sibling load flags (loader only).
1. **6d** Structured gray (grid paint; no `"Error"` text compare).
1. **6e** Rename `FieldError` → `LoadErrors` (names match the menu).
1. Then **8a** Original Refresh.

## 6c — TagLib sibling attempted flags

**Shipped as one `TagLibLoadAttempted` flag** on `RenameItem` (not two sibling booleans). Tags and
media share one `TagLibFileReader.Read`; `EnsureTagLibLoaded` fills both buckets. A failed tags load
does not retry the same Read for media on the same hydrate. `HasLoadError` for audio and media columns
already shares the one TagLib exception.

**Test:** after `TryEnsureLoaded` with only an audio key on a missing file, `TagLibLoadAttempted` is
true and the media requirement is satisfied (media key need not be requested).

**Also:** delete
`RenameListMetadataLoadErrorsTests.ImageProperties_flag_does_not_include_audio_or_media`
(asserts enum bit values only).

**Out of scope:** image catch (no TagLib sibling). Audio, media, and MPEG columns share
`RenameListMetadataRequirement.TagLib`; image/EXIF stays `ImageProperties`.

## 6d — Structured gray (no display-text sentinel)

[`ApplyFromCellText`](../Mfr.App.Ui/ViewModels/RenameList/RenameListFieldForegroundConverter.cs)
grays any `TextBlock` whose text is `"Error"`. Sort already uses
`RenameListMetadataLoadErrors.HasLoadError`; a real Title of `"Error"` would still paint gray.
`LoadingRow` + `TextProperty` listeners exist because Avalonia `DataGridCell` has no `Column`.

### Approach

Use **`DataGridTemplateColumn`** with `FuncDataTemplate<RenameListEntry>` and the **`entry`
argument** (not an empty `{Binding}` — that was the blank-cell bug).

For each cell:

- `Text = entry?.GetFieldText(key) ?? ""`
- If `entry?.IsFieldLoadError(key) == true`, set `Foreground` to the gray brush
- Otherwise **do not set `Foreground`** (do **not** use `Brushes.Transparent` — that made
  basic columns invisible)

Keep header templates, widths, `CanUserSort`, and `RenameListGridColumns.SetFieldKey` as
today.

### Delete after paint works

- `_ApplyFieldLoadErrorForeground` / `_WireErrorForeground` /
  `_OnCellTextForegroundPropertyChanged` in
  [`RenameListView.axaml.cs`](../Mfr.App.Ui/Views/RenameList/RenameListView.axaml.cs)
- `ApplyFromCellText` and the `"mfr-error-fg"` once-flag
- Tests that only exist to lock in text-sentinel paint
  (`ApplyFromCellText_clears_gray_when_text_is_not_Error`)

### Tests that must keep passing

- [`Grid_cells_show_basic_field_text`](../Mfr.Tests/Ui/RenameList/RenameListViewColumnTests.cs)
- `Grid_shows_basic_text_and_Error_for_metadata_failure_on_same_row` — gray on the **Title**
  cell, not Full File Name; Full File Name not gray

**Add:** a Title value of `"Error"` without a stored load exception is **not** gray (same
fixture idea as `CompareForSort_does_not_treat_literal_Error_tag_value_as_load_failure`).

**If templates still break autofit/sort:** stop and fix the template; do not restore
text-sentinel paint.

Name any remaining brush helper for **load-error foreground**, not `*Converter` (it is not
an `IValueConverter`).

## 6e — `FieldError` → `LoadErrors` names

Menu is already **Show Load Errors**. Types and commands still say Field Error. Mechanical
rename after 6d so grid paint types are not renamed twice.

| Current                                                                                                 | Target                                                                      |
| ------------------------------------------------------------------------------------------------------- | --------------------------------------------------------------------------- |
| `ShowFieldError` / `CanShowFieldError` / `ShowFieldErrorCommand`                                        | `ShowLoadErrors` / `CanShowLoadErrors` / `ShowLoadErrorsCommand`            |
| `FieldErrorDialogRequested`                                                                             | `LoadErrorsDialogRequested`                                                 |
| `RenameListViewModel.FieldError.cs`                                                                     | `RenameListViewModel.LoadErrors.cs`                                         |
| `RenameListFieldErrorDialog` + `.axaml`                                                                 | `RenameListLoadErrorsDialog`                                                |
| `RenameListFieldErrorDialogContent`                                                                     | `RenameListLoadErrorsDialogContent`                                         |
| `RenameListFieldErrorDisplay`                                                                           | `RenameListLoadErrorDisplay`                                                |
| Catalog `HasFieldLoadError` / `HasAnyFieldLoadError` / `ListFieldLoadErrors` / `DescribeFieldLoadError` | `HasLoadError` / `HasAnyLoadError` / `ListLoadErrors` / `DescribeLoadError` |
| `RenameListCellHint.FormatFieldError`                                                                   | `FormatLoadError`                                                           |
| Matching test class/file names                                                                          | same stems                                                                  |

Keep `FieldLoadErrorText = "Error"` (that is the **cell word**, not the command).

Keep `_NotifyShowFieldErrorChanged` inlined under the new command name.

No behavior change. Catalog wrappers stay public (UI has no `InternalsVisibleTo` on Models).

## What not to do

- Per-field exception maps; one TagLib + one image slot is enough
- Format-specific `DescribeUserMessage` branches (playlist vs jpeg vs …)
- Preview-error UI (**8c**) or missing-on-disk gray (**Phase 10**) — Phase 10 should reuse
  6d’s structured cell foreground, not a second text sentinel
- Starting **8a** while gray still compares cell text to `"Error"`
