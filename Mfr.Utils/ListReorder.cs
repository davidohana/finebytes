namespace Mfr.Utils
{
    /// <summary>
    /// Neighbor-swap moves for ordered lists with multi-selection (MFR7 manual sort).
    /// </summary>
    /// <remarks>
    /// <para>
    /// Contiguous selected blocks slide as a unit. A scattered selection only swaps items that have an
    /// unselected neighbor in the move direction.
    /// </para>
    /// </remarks>
    public static class ListReorder
    {
        /// <summary>
        /// Gets whether any selected item can swap one step toward <paramref name="offset"/>.
        /// </summary>
        /// <typeparam name="T">List element type.</typeparam>
        /// <param name="items">Ordered list.</param>
        /// <param name="selected">Items to move.</param>
        /// <param name="offset">Direction to move (-1 up, +1 down).</param>
        /// <returns>
        /// <see langword="true"/> when at least one selected item has an unselected neighbor in that direction.
        /// </returns>
        public static bool CanMoveSelectedTowardNeighbor<T>(IList<T> items, ISet<T> selected, int offset)
        {
            ArgumentNullException.ThrowIfNull(items);
            ArgumentNullException.ThrowIfNull(selected);

            if (offset is not (-1 or 1) || selected.Count == 0 || items.Count == 0)
            {
                return false;
            }

            var walkStep = -offset;
            var startIndex = walkStep > 0 ? 0 : items.Count - 1;
            for (var index = startIndex; index >= 0 && index < items.Count; index += walkStep)
            {
                if (_CanSwapTowardNeighbor(items, selected, index, offset))
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// Moves selected items one step toward <paramref name="offset"/> using neighbor swaps.
        /// </summary>
        /// <typeparam name="T">List element type.</typeparam>
        /// <param name="items">Ordered list to mutate in place.</param>
        /// <param name="selected">Items to move.</param>
        /// <param name="offset">Direction to move (-1 up, +1 down).</param>
        /// <returns><see langword="true"/> when at least one item changed position.</returns>
        public static bool TryMoveSelectedTowardNeighbor<T>(IList<T> items, ISet<T> selected, int offset)
        {
            ArgumentNullException.ThrowIfNull(items);
            ArgumentNullException.ThrowIfNull(selected);

            if (!CanMoveSelectedTowardNeighbor(items, selected, offset))
            {
                return false;
            }

            var moved = false;
            var walkStep = -offset;
            var startIndex = walkStep > 0 ? 0 : items.Count - 1;
            for (var index = startIndex; index >= 0 && index < items.Count; index += walkStep)
            {
                if (!_CanSwapTowardNeighbor(items, selected, index, offset))
                {
                    continue;
                }

                var neighborIndex = index + offset;
                (items[index], items[neighborIndex]) = (items[neighborIndex], items[index]);
                moved = true;
            }

            return moved;
        }

        /// <summary>
        /// Moves items at <paramref name="sourceIndices"/> to <paramref name="targetIndex"/>, preserving their order.
        /// </summary>
        /// <typeparam name="T">List element type.</typeparam>
        /// <param name="items">Ordered list to mutate in place.</param>
        /// <param name="sourceIndices">Indices of items to move.</param>
        /// <param name="targetIndex">Destination index in the list before the move.</param>
        /// <param name="newIndices">Indices of the moved items after a successful move.</param>
        /// <returns><see langword="false"/> when the move is not allowed or is a no-op.</returns>
        public static bool TryMoveIndicesTo<T>(
            IList<T> items,
            IReadOnlyList<int> sourceIndices,
            int targetIndex,
            out IReadOnlyList<int> newIndices
        )
        {
            ArgumentNullException.ThrowIfNull(items);
            ArgumentNullException.ThrowIfNull(sourceIndices);

            newIndices = [];
            var sortedSources = sourceIndices.Distinct().OrderBy(index => index).ToList();
            if (sortedSources.Count == 0)
            {
                return false;
            }

            if (sortedSources.Any(index => index < 0 || index >= items.Count))
            {
                return false;
            }

            var before = items.ToList();
            targetIndex = Math.Clamp(targetIndex, 0, items.Count);
            var movingItems = sortedSources.Select(index => items[index]).ToList();

            foreach (var index in sortedSources.OrderByDescending(index => index))
            {
                items.RemoveAt(index);
            }

            var insertIndex = targetIndex - sortedSources.Count(index => index < targetIndex);
            insertIndex = Math.Clamp(insertIndex, 0, items.Count);
            for (var offset = 0; offset < movingItems.Count; offset++)
            {
                items.Insert(insertIndex + offset, movingItems[offset]);
            }

            if (before.SequenceEqual(items))
            {
                return false;
            }

            newIndices = [.. Enumerable.Range(insertIndex, movingItems.Count)];
            return true;
        }

        /// <summary>
        /// Whether the item at <paramref name="index"/> is selected and can swap with the neighbor toward
        /// <paramref name="offset"/>.
        /// </summary>
        private static bool _CanSwapTowardNeighbor<T>(IList<T> items, ISet<T> selected, int index, int offset)
        {
            if (!selected.Contains(items[index]))
            {
                return false;
            }

            var neighborIndex = index + offset;
            if (neighborIndex < 0 || neighborIndex >= items.Count)
            {
                return false;
            }

            return !selected.Contains(items[neighborIndex]);
        }
    }
}
