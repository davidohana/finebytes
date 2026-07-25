using System.Collections.Immutable;
using Mfr.Utils;

namespace Mfr.Models.Tags
{
    /// <summary>
    /// One known text key and its values in a Xiph or APE tag snapshot.
    /// </summary>
    /// <param name="Key">Canonical field key (typically uppercase for Xiph).</param>
    /// <param name="Values">Trimmed non-empty values for that key.</param>
    public readonly record struct TextFieldRow(string Key, ImmutableArray<string> Values)
    {
        /// <summary>
        /// Returns whether two rows have the same key and the same values in the same order.
        /// </summary>
        /// <param name="other">Row to compare with.</param>
        /// <returns><see langword="true"/> when key and values are ordinal-equal.</returns>
        public bool Equals(TextFieldRow other)
        {
            if (!string.Equals(Key, other.Key, StringComparison.Ordinal))
                return false;

            return OrdinalSequence.AreEqual(Values, other.Values);
        }

        /// <inheritdoc />
        public override int GetHashCode()
        {
            var hash = new HashCode();
            hash.Add(Key, StringComparer.Ordinal);
            foreach (var value in Values.AsSpan())
                hash.Add(value, StringComparer.Ordinal);

            return hash.ToHashCode();
        }

        /// <summary>
        /// Returns whether two field-row arrays are ordinal-equal in order and content.
        /// </summary>
        /// <param name="left">First array of rows, expected to be sorted.</param>
        /// <param name="right">Second array of rows, expected to be sorted.</param>
        /// <returns><see langword="true"/> when both arrays hold equal rows in the same order.</returns>
        public static bool SequenceEquals(ImmutableArray<TextFieldRow> left, ImmutableArray<TextFieldRow> right)
        {
            return left.AsSpan().SequenceEqual(right.AsSpan());
        }

        /// <summary>
        /// Computes a content hash for an ordered sequence of field rows.
        /// </summary>
        /// <param name="fields">Rows to hash, expected to be sorted.</param>
        /// <returns>Hash code derived from every row key and value.</returns>
        public static int GetSequenceHashCode(ImmutableArray<TextFieldRow> fields)
        {
            var hash = new HashCode();
            foreach (var row in fields.AsSpan())
                hash.Add(row);

            return hash.ToHashCode();
        }
    }
}
