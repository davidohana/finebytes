using System.Collections.Immutable;

namespace Mfr.Models.Tags
{
    /// <summary>
    /// Raw embedded tag bytes for a single TagLib container (for example Xiph comment or APEv2) used for stable overlay identity.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Equality is by byte sequence only (see <see cref="CanonicalTagBytes"/>); the source container is implied by which
    /// <see cref="AudioTagOverlay"/> property holds this instance.
    /// </para>
    /// </remarks>
    public sealed class SerializedTagBlob : IEquatable<SerializedTagBlob?>
    {
        /// <summary>
        /// Defensive copy of the tag’s canonical render (for example <c>XiphComment.Render</c> or <c>Ape.Tag.Render</c>).
        /// </summary>
        public ImmutableArray<byte> CanonicalTagBytes { get; init; } = [];

        /// <inheritdoc />
        public bool Equals(SerializedTagBlob? other)
        {
            if (other is null)
                return false;

            if (ReferenceEquals(this, other))
                return true;

            return CanonicalTagBytes.AsSpan().SequenceEqual(other.CanonicalTagBytes.AsSpan());
        }

        /// <inheritdoc />
        public override bool Equals(object? obj)
        {
            return Equals(obj as SerializedTagBlob);
        }

        /// <inheritdoc />
        public override int GetHashCode()
        {
            var hash = new HashCode();
            hash.AddBytes(CanonicalTagBytes.AsSpan());
            return hash.ToHashCode();
        }
    }
}
