using Mfr.Models.Tags.Ape;
using Mfr.Models.Tags.Apple;
using Mfr.Models.Tags.Asf;
using Mfr.Models.Tags.Id3v1;
using Mfr.Models.Tags.Id3v2;
using Mfr.Models.Tags.RiffInfo;
using Mfr.Models.Tags.Xiph;

namespace Mfr.Models.Tags
{
    /// <summary>
    /// Structured embedded audio tags: one parsed-field snapshot per <c>TagTypes</c> block.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Semantic values (title, album, performers, …) are obtained by projecting blocks in <c>Mfr.Metadata</c> (for example
    /// <c>CommonAudioTag.FromOverlay</c>). There are no mirrored scalar properties on this type.
    /// </para>
    /// </remarks>
    public sealed class AudioTagOverlay : IEquatable<AudioTagOverlay?>
    {
        /// <summary>
        /// Gets or sets the optional ID3v1 snapshot when the row is backed by MPEG/MP3 structured tags.
        /// </summary>
        public Id3v1TagData? Id3v1 { get; set; }

        /// <summary>
        /// Gets or sets the optional ID3v2 snapshot (modeled text frames) when the row is backed by MPEG/MP3 structured tags.
        /// </summary>
        public Id3v2TagData? Id3v2 { get; set; }

        /// <summary>
        /// Gets or sets the optional Xiph comment block (FLAC, Ogg, Opus, etc.) as known-key fields.
        /// </summary>
        public XiphTagData? Xiph { get; set; }

        /// <summary>
        /// Gets or sets the optional APEv2 tag block as known text-key fields.
        /// </summary>
        public ApeTagData? Ape { get; set; }

        /// <summary>
        /// Gets or sets the optional Apple <c>ilst</c> / MP4 metadata snapshot.
        /// </summary>
        public AppleTagData? Apple { get; set; }

        /// <summary>
        /// Gets or sets the optional ASF extended content descriptor snapshot when the file uses WMA/ASF tagging.
        /// </summary>
        public AsfTagData? Asf { get; set; }

        /// <summary>
        /// Gets or sets the optional RIFF LIST INFO block (classic WAV LIST/INAM, etc.) as known INFO fields.
        /// </summary>
        public RiffInfoTagData? RiffInfo { get; set; }

        /// <summary>
        /// Whether the block for <paramref name="kind"/> is present (non-null) on this overlay.
        /// </summary>
        /// <param name="kind">Block type to probe.</param>
        /// <returns><see langword="true"/> when the logical tag carries that block.</returns>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="kind"/> is not a known block type.</exception>
        public bool HasBlock(AudioTagBlockKind kind)
        {
            return kind switch
            {
                AudioTagBlockKind.Id3v1 => Id3v1 is not null,
                AudioTagBlockKind.Id3v2 => Id3v2 is not null,
                AudioTagBlockKind.Xiph => Xiph is not null,
                AudioTagBlockKind.Ape => Ape is not null,
                AudioTagBlockKind.Apple => Apple is not null,
                AudioTagBlockKind.Asf => Asf is not null,
                AudioTagBlockKind.RiffInfo => RiffInfo is not null,
                _ => throw new ArgumentOutOfRangeException(nameof(kind), kind, "Unknown audio tag block kind."),
            };
        }

        /// <summary>
        /// Whether any tag block is present on this overlay.
        /// </summary>
        /// <returns><see langword="true"/> when at least one block is non-null.</returns>
        public bool HasAnyBlock()
        {
            return Id3v1 is not null
                || Id3v2 is not null
                || Xiph is not null
                || Ape is not null
                || Apple is not null
                || Asf is not null
                || RiffInfo is not null;
        }

        /// <summary>
        /// Drops the block for <paramref name="kind"/> from the logical tag.
        /// </summary>
        /// <remarks>
        /// <para>
        /// A null block on a preview overlay whose original snapshot carried that block is a removal instruction:
        /// commit deletes the whole tag type, including frames this model never parsed (embedded art on that type).
        /// </para>
        /// </remarks>
        /// <param name="kind">Block type to drop; already-absent blocks are left alone.</param>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="kind"/> is not a known block type.</exception>
        public void ClearBlock(AudioTagBlockKind kind)
        {
            switch (kind)
            {
                case AudioTagBlockKind.Id3v1:
                    Id3v1 = null;
                    break;
                case AudioTagBlockKind.Id3v2:
                    Id3v2 = null;
                    break;
                case AudioTagBlockKind.Xiph:
                    Xiph = null;
                    break;
                case AudioTagBlockKind.Ape:
                    Ape = null;
                    break;
                case AudioTagBlockKind.Apple:
                    Apple = null;
                    break;
                case AudioTagBlockKind.Asf:
                    Asf = null;
                    break;
                case AudioTagBlockKind.RiffInfo:
                    RiffInfo = null;
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(kind), kind, "Unknown audio tag block kind.");
            }
        }

