using System.Collections.Immutable;

namespace Mfr.Models
{
    /// <summary>
    /// Detached snapshot of an ID3v2 tag (full frame inventory for MP3 overlay persistence).
    /// </summary>
    /// <remarks>
    /// <para>
    /// Equality compares only <see cref="Version"/> and <see cref="CanonicalTagBytes"/> (an exact TagLib
    /// <c>Id3v2.Tag.Render()</c> copy). Derived <see cref="Frames"/> can vary with TagLib enumeration details while still
    /// representing the same on-disk tag; commit paths use the frame list, not structural equality of each frame blob.
    /// </para>
    /// </remarks>
    public sealed class Id3v2TagData : IEquatable<Id3v2TagData?>
    {
        /// <summary>
        /// ID3v2 minor version supplied by TagLib when the tag was read (for example <c>3</c> for v2.3, <c>4</c> for v2.4).
        /// </summary>
        public byte Version { get; init; }

        /// <summary>
        /// Full raw tag as returned by TagLib <c>Id3v2.Tag.Render()</c> (defensive copy of the buffer).
        /// </summary>
        public ImmutableArray<byte> CanonicalTagBytes { get; init; } = [];

        /// <summary>
        /// Ordered frames captured from disk; semantics in <see cref="AudioTagOverlay"/> overwrite matching text
        /// frames on commit.
        /// </summary>
        public ImmutableArray<Id3v2SerializedFrame> Frames { get; init; } =
            [];

        /// <inheritdoc />
        public bool Equals(Id3v2TagData? other)
        {
            if (other is null)
                return false;

            if (ReferenceEquals(this, other))
                return true;

            return Version == other.Version
                && CanonicalTagBytes.AsSpan().SequenceEqual(other.CanonicalTagBytes.AsSpan());
        }

        /// <inheritdoc />
        public override bool Equals(object? obj)
        {
            return Equals(obj as Id3v2TagData);
        }

        /// <inheritdoc />
        public override int GetHashCode()
        {
            var hash = new HashCode();
            hash.Add(Version);
            hash.AddBytes(CanonicalTagBytes.AsSpan());
            return hash.ToHashCode();
        }
    }
}
