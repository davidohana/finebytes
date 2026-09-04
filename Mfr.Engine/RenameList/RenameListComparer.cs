namespace Mfr.Engine.RenameList
{
    /// <summary>
    /// Provides sorting logic for <see cref="RenameItem"/> collections based on prioritized sort keys.
    /// </summary>
    internal static class RenameListComparer
    {
        /// <summary>
        /// Compares two rename items based on the provided sort keys in priority order.
        /// </summary>
        /// <param name="left">The first item to compare.</param>
        /// <param name="right">The second item to compare.</param>
        /// <param name="keys">The ordered list of sort keys to evaluate.</param>
        /// <returns>An integer indicating the relative order of the items.</returns>
        public static int CompareItems(RenameItem left, RenameItem right, IReadOnlyList<RenameListSortKey> keys)
        {
            foreach (var key in keys)
            {
                if (!RenameListFieldCatalog.IsSortableKey(key.FieldKey))
                {
                    continue;
                }

                var cmp = RenameListFieldCatalog.CompareForSort(left, key.FieldKey, right);
                if (key.Descending)
                {
                    cmp = -cmp;
                }

                if (cmp != 0)
                {
                    return cmp;
                }
            }

            // MFR7 RenameItemComparer: equal keys keep add order so a second Sort does not reshuffle ties.
            return left.Original.RenameListIndex.CompareTo(right.Original.RenameListIndex);
        }
    }
}
