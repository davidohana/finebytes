using Mfr.Models.Rename;

namespace Mfr.Models.Filters
{
    /// <summary>
    /// Public read access to the string addressed by a <see cref="FilterTarget"/> on a <see cref="FileMeta"/> snapshot.
    /// </summary>
    public static class FilterTargetText
    {
        /// <summary>
        /// Tries to read the string for <paramref name="target"/> from <paramref name="meta"/>.
        /// </summary>
        /// <param name="meta">Metadata snapshot (often <see cref="RenameItem.Original"/> for sample helpers).</param>
        /// <param name="target">Filter apply target.</param>
        /// <param name="text">Resolved string when the call succeeds; otherwise empty.</param>
        /// <returns>
        /// <see langword="true"/> when the target is supported and resolution did not throw; otherwise <see langword="false"/>.
        /// </returns>
        public static bool TryGet(FileMeta meta, FilterTarget target, out string text)
        {
            ArgumentNullException.ThrowIfNull(meta);
            ArgumentNullException.ThrowIfNull(target);

            try
            {
                text = meta.GetTargetString(target);
                return true;
            }
            catch (Exception ex)
                when (ex is NotSupportedException or ArgumentOutOfRangeException or InvalidOperationException)
            {
                text = string.Empty;
                return false;
            }
        }

        /// <summary>
        /// Tries to read the string for <paramref name="target"/> from <paramref name="item"/>'s original snapshot.
        /// </summary>
        /// <param name="item">Rename list row.</param>
        /// <param name="target">Filter apply target.</param>
        /// <param name="text">Resolved string when the call succeeds; otherwise empty.</param>
        /// <returns>
        /// <see langword="true"/> when the target is supported and resolution did not throw; otherwise <see langword="false"/>.
        /// </returns>
        public static bool TryGet(RenameItem item, FilterTarget target, out string text)
        {
            ArgumentNullException.ThrowIfNull(item);
            return TryGet(item.Original, target, out text);
        }
    }
}
