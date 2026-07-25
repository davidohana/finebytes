using System.Collections.Immutable;

namespace Mfr.Models.Tags
{
    /// <summary>
    /// Detached snapshot of an MP4 QuickTime <c>ilst</c> / Apple tag (atom ids and text values).
    /// </summary>
    /// <remarks>
    /// <para>
    /// Used when an MP4 file exposes Apple <c>ilst</c> metadata via TagLib without a single portable binary render; rows are sorted
    /// by <see cref="AppleAtomRow.AtomType"/> (four-byte box id) then values for stable equality.
    /// </para>
    /// </remarks>
    public sealed class AppleTagData : IEquatable<AppleTagData?>
    {
        /// <summary>
        /// Sorted atom rows (four-byte <c>ilst</c> item types and text values) when the file has Apple metadata.
        /// </summary>
        public ImmutableArray<AppleAtomRow> Atoms { get; init; } = [];

        /// <inheritdoc />
        public bool Equals(AppleTagData? other)
        {
            if (other is null)
                return false;

            if (ReferenceEquals(this, other))
                return true;

            if (Atoms.Length != other.Atoms.Length)
                return false;

            var comparer = EqualityComparer<AppleAtomRow>.Default;
            for (var i = 0; i < Atoms.Length; i++)
            {
                if (!comparer.Equals(Atoms[i], other.Atoms[i]))
                    return false;
            }

            return true;
        }

        /// <inheritdoc />
        public override bool Equals(object? obj)
        {
            return Equals(obj as AppleTagData);
        }

        /// <inheritdoc />
        public override int GetHashCode()
        {
            var hash = new HashCode();
            foreach (var row in Atoms)
                hash.Add(row);

            return hash.ToHashCode();
        }
    }

    /// <summary>
    /// One Apple <c>ilst</c> entry: four-byte atom / box type and text values.
    /// </summary>
    public readonly struct AppleAtomRow : IEquatable<AppleAtomRow>
    {
        /// <summary>
        /// Four-byte MP4 box type (matches TagLib <c>Box.BoxType</c> bytes).
        /// </summary>
        public ImmutableArray<byte> AtomType { get; init; }

        /// <summary>
        /// Trimmed text values for that atom (order preserved).
        /// </summary>
        public ImmutableArray<string> Values { get; init; }

        /// <inheritdoc />
        public bool Equals(AppleAtomRow other)
        {
            if (AtomType.Length != other.AtomType.Length || Values.Length != other.Values.Length)
                return false;

            if (!AtomType.AsSpan().SequenceEqual(other.AtomType.AsSpan()))
                return false;

            for (var i = 0; i < Values.Length; i++)
            {
                if (!string.Equals(Values[i], other.Values[i], StringComparison.Ordinal))
                    return false;
            }

            return true;
        }

        /// <inheritdoc />
        public override bool Equals(object? obj)
        {
            return obj is AppleAtomRow r && Equals(r);
        }

        /// <inheritdoc />
        public override int GetHashCode()
        {
            var hash = new HashCode();
            hash.AddBytes(AtomType.AsSpan());

            foreach (var v in Values)
                hash.Add(v, StringComparer.Ordinal);

            return hash.ToHashCode();
        }

        /// <summary>
        /// Equality operator.
        /// </summary>
        public static bool operator ==(AppleAtomRow left, AppleAtomRow right)
        {
            return left.Equals(right);
        }

        /// <summary>
        /// Inequality operator.
        /// </summary>
        public static bool operator !=(AppleAtomRow left, AppleAtomRow right)
        {
            return !left.Equals(right);
        }
    }
}
