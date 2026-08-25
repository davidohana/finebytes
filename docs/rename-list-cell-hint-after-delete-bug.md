# Handoff: Rename List status-bar hint jumps after Delete

**Status:** Fixed (Aug 2026).

**Fix:** After remove, freeze the status-bar hint to a snapshot of the new selection and last hovered column. Ignore pointer hit-test and grid selection events until the mouse moves ≥8px or the user clicks a cell. Suppress row `:pointerover` styling while frozen (`hint-frozen` class). Restore vertical scroll offset after layout so the viewport does not jump under a stationary cursor. `RenameListViewModel.SelectedEntriesRemoving` fires before any `RemoveAt`.

Regression: `Mfr.Tests/Ui/RenameListViewHintTests.cs`.

**Goal:** After pressing **Del** on a Rename List row, the status-bar cell hint should show the **new selection** (row at same index that slid up). It must not jump to another row—especially the **last visible row in the viewport** when the list is scrolled and the mouse stays on the deleted row’s screen position.

______________________________________________________________________

## Symptom

- Works: Grid **selection** after Delete (same index, MFR7 parity)
  - Breaks: Status-bar **hover hint** (`CellStatusHintDisplay`)
- Works: Hint when list is **not** scrolled
  - Breaks: Hint when a **vertical scrollbar** is present
- Works: Hint when mouse is **not** over the deleted row
  - Breaks: Hint when cursor **stays** on the deleted row’s screen position

**Observed wrong behavior:** Hint text changes to match the **bottom visible item on screen** (virtualized DataGrid recycle), not the newly selected row and not necessarily the row under the cursor.

**Not the issue:** Initial report confused selection jump with hint jump; selection behavior is fixed in `RenameListViewModel.RemoveSelected`.

______________________________________________________________________

## Reproduction

1. Add enough Rename List entries that the grid shows a vertical scrollbar (20+ rows; `RowHeight="20"`).
1. Scroll so the target row is somewhere in the middle of the viewport.
1. Hover a cell — status bar shows `**Column**: value` (rich text via `StatusHintView`).
1. With the mouse **still over that row**, press **Del** (grid focused).
1. **Expected:** Hint updates to the row that slid into the deleted index (same as new selection).
1. **Actual:** Hint jumps to the **bottom visible row** on screen (or otherwise wrong row). User reports this persists after all fixes below.

Also try: toolbar Remove, Ctrl+Shift+R — focus whether Del-only or all remove paths.

______________________________________________________________________

## Architecture (hint pipeline)

```text
RenameListView (pointer / selection events)
  → RenameListViewModel.CellStatusHintDisplay
  → MainWindowViewModel merges with transient errors
  → StatusHintView (TextBlock inlines)
```

- **`Mfr.App.Ui/Views/RenameList/RenameListView.axaml.cs`** — **All hint event handling** — primary bug surface
- **`Mfr.App.Ui/ViewModels/RenameList/RenameListViewModel.cs`**
  - `RemoveSelected`, `SetSelectedEntries`, `CellStatusHintDisplay`
- **`Mfr.App.Ui/ViewModels/RenameList/RenameListCellHint.cs`** — Column → cell text formatting
- **`Mfr.App.Ui/ViewModels/MainWindowViewModel.cs`** — `_paneStatusHintDisplay` merge
- **`Mfr.App.Ui/Views/StatusHintView.axaml.cs`** — Renders `StatusHintRun` segments
- **`Mfr.App.Ui/Views/RenameList/RenameListView.axaml`** — Virtualized `DataGrid` (`ItemsSource="{Binding Entries}"`)

Hint format: **`Column Name: value`** with bold column (`RenameListCellHint.FormatParts`).

______________________________________________________________________

## Correct delete + selection behavior (already implemented)

`RenameListViewModel.RemoveSelected` (lines ~155–207):

- Removes selected rows from `Entries` and engine.
- Keeps selection at **same index** via `_FindFirstSelectedIndex` + `_SelectEntryAfterRemove`.
- Calls `SetSelectedEntries([nextSelection])`.

