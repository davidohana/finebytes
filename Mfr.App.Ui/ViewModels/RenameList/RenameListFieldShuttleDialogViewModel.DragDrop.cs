using Mfr.Models.RenameList;

namespace Mfr.App.Ui.ViewModels.RenameList
{
    public sealed partial class RenameListFieldShuttleDialogViewModel
    {
        /// <summary>
        /// Inserts visible columns at <paramref name="index"/> in draft order.
        /// </summary>
        /// <param name="keys">Column keys to insert.</param>
        /// <param name="index">Target index in <c>[0, Count]</c>.</param>
        public void InsertColumnsAt(IReadOnlyList<RenameListFieldKey> keys, int index)
        {
            ArgumentNullException.ThrowIfNull(keys);

            if (keys.Count == 0)
            {
                return;
            }

            var items = keys.Select(key => new RenameListVisibleColumn(key));
            var insertedCount = _columns.TryInsertMany(index, items);
            if (insertedCount == 0)
            {
                return;
            }

            _AssignColumnSelection(
                _BuildInsertedSelectionIndices(index, insertedCount, _columns.Items.Count),
                _columns.SelectedIndex
            );
            _RefreshLists();
        }

        /// <summary>
        /// Removes visible columns by draft index.
        /// </summary>
        /// <param name="indices">Row indices to remove.</param>
        public void RemoveColumnsAtIndices(IReadOnlyList<int> indices)
        {
            ArgumentNullException.ThrowIfNull(indices);

            if (_columns.TryRemoveAtIndices(indices) == 0)
            {
                return;
            }

            _AssignColumnSelection([], -1);
            _RefreshLists();
        }

        /// <summary>
        /// Moves visible columns to <paramref name="targetIndex"/>.
        /// </summary>
        /// <param name="sourceIndices">Indices of rows to move.</param>
        /// <param name="targetIndex">Destination index before the move.</param>
        public void MoveColumnsTo(IReadOnlyList<int> sourceIndices, int targetIndex)
        {
            ArgumentNullException.ThrowIfNull(sourceIndices);

            if (!_columns.TryMoveIndicesTo(sourceIndices, targetIndex))
            {
                return;
            }

            _AssignColumnSelection(
                _BuildMovedSelectionIndices(sourceIndices, targetIndex, _columns.Items.Count),
                _columns.SelectedIndex
            );
            _RefreshLists();
        }

        /// <summary>
        /// Inserts sort keys at <paramref name="index"/> in draft order.
        /// </summary>
        /// <param name="fieldKeys">Sort field keys to insert.</param>
        /// <param name="index">Target index in <c>[0, Count]</c>.</param>
        public void InsertSortKeysAt(IReadOnlyList<RenameListFieldKey> fieldKeys, int index)
        {
            ArgumentNullException.ThrowIfNull(fieldKeys);

            if (fieldKeys.Count == 0)
            {
                return;
            }

            var items = fieldKeys.Select(fieldKey => new RenameListSortKey(fieldKey));
            var insertedCount = _sortKeys.TryInsertMany(index, items);
            if (insertedCount == 0)
            {
                return;
            }

            _AssignSortSelection(
                _BuildInsertedSelectionIndices(index, insertedCount, _sortKeys.Items.Count),
                _sortKeys.SelectedIndex
            );
            _RefreshLists();
        }

        /// <summary>
        /// Removes sort keys by draft index.
        /// </summary>
        /// <param name="indices">Row indices to remove.</param>
        public void RemoveSortKeysAtIndices(IReadOnlyList<int> indices)
        {
            ArgumentNullException.ThrowIfNull(indices);

            if (_sortKeys.TryRemoveAtIndices(indices) == 0)
            {
                return;
            }

            _AssignSortSelection([], -1);
            _RefreshLists();
        }

        /// <summary>
        /// Moves sort keys to <paramref name="targetIndex"/>.
        /// </summary>
        /// <param name="sourceIndices">Indices of rows to move.</param>
        /// <param name="targetIndex">Destination index before the move.</param>
        public void MoveSortKeysTo(IReadOnlyList<int> sourceIndices, int targetIndex)
        {
            ArgumentNullException.ThrowIfNull(sourceIndices);

            if (!_sortKeys.TryMoveIndicesTo(sourceIndices, targetIndex))
            {
                return;
            }

            _AssignSortSelection(
                _BuildMovedSelectionIndices(sourceIndices, targetIndex, _sortKeys.Items.Count),
                _sortKeys.SelectedIndex
            );
            _RefreshLists();
        }

        /// <summary>
        /// Removes a single visible column by key (double-tap remove).
        /// </summary>
        /// <param name="key">Column key to remove.</param>
        public void RemoveColumnByKey(RenameListFieldKey key)
        {
            var index = _columns.Items.ToList().FindIndex(column => column.Key.Equals(key));
            if (index < 0)
            {
                return;
            }

            RemoveColumnsAtIndices([index]);
        }

        /// <summary>
        /// Removes a single sort key by field key (double-tap remove).
        /// </summary>
        /// <param name="fieldKey">Sort field key to remove.</param>
        public void RemoveSortKeyByFieldKey(RenameListFieldKey fieldKey)
        {
            var index = _sortKeys.Items.ToList().FindIndex(key => key.FieldKey.Equals(fieldKey));
            if (index < 0)
            {
                return;
            }

            RemoveSortKeysAtIndices([index]);
        }

        private static IReadOnlyList<int> _BuildInsertedSelectionIndices(
            int insertIndex,
            int insertedCount,
            int totalCount
        )
        {
            if (insertedCount == 0 || totalCount == 0)
            {
                return [];
            }

            var start = Math.Clamp(insertIndex, 0, totalCount - insertedCount);
            return [.. Enumerable.Range(start, insertedCount)];
        }

        private static IReadOnlyList<int> _BuildMovedSelectionIndices(
            IReadOnlyList<int> sourceIndices,
            int targetIndex,
            int totalCount
        )
        {
            var sortedSources = sourceIndices.Distinct().OrderBy(i => i).ToList();
            if (sortedSources.Count == 0 || totalCount == 0)
            {
                return [];
            }

            var insertIndex = targetIndex - sortedSources.Count(index => index < targetIndex);
            insertIndex = Math.Clamp(insertIndex, 0, totalCount - sortedSources.Count);
            return [.. Enumerable.Range(insertIndex, sortedSources.Count)];
        }
    }
}
