---
title: Rename List Phase 6b follow-ups
description: Leftovers after original field-load errors. 6c–6e, 8a Refresh, and missing-on-disk gray shipped.
---

# Rename List Phase 6b follow-ups

Handover from the Phase 6b review. **6b is shipped:** row-level TagLib + image exceptions,
muted em-dash load-error cells, Show Load Errors dialog, status-bar `Could not read metadata:` hint,
`ErrorsLast` sort.

These phases are **6b leftovers**, not preview work. **6c–6e, 8a Original Refresh, and
missing-on-disk gray (pulled forward from Phase 10) are shipped.** Next preview work is **8b**.

Do **not** go back to per-field stored exceptions or format-specific user-message parsers.
Keep two slots on `RenameItem` (`TagLibMetadataLoadError`, `ImagePropertiesLoadError`).

## Suggested order

1. ~~**6c** TagLib sibling load flags (loader only).~~
1. ~~**6d** Structured gray (grid paint; no `"Error"` text compare).~~
1. ~~**6e** Rename `FieldError` → `LoadErrors` (names match the menu).~~
1. ~~**8a** Original Refresh (F5, re-read disk; missing-on-disk gray shipped with it).~~

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
Text comes from `GetFieldText`; load-error styling from `IsLoadError` via the
`rename-list-load-error` cell class (theme muted foreground + italic), not display-text heuristics.
Unused
`RenameListFieldTextConverter` and the `LoadingRow` text-sentinel listeners are gone.

**Tests:** `Grid_cells_show_basic_field_text`; muted dash Title + normal Full File Name on the
same failed-metadata row; a literal Title of `"Error"` without a stored load exception is
not styled as load-error.

## 6e — `FieldError` → `LoadErrors` names

**Shipped.** Menu, commands, and types say Load Errors. Failed cells show
`LoadErrorText` (`—`) with the `rename-list-load-error` class. Grid predicate is
`IsLoadError`. Catalog wrappers stay public (UI has no `InternalsVisibleTo` on
Models): `HasLoadError` / `HasAnyLoadError` / `ListLoadErrors` / `DescribeLoadError`.
`_NotifyShowLoadErrorsChanged` stays inlined next to `ShowLoadErrors`. Focused-cell
state was removed; the command is row-level.

## What not to do

- Per-field exception maps; one TagLib + one image slot is enough
- Format-specific `DescribeUserMessage` branches (playlist vs jpeg vs …)
- Preview-error UI (**8c**). Missing-on-disk gray reuses 6d’s structured cell foreground (not a
  text sentinel). Presence is snapshotted on add/refresh, not probed with live `Exists` on paint.
