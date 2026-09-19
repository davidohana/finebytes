using System.Runtime.ExceptionServices;
using Mfr.Models.RenameList;
using Mfr.Utils;

namespace Mfr.Filters
{
    /// <summary>
    /// Shared lazy-load shell for disk metadata buckets on <see cref="RenameItem"/>.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Soft load errors stored by the Rename List grid (or by a prior Ensure* failure) are rethrown
    /// on later Ensure* calls so formatter tokens still surface PreviewError instead of expanding empty.
    /// </para>
    /// </remarks>
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
                _RethrowSoftLoadErrorIfAny(item, bucket);
                return;
            }

            item.MarkMetadataLoadAttempted(bucket);

            try
            {
                if (item.Original.Attributes.IsDirectory())
                {
                    throw new InvalidOperationException(directoryErrorMessage);
                }

                loadFromDisk();
            }
            catch (Exception ex) when (RenameItemMetadataLoadFailures.IsReadFailure(ex))
            {
                item.SetMetadataLoadError(bucket, ex);
                throw;
            }
        }

        private static void _RethrowSoftLoadErrorIfAny(RenameItem item, RenameListMetadataRequirement bucket)
        {
            if (item.GetMetadataLoadError(bucket) is not { } loadError)
            {
                return;
            }

            ExceptionDispatchInfo.Capture(loadError).Throw();
        }
    }
}
