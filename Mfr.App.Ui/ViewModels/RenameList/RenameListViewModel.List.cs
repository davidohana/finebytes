using CommunityToolkit.Mvvm.Input;
using Mfr.Filters.Formatting;
using Mfr.Models.RenameList;

namespace Mfr.App.Ui.ViewModels.RenameList
{
    /// <summary>
    /// Remove, clear, move, reorder, and locate commands for <see cref="RenameListViewModel"/>.
    /// </summary>
    public sealed partial class RenameListViewModel
    {
        /// <summary>
        /// Removes the selected Rename List rows.
        /// </summary>
        [RelayCommand(CanExecute = nameof(_CanRemoveSelected))]
        public void RemoveSelected()
        {
            if (_selectedEntries.Count == 0)
            {
                return;
            }

            var selected = _selectedEntries.ToHashSet();
            var anchorIndex = _FindFirstSelectedIndex(selected);
            _renameList.Remove(_selectedEntries.Select(entry => entry.EngineItem));
            _PruneEntriesNotInEngine();

            var nextSelection = _SelectEntryAfterRemove(anchorIndex);
            SetSelectedEntries(nextSelection is null ? [] : [nextSelection]);
            _NotifyMembershipChanged();
        }

        /// <summary>
        /// Removes every row whose preview for <paramref name="key"/> is unchanged (MFR7 Remove Unchanged Items).
        /// </summary>
        /// <param name="key">Preview column field key from the header menu.</param>
        /// <remarks>
        /// <para>
        /// No-op for original keys, an empty list, or when busy. Always clears selection afterward
        /// (MFR7), even when nothing was removed. Raises <see cref="MembershipChanged"/> only when
        /// at least one row was removed.
        /// </para>
        /// </remarks>
        public void RemoveUnchanged(RenameListFieldKey key)
        {
            if (IsBusy || !key.IsPreview || Entries.Count == 0)
            {
                return;
            }

            var removedCount = _renameList.RemoveUnchanged(key);
            if (removedCount > 0)
            {
                _PruneEntriesNotInEngine();
                _NotifyMembershipChanged();
            }

            SetSelectedEntries([]);
        }

        /// <summary>
        /// Creates a Name List filter from this column's display lines and adds it to Applied Filters (MFR7 Free Names Edit).
        /// </summary>
        /// <param name="key">Original or preview field key from the header menu.</param>
        /// <remarks>
        /// <para>
        /// Writable fields only (<see cref="RenameListField.SupportsWrite"/>). Embeds lines in
        /// <see cref="NameListOptions.Entries"/> — no temp file. Selects the
        /// new step so Filter Configuration shows the existing Name List editor.
        /// </para>
        /// </remarks>
        public void EditAsNameList(RenameListFieldKey key)
        {
            if (IsBusy || _appliedFilters is null)
            {
                return;
            }

            var field = RenameListFieldCatalog.GetField(key);
            if (field.WriteTarget is not { } writeTarget)
            {
                return;
            }

            var lines = _renameList.CollectNameList(key);
            var filter = new NameListFilter(writeTarget, new NameListOptions(Entries: lines));
            _appliedFilters.AddAndSelect(filter, $"{field.DisplayName} List");
        }

        /// <summary>
        /// Exports this column's display lines to a UTF-8 text file (one line per row).
        /// </summary>
        /// <param name="key">Original or preview field key from the header menu.</param>
        /// <remarks>
        /// <para>
        /// Uses <see cref="ExportHooks"/> for the save dialog and error UI. On success, reveals the file
        /// in Explorer. After a path is chosen, aborts if the list became busy. Cancelled pick leaves disk unchanged.
        /// </para>
        /// </remarks>
        public Task ExportThisColumnAsync(RenameListFieldKey key)
        {
            return _ExportAsync(
                title: "Save Name List as",
                defaultExtension: "txt",
                fileTypeName: "Text files",
                write: path => _renameList.ExportNameList(path, key),
                errorLeadIn: $"Failed to export column {RenameListFieldCatalog.GetField(key).DisplayName}"
            );
        }

