---
title: Rename List Phase 6b follow-ups
description: Leftovers after original field-load errors. 6c–6e shipped; next is Phase 8a Refresh.
---

# Rename List Phase 6b follow-ups

Handover from the Phase 6b review. **6b is shipped:** row-level TagLib + image exceptions,
gray `"Error"` cells, Show Load Errors dialog, status-bar `[Field value error]` hint,
`ErrorsLast` sort.

These phases are **6b leftovers**, not preview work. **6c–6e are shipped.** **8a Refresh**
can start (Load Errors type names are in place).

Do **not** go back to per-field stored exceptions or format-specific user-message parsers.
Keep two slots on `RenameItem` (`TagLibMetadataLoadError`, `ImagePropertiesLoadError`).

## Suggested order

1. ~~**6c** TagLib sibling load flags (loader only).~~
1. ~~**6d** Structured gray (grid paint; no `"Error"` text compare).~~
1. ~~**6e** Rename `FieldError` → `LoadErrors` (names match the menu).~~
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

**Shipped.** Cells are **`DataGridTemplateColumn`** + `FuncDataTemplate<RenameListEntry>`.
Text comes from `GetFieldText`; gray from `IsFieldLoadError` (not `"Error"` text).
`DataContextChanged` re-applies both when DataGrid recycles the row. Unused
`RenameListFieldTextConverter` and the `LoadingRow` text-sentinel listeners are gone.
Brush helper is [`RenameListLoadErrorForeground`](../Mfr.App.Ui/ViewModels/RenameList/RenameListLoadErrorForeground.cs).

**Tests:** `Grid_cells_show_basic_field_text`; gray Title + not-gray Full File Name on the
same failed-metadata row; a literal Title of `"Error"` without a stored load exception is
not gray.

## 6e — `FieldError` → `LoadErrors` names

**Shipped.** Menu, commands, and types say Load Errors. Cell word stays
`FieldLoadErrorText = "Error"`. Catalog wrappers stay public (UI has no `InternalsVisibleTo` on
Models): `HasLoadError` / `HasAnyLoadError` / `ListLoadErrors` / `DescribeLoadError`.
`_NotifyShowLoadErrorsChanged` stays inlined next to `ShowLoadErrors`.

## What not to do

- Per-field exception maps; one TagLib + one image slot is enough
- Format-specific `DescribeUserMessage` branches (playlist vs jpeg vs …)
- Preview-error UI (**8c**) or missing-on-disk gray (**Phase 10**) — Phase 10 should reuse
  6d’s structured cell foreground, not a second text sentinel
