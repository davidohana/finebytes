using CommunityToolkit.Mvvm.Input;
using Mfr.Models.Config;
using Mfr.Models.RenameList;

namespace Mfr.App.Ui.ViewModels.RenameList
{
    /// <summary>
    /// Auto-Sort keys and sort-editor commands for <see cref="RenameListViewModel"/>.
    /// </summary>
    public sealed partial class RenameListViewModel
    {
        /// <summary>
        /// Turns off Auto-Sort for a manual reorder (drag or keyboard move) without resorting.
        /// </summary>
        public void CancelAutoSort()
        {
            if (!IsAutoSort)
            {
                return;
            }

            _SetSortKeys([], resort: false);
        }

        /// <summary>
        /// Toggles Auto-Sort. Turning it on restores the default keys.
        /// </summary>
        [RelayCommand]
        public void ToggleAutoSort()
        {
            if (IsAutoSort)
            {
                _SetSortKeys([], resort: false);
                return;
            }

            _SetSortKeys(RenameListSortKey.DefaultKeys, resort: true);
        }

        /// <summary>
        /// Replaces the active sort keys and resorts when non-empty.
        /// </summary>
        /// <param name="keys">New sort keys in priority order.</param>
        public void SetSortKeys(IReadOnlyList<RenameListSortKey> keys)
        {
            ArgumentNullException.ThrowIfNull(keys);
            _SetSortKeys(keys, resort: true);
        }

        /// <summary>
        /// Reorders one sort key by <paramref name="offset"/> positions (-1 up, +1 down).
        /// </summary>
        /// <param name="index">Zero-based key index.</param>
        /// <param name="offset">Position delta.</param>
        public void MoveSortKey(int index, int offset)
        {
            var newIndex = index + offset;
            if (index < 0 || index >= _sortKeys.Count || newIndex < 0 || newIndex >= _sortKeys.Count)
            {
                return;
            }

            var keys = _sortKeys.ToList();
            var key = keys[index];
            keys.RemoveAt(index);
            keys.Insert(newIndex, key);
            _SetSortKeys(keys, resort: true);
        }

        /// <summary>
        /// Removes the sort key at <paramref name="index"/>.
        /// </summary>
        /// <param name="index">Zero-based key index.</param>
        public void RemoveSortKey(int index)
        {
            if (index < 0 || index >= _sortKeys.Count)
            {
                return;
            }

            var keys = _sortKeys.Where((_, i) => i != index).ToList();
            _SetSortKeys(keys, resort: keys.Count > 0);
        }

        /// <summary>
        /// Restores the default Auto-Sort keys.
        /// </summary>
        [RelayCommand]
        public void ResetSortToDefault()
        {
            _SetSortKeys(RenameListSortKey.DefaultKeys, resort: true);
        }

        /// <summary>
        /// Clears all sort keys (Auto-Sort off).
        /// </summary>
        [RelayCommand]
        public void ClearAllSortKeys()
        {
            _SetSortKeys([], resort: false);
        }

        /// <summary>
        /// Raised when the view should open the sort editor flyout.
        /// </summary>
        public event EventHandler? SortEditorRequested;

        /// <summary>
        /// Requests the sort editor flyout (Rename List context menu).
        /// </summary>
        [RelayCommand]
        public void OpenSortEditor()
        {
            SortEditorRequested?.Invoke(this, EventArgs.Empty);
        }

        /// <summary>
        /// Appends a sort key for the first unused editor column.
        /// </summary>
        [RelayCommand(CanExecute = nameof(CanAddSortKey))]
        public void AddSortKey()
        {
            var usedColumns = _sortKeys.Select(key => key.Column).ToHashSet();
            var nextColumn = RenameListSortDisplay.EditorColumns.First(column => !usedColumns.Contains(column));
            var keys = new List<RenameListSortKey>(_sortKeys) { new(nextColumn) };
            _SetSortKeys(keys, resort: true);
        }

        /// <summary>
        /// Changes the column for one sort key.
        /// </summary>
        /// <param name="index">Zero-based key index.</param>
        /// <param name="column">New column.</param>
        public void SetSortKeyColumn(int index, RenameListSortColumn column)
        {
            if (index < 0 || index >= _sortKeys.Count)
            {
                return;
            }

            var existing = _sortKeys[index];
            if (existing.Column == column)
            {
                return;
            }

            var keys = _sortKeys.ToList();
            keys[index] = existing with { Column = column };
            _SetSortKeys(keys, resort: true);
        }

        /// <summary>
        /// Toggles ascending/descending for one sort key.
        /// </summary>
        /// <param name="index">Zero-based key index.</param>
        public void ToggleSortKeyDirection(int index)
        {
            if (index < 0 || index >= _sortKeys.Count)
            {
                return;
            }

            var keys = _sortKeys.ToList();
            var existing = keys[index];
            keys[index] = existing with { Descending = !existing.Descending };
            _SetSortKeys(keys, resort: true);
        }

        /// <summary>
        /// Moves one sort key up in priority.
        /// </summary>
        /// <param name="index">Zero-based key index.</param>
        [RelayCommand(CanExecute = nameof(_CanMoveSortKeyUp))]
        public void MoveSortKeyUp(int index)
        {
            MoveSortKey(index, offset: -1);
        }