Tests in `Mfr.Tests/Ui/RenameListViewModelTests.cs`:

- `RemoveSelected_Selects_Row_At_Same_Index`
- `RemoveSelected_Removes_Only_Selected_Rows`
- `RemoveSelected_Preserves_Remaining_Entry_Identity`

**No automated test** for post-delete status hint with virtualized grid + scroll.

______________________________________________________________________

## Current mitigation code (still insufficient)

All in `RenameListView.axaml.cs`:

### `_preferSelectionHint` + pointer anchor lock

After delete, pointer-based hint updates should be suppressed until the mouse moves ≥2px from anchor.

- `_BeginSelectionHintLock()` — set on Del key, `Entries` Remove/Reset, `SelectedEntries` PropertyChanged.
- `_IsPointerHintLocked(position)` — compares to `_pointerHintAnchor`.
- `_EndSelectionHintLock()` — on cell click or after real pointer move.

### Event handlers

| Event                           | Behavior while locked                                                |
| ------------------------------- | -------------------------------------------------------------------- |
| `PointerMoved` (tunnel on grid) | Re-publish selection hint; skip hit-test                             |
| `PointerExited`                 | **Ignored** while locked (spurious recycle exits)                    |
| `CurrentCellChanged`            | Ignored while locked                                                 |
| `SelectionChanged`              | Ignored while locked; also ignores stale selection (removed entries) |
| `_PublishCellHint`              | Forces `_ReadFocusedEntry()` from VM when locked                     |

### Deferred refresh

`_ScheduleSelectionHintRefresh()` posts `_PublishFocusedCellHint` at `Loaded`, then again at `Background` priority.

### Hit testing

`_HitTestRowAt(position)` uses `RenameGrid.InputHitTest(position)` instead of `e.Source`.

______________________________________________________________________

## Leading hypotheses (for next agent)

1. **Wrong entry despite lock** — `_ReadFocusedEntry()` may still fall through to `RenameGrid.SelectedItem` if VM selection is briefly empty or stale during recycle; bottom row may be grid’s transient `SelectedItem`.

1. **Lock cleared unexpectedly** — `_EndSelectionHintLock()` still runs from `_OnCellPointerPressed` or pointer move ≥2px from layout jitter (not just user move). Anchor uses `_pointerHintAnchor ??= _lastPointerPositionOverGrid`; if anchor is null/wrong, behavior differs.

1. **`SelectedEntries` PropertyChanged always locks** — `_OnViewModelPropertyChanged` calls `_BeginSelectionHintLock()` on **every** selection change, not only delete. May interact badly with normal selection sync.

1. **Grid fires events before lock** — If Avalonia DataGrid handles `CollectionChanged` before the view’s handler, `SelectionChanged` might push wrong grid selection into VM before `_preferSelectionHint` is set (toolbar path; Del path sets lock _before_ `Execute`).

1. **Another publisher** — Something outside `RenameListView` may set `CellStatusHintDisplay` (grep shows only ViewModel default clear on `Clear()` and this view).

1. **`RenameGrid.CurrentColumn` stale** — Hint uses `CurrentColumn ?? _lastHintColumn` for column; entry is forced from selection when locked, but if **entry** is wrong the column doesn’t explain “bottom row” symptom unless entry itself is wrong.

1. **Virtualization + scroll offset** — After remove, scroll position may shift; hit-test at fixed screen Y resolves to last slot in viewport. Lock should prevent hit-test; if lock fails, this explains “bottom item on screen.”

1. **Double PropertyChanged / race** — `RemoveSelected` → `SetSelectedEntries` → PropertyChanged schedules refresh; Del handler also schedules refresh. Order vs grid layout pass may still allow a late pointer/grid event to win.

______________________________________________________________________

## Suggested approaches (not yet tried)

### A. Simpler model: freeze hint on delete

