namespace Mfr.App.Ui.ViewModels.RenameList
{
    /// <summary>
    /// Ordered draft list with unique keys, multi-selection, and shuttle-style add/remove/move/clear.
    /// </summary>
    /// <typeparam name="TKey">Unique key for duplicate detection.</typeparam>
    /// <typeparam name="TItem">Draft item stored in list order.</typeparam>
    internal sealed class OrderedDraft<TKey, TItem>
        where TKey : notnull
    {
        private readonly List<TItem> _items;
        private readonly HashSet<TKey> _keys;
        private readonly Func<TItem, TKey> _keyOf;
        private int _selectedIndex = -1;

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
        /// Gets selected item indices in list order.
        /// </summary>
        public IReadOnlyList<int> SelectedIndices { get; private set; } = [];

        /// <summary>
        /// Gets or sets the selected-item anchor, or <c>-1</c> when nothing is selected.
        /// </summary>
        /// <remarks>
        /// <para>
        /// Setting this value replaces multi-selection with that single index (or clears it when
        /// <c>-1</c>). Use <see cref="SetSelection"/> to keep a multi-selection.
        /// </para>
        /// </remarks>
        public int SelectedIndex
        {
            get => _selectedIndex;
            set => SetSelection(value >= 0 ? [value] : [], value);
        }

        /// <summary>
        /// Sets multi-selection and keeps the anchor on a selected row.
        /// </summary>
        /// <param name="indices">Selected row indices in list order.</param>
        /// <param name="anchorIndex">Primary selected row used for insert-below and direction toggles.</param>
        public void SetSelection(IReadOnlyList<int> indices, int anchorIndex)
        {
            ArgumentNullException.ThrowIfNull(indices);

            SelectedIndices = _NormalizeIndices(indices);
            _selectedIndex = _ResolveAnchor(SelectedIndices, anchorIndex);
        }

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
        public bool CanRemove => SelectedIndices.Count > 0;

        /// <summary>
        /// Gets whether any selected item can move one step toward <paramref name="offset"/>.
        /// </summary>
        /// <param name="offset">Direction to move (-1 up, +1 down).</param>
        /// <returns>
        /// <see langword="true"/> when at least one selected item has an unselected neighbor in that direction.
        /// </returns>
        public bool CanMoveBlock(int offset)
        {
            return CanMoveBlock(SelectedIndices, offset);
        }

        /// <summary>
        /// Gets whether any item at <paramref name="sourceIndices"/> can move one step toward <paramref name="offset"/>.
        /// </summary>
        /// <param name="sourceIndices">Indices of items to move.</param>
        /// <param name="offset">Direction to move (-1 up, +1 down).</param>
        /// <returns>
        /// <see langword="true"/> when at least one selected item has an unselected neighbor in that direction.
        /// </returns>
        public bool CanMoveBlock(IReadOnlyList<int> sourceIndices, int offset)
        {
            ArgumentNullException.ThrowIfNull(sourceIndices);

            if (offset is not (-1 or 1))
            {
                return false;
            }

            var selectedKeys = _KeysAt(sourceIndices);
            if (selectedKeys.Count == 0)
            {
                return false;
            }

            for (var index = 0; index < _items.Count; index++)
            {
                if (_CanSwapTowardNeighbor(selectedKeys, index, offset))
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// Gets the index where a new item should be inserted below the last selected row.
        /// </summary>
        /// <returns>Index in <c>[0, Count]</c>; when nothing is selected, returns <see cref="IReadOnlyCollection{T}.Count"/>.</returns>
        public int GetInsertIndexBelow()
        {
            return GetInsertIndexBelow(SelectedIndices);
        }

        /// <summary>
        /// Gets the index where a new item should be inserted below the last selected row.
        /// </summary>
        /// <param name="selectedIndices">Selected row indices in list order.</param>
        /// <returns>Index in <c>[0, Count]</c>; when nothing is selected, returns <see cref="IReadOnlyCollection{T}.Count"/>.</returns>
        public int GetInsertIndexBelow(IReadOnlyList<int> selectedIndices)
        {
            ArgumentNullException.ThrowIfNull(selectedIndices);

            var lastSelected = -1;
            foreach (var index in selectedIndices)
            {
                if (index > lastSelected && index >= 0 && index < _items.Count)
                {
                    lastSelected = index;
                }
            }

            return lastSelected >= 0 ? lastSelected + 1 : _items.Count;
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
            if (!_TryInsertItem(index, item, out var insertedIndex))
            {
                return false;
            }

            SetSelection([insertedIndex], insertedIndex);
            return true;
        }

        /// <summary>
        /// Inserts items at <paramref name="index"/> in order, skipping duplicate keys, and selects the inserted rows.
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
                if (!_TryInsertItem(index, item, out var insertedIndex))
                {
                    continue;
                }

                insertedCount++;
                lastInsertedIndex = insertedIndex;
                index = insertedIndex + 1;
            }

            if (insertedCount > 0)
            {
                var start = lastInsertedIndex - insertedCount + 1;
                SetSelection([.. Enumerable.Range(start, insertedCount)], lastInsertedIndex);
            }

            return insertedCount;
        }

        /// <summary>
        /// Removes items at <paramref name="indices"/> and clamps selection to a remaining row.
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

            var anchorIndex = _selectedIndex;
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

            var newAnchor = _ClampSelectionIndex(anchorIndex, _items.Count);
            SetSelection(newAnchor >= 0 ? [newAnchor] : [], newAnchor);
            return removedCount;
        }

