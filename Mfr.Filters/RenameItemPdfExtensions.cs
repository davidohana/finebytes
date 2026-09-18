using Mfr.Metadata;
using Mfr.Utils;

namespace Mfr.Filters
{
    /// <summary>
    /// Lazily loads PdfPig PDF Info onto rename rows for formatter tokens.
    /// </summary>
    internal static class RenameItemPdfExtensions
    {
        /// <summary>
        /// Ensures <see cref="RenameItem.Original"/> carries PDF Info read from disk.
        /// </summary>
        /// <param name="item">Rename row to load.</param>
        /// <exception cref="InvalidOperationException">The rename row is a directory.</exception>
        internal static void EnsurePdfLoaded(this RenameItem item)
        {
            ArgumentNullException.ThrowIfNull(item);

            if (item.PdfLoadAttempted)
            {
                return;
            }

            item.MarkPdfLoadAttempted();

            if (item.Original.Attributes.IsDirectory())
            {
                throw new InvalidOperationException("Cannot read PDF Info for a directory.");
            }

            item.SetPdfDocumentInfo(PdfFileReader.Read(item.Original.FullPath));
        }
    }
}
