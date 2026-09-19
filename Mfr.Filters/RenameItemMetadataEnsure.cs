using Mfr.Models.RenameList;
using Mfr.Utils;

namespace Mfr.Filters
{
    /// <summary>
    /// Shared lazy-load shell for disk metadata buckets on <see cref="RenameItem"/>.
    /// </summary>
    internal static class RenameItemMetadataEnsure
    {
        /// <summary>
        /// Marks the bucket attempted once, rejects directories, then runs <paramref name="loadFromDisk"/>.
        /// </summary>
        /// <param name="item">Rename row to load.</param>
        /// <param name="bucket">Single flag from <see cref="RenameListMetadataBuckets.All"/>.</param>
        /// <param name="directoryErrorMessage">Thrown when the row is a directory.</param>
        /// <param name="loadFromDisk">Reader + Set* call for this bucket.</param>
        /// <exception cref="InvalidOperationException">The rename row is a directory.</exception>
        internal static void EnsureLoaded(
            RenameItem item,
            RenameListMetadataRequirement bucket,
            string directoryErrorMessage,
            Action loadFromDisk
        )
        {
            ArgumentNullException.ThrowIfNull(item);
            ArgumentNullException.ThrowIfNull(directoryErrorMessage);
            ArgumentNullException.ThrowIfNull(loadFromDisk);

            if (item.WasMetadataLoadAttempted(bucket))
            {
                return;
            }

            item.MarkMetadataLoadAttempted(bucket);

            if (item.Original.Attributes.IsDirectory())
            {
                throw new InvalidOperationException(directoryErrorMessage);
            }

            loadFromDisk();
        }
    }
}
