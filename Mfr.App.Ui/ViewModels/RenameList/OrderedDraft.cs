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
        /// Appends an item when its key is not already present and selects the new last row.
        /// </summary>
        /// <param name="item">Item to append.</param>
        /// <returns><see langword="false"/> when the key already exists; otherwise <see langword="true"/>.</returns>
        public bool TryAdd(TItem item)
        {
            var key = _keyOf(item);
            if (!_keys.Add(key))
            {
                return false;
            }

            _items.Add(item);
            SelectedIndex = _items.Count - 1;
            return true;
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

            _keys.Remove(_keyOf(_items[index]));
            _items.RemoveAt(index);
            SelectedIndex = _ClampSelectionIndex(index, _items.Count);
            return true;
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

            var targetIndex = index + offset;
            if (targetIndex < 0 || targetIndex >= _items.Count)
            {
                return false;
            }

            (_items[index], _items[targetIndex]) = (_items[targetIndex], _items[index]);
            SelectedIndex = targetIndex;
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