        /// <summary>
        /// Moves the current selection one position toward <paramref name="offset"/>, independently.
        /// </summary>
        /// <param name="offset">Direction to move (-1 up, +1 down).</param>
        /// <returns><see langword="false"/> when nothing could move.</returns>
        public bool TryMoveBlock(int offset)
        {
            return TryMoveBlock(SelectedIndices, offset, out _);
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
            if (!CanMoveBlock(sourceIndices, offset))
            {
                return false;
            }

            var selectedKeys = _KeysAt(sourceIndices);
            var hasAnchor = _selectedIndex >= 0 && _selectedIndex < _items.Count;
            var trackedAnchor = hasAnchor ? _keyOf(_items[_selectedIndex]) : default;

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
            var newAnchor = hasAnchor
                ? _items.FindIndex(item => EqualityComparer<TKey>.Default.Equals(_keyOf(item), trackedAnchor))
                : -1;
            SetSelection(newIndices, newAnchor);
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
            return TryMoveIndicesTo(sourceIndices, targetIndex, out _);
        }

        /// <summary>
        /// Moves items at <paramref name="sourceIndices"/> to <paramref name="targetIndex"/>, preserving their order.
        /// </summary>
        /// <param name="sourceIndices">Indices of items to move.</param>
        /// <param name="targetIndex">Destination index in the list before the move.</param>
        /// <param name="newIndices">Indices of the moved items after a successful move.</param>
        /// <returns><see langword="false"/> when the move is not allowed.</returns>
        public bool TryMoveIndicesTo(
            IReadOnlyList<int> sourceIndices,
            int targetIndex,
            out IReadOnlyList<int> newIndices
        )
        {
            ArgumentNullException.ThrowIfNull(sourceIndices);

            newIndices = [];
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
            var hasAnchor = _selectedIndex >= 0 && _selectedIndex < _items.Count;
            var trackedAnchor = hasAnchor ? _keyOf(_items[_selectedIndex]) : default;

            foreach (var index in sortedSources.OrderByDescending(i => i))
            {
                _items.RemoveAt(index);
            }

            var insertIndex = targetIndex - sortedSources.Count(index => index < targetIndex);
            insertIndex = Math.Clamp(insertIndex, 0, _items.Count);
            _items.InsertRange(insertIndex, movingItems);

            newIndices = [.. Enumerable.Range(insertIndex, movingItems.Count)];
            var newAnchor = hasAnchor
                ? _items.FindIndex(item => EqualityComparer<TKey>.Default.Equals(_keyOf(item), trackedAnchor))
                : -1;
            SetSelection(newIndices, newAnchor);
            return true;
        }

        /// <summary>
        /// Removes all items and clears selection.
        /// </summary>
        public void Clear()
        {
            _items.Clear();
            _keys.Clear();
            SetSelection([], -1);
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

        private bool _TryInsertItem(int index, TItem item, out int insertedIndex)
        {
            var key = _keyOf(item);
            if (!_keys.Add(key))
            {
                insertedIndex = -1;
                return false;
            }

            insertedIndex = Math.Clamp(index, 0, _items.Count);
            _items.Insert(insertedIndex, item);
            return true;
        }

        /// <summary>
        /// Keys of items at valid indices in <paramref name="indices"/>.
        /// </summary>
        /// <param name="indices">Candidate item indices.</param>
        /// <returns>Unique keys for in-range indices.</returns>
        private HashSet<TKey> _KeysAt(IReadOnlyList<int> indices)
        {
            return
            [
                .. indices.Where(index => index >= 0 && index < _items.Count).Select(index => _keyOf(_items[index])),
            ];
        }

        /// <summary>
        /// Gets whether the item at <paramref name="index"/> is selected and can swap with the neighbor toward
        /// <paramref name="offset"/>.
        /// </summary>
        /// <param name="selectedKeys">Keys of the items being moved.</param>
        /// <param name="index">Candidate item index.</param>
        /// <param name="offset">Direction to move (-1 up, +1 down).</param>
        /// <returns><see langword="true"/> when this item should swap with its neighbor.</returns>
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

        private IReadOnlyList<int> _NormalizeIndices(IReadOnlyList<int> indices)
        {
            return [.. indices.Where(index => index >= 0 && index < _items.Count).Distinct().OrderBy(index => index)];
        }

        private static int _ResolveAnchor(IReadOnlyList<int> indices, int anchorIndex)
        {
            if (indices.Count == 0)
            {
                return -1;
            }

            if (indices.Contains(anchorIndex))
            {
                return anchorIndex;
            }

            return indices[^1];
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
