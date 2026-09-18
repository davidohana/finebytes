using Mfr.Metadata;
using Mfr.Models.RenameList;
using Mfr.Utils;

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
            ArgumentNullException.ThrowIfNull(item);

            if (item.WasMetadataLoadAttempted(RenameListMetadataRequirement.Epub))
            {
                return;
            }

            item.MarkMetadataLoadAttempted(RenameListMetadataRequirement.Epub);

            if (item.Original.Attributes.IsDirectory())
            {
                throw new InvalidOperationException("Cannot read EPUB document Info for a directory.");
            }

            item.SetEpubDocumentInfo(EpubFileReader.Read(item.Original.FullPath));
        }
    }
}
