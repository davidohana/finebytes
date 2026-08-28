using CommunityToolkit.Mvvm.Input;
using Mfr.Models.Config;
using Mfr.Models.RenameList;

namespace Mfr.App.Ui.ViewModels.RenameList
{
    /// <summary>
    /// Auto-Sort keys for <see cref="RenameListViewModel"/>.
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
        /// Sets Auto-Sort from a visible grid column field key.
        /// </summary>
        /// <param name="key">Catalog field key for the clicked column.</param>
        /// <param name="append">
        /// When <see langword="true"/> (Shift+click), append or adjust an existing key instead of replacing the list.
        /// </param>
        public void SortByFieldKey(RenameListFieldKey key, bool append = false)
        {
            if (key.IsPreview || !RenameListFieldCatalog.IsSortableKey(key))
            {
                return;
            }

            _SortByFieldKey(key, append);
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

        private void _SortByFieldKey(RenameListFieldKey fieldKey, bool append)
        {
            if (append)
            {
                _SortByFieldKeyAppend(fieldKey);
                return;
            }

            // MFR7 RenameGridCells.SetSortMode: desc = GetSortMode() == Ascending (None → ascending).
            var descending = _sortKeys.Any(key => key.FieldKey == fieldKey && !key.Descending);
            _SetSortKeys([new RenameListSortKey(fieldKey, descending)], resort: true);
        }

        /// <summary>
        /// Appends a sort key, toggles direction on an existing key, or removes a descending key (Shift+click).
        /// </summary>
        private void _SortByFieldKeyAppend(RenameListFieldKey fieldKey)
        {
            var index = _sortKeys.FindIndex(key => key.FieldKey == fieldKey);
            if (index < 0)
            {
                var keys = new List<RenameListSortKey>(_sortKeys) { new(fieldKey) };
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

            var withoutField = _sortKeys.Where((_, i) => i != index).ToList();
            _SetSortKeys(withoutField, resort: withoutField.Count > 0);
        }

        private void _SetSortKeys(IReadOnlyList<RenameListSortKey> keys, bool resort)
        {
            _sortKeys = [.. keys];
            ColumnSortStates = RenameListSortDisplay.BuildColumnSortStates(_sortKeys);
            OnPropertyChanged(nameof(IsAutoSort));
            OnPropertyChanged(nameof(SortKeys));
            OnPropertyChanged(nameof(SortSummaryText));
            OnPropertyChanged(nameof(ColumnSortStates));
            SetDropMarkIndex(null);

            if (resort && IsAutoSort && Entries.Count > 1)
            {
                _renameList.Sort(_sortKeys);
                _SyncEntriesToEngineOrder();
            }
        }
    }
}
