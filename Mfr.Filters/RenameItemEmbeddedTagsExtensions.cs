using Mfr.Metadata;
using Mfr.Models;
using Mfr.Utils;

namespace Mfr.Filters
{
    /// <summary>
    /// Loads embedded tag overlays onto rename rows before audio-targeting filters run.
    /// </summary>
    internal static class RenameItemEmbeddedTagsExtensions
    {
        /// <summary>
        /// Ensures <see cref="RenameItem.Original"/> and <see cref="RenameItem.Preview"/> carry embedded tags read from disk.
        /// </summary>
        /// <param name="item">Rename row to load.</param>
        /// <exception cref="InvalidOperationException">The rename row is a directory.</exception>
        internal static void EnsureEmbeddedTagsLoaded(this RenameItem item)
        {
            ArgumentNullException.ThrowIfNull(item);

            if (item.EmbeddedTagsLoadAttempted)
                return;

            item.MarkEmbeddedTagsLoadAttempted();

            if (item.Original.Attributes.IsDirectory())
            {
                throw new InvalidOperationException(
                    "Cannot read audio tags for a directory.");
            }

            var overlay = AudioTagPersistence.Read(item.Original.FullPath);
            item.SetEmbeddedTagOverlay(overlay);
        }
    }
}