        /// <summary>
        /// Exports all visible columns' display text to a UTF-8 CSV file.
        /// </summary>
        /// <remarks>
        /// <para>
        /// Column order matches <see cref="VisibleColumns"/>. No-op when there are no visible columns.
        /// Uses <see cref="ExportHooks"/> for the save dialog and error UI. On success, reveals the file in Explorer.
        /// </para>
        /// </remarks>
        [RelayCommand]
        public Task ExportVisibleColumnsAsync()
        {
            if (VisibleColumns.Count == 0)
            {
                return Task.CompletedTask;
            }

            var keys = VisibleColumns.Select(column => column.Key).ToArray();
            return _ExportAsync(
                title: "Export as CSV",
                defaultExtension: "csv",
                fileTypeName: "CSV files",
                write: path => _renameList.ExportCsv(path, keys),
                errorLeadIn: "Failed to export visible columns"
            );
        }

        /// <summary>
        /// Shared export: pick path, write, error UI, then reveal in Explorer.
        /// </summary>
        private async Task _ExportAsync(
            string title,
            string defaultExtension,
            string fileTypeName,
            Action<string> write,
            string errorLeadIn
        )
        {
            var hooks = ExportHooks;
            if (IsBusy || hooks?.PickSavePathAsync is null)
            {
                return;
            }

            var path = await hooks.PickSavePathAsync(title, defaultExtension, fileTypeName).ConfigureAwait(true);
            if (string.IsNullOrWhiteSpace(path) || IsBusy)
            {
                return;
            }

            try
            {
                write(path);
            }
            catch (Exception ex)
            {
                if (hooks.ShowErrorAsync is not null)
                {
                    await hooks
                        .ShowErrorAsync("Magic File Renamer", $"{errorLeadIn}\n\n{path}\n\n{ex.Message}")
                        .ConfigureAwait(true);
                }

                return;
            }

            _shellOpener.RevealInFileManager(path);
        }

        /// <summary>
        /// Removes every Rename List row that is not selected, keeping the selection.
        /// </summary>
        [RelayCommand(CanExecute = nameof(_CanRemoveAllButSelected))]
        public void RemoveAllButSelected()
        {
            if (_selectedEntries.Count == 0 || _selectedEntries.Count >= Entries.Count)
            {
                return;
            }

            var selected = _selectedEntries.ToHashSet();
            var toRemove = Entries.Where(entry => !selected.Contains(entry)).Select(entry => entry.EngineItem).ToList();
            _renameList.Remove(toRemove);
            _PruneEntriesNotInEngine();

            SetSelectedEntries([.. Entries.Where(selected.Contains)]);
            _NotifyMembershipChanged();
        }

        /// <summary>
        /// Drops UI rows whose engine items are no longer in the rename list.
        /// </summary>
        private void _PruneEntriesNotInEngine()
        {
            var remaining = _renameList.RenameItems.ToHashSet();
            for (var i = Entries.Count - 1; i >= 0; i--)
            {
                if (!remaining.Contains(Entries[i].EngineItem))
                {
                    Entries.RemoveAt(i);
                }
            }
        }

        /// <summary>
        /// Finds the list index of the first selected row, matching MFR7 remove behavior.
        /// </summary>
        private int _FindFirstSelectedIndex(IReadOnlyCollection<RenameListEntry> selected)
        {
            for (var i = 0; i < Entries.Count; i++)
            {
                if (selected.Contains(Entries[i]))
                {
                    return i;
                }
            }

            return -1;
        }

        /// <summary>
        /// Picks the row to focus after delete: same index when possible, otherwise the last row.
        /// </summary>
        private RenameListEntry? _SelectEntryAfterRemove(int anchorIndex)
        {
            if (Entries.Count == 0 || anchorIndex < 0)
            {
                return null;
            }

            var nextIndex = Math.Min(anchorIndex, Entries.Count - 1);
            return Entries[nextIndex];
        }

