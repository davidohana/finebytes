using Mfr.Metadata;
using Mfr.Models;
using Mfr.Utils;

namespace Mfr.Filters.Audio
{
    /// <summary>
    /// Keeps structured per–tag blocks aligned with façade fields during preview filter application.
    /// </summary>
    internal static class RenameItemPreviewAudioSemantics
    {
        /// <summary>
        /// Best-effort merge of façade fields on <see cref="RenameItem.Preview"/><c>.AudioTagOverlay</c>
        /// into native tag blocks using the same rules as <see cref="AudioTagPersistence.Apply"/>.
        /// </summary>
        /// <remarks>
        /// <para>
        /// Uses <see cref="RenameItem.Original"/><c>.FullPath</c> (the path tags were read from). No-ops for directories,
        /// missing files, or when TagLib cannot open the path.
        /// </para>
        /// </remarks>
        /// <param name="item">Active rename row.</param>
        internal static void TryFlushPreviewAudioFacadeIntoNativeBlocks(RenameItem item)
        {
            ArgumentNullException.ThrowIfNull(item);

            if (item.Original.Attributes.IsDirectory())
                return;

            if (!File.Exists(item.Original.FullPath))
                return;

            _ = AudioTagPersistence.TryMaterializePreviewFacadeIntoNativeBlocks(
                item.Preview.AudioTagOverlay,
                item.Original.FullPath);
        }
    }
}
