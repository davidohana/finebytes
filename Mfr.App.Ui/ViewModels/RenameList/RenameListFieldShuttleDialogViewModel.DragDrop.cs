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
            if (_columns.TryInsertMany(index, items) == 0)
            {
                return;
            }

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

            _columns.SetSelection([], -1);
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
            if (_sortKeys.TryInsertMany(index, items) == 0)
            {
                return;
            }

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

            _sortKeys.SetSelection([], -1);
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
    }
}
