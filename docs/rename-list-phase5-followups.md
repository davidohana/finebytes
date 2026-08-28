---
title: Rename List Phase 5 follow-ups
description: Deferred refactors after the field shuttle and dynamic columns. Do these when Phase 7 grows groups, or when the shuttle VM is next touched.
---

# Rename List Phase 5 follow-ups

Handover from the Phase 5 (unified field shuttle + dynamic columns) review. Phase 5 is
shipped; these items were skipped as YAGNI for a single File Name group. They become worth
the churn when Phase 7 adds Dates / attributes / audio / image groups, or when the shuttle
dialog is edited for another reason.

Do **not** preserve leftover flyout APIs or dual label tables if those return — Phase 5
cleanup already made the catalog the source of truth for display names.

## 1. Shared `OrderedDraft` for the shuttle

**Do this first** if the shuttle view model is opened for Phase 7.

### Motivation

[`RenameListFieldShuttleDialogViewModel`](../Mfr.App.Ui/ViewModels/RenameList/RenameListFieldShuttleDialogViewModel.cs)
hosts two selected lists that are the same state machine:

| Behavior                              | Columns                                              | Sort                                      |
| ------------------------------------- | ---------------------------------------------------- | ----------------------------------------- |
| Storage                               | `_draftColumns` + `_selectedColumnKeys`              | `_draftSortKeys` + `_selectedSortColumns` |
| Add (skip if key exists, select last) | `_AddColumn`                                         | `_AddSortKey`                             |
| Remove selected + clamp index         | `RemoveSelectedColumn`                               | `RemoveSelectedSortKey`                   |
| Move up / down                        | `MoveSelectedColumn*` + `_SwapColumns`               | `MoveSelectedSortKey*` + `_SwapSortKeys`  |
| Clear                                 | `ClearSelectedColumns`                               | `ClearSelectedSortKeys`                   |
| CanExecute                            | `_CanRemove` / `_CanMove*Up/Down` / `_HasSelected*`  | same for sort                             |
| Index helpers                         | `_TryGetSelectedColumnIndex`, `_ClampSelectionIndex` | `_TryGetSelectedSortIndex`, same clamp    |

That is roughly 120–150 lines of twins. One File Name group is tolerable. Phase 7 will add
more catalog groups, more available-list filters, and more NotifyCanExecute noise in
`_RefreshLists`. Leaving two draft machines in the same file makes that growth harder to
read without buying any extra behavior.

The two lists are independent on purpose (MFR7 FieldSelector vs SortFieldSelector). The
helper must not couple them: clearing sort keys must not touch columns, and `CanConfirm`
stays “at least one visible column” even when sort is empty (Auto-Sort off).

### What to extract

A small UI helper next to the shuttle VM, not a Models type. Suggested shape:

```csharp
internal sealed class OrderedDraft<TKey, TItem>
    where TKey : notnull
{
    public OrderedDraft(IEnumerable<TItem> items, Func<TItem, TKey> keyOf);

    public IReadOnlyList<TItem> Items { get; }
    public int SelectedIndex { get; set; } // -1 when empty
    public bool Contains(TKey key);

    public bool TryAdd(TItem item); // false if key already present; selects the new last item
    public bool TryRemoveSelected();
    public bool TryMoveSelected(int offset); // -1 up, +1 down
    public void Clear();

    public bool CanRemove { get; }
    public bool CanMoveUp { get; }
    public bool CanMoveDown { get; }
    public bool HasItems { get; }
}
```

Clamp-after-remove stays inside `TryRemoveSelected` (same rules as today’s
`_ClampSelectionIndex`: empty → `-1`, otherwise keep the index if it still lands on an
item, else last item).

`TryAdd` / `TryMove` / `Clear` only mutate. The view model still owns `_RefreshLists()`
(rebuild available lists, row wrappers, `OnPropertyChanged`, command notify). That keeps
Add-all cheap: loop `TryAdd` with no refresh, then one `_RefreshLists()`.

