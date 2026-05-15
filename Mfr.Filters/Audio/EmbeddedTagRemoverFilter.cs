using Mfr.Models;
using Mfr.Models.Tags;

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
    /// <para>
    /// On commit, embedded tags are stripped from the destination file before the empty overlay is applied,
    /// matching the Core finalize ordering so disk state matches the cleared preview.
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
