namespace Mfr.App.Ui.ViewModels.RenameList
{
    /// <summary>
    /// Ordered draft list with unique keys, selection index, and shuttle-style add/remove/move/clear.
    /// </summary>
    /// <typeparam name="TKey">Unique key for duplicate detection.</typeparam>
    /// <typeparam name="TItem">Draft item stored in list order.</typeparam>
    internal sealed class OrderedDraft<TKey, TItem>
        where TKey : notnull
    {
        private readonly List<TItem> _items;
        private readonly HashSet<TKey> _keys;
        private readonly Func<TItem, TKey> _keyOf;

        /// <summary>
        /// Initializes a draft from existing items.
        /// </summary>
        /// <param name="items">Initial items in display order.</param>
        /// <param name="keyOf">Extracts the unique key from an item.</param>
        public OrderedDraft(IEnumerable<TItem> items, Func<TItem, TKey> keyOf)
        {
            ArgumentNullException.ThrowIfNull(items);
            ArgumentNullException.ThrowIfNull(keyOf);

            _keyOf = keyOf;
            _items = [.. items];
            _keys = [.. _items.Select(keyOf)];
        }

        /// <summary>
        /// Gets draft items in display order.
        /// </summary>
        public IReadOnlyList<TItem> Items => _items;

        /// <summary>
        /// Gets or sets the selected item index, or <c>-1</c> when empty or nothing selected.
        /// </summary>
        public int SelectedIndex { get; set; } = -1;

        /// <summary>
        /// Gets whether the draft contains an item with <paramref name="key"/>.
        /// </summary>
        /// <param name="key">Item key to look up.</param>
        /// <returns><see langword="true"/> when an item with that key is present.</returns>
        public bool Contains(TKey key)
        {
            return _keys.Contains(key);
        }

        /// <summary>
        /// Gets whether at least one item is in the draft.
        /// </summary>
        public bool HasItems => _items.Count > 0;

        /// <summary>
        /// Gets whether the current selection can be removed.
        /// </summary>
        public bool CanRemove => _TryGetSelectedIndex(out _);

        /// <summary>
        /// Gets whether the current selection can move up.
        /// </summary>
        public bool CanMoveUp => _TryGetSelectedIndex(out var index) && index > 0;

        /// <summary>
        /// Gets whether the current selection can move down.
        /// </summary>
        public bool CanMoveDown => _TryGetSelectedIndex(out var index) && index < _items.Count - 1;

        /// <summary>
        /// Gets the index where a new item should be inserted below the current selection.
        /// </summary>
        /// <returns>Index in <c>[0, Count]</c>; when nothing is selected, returns <see cref="IReadOnlyCollection{T}.Count"/>.</returns>
        public int GetInsertIndexBelowSelection()
        {
            return SelectedIndex >= 0 ? SelectedIndex + 1 : _items.Count;
        }

        /// <summary>
        /// Appends an item when its key is not already present and selects the new last row.
        /// </summary>
        /// <param name="item">Item to append.</param>
        /// <returns><see langword="false"/> when the key already exists; otherwise <see langword="true"/>.</returns>
        public bool TryAdd(TItem item)
        {
            return TryInsertAt(_items.Count, item);
        }

        /// <summary>
        /// Inserts an item at <paramref name="index"/> when its key is not already present and selects the new row.
        /// </summary>
        /// <param name="index">Insertion index in <c>[0, Count]</c>.</param>
        /// <param name="item">Item to insert.</param>
        /// <returns><see langword="false"/> when the key already exists; otherwise <see langword="true"/>.</returns>
        public bool TryInsertAt(int index, TItem item)
        {
            var key = _keyOf(item);
            if (!_keys.Add(key))
            {
                return false;
            }

            index = Math.Clamp(index, 0, _items.Count);
            _items.Insert(index, item);
            SelectedIndex = index;
            return true;
        }

        /// <summary>
        /// Inserts items at <paramref name="index"/> in order, skipping duplicate keys, and selects the last inserted row.
        /// </summary>
        /// <param name="index">Starting insertion index in <c>[0, Count]</c>.</param>
        /// <param name="items">Items to insert.</param>
        /// <returns>Number of items inserted.</returns>
        public int TryInsertMany(int index, IEnumerable<TItem> items)
        {
            ArgumentNullException.ThrowIfNull(items);

            index = Math.Clamp(index, 0, _items.Count);
            var insertedCount = 0;
            var lastInsertedIndex = -1;

            foreach (var item in items)
            {
                if (!TryInsertAt(index, item))
                {
                    continue;
                }

                insertedCount++;
                lastInsertedIndex = index;
                index++;
            }

            if (lastInsertedIndex >= 0)
            {
                SelectedIndex = lastInsertedIndex;
            }

            return insertedCount;
        }

        /// <summary>
        /// Removes the selected item and clamps <see cref="SelectedIndex"/>.
        /// </summary>
        /// <returns><see langword="false"/> when nothing valid is selected.</returns>
        public bool TryRemoveSelected()
        {
            if (!_TryGetSelectedIndex(out var index))
            {
                return false;
            }

            return TryRemoveAtIndices([index]) == 1;
        }

        /// <summary>
        /// Removes items at <paramref name="indices"/> and clamps <see cref="SelectedIndex"/>.
        /// </summary>
        /// <param name="indices">Item indices to remove.</param>
        /// <returns>Number of items removed.</returns>
        public int TryRemoveAtIndices(IReadOnlyList<int> indices)
        {
            ArgumentNullException.ThrowIfNull(indices);

            if (indices.Count == 0)
            {
                return 0;
            }

            var anchorIndex = SelectedIndex;
            var removedCount = 0;

            foreach (var index in indices.Distinct().OrderByDescending(i => i))
            {
                if (index < 0 || index >= _items.Count)
                {
                    continue;
                }

                _keys.Remove(_keyOf(_items[index]));
                _items.RemoveAt(index);
                removedCount++;
            }

            if (removedCount == 0)
            {
                return 0;
            }

            SelectedIndex = _ClampSelectionIndex(anchorIndex, _items.Count);
            return removedCount;
        }

        /// <summary>
        /// Moves the selected item by <paramref name="offset"/> positions (-1 up, +1 down).
        /// </summary>
        /// <param name="offset">Direction and distance to move.</param>
        /// <returns><see langword="false"/> when the move is not allowed.</returns>
        public bool TryMoveSelected(int offset)
        {
            if (!_TryGetSelectedIndex(out var index))
            {
                return false;
            }

            return TryMoveBlock([index], offset);
        }

        /// <summary>
        /// Moves selected items one position toward <paramref name="offset"/>, independently.
        /// </summary>
        /// <param name="sourceIndices">Indices of items to move.</param>
        /// <param name="offset">Direction to move (-1 up, +1 down).</param>
        /// <returns><see langword="false"/> when nothing could move.</returns>
        public bool TryMoveBlock(IReadOnlyList<int> sourceIndices, int offset)
        {
            return TryMoveBlock(sourceIndices, offset, out _);
        }

        /// <summary>
        /// Moves selected items one position toward <paramref name="offset"/>, independently.
        /// </summary>
        /// <param name="sourceIndices">Indices of items to move.</param>
        /// <param name="offset">Direction to move (-1 up, +1 down).</param>
        /// <param name="newIndices">Indices of the moved items after a successful move.</param>
        /// <returns><see langword="false"/> when nothing could move.</returns>
        /// <remarks>
        /// <para>
        /// Matches Rename List / MFR7: a contiguous block slides as a unit; a scattered selection
        /// only swaps items that have an unselected neighbor in that direction.
        /// </para>
        /// </remarks>
        public bool TryMoveBlock(IReadOnlyList<int> sourceIndices, int offset, out IReadOnlyList<int> newIndices)
        {
            ArgumentNullException.ThrowIfNull(sourceIndices);

            newIndices = [];
            if (offset is not (-1 or 1))
            {
                return false;
            }

            var sortedSources = sourceIndices
                .Where(index => index >= 0 && index < _items.Count)
                .Distinct()
                .OrderBy(index => index)
                .ToList();
            if (sortedSources.Count == 0)
            {
                return false;
            }

            var selectedKeys = sortedSources.Select(index => _keyOf(_items[index])).ToHashSet();
            var hasAnchor = _TryGetSelectedIndex(out var anchorIndex);
            var trackedAnchor = hasAnchor ? _keyOf(_items[anchorIndex]) : default;

            var moved = false;
            var walkStep = -offset;
            var startIndex = walkStep > 0 ? 0 : _items.Count - 1;
            for (var index = startIndex; index >= 0 && index < _items.Count; index += walkStep)
            {
                if (!_CanSwapTowardNeighbor(selectedKeys, index, offset))
                {
                    continue;
                }

                var neighborIndex = index + offset;
                (_items[index], _items[neighborIndex]) = (_items[neighborIndex], _items[index]);
                moved = true;
            }

            if (!moved)
            {
                return false;
            }

            newIndices =
            [
                .. Enumerable.Range(0, _items.Count).Where(index => selectedKeys.Contains(_keyOf(_items[index]))),
            ];
            if (hasAnchor)
            {
                SelectedIndex = _items.FindIndex(item =>
                    EqualityComparer<TKey>.Default.Equals(_keyOf(item), trackedAnchor)
                );
            }

            return true;
        }

        /// <summary>
        /// Moves items at <paramref name="sourceIndices"/> to <paramref name="targetIndex"/>, preserving their order.
        /// </summary>
        /// <param name="sourceIndices">Indices of items to move.</param>
        /// <param name="targetIndex">Destination index in the list before the move.</param>
        /// <returns><see langword="false"/> when the move is not allowed.</returns>
        public bool TryMoveIndicesTo(IReadOnlyList<int> sourceIndices, int targetIndex)
        {
            ArgumentNullException.ThrowIfNull(sourceIndices);

            var sortedSources = sourceIndices.Distinct().OrderBy(i => i).ToList();
            if (sortedSources.Count == 0)
            {
                return false;
            }

            if (sortedSources.Any(index => index < 0 || index >= _items.Count))
            {
                return false;
            }

            targetIndex = Math.Clamp(targetIndex, 0, _items.Count);
            var movingItems = sortedSources.Select(index => _items[index]).ToList();
            var hasAnchor = _TryGetSelectedIndex(out var anchorIndex);
            var anchorKey = hasAnchor ? _keyOf(_items[anchorIndex]) : default(TKey?);

            foreach (var index in sortedSources.OrderByDescending(i => i))
            {
                _items.RemoveAt(index);
            }

            var insertIndex = targetIndex - sortedSources.Count(index => index < targetIndex);
            insertIndex = Math.Clamp(insertIndex, 0, _items.Count);
            _items.InsertRange(insertIndex, movingItems);

            if (hasAnchor && anchorKey is not null)
            {
                var newAnchorIndex = _items.FindIndex(item => _keyOf(item).Equals(anchorKey));
                SelectedIndex = newAnchorIndex;
            }

            return true;
        }

        /// <summary>
        /// Removes all items and clears selection.
        /// </summary>
        public void Clear()
        {
            _items.Clear();
            _keys.Clear();
            SelectedIndex = -1;
        }

        /// <summary>
        /// Replaces an item at <paramref name="index"/> when the key is unchanged.
        /// </summary>
        /// <param name="index">Item index to replace.</param>
        /// <param name="item">Replacement item.</param>
        /// <returns><see langword="false"/> when the index is invalid or the key differs.</returns>
        public bool TrySetItem(int index, TItem item)
        {
            if (index < 0 || index >= _items.Count)
            {
                return false;
            }

            if (!_keyOf(_items[index]).Equals(_keyOf(item)))
            {
                return false;
            }

            _items[index] = item;
            return true;
        }

        private bool _CanSwapTowardNeighbor(HashSet<TKey> selectedKeys, int index, int offset)
        {
            if (!selectedKeys.Contains(_keyOf(_items[index])))
            {
                return false;
            }

            var neighborIndex = index + offset;
            if (neighborIndex < 0 || neighborIndex >= _items.Count)
            {
                return false;
            }

            return !selectedKeys.Contains(_keyOf(_items[neighborIndex]));
        }

        private bool _TryGetSelectedIndex(out int index)
        {
            index = SelectedIndex;
            return index >= 0 && index < _items.Count;
        }

        private static int _ClampSelectionIndex(int index, int count)
        {
            if (count == 0)
            {
                return -1;
            }

            if (index < 0)
            {
                return -1;
            }

            if (index >= count)
            {
                return count - 1;
            }

            return index;
        }
    }
}
