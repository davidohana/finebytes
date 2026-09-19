using Mfr.Metadata;
using Mfr.Models.RenameList;

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
            RenameItemMetadataEnsure.EnsureLoaded(
                item,
                RenameListMetadataRequirement.Office,
                "Cannot read Office document Info for a directory.",
                () => item.SetOfficeDocumentInfo(OfficeFileReader.Read(item.Original.FullPath))
            );
        }
    }
}
