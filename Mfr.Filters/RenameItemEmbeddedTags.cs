using Mfr.Metadata;
using Mfr.Models;
using Mfr.Models.Tags;
using Mfr.Utils;

namespace Mfr.Filters
{
    /// <summary>
    /// Loads embedded tag overlays onto rename rows before audio-targeting filters run.
    /// </summary>
    public static class RenameItemEmbeddedTags
    {
        /// <summary>
        /// Maps an absolute path to a detached embedded-tag overlay; defaults to <see cref="AudioTagPersistence.Read"/>.
        /// </summary>
        internal static Func<string, AudioTagOverlay> TagReader { get; set; } = AudioTagPersistence.Read;

        /// <summary>
        /// Ensures <see cref="RenameItem.Original"/> and <see cref="RenameItem.Preview"/> carry embedded tags read from disk.
        /// </summary>
        /// <param name="item">Rename row to load.</param>
        /// <exception cref="InvalidOperationException">The rename row is a directory.</exception>
        internal static void EnsureLoaded(RenameItem item)
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

            var overlay = TagReader(item.Original.FullPath);
            item.SetEmbeddedTagOverlays(overlay);
        }
    }
}