        /// <summary>
        /// Ensures a present (possibly empty) block of <paramref name="kind"/> exists for subsequent field writes.
        /// </summary>
        /// <remarks>
        /// <para>
        /// Used when a generic semantic write targets a file that carries no tags yet: create the container's
        /// recommended empty block, then merge fields into it. Already-present blocks are left unchanged.
        /// ID3v2 creates use version <c>3</c> (ID3v2.3).
        /// </para>
        /// </remarks>
        /// <param name="kind">Block type to materialize when absent.</param>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="kind"/> is not a known block type.</exception>
        public void EnsureEmptyBlock(AudioTagBlockKind kind)
        {
            if (HasBlock(kind))
                return;

            switch (kind)
            {
                case AudioTagBlockKind.Id3v1:
                    Id3v1 = new Id3v1TagData();
                    break;
                case AudioTagBlockKind.Id3v2:
                    Id3v2 = new Id3v2TagData { Version = 3, Frames = [] };
                    break;
                case AudioTagBlockKind.Xiph:
                    Xiph = new XiphTagData { Fields = [] };
                    break;
                case AudioTagBlockKind.Ape:
                    Ape = new ApeTagData { Fields = [] };
                    break;
                case AudioTagBlockKind.Apple:
                    Apple = new AppleTagData { Atoms = [] };
                    break;
                case AudioTagBlockKind.Asf:
                    Asf = new AsfTagData { Descriptors = [] };
                    break;
                case AudioTagBlockKind.RiffInfo:
                    RiffInfo = new RiffInfoTagData { Fields = [] };
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(kind), kind, "Unknown audio tag block kind.");
            }
        }

        /// <summary>
        /// Lists the block types currently present on this overlay, in <see cref="AudioTagBlockKind"/> declaration order.
        /// </summary>
        /// <returns>Present block types; empty when the overlay carries no tags.</returns>
        public IReadOnlyList<AudioTagBlockKind> GetPresentBlockKinds()
        {
            return [.. Enum.GetValues<AudioTagBlockKind>().Where(HasBlock)];
        }

        /// <inheritdoc cref="Equals(AudioTagOverlay?)" />
        public bool TagBlocksStructurallyEquals(AudioTagOverlay? other)
        {
            if (other is null)
                return false;

            if (ReferenceEquals(this, other))
                return true;

            if (!Equals(Id3v1, other.Id3v1))
                return false;

            if (!Equals(Id3v2, other.Id3v2))
                return false;

            if (!Equals(Xiph, other.Xiph))
                return false;

            if (!Equals(Ape, other.Ape))
                return false;

            if (!Equals(RiffInfo, other.RiffInfo))
                return false;

            if (!Equals(Apple, other.Apple))
                return false;

            return Equals(Asf, other.Asf);
        }

        /// <summary>
        /// Creates a detached copy suitable for cloning <see cref="FileMeta"/>.
        /// </summary>
        /// <returns>New instance with copied values.</returns>
        public AudioTagOverlay Clone()
        {
            return new AudioTagOverlay
            {
                Id3v1 = Id3v1 is null ? null : Id3v1 with { },
                Id3v2 = Id3v2 is null
                    ? null
                    : new Id3v2TagData
                    {
                        Version = Id3v2.Version,
                        Frames = Id3v2.Frames,
                    },
                Xiph = Xiph is null ? null : new XiphTagData { Fields = Xiph.Fields },
                Ape = Ape is null ? null : new ApeTagData { Fields = Ape.Fields },
                RiffInfo = RiffInfo is null ? null : new RiffInfoTagData { Fields = RiffInfo.Fields },
                Apple = Apple is null ? null : new AppleTagData { Atoms = Apple.Atoms },
                Asf = Asf is null ? null : new AsfTagData { Descriptors = Asf.Descriptors },
            };
        }

        /// <inheritdoc />
        public bool Equals(AudioTagOverlay? other)
        {
            return TagBlocksStructurallyEquals(other);
        }

        /// <inheritdoc />
        public override bool Equals(object? obj)
        {
            return Equals(obj as AudioTagOverlay);
        }

        /// <inheritdoc />
        public override int GetHashCode()
        {
            var hashCode = new HashCode();
            hashCode.Add(Id3v1);
            hashCode.Add(Id3v2);
            hashCode.Add(Xiph);
            hashCode.Add(Ape);
            hashCode.Add(RiffInfo);
            hashCode.Add(Apple);
            hashCode.Add(Asf);
            return hashCode.ToHashCode();
        }
    }
}
