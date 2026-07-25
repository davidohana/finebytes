using System.Text.Json.Serialization;
using Mfr.Models;
using Mfr.Models.Tags;

namespace Mfr.Filters.Audio
{
    /// <summary>
    /// Options for <see cref="EmbeddedTagTypeRemoverFilter"/>.
    /// </summary>
    /// <param name="Blocks">
    /// Tag block types to drop, by JSON name (<c>id3v1</c>, <c>id3v2</c>, <c>xiph</c>, <c>ape</c>, <c>apple</c>,
    /// <c>asf</c>, <c>riffInfo</c>). At least one entry is required.
    /// </param>
    public sealed record EmbeddedTagTypeRemoverOptions(
        [property: JsonPropertyName("blocks")] IReadOnlyList<AudioTagBlockKind> Blocks);

    /// <summary>
    /// Removes selected embedded tag types (for example the ID3v1 trailer) and leaves the file's other tag blocks intact.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Preview nulls the chosen blocks on the row's overlay; commit deletes those <c>TagTypes</c> from the destination
    /// file. Unlike <see cref="EmbeddedTagRemoverFilter"/> this never requests a full strip, so later filters can still
    /// write semantics into the blocks that remain.
    /// </para>
    /// <para>
    /// Removing a block the row's container cannot hold is an error, not a silent skip: the row surfaces the
    /// <see cref="NotSupportedException"/> as its <see cref="RenameItem.PreviewError"/>. Removing a supported block the
    /// file does not actually carry is a no-op.
    /// </para>
    /// <para>
    /// Deleting a tag type also deletes content this model never parses, such as embedded art stored on that block.
    /// </para>
    /// </remarks>
    /// <param name="Options">Which tag block types to remove.</param>
    public sealed record EmbeddedTagTypeRemoverFilter(
        EmbeddedTagTypeRemoverOptions Options) : BaseFilter
    {
        /// <inheritdoc />
        public override string Type => "EmbeddedTagTypeRemover";

        /// <inheritdoc />
        /// <exception cref="ArgumentException"><c>blocks</c> is empty, which would make the filter a no-op.</exception>
        protected override void _Setup()
        {
            if (Options.Blocks.Count == 0)
            {
                throw new ArgumentException(
                    "EmbeddedTagTypeRemover requires at least one tag block type in 'blocks'.",
                    nameof(Options));
            }
        }

        /// <inheritdoc />
        protected internal override void ApplyCore(RenameItem item)
        {
            item.EnsureEmbeddedTagsLoaded();

            foreach (var block in Options.Blocks)
            {
                item.EnsureAudioTagBlockSupported(block);
                item.Preview.AudioTagOverlay.ClearBlock(block);
            }
        }
    }
}
