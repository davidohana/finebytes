using System.Collections.Immutable;

namespace Mfr.Metadata.TagFields
{
    /// <summary>
    /// Text normalization shared by the per-type tag field readers and writers.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Overlay blocks store trimmed, non-empty text only: a whitespace-only value means the field is absent.
    /// </para>
    /// </remarks>
    internal static class TagFieldText
    {
        private static readonly string[] _ListSeparators = [";"];

        /// <summary>
        /// Trims <paramref name="text"/>, mapping blank input to <see langword="null"/>.
        /// </summary>
        /// <param name="text">Raw text from a live tag or overlay row.</param>
        /// <returns>Trimmed text, or <see langword="null"/> when empty or whitespace.</returns>
        public static string? NullIfEmpty(string? text)
        {
            return string.IsNullOrWhiteSpace(text) ? null : text.Trim();
        }

        /// <summary>
        /// Trims every value and drops the blank ones.
        /// </summary>
        /// <param name="values">Raw values from a live tag.</param>
        /// <returns>Trimmed non-empty values in source order.</returns>
        public static ImmutableArray<string> TrimNonEmpty(IEnumerable<string>? values)
        {
            if (values is null)
                return [];

            return [.. values
                .Where(static v => !string.IsNullOrWhiteSpace(v))
                .Select(static v => v.Trim())];
        }

        /// <summary>
        /// Wraps one text value as a value array, or an empty array when blank.
        /// </summary>
        /// <param name="text">Raw single-value text (for example a comment frame payload).</param>
        /// <returns>Single-element array, or empty when <paramref name="text"/> is blank.</returns>
        public static ImmutableArray<string> SingleText(string? text)
        {
            var trimmed = NullIfEmpty(text);
            return trimmed is null ? [] : [trimmed];
        }

        /// <summary>
        /// Splits a semicolon-joined list into its parts.
        /// </summary>
        /// <param name="joined">Joined list text (for example <c>Alice; Bob</c>).</param>
        /// <returns>Trimmed non-empty parts, or an empty array when <paramref name="joined"/> is blank.</returns>
        public static string[] SplitJoinedList(string? joined)
        {
            if (string.IsNullOrWhiteSpace(joined))
                return [];

            return [.. joined.Split(_ListSeparators, StringSplitOptions.TrimEntries)
                .Where(static part => !string.IsNullOrEmpty(part))];
        }

        /// <summary>
        /// Joins list values with <c>"; "</c>, the inverse of <see cref="SplitJoinedList"/>.
        /// </summary>
        /// <param name="values">List values to join.</param>
        /// <returns>Joined text, or <see langword="null"/> when no value survives trimming.</returns>
        public static string? JoinList(IEnumerable<string>? values)
        {
            var parts = TrimNonEmpty(values);
            return parts.Length == 0 ? null : string.Join("; ", parts);
        }

        /// <summary>
        /// Compares two value sequences ordinally, element by element then by length.
        /// </summary>
        /// <param name="a">First sequence.</param>
        /// <param name="b">Second sequence.</param>
        /// <returns>Negative, zero, or positive per <see cref="IComparer{T}"/> conventions.</returns>
        public static int CompareSequence(ImmutableArray<string> a, ImmutableArray<string> b)
        {
            var len = Math.Min(a.Length, b.Length);
            for (var i = 0; i < len; i++)
            {
                var c = string.CompareOrdinal(a[i], b[i]);
                if (c != 0)
                    return c;
            }

            return a.Length.CompareTo(b.Length);
        }

        /// <summary>
        /// Returns whether two value sequences are ordinal-equal in order.
        /// </summary>
        /// <param name="a">First sequence.</param>
        /// <param name="b">Second sequence.</param>
        /// <returns><see langword="true"/> when both hold the same values in the same order.</returns>
        public static bool SequenceEquals(ImmutableArray<string> a, ImmutableArray<string> b)
        {
            return a.AsSpan().SequenceEqual(b.AsSpan(), StringComparer.Ordinal);
        }
    }
}
