using System.Collections.Immutable;

namespace Mfr.Utils
{
    /// <summary>
    /// Compares string sequences ordinally, element by element then by length.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Used to give value arrays a stable order and structural equality. A default array counts as empty, so it
    /// sorts before and compares equal to an empty one.
    /// </para>
    /// </remarks>
    public static class OrdinalSequence
    {
        /// <summary>
        /// Compares two sequences by their elements, falling back to length when one is a prefix of the other.
        /// </summary>
        /// <param name="a">First sequence.</param>
        /// <param name="b">Second sequence.</param>
        /// <returns>Negative, zero, or positive per <see cref="IComparer{T}"/> conventions.</returns>
        public static int Compare(ImmutableArray<string> a, ImmutableArray<string> b)
        {
            var left = a.AsSpan();
            var right = b.AsSpan();
            var shared = Math.Min(left.Length, right.Length);

            for (var i = 0; i < shared; i++)
            {
                var byValue = string.CompareOrdinal(left[i], right[i]);
                if (byValue != 0)
                    return byValue;
            }

            return left.Length.CompareTo(right.Length);
        }

        /// <summary>
        /// Returns whether two sequences hold the same values in the same order.
        /// </summary>
        /// <param name="a">First sequence.</param>
        /// <param name="b">Second sequence.</param>
        /// <returns><see langword="true"/> when both sequences are ordinal-equal.</returns>
        public static bool AreEqual(ImmutableArray<string> a, ImmutableArray<string> b)
        {
            return a.AsSpan().SequenceEqual(b.AsSpan(), StringComparer.Ordinal);
        }
    }
}