        /// <summary>
        /// Removes every row from the Rename List.
        /// </summary>
        [RelayCommand(CanExecute = nameof(_CanClear))]
        public void Clear()
        {
            if (Entries.Count == 0)
            {
                return;
            }

            _renameList.Clear();
            Entries.Clear();
            SetDropMarkIndex(null);
            SetSelectedEntries([]);
            CellStatusHintDisplay = StatusHintDisplay.Empty;
            _NotifyMembershipChanged();
        }

        /// <summary>
        /// Moves the selected Rename List rows one position up.
        /// </summary>
        [RelayCommand(CanExecute = nameof(_CanRemoveSelected))]
        public void MoveSelectedUp()
        {
            _MoveSelected(offset: -1);
        }

        /// <summary>
        /// Moves the selected Rename List rows one position down.
        /// </summary>
        [RelayCommand(CanExecute = nameof(_CanRemoveSelected))]
        public void MoveSelectedDown()
        {
            _MoveSelected(offset: 1);
        }

        /// <summary>
        /// Reorders the selection to insert before the drop mark (or appends when unset).
        /// </summary>
        /// <returns><see langword="true"/> when the list order changed.</returns>
        /// <remarks>
        /// <para>
        /// Dropping onto a marked row that is part of the selection is a no-op (MFR7). Clears the
        /// drop mark afterward. Cancels Auto-Sort (MFR7 manual reorder).
        /// </para>
        /// </remarks>
        public bool ReorderSelectedToDropMark()
        {
            try
            {
                if (IsBusy || _selectedEntries.Count == 0)
                {
                    return false;
                }

                var beforeItem =
                    DropMarkIndex is { } markIndex && markIndex < Entries.Count ? Entries[markIndex].EngineItem : null;
                var engineItems = _selectedEntries.Select(entry => entry.EngineItem);
                if (!_renameList.MoveSelectedBefore(engineItems, beforeItem: beforeItem))
                {
                    return false;
                }

                CancelAutoSort();

                _SyncEntriesToEngineOrder();
                return true;
            }
            finally
            {
                SetDropMarkIndex(null);
            }
        }

        /// <summary>
        /// Reorders selected rows in the engine and grid by one step.
        /// </summary>
        private void _MoveSelected(int offset)
        {
            if (_selectedEntries.Count == 0)
            {
                return;
            }

            var engineItems = _selectedEntries.Select(entry => entry.EngineItem);
            if (!_renameList.MoveSelected(engineItems, offset))
            {
                return;
            }

            CancelAutoSort();

            _SyncEntriesToEngineOrder();
        }

        /// <summary>
        /// Navigates the File List to the focused Rename List row and selects it there.
        /// </summary>
        [RelayCommand(CanExecute = nameof(_CanLocateInFileList))]
        public void LocateInFileList()
        {
            var entry = _GetFocusedSelectedEntry();
            if (entry is null)
            {
                return;
            }

            var fullPath = entry.EngineItem.Original.FullPath;
            if (_fileListViewModel.TryLocatePath(fullPath))
            {
                LastLocateError = string.Empty;
                return;
            }

            LastLocateError = $"Failed to locate \"{fullPath}\" in the File List.";
        }

        /// <summary>
        /// Reveals the focused Rename List row in the OS file manager.
        /// </summary>
        [RelayCommand(CanExecute = nameof(_CanShowInExplorer))]
        public void ShowInExplorer()
        {
            var entry = _GetFocusedSelectedEntry();
            if (entry is null)
            {
                return;
            }

            _shellOpener.RevealInFileManager(entry.EngineItem.Original.FullPath);
        }

        /// <summary>
        /// Shows the OS property sheet for the single selected Rename List row.
        /// </summary>
        [RelayCommand(CanExecute = nameof(_CanShowProperties))]
        public void ShowProperties()
        {
            if (_selectedEntries.Count != 1)
            {
                return;
            }

            var entry = _GetFocusedSelectedEntry();
            if (entry is null)
            {
                return;
            }

            _shellOpener.ShowProperties(entry.EngineItem.Original.FullPath);
        }
    }
}
