namespace Mfr.App.Ui.Views
{
    /// <summary>
    /// Shared right-click policy: keep a selection that already includes the hit row, otherwise select only that row.
    /// </summary>
    internal static class ContextMenuHitSelection
    {
        /// <summary>
        /// Runs <paramref name="selectHitOnly"/> when <paramref name="hit"/> is not already selected.
        /// </summary>
        /// <typeparam name="T">Row / entry type.</typeparam>
        /// <param name="selected">Current selection.</param>
        /// <param name="hit">Row under the pointer.</param>
        /// <param name="selectHitOnly">Selects only <paramref name="hit"/> and syncs the control.</param>
        /// <param name="equals">Optional equality; defaults to <see cref="EqualityComparer{T}.Default"/>.</param>
        /// <returns><see langword="true"/> when the selection was changed to the hit alone.</returns>
        public static bool SelectHitIfNeeded<T>(
            IReadOnlyList<T> selected,
            T hit,
            Action selectHitOnly,
            Func<T, T, bool>? equals = null
        )
        {
            ArgumentNullException.ThrowIfNull(selected);
            ArgumentNullException.ThrowIfNull(hit);
            ArgumentNullException.ThrowIfNull(selectHitOnly);

            equals ??= EqualityComparer<T>.Default.Equals;
            foreach (var item in selected)
            {
                if (equals(item, hit))
                {
                    return false;
                }
            }

            selectHitOnly();
            return true;
        }
    }
}
