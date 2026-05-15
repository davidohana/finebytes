using Mfr.Models;

namespace Mfr.Filters.Audio
{
    /// <summary>
    /// Strips all embedded TagLib metadata blobs on each file row (any format TagLib supports); clears the preview overlay.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Directory rows cannot load tags and surface the same <see cref="InvalidOperationException"/> as other audio-overlay operations
    /// (caught during preview and shown as the row’s <see cref="RenameItem.PreviewError"/>).
    /// </para>
    /// </remarks>
    public sealed record EmbeddedTagRemoverFilter() : BaseFilter
    {
        /// <inheritdoc />
        public override string Type => "EmbeddedTagRemover";

        /// <inheritdoc />
        protected internal override void ApplyCore(RenameItem item)
        {
            item.EnsureAudioTagsLoaded();
            item.Preview.AudioTagOverlay = new AudioTagOverlay();
            item.StripAllEmbeddedTagsOnCommit = true;
        }
    }
}