        /// <summary>
        /// Moves one sort key down in priority.
        /// </summary>
        /// <param name="index">Zero-based key index.</param>
        [RelayCommand(CanExecute = nameof(_CanMoveSortKeyDown))]
        public void MoveSortKeyDown(int index)
        {
            MoveSortKey(index, offset: 1);
        }

        /// <summary>
        /// Removes one sort key from the editor.
        /// </summary>
        /// <param name="index">Zero-based key index.</param>
        [RelayCommand(CanExecute = nameof(_CanRemoveSortKey))]
        public void RemoveSortKeyAt(int index)
        {
            RemoveSortKey(index);
        }

        private bool _CanMoveSortKeyUp(int index)
        {
            return index > 0 && index < _sortKeys.Count;
        }

        private bool _CanMoveSortKeyDown(int index)
        {
            return index >= 0 && index < _sortKeys.Count - 1;
        }

        private bool _CanRemoveSortKey(int index)
        {
            return index >= 0 && index < _sortKeys.Count;
        }

        /// <summary>
        /// Sets Auto-Sort from a column header click.
        /// </summary>
        /// <param name="memberPath">
        /// <see cref="RenameListEntry"/> property name (<c>FileFolder</c>, <c>ParentFolder</c>, <c>FullFileName</c>).
        /// </param>
        /// <param name="append">
        /// When <see langword="true"/> (Shift+click), append or adjust an existing key instead of replacing the list.
        /// </param>
        public void SortByColumn(string? memberPath, bool append = false)
        {
            if (!RenameListSortDisplay.TryMapMemberPath(memberPath, out var column))
            {
                return;
            }

            _SortByColumn(column, append);
        }

        /// <summary>
        /// Sets Auto-Sort from a visible grid column field key.
        /// </summary>
        /// <param name="key">Catalog field key for the clicked column.</param>
        /// <param name="append">
        /// When <see langword="true"/> (Shift+click), append or adjust an existing key instead of replacing the list.
        /// </param>
        public void SortByFieldKey(RenameListFieldKey key, bool append = false)
        {
            if (key.IsPreview || !RenameListFieldCatalog.TryMapFieldKeyToSortColumn(key, out var column))
            {
                return;
            }

            _SortByColumn(column, append);
        }

        private void _SortByColumn(RenameListSortColumn column, bool append)
        {
            if (append)
            {
                _SortByColumnAppend(column);
                return;
            }

            // MFR7 RenameGridCells.SetSortMode: desc = GetSortMode() == Ascending (None → ascending).
            var descending = _sortKeys.Any(key => key.Column == column && !key.Descending);
            _SetSortKeys([new RenameListSortKey(column, descending)], resort: true);
        }

        /// <summary>
        /// Appends a sort key, toggles direction on an existing key, or removes a descending key (Shift+click).
        /// </summary>
        private void _SortByColumnAppend(RenameListSortColumn column)
        {
            var index = _sortKeys.FindIndex(key => key.Column == column);
            if (index < 0)
            {
                var keys = new List<RenameListSortKey>(_sortKeys) { new(column) };
                _SetSortKeys(keys, resort: true);
                return;
            }

            var existing = _sortKeys[index];
            if (!existing.Descending)
            {
                var keys = _sortKeys.ToList();
                keys[index] = existing with { Descending = true };
                _SetSortKeys(keys, resort: true);
                return;
            }

            var withoutColumn = _sortKeys.Where((_, i) => i != index).ToList();
            _SetSortKeys(withoutColumn, resort: withoutColumn.Count > 0);
        }

        /// <summary>
        /// Restores Auto-Sort keys from a session value.
        /// </summary>
        /// <param name="sortFields">
        /// Session sort fields, empty to disable Auto-Sort, or <see langword="null"/> for the default keys.
        /// </param>
        internal void ApplySession(IReadOnlyList<SessionStateRenameListSortField>? sortFields)
        {
            var keys = sortFields is null
                ? RenameListSortKey.DefaultKeys
                : SessionStateRenameList.ToSortKeys(sortFields);
            _SetSortKeys(keys, resort: true);
        }

        /// <summary>
        /// Captures the current Auto-Sort keys for session save.
        /// </summary>
        /// <returns>Session sort fields, or empty when Auto-Sort is off.</returns>
        internal IReadOnlyList<SessionStateRenameListSortField> CaptureSortFields()
        {
            return SessionStateRenameList.FromSortKeys(_sortKeys);
        }

        private void _SetSortKeys(IReadOnlyList<RenameListSortKey> keys, bool resort)
        {
            _sortKeys = [.. keys];
            ColumnSortStates = RenameListSortDisplay.BuildColumnSortStates(_sortKeys);
            OnPropertyChanged(nameof(IsAutoSort));
            OnPropertyChanged(nameof(SortKeys));
            OnPropertyChanged(nameof(SortEditorRows));
            OnPropertyChanged(nameof(CanAddSortKey));
            OnPropertyChanged(nameof(SortSummaryText));
            OnPropertyChanged(nameof(ColumnSortStates));
            AddSortKeyCommand.NotifyCanExecuteChanged();
            MoveSortKeyUpCommand.NotifyCanExecuteChanged();
            MoveSortKeyDownCommand.NotifyCanExecuteChanged();
            RemoveSortKeyAtCommand.NotifyCanExecuteChanged();
            SetDropMarkIndex(null);

            if (resort && IsAutoSort && Entries.Count > 1)
            {
                _renameList.Sort(_sortKeys);
                _SyncEntriesToEngineOrder();
            }
        }
    }
}
