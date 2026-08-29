using Mfr.Metadata;
using Mfr.Models.Tags;
using Mfr.Utils;

namespace Mfr.Filters
{
    /// <summary>
    /// Loads TagLib snapshots onto rename rows before audio-targeting filters and media tokens run.
    /// </summary>
    internal static class RenameItemEmbeddedTagsExtensions
    {
        /// <summary>
        /// Ensures <see cref="RenameItem.Original"/> and <see cref="RenameItem.Preview"/> carry TagLib tags and media from disk.
        /// </summary>
        /// <param name="item">Rename row to load.</param>
        /// <exception cref="InvalidOperationException">The rename row is a directory.</exception>
        internal static void EnsureTagLibLoaded(this RenameItem item)
        {
            ArgumentNullException.ThrowIfNull(item);

            if (item.TagLibLoadAttempted)
            {
                return;
            }

            item.MarkTagLibLoadAttempted();

            if (item.Original.Attributes.IsDirectory())
            {
                throw new InvalidOperationException("Cannot read TagLib metadata for a directory.");
            }

            var snapshot = TagLibFileReader.Read(item.Original.FullPath);
            item.SetEmbeddedTagOverlay(snapshot.Overlay);
            item.SetMediaProperties(snapshot.Media);
        }

        /// <summary>
        /// Ensures the row's container can hold <paramref name="blockKind"/> before a format-specific tag edit runs.
        /// </summary>
        /// <param name="item">Rename row whose container was detected by <see cref="EnsureTagLibLoaded"/>.</param>
        /// <param name="blockKind">Tag block the caller is about to read or write.</param>
        /// <exception cref="NotSupportedException">The row's container does not support that block type.</exception>
        internal static void EnsureAudioTagBlockSupported(this RenameItem item, AudioTagBlockKind blockKind)
        {
            ArgumentNullException.ThrowIfNull(item);

            item.EnsureTagLibLoaded();
            var containerFormat = item.Preview.AudioTagOverlay.ContainerFormat;
            AudioTagContainerPolicy.EnsureSupported(containerFormat, blockKind);
        }
    }
}
