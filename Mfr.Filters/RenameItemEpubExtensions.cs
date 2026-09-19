using Mfr.Metadata;
using Mfr.Models.RenameList;

namespace Mfr.Filters
{
    /// <summary>
    /// Lazily loads VersOne.Epub document Info onto rename rows for formatter tokens.
    /// </summary>
    internal static class RenameItemEpubExtensions
    {
        /// <summary>
        /// Ensures <see cref="RenameItem.Original"/> carries EPUB Info read from disk.
        /// </summary>
        /// <param name="item">Rename row to load.</param>
        /// <exception cref="InvalidOperationException">The rename row is a directory.</exception>
        internal static void EnsureEpubLoaded(this RenameItem item)
        {
            RenameItemMetadataEnsure.EnsureLoaded(
                item,
                RenameListMetadataRequirement.Epub,
                "Cannot read EPUB document Info for a directory.",
                () => item.SetEpubDocumentInfo(EpubFileReader.Read(item.Original.FullPath))
            );
        }
    }
}
