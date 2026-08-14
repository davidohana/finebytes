using Mfr.Metadata;
using Mfr.Utils;

namespace Mfr.Filters
{
    /// <summary>
    /// Lazily loads MetadataExtractor image properties and EXIF onto rename rows for formatter tokens.
    /// </summary>
    internal static class RenameItemImagePropertiesExtensions
    {
        /// <summary>
        /// Ensures <see cref="RenameItem.Original"/> carries image properties and EXIF read from disk.
        /// </summary>
        /// <param name="item">Rename row to load.</param>
        /// <exception cref="InvalidOperationException">The rename row is a directory.</exception>
        internal static void EnsureImagePropertiesLoaded(this RenameItem item)
        {
            ArgumentNullException.ThrowIfNull(item);

            if (item.ImagePropertiesLoadAttempted)
                return;

            item.MarkImagePropertiesLoadAttempted();

            if (item.Original.Attributes.IsDirectory())
            {
                throw new InvalidOperationException(
                    "Cannot read image properties for a directory.");
            }

            var snapshot = ImageFileReader.Read(item.Original.FullPath);
            item.SetImageProperties(snapshot.Image);
            item.SetExifData(snapshot.Exif);
        }
    }
}
