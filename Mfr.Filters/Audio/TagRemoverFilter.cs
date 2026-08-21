using System.Text.Json.Serialization;
using Mfr.Models.Tags;

namespace Mfr.Filters.Audio
{
    /// <summary>
    /// Options for <see cref="TagRemoverFilter"/>.
    /// </summary>
    /// <param name="All">
    /// When <see langword="true"/>, nuclear strip: preview clears every modeled overlay block, and commit calls
    /// <c>RemoveTags(AllTags)</c>. That removes not only the seven <see cref="AudioTagBlockKind"/> values listed in
    /// <paramref name="Blocks"/>, but also TagLib types this overlay never models
    /// (<c>MovieId</c>, <c>DivX</c>, <c>FlacMetadata</c>, <c>TiffIFD</c>, <c>XMP</c>, <c>JpegComment</c>,
    /// <c>GifComment</c>, <c>Png</c>, <c>IPTCIIM</c>, <c>AudibleMetadata</c>, <c>Matroska</c>).
    /// <paramref name="Blocks"/> is ignored.
    /// </param>
    /// <param name="Blocks">
    /// Tag block types to drop when <paramref name="All"/> is <see langword="false"/>, by JSON name
    /// (<c>id3v1</c>, <c>id3v2</c>, <c>xiph</c>, <c>ape</c>, <c>apple</c>, <c>asf</c>, <c>riffInfo</c>).
    /// At least one entry is required unless <paramref name="All"/> is <see langword="true"/>.
    /// Listing every modeled kind is still not a nuclear strip: unmodeled TagLib types stay on disk.
    /// </param>
    public sealed record TagRemoverOptions(
        [property: JsonPropertyName("all")] bool All = false,
        [property: JsonPropertyName("blocks")] IReadOnlyList<AudioTagBlockKind>? Blocks = null
    );

    /// <summary>
    /// Removes embedded tag blocks (selected types, or every TagLib type when <c>all</c> is set).
    /// </summary>
    /// <remarks>
    /// <para>
    /// With <c>all: true</c> (nuclear), preview clears all modeled blocks via <see cref="AudioTagOverlay.ClearAllBlocks"/>
    /// so <see cref="AudioTagOverlay.ContainerFormat"/> stays available for a later generic write, and commit sets
    /// <see cref="RenameItem.StripAllEmbeddedTagsOnCommit"/> so the engine runs <c>RemoveTags(AllTags)</c> before the
    /// (possibly recreated) overlay is applied. Preview looks the same as nulling every modeled block; the extra work
    /// is on disk only — TagLib types outside <see cref="AudioTagBlockKind"/> are wiped too.
    /// </para>
    /// <para>
    /// With selected <c>blocks</c>, preview nulls those blocks only; commit deletes those <c>TagTypes</c> and never
    /// requests a full strip, so later filters can still write into surviving blocks. Unmodeled TagLib types are left alone.
    /// </para>
    /// <para>
    /// Removing a block the row's container cannot hold is an error (surfaced as
    /// <see cref="RenameItem.PreviewError"/>). Removing a supported block the file does not carry is a no-op.
    /// Deleting a tag type also deletes unmodeled content on that block (for example embedded art).
    /// </para>
    /// <para>
    /// Directory rows cannot load tags and surface the same <see cref="InvalidOperationException"/> as other
    /// audio-overlay operations (caught during preview and shown as the row’s <see cref="RenameItem.PreviewError"/>).
    /// </para>
    /// </remarks>
    /// <param name="Options">Whether to strip all tags, or which block types to remove.</param>
    [FilterPalette(FilterGroup.Audio, "Audio Tag Remover")]
    public sealed record TagRemoverFilter(TagRemoverOptions Options) : BaseFilter
    {
        /// <inheritdoc />
        public override string Type => "TagRemover";

        /// <inheritdoc />
        /// <exception cref="ArgumentException">
        /// <c>all</c> is false and <c>blocks</c> is missing or empty, which would make the filter a no-op.
        /// </exception>
        protected override void _Setup()
        {
            if (Options.All)
            {
                return;
            }

            var blocks = Options.Blocks ?? [];
            if (blocks.Count == 0)
            {
                throw new ArgumentException(
                    "TagRemover requires at least one tag block type in 'blocks' when 'all' is false.",
                    nameof(Options)
                );
            }
        }

        /// <inheritdoc />
        protected internal override void ApplyCore(RenameItem item)
        {
            item.EnsureEmbeddedTagsLoaded();

            if (Options.All)
            {
                item.Preview.AudioTagOverlay.ClearAllBlocks();
                item.StripAllEmbeddedTagsOnCommit = true;
                return;
            }

            foreach (var block in Options.Blocks ?? [])
            {
                item.EnsureAudioTagBlockSupported(block);
                item.Preview.AudioTagOverlay.ClearBlock(block);
            }
        }
    }
}
