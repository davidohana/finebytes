# Field shuttle search

## Decisions (locked)

- **Scope:** Global when query non-empty — search `RenameListFieldCatalog.All` (still apply already-selected / preview / sortable gates). Empty or whitespace-only keeps current **Groups + `_FieldsInSelectedGroup()`** browse.
- **Match (OR, `OrdinalIgnoreCase`, after `Trim`):** `DisplayName`, `PropertyKey`, `GroupDisplayName`, `GroupId`, and `Tip` when non-null — same policy as token picker’s four-field OR.
- **One `SearchText`** on the dialog VM shared by Columns and Sort tabs.
- **Keep search after Add** (shuttle is multi-add; do not clear like token insert).
- **While searching:** disable Groups lists (`IsEnabled` bound to `!IsFieldSearchActive`); show a muted `GroupDisplayName` subtitle under each available row (token flat-mode subtitle).
- **No shared catalog-search helper** yet (rule of three: token picker + filter palette only).

## MFR7 reference brief

- **Sources:** `FieldSelector` / `SortFieldSelector` (`D:\Devl\mfr7\Core\MFRGui\Forms\RenameList\`); help `fieldselector.html` / `sorteditor.html`.
- **Behavior / UX:** Group combo/list only; Original vs Preview tabs; no text search.
- **Parity:** Finebytes **enhancement** beyond MFR7 (mirrors existing format-token search, not legacy Field Selector).

## Non-goals

- TreeView / collapsing Groups into a token-style tree.
- Searching the Selected columns/sort lists.
- Persisting search text or clearing it on OK/Cancel beyond dialog lifetime.
- Extracting a shared search utility.

## Approach

Hook in `_RefreshLists` in `RenameListFieldShuttleDialogViewModel`: resolve candidate fields from group or catalog+match, then existing list builders. `OnSearchTextChanged` → `_RefreshLists()` + raise `IsFieldSearchActive`.

**UI:** Search `TextBox` above the available-fields column on both tabs (`PlaceholderText="Search fields…"`, classes like token search / `filter-editor-field`). Item templates gain a subtitle `TextBlock` for `GroupDisplayName` visible when `IsFieldSearchActive` (parent VM binding; keep `Available*` as `RenameListField`).

**Reference:** `FormatTokenPickerViewModel` `_RefreshVisibleItems` / `_BuildFlatFilteredItems`.

## Phases

### P1 — VM filter + tests

- [x] Add `SearchText` / `IsFieldSearchActive` to `RenameListFieldShuttleDialogViewModel`; wire into `_RefreshLists`.
- [x] Tests in `RenameListFieldShuttleDialogViewModelTests.cs`: empty keeps group scope; whitespace = empty; filters flat across groups; matches group name / property key; already-selected keys still excluded; Sort list respects `IsSortable`.

**Exit:** VM-only behavior correct; no AXAML yet (or minimal if needed to compile).

### P2 — AXAML + help + light headless

- [ ] Search boxes on Columns + Sort; disable Groups while searching; group subtitles on available rows.
- [ ] Optional: focus search when dialog opens only if cheap in existing dialog open path.
- [ ] Update `help/ui/fieldselector.html` (+ whatsnew if that page tracks UI tweaks).
- [ ] One headless assert in `RenameListFieldShuttleDialogTests.cs` that setting search refreshes the available list.

**Exit:** User can type in the shuttle and find fields across groups; help mentions search.

## Key files

- `Mfr.App.Ui/ViewModels/RenameList/RenameListFieldShuttleDialogViewModel.cs`
- `Mfr.App.Ui/Views/RenameList/RenameListFieldShuttleDialog.axaml`
- `Mfr.Tests/Ui/RenameList/RenameListFieldShuttleDialogViewModelTests.cs`
- `help/ui/fieldselector.html`
