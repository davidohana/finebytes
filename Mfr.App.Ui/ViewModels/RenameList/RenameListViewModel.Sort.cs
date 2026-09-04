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

            _ApplySortKeys(RenameListSortKey.DefaultKeys, resort: true);
        }

        /// <summary>
        /// Replaces the active sort keys and resorts when non-empty.
        /// </summary>
        /// <param name="keys">New sort keys in priority order.</param>
        public void SetSortKeys(IReadOnlyList<RenameListSortKey> keys)
        {
            ArgumentNullException.ThrowIfNull(keys);
            _ApplySortKeys(keys, resort: true);
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
            if (!RenameListFieldCatalog.IsSortableKey(key) || IsBusy)
            {
                return;
            }

            _ApplySortKeys(_ComputeSortKeys(key, append), resort: true);
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
            _ApplySortKeys(keys, resort: true);
        }

        /// <summary>
        /// Captures the current Auto-Sort keys for session save.
        /// </summary>
        /// <returns>Session sort fields, or empty when Auto-Sort is off.</returns>
        internal IReadOnlyList<SessionStateRenameListSortField> CaptureSortFields()
        {
            return SessionStateRenameList.FromSortKeys(_sortKeys);
        }

        private void _ApplySortKeys(IReadOnlyList<RenameListSortKey> keys, bool resort)
        {
            var sanitized = _SanitizeSortKeys(keys);
            var requirement = _CombinedMetadataRequirement(_visibleColumns, sanitized);
            if (_NeedsHydrate(requirement))
            {
                _ = _HydrateThenSetSortKeysAsync(sanitized, resort);
                return;
            }

            _SetSortKeys(sanitized, resort);
        }

        private List<RenameListSortKey> _ComputeSortKeys(RenameListFieldKey fieldKey, bool append)
        {
            if (append)
            {
                return _ComputeSortKeysAppend(fieldKey);
            }

            // MFR7 RenameGridCells.SetSortMode: desc = GetSortMode() == Ascending (None → ascending).
            var descending = _sortKeys.Any(key => key.FieldKey == fieldKey && !key.Descending);
            return [new RenameListSortKey(fieldKey, descending)];
        }

        /// <summary>
        /// Appends a sort key, toggles direction on an existing key, or removes a descending key (Shift+click).
        /// </summary>
        private List<RenameListSortKey> _ComputeSortKeysAppend(RenameListFieldKey fieldKey)
        {
            var index = _sortKeys.FindIndex(key => key.FieldKey == fieldKey);
            if (index < 0)
            {
                return [.. _sortKeys, new RenameListSortKey(fieldKey)];
            }

            var existing = _sortKeys[index];
            if (!existing.Descending)
            {
                var keys = _sortKeys.ToList();
                keys[index] = existing with { Descending = true };
                return keys;
            }

            return [.. _sortKeys.Where((_, i) => i != index)];
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

        private static List<RenameListSortKey> _SanitizeSortKeys(IReadOnlyList<RenameListSortKey> keys)
        {
            var seenFieldKeys = new HashSet<RenameListFieldKey>();
            var sanitized = new List<RenameListSortKey>(keys.Count);
            foreach (var key in keys)
            {
                if (!RenameListFieldCatalog.IsSortableKey(key.FieldKey))
                {
                    continue;
                }

                if (!seenFieldKeys.Add(key.FieldKey))
                {
                    continue;
                }

                sanitized.Add(key);
            }

            return sanitized;
        }
    }
}