On delete while pointer over grid:

1. Capture **entry + column** from VM selection and `_lastHintColumn` immediately after `RemoveSelected`.
1. Set hint once from that snapshot.
1. Ignore **all** pointer/grid hint updates until `PointerMoved` exceeds a **larger** threshold (e.g. 8px) **or** explicit cell click.
1. Do not use hit-test at all during freeze.

### B. Clear hint on delete when mouse was on deleted row

Minimal UX: set `CellStatusHintDisplay = Empty` on delete; restore on next intentional hover. Avoids wrong row entirely.

### C. Index-based hint instead of hit-test

Map pointer Y → item index via scroll offset + row height (fragile but bypasses recycled `DataGridRow` DataContext).

### D. Suppress grid → VM selection sync during entire remove transaction

ViewModel callback or flag `_isRemoving` from first `RemoveAt` through deferred layout; block `_OnSelectionChanged` entirely (not only when `_preferSelectionHint`).

### E. Headless UI test

Extend `Mfr.Tests/Ui/` pattern (`RenameListViewDropTests._Show`):

- Populate many rows, scroll grid, simulate Del, assert `CellStatusHintDisplay` matches expected entry/column.
- May require Avalonia headless input simulation or direct view method calls.

### F. Debug logging (temporary)

Log on every `_PublishCellHint`: source (pointer/selection/scheduled), entry path, `_preferSelectionHint`, lock state, `SelectedEntries` count, grid `SelectedItem`, hit-test result. User can capture one delete sequence.

______________________________________________________________________

## Key code references

**Del handler + lock start:**

```csharp
// RenameListView.axaml.cs ~121-128
_BeginSelectionHintLock();
_viewModel.RemoveSelectedCommand.Execute(null);
_ScheduleSelectionHintRefresh();
```

**Publish with lock override:**

```csharp
// ~247-250
if (_preferSelectionHint)
{
    entry = _ReadFocusedEntry();
}
```

**Read focused entry (VM first, grid fallback):**

```csharp
// ~224-237
var selected = _viewModel?.SelectedEntries;
if (selected is { Count: > 0 })
    return selected[^1];
if (RenameGrid.SelectedItem is RenameListEntry entry)
    return entry;
```

**Remove + reselect:**

```csharp
// RenameListViewModel.cs ~155-176
var anchorIndex = _FindFirstSelectedIndex(selected);
// ... remove from Entries ...
var nextSelection = _SelectEntryAfterRemove(anchorIndex);
SetSelectedEntries(nextSelection is null ? [] : [nextSelection]);
```

______________________________________________________________________

## Tests to run after fix

```bash
just build
dotnet test .\Mfr.Tests\Mfr.Tests.csproj --filter "FullyQualifiedName~RenameList"
```

Manual checklist:

- [ ] Scrolled list, mouse on deleted row, Del
- [ ] Scrolled list, mouse elsewhere, Del
- [ ] Unscrolled list, Del
- [ ] Toolbar remove vs Del
- [ ] After fix: move mouse — hover hint resumes on correct row
- [ ] Click different cell after delete — hint follows click

______________________________________________________________________

## Related docs / constraints

- `docs/keyboard-shortcuts.md` — Del remove, F4 locate
- `AGENTS.md` — `just format`, CSharpier, no legacy compat shims
- `.cursor/rules/refactor-no-legacy-compat.mdc` — prefer clean fix over compatibility layers
- Prior chat: [Rename List Phase 2 / hint jump fixes](agent-transcripts) — search “hover”, “bottom item”, “scrollbar”

______________________________________________________________________

## Agent checklist

1. Reproduce locally with `just run-ui` (scroll + Del + mouse on row).
1. Add temporary logging or a failing headless test to capture the bad publish path.
1. Prefer a **simple, deterministic** model (snapshot or clear-on-delete) over more event-order guards unless necessary.
1. Add regression test if feasible in `Mfr.Tests/Ui/`.
1. Run `just format` before commit.
