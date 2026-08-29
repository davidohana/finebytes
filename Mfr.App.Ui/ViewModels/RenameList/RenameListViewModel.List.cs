using CommunityToolkit.Mvvm.Input;

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

            for (var i = Entries.Count - 1; i >= 0; i--)
            {
                if (selected.Contains(Entries[i]))
                {
                    Entries.RemoveAt(i);
                }
            }

            var nextSelection = _SelectEntryAfterRemove(anchorIndex);
            SetSelectedEntries(nextSelection is null ? [] : [nextSelection]);
            _NotifyListChanged();
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

            for (var i = Entries.Count - 1; i >= 0; i--)
            {
                if (!selected.Contains(Entries[i]))
                {
                    Entries.RemoveAt(i);
                }
            }

            SetSelectedEntries([.. Entries.Where(selected.Contains)]);
            _NotifyListChanged();
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
            _NotifyListChanged();
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
                if (IsAdding || _selectedEntries.Count == 0)
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
    }
}
