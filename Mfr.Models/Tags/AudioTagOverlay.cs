namespace Mfr.Models.Tags
{
    /// <summary>
    /// Structured embedded audio tags: one snapshot per <c>TagTypes</c> block (no mirrored scalar fields).
    /// </summary>
    /// <remarks>
    /// <para>
    /// Semantic values (title, album, performers, …) are obtained by projecting blocks in <c>Mfr.Metadata</c> (for example
    /// <c>AudioTagSemanticSurface.FromBlocks</c>). There are no mirrored scalar properties on this type.
    /// </para>
    /// </remarks>
    public sealed class AudioTagOverlay : IEquatable<AudioTagOverlay?>
    {
        /// <summary>
        /// Gets or sets the optional ID3v1 snapshot when the row is backed by MPEG/MP3 structured tags.
        /// </summary>
        public Id3v1TagData? Id3v1 { get; set; }

        /// <summary>
        /// Gets or sets the optional ID3v2 snapshot (full frame inventory) when the row is backed by MPEG/MP3 structured tags.
        /// </summary>
        public Id3v2TagData? Id3v2 { get; set; }

        /// <summary>
        /// Gets or sets the optional Xiph comment block (FLAC, Ogg, Opus, etc.) as canonical serialized bytes.
        /// </summary>
        public SerializedTagBlob? Xiph { get; set; }

        /// <summary>
        /// Gets or sets the optional APEv2 tag block as canonical serialized bytes.
        /// </summary>
        public SerializedTagBlob? Ape { get; set; }

        /// <summary>
        /// Gets or sets the optional Apple <c>ilst</c> / MP4 metadata snapshot.
        /// </summary>
        public AppleTagData? Apple { get; set; }

        /// <summary>
        /// Gets or sets the optional ASF extended content descriptor snapshot when the file uses WMA/ASF tagging.
        /// </summary>
        public AsfTagData? Asf { get; set; }

        /// <summary>
        /// Gets or sets the optional RIFF LIST INFO block (classic WAV LIST/INAM, etc.) as canonical serialized bytes.
        /// </summary>
        public SerializedTagBlob? RiffInfo { get; set; }

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
                        CanonicalTagBytes = Id3v2.CanonicalTagBytes,
                        Frames = Id3v2.Frames,
                    },
                Xiph = Xiph is null ? null : new SerializedTagBlob { CanonicalTagBytes = Xiph.CanonicalTagBytes },
                Ape = Ape is null ? null : new SerializedTagBlob { CanonicalTagBytes = Ape.CanonicalTagBytes },
                RiffInfo = RiffInfo is null ? null : new SerializedTagBlob { CanonicalTagBytes = RiffInfo.CanonicalTagBytes },
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
