using System.Collections.Immutable;

namespace Mfr.Models.Tags.Id3v2
{
    /// <summary>
    /// Detached ID3v2 snapshot as modeled text frames (no binary tag blob).
    /// </summary>
    /// <remarks>
    /// <para>
    /// Equality is structural on <see cref="Version"/> and sorted <see cref="Frames"/>. Create paths use
    /// version <c>3</c> (ID3v2.3); patch paths preserve the version read from disk.
    /// </para>
    /// </remarks>
    public sealed class Id3v2TagData : IEquatable<Id3v2TagData?>
    {
        /// <summary>
        /// ID3v2 minor version (for example <c>3</c> for v2.3, <c>4</c> for v2.4).
        /// </summary>
        public byte Version { get; init; }

        /// <summary>
        /// Modeled text frames, sorted by frame identity then text for stable equality.
        /// </summary>
        public ImmutableArray<Id3v2ModeledFrame> Frames { get; init; } = [];

        /// <inheritdoc />
        public bool Equals(Id3v2TagData? other)
        {
            if (other is null)
                return false;

            if (ReferenceEquals(this, other))
                return true;

            if (Version != other.Version || Frames.Length != other.Frames.Length)
                return false;

            for (var i = 0; i < Frames.Length; i++)
            {
                if (!Equals(Frames[i], other.Frames[i]))
                    return false;
            }

            return true;
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
            foreach (var frame in Frames)
                hash.Add(frame);

            return hash.ToHashCode();
        }
    }
}
