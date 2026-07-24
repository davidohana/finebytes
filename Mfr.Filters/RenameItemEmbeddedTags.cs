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
        /// Reads embedded tags from disk for production preview/commit paths.
        /// </summary>
        internal static Func<string, AudioTagOverlay> TagReader { get; set; } = AudioTagPersistence.Read;

        /// <summary>
        /// Optional per-async-context reader override for unit tests with synthetic paths.
        /// </summary>
        internal static AsyncLocal<Func<string, AudioTagOverlay>?> TestTagReaderOverride { get; } = new();

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

            var reader = TestTagReaderOverride.Value ?? TagReader;
            var overlay = reader(item.Original.FullPath);
            item.SetEmbeddedTagOverlays(overlay);
        }
    }
}
