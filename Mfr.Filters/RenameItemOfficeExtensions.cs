using Mfr.Metadata;
using Mfr.Models.RenameList;
using Mfr.Utils;

namespace Mfr.Filters
{
    /// <summary>
    /// Lazily loads OpenXml Office document Info onto rename rows for formatter tokens.
    /// </summary>
    internal static class RenameItemOfficeExtensions
    {
        /// <summary>
        /// Ensures <see cref="RenameItem.Original"/> carries Office document Info read from disk.
        /// </summary>
        /// <param name="item">Rename row to load.</param>
        /// <exception cref="InvalidOperationException">The rename row is a directory.</exception>
        internal static void EnsureOfficeLoaded(this RenameItem item)
        {
            ArgumentNullException.ThrowIfNull(item);

            if (item.WasMetadataLoadAttempted(RenameListMetadataRequirement.Office))
            {
                return;
            }

            item.MarkMetadataLoadAttempted(RenameListMetadataRequirement.Office);

            if (item.Original.Attributes.IsDirectory())
            {
                throw new InvalidOperationException("Cannot read Office document Info for a directory.");
            }

            item.SetOfficeDocumentInfo(OfficeFileReader.Read(item.Original.FullPath));
        }
    }
}
