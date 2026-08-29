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
