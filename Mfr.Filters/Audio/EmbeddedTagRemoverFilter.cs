using Mfr.Models;
using Mfr.Models.Tags;

namespace Mfr.Filters.Audio
{
    /// <summary>
    /// Strips all embedded TagLib metadata on each file row (any format TagLib supports); clears the preview overlay.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Directory rows cannot load tags and surface the same <see cref="InvalidOperationException"/> as other audio-overlay operations
    /// (caught during preview and shown as the row’s <see cref="RenameItem.PreviewError"/>).
    /// </para>
    /// <para>
    /// Preview clears all modeled blocks via <see cref="AudioTagOverlay.ClearAllBlocks"/> so
    /// <see cref="AudioTagOverlay.ContainerFormat"/> stays available for a later generic write that creates the
    /// recommended empty block. On commit, embedded tags are stripped from the destination file before the
    /// (possibly recreated) overlay is applied.
    /// </para>
    /// </remarks>
    public sealed record EmbeddedTagRemoverFilter() : BaseFilter
    {
        /// <inheritdoc />
        public override string Type => "EmbeddedTagRemover";

        /// <inheritdoc />
        protected internal override void ApplyCore(RenameItem item)
        {
            item.EnsureEmbeddedTagsLoaded();
            item.Preview.AudioTagOverlay.ClearAllBlocks();
            item.StripAllEmbeddedTagsOnCommit = true;
        }
    }
}
