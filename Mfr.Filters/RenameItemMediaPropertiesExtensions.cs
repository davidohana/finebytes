using Mfr.Metadata;
using Mfr.Utils;

namespace Mfr.Filters
{
    /// <summary>
    /// Lazily loads TagLib media and MPEG stream properties onto rename rows for formatter tokens.
    /// </summary>
    internal static class RenameItemMediaPropertiesExtensions
    {
        /// <summary>
        /// Ensures <see cref="RenameItem.Original"/> carries media properties read from disk.
        /// </summary>
        /// <param name="item">Rename row to load.</param>
        /// <exception cref="InvalidOperationException">The rename row is a directory.</exception>
        internal static void EnsureMediaPropertiesLoaded(this RenameItem item)
        {
            ArgumentNullException.ThrowIfNull(item);

            if (item.MediaPropertiesLoadAttempted)
            {
                return;
            }

            item.MarkMediaPropertiesLoadAttempted();

            if (item.Original.Attributes.IsDirectory())
            {
                throw new InvalidOperationException("Cannot read media properties for a directory.");
            }

            var snapshot = TagLibFileReader.Read(item.Original.FullPath);
            item.SetMediaProperties(snapshot.Media);
            if (item.EmbeddedTagsLoadAttempted)
            {
                return;
            }

            item.MarkEmbeddedTagsLoadAttempted();
            item.SetEmbeddedTagOverlay(snapshot.Overlay);
        }
    }
}
