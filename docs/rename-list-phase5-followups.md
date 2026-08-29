---
title: Rename List Phase 5 follow-ups
description: Remaining shuttle/grid hygiene after Phase 5–7. OrderedDraft and field-key Auto-Sort are done.
---

# Rename List Phase 5 follow-ups

Handover from the Phase 5 (unified field shuttle + dynamic columns) review. Phase 5–7 are
shipped. Remaining items are UI hygiene, not Auto-Sort work.

Do **not** preserve leftover flyout APIs or dual label tables if those return — Phase 5
cleanup already made the catalog the source of truth for display names.

## 1. ~~Shared `OrderedDraft` for the shuttle~~ (done)

Extracted to [`OrderedDraft.cs`](../Mfr.App.Ui/ViewModels/RenameList/OrderedDraft.cs). Columns and
sort keys are independent drafts keyed by `RenameListFieldKey`. Tests:
[`OrderedDraftTests.cs`](../Mfr.Tests/Ui/RenameList/OrderedDraftTests.cs).

## 2. Preview glyph styles in one place

[`RenameListView.axaml`](../Mfr.App.Ui/Views/RenameList/RenameListView.axaml) and
[`RenameListFieldShuttleDialog.axaml`](../Mfr.App.Ui/Views/RenameList/RenameListFieldShuttleDialog.axaml)
both define `Border.rename-list-preview-glyph` (and the inner `TextBlock`). The control
factory is already shared ([`RenameListPreviewGlyph`](../Mfr.App.Ui/Views/RenameList/RenameListPreviewGlyph.cs));
the brushes are not.

Promote the two selectors to an app-level resource dictionary (same pattern as File List
sort-glyph brushes). Shuttle AXAML still inlines `<TextBlock Text="P" />` instead of the
factory; after the style move, either keep the class name on a `Border` or call
`RenameListPreviewGlyph.Create()` from a template converter. Do this when a third surface
needs the badge or when the badge look changes.

## 3. ~~`IsSortable` vs `SortColumn`~~ (done in Phase 7d)

Resolved in Phase 7d: Auto-Sort keys are original `RenameListFieldKey` values; `IsSortable` is the sole gate (preview columns never sort). The fixed `RenameListSortColumn` enum and per-field `SortColumn` mapping were removed.

## 4. `RenameListEntry` convenience properties

[`RenameListEntry`](../Mfr.App.Ui/ViewModels/RenameList/RenameListEntry.cs) still exposes
`FileFolder`, `ParentFolder`, `FullFileName`, `FullPath`, and `FullFileNamePreview`. Those
are `GetFieldText` over fixed keys. The grid paints cells from `GetFieldText` (and
`IsFieldLoadError` for gray); the named properties exist for tests and a few call sites.

After Phase 6 preview coloring, prefer `GetFieldText(key)` (or engine item snapshots) in
new code. Delete the named properties when
[`RenameListViewModelTests`](../Mfr.Tests/Ui/RenameList/RenameListViewModelTests.cs) and
hint tests no longer use `entry.FullFileName` as the row identity.

## 5. Headless test fixture for remaining Rename List suites

[`RenameListUiTestContext`](../Mfr.Tests/Ui/RenameList/RenameListTestHelpers.cs) already
owns temp dirs, File List hosts, add-policy pinning, and `ShowWithRowsAsync`. Still
duplicated:

- [`RenameListViewDropTests`](../Mfr.Tests/Ui/RenameList/RenameListViewDropTests.cs) —
  same config snapshot + File List factory; window size differs (`_Show` is 600×300).
- [`RenameListViewModelTests`](../Mfr.Tests/Ui/RenameList/RenameListViewModelTests.cs) —
  same snapshot via `RenameListTestHelpers.SnapshotSessionUi`; many private folder/file helpers stay local.

When those files are next edited, construct `new RenameListUiTestContext(pinAddPolicy: true)`
and keep scenario helpers (`_CreateSampleFolder`, `_FileEntry`) on the test class. Extend
the context with an optional window size rather than a second `_Show`.

## Suggested order

1. Preview glyph resources when a third UI needs the badge or the look changes.
1. Test fixture when touching drop or view-model test setup.
1. Entry convenience properties last (call-site grind, no behavior change).