### What stays in the view model

Do **not** genericize these; they are tab-specific:

- Available lists: original vs preview vs sort filters (`SupportsPreview`,
  `IsSortable && SortColumn`).
- Add All on Columns (original tab vs preview tab).
- Sort-only `ToggleSelectedSortDirection` (mutates `Descending` on the selected
  `RenameListSortKey`).
- `CanConfirm` → `_columns.HasItems` (sort may be empty).
- Row wrappers: `RenameListFieldShuttleColumnRow` / `RenameListFieldShuttleSortRow` still
  built from `draft.Items` in `_RefreshLists`.
- RelayCommands: keep as thin wrappers (`RemoveSelectedColumn` → `_columns.TryRemoveSelected()`
  then `_RefreshLists()`).

Wire-up sketch:

```csharp
private readonly OrderedDraft<RenameListFieldKey, RenameListVisibleColumn> _columns;
private readonly OrderedDraft<RenameListSortColumn, RenameListSortKey> _sortKeys;

// ctor
_columns = new(visibleColumns, column => column.Key);
_sortKeys = new(sortKeys, key => key.Column);

public IReadOnlyList<RenameListVisibleColumn> ResultColumns => _columns.Items;
public IReadOnlyList<RenameListSortKey> ResultSortKeys => _sortKeys.Items;
public bool CanConfirm => _columns.HasItems;
```

Bind `SelectedColumnRowIndex` / `SelectedSortRowIndex` to each draft’s `SelectedIndex`
(or keep the VM properties and assign through). ListBox `SelectedIndex` two-way must still
update CanExecute for move/remove.

### Tests

Move clamp / duplicate-add / move-at-ends coverage onto `OrderedDraft` unit tests (no
Avalonia). Keep shuttle VM tests as wiring: add original vs preview, sort direction toggle,
`CanConfirm` after clear columns, drafts stay independent.

Existing tests:
[`RenameListFieldShuttleDialogViewModelTests.cs`](../Mfr.Tests/Ui/RenameList/RenameListFieldShuttleDialogViewModelTests.cs).

### Out of scope

- A shared “available fields” list. Original / preview / sort filters are different
  queries over the catalog.
- Putting this in `Mfr.Models`. It is dialog draft state.
- A framework with extra type parameters for “row view model” or “refresh callback”.
  If the helper needs more than key + item + selected index, stop and keep the twins.

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
needs the badge (Phase 7 field pickers) or when the badge look changes.

## 3. ~~`IsSortable` vs `SortColumn`~~ (done in Phase 7d)

Resolved in Phase 7d: Auto-Sort keys are original `RenameListFieldKey` values; `IsSortable` is the sole gate (preview columns never sort). The fixed `RenameListSortColumn` enum and per-field `SortColumn` mapping were removed.

## 4. `RenameListEntry` convenience properties

[`RenameListEntry`](../Mfr.App.Ui/ViewModels/RenameList/RenameListEntry.cs) still exposes
`FileFolder`, `ParentFolder`, `FullFileName`, `FullPath`, and `FullFileNamePreview`. Those
are `GetFieldText` over fixed keys. The grid binds through
`RenameListFieldTextConverter` + catalog keys; the named properties exist for tests and a
few call sites.

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
  same snapshot via `_CloneUiConfig`; many private folder/file helpers stay local.

When those files are next edited, construct `new RenameListUiTestContext(pinAddPolicy: true)`
and keep scenario helpers (`_CreateSampleFolder`, `_FileEntry`) on the test class. Extend
the context with an optional window size rather than a second `_Show`.

## Suggested order

1. `IsSortable` / `SortColumn` — done in Phase 7d (field-key Auto-Sort).
1. `OrderedDraft` when adding Phase 7 groups or otherwise editing the shuttle VM.
1. Preview glyph resources when a third UI needs the badge or the look changes.
1. Test fixture when touching drop or view-model test setup.
1. Entry convenience properties last (call-site grind, no behavior change).
