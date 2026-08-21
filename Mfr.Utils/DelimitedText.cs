using System.Collections.Immutable;

namespace Mfr.Utils
{
    /// <summary>
    /// Normalizes multi-value text: trimming, splitting, and joining <c>;</c>-delimited lists.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Blank values are treated as absent throughout: they are dropped when splitting or trimming, and a list
    /// with nothing left joins to <see cref="string.Empty"/> or <see langword="null"/> depending on the overload.
    /// Splitting accepts <c>;</c> without spaces; joining always emits <c>"; "</c>.
    /// </para>
    /// </remarks>
    public static class DelimitedText
    {
        private const string _JoinSeparator = "; ";

        private static readonly string[] _ListSeparators = [";"];

        /// <summary>
        /// Trims every value and drops the blank ones.
        /// </summary>
        /// <param name="values">Values to normalize.</param>
        /// <returns>Trimmed non-empty values in source order; empty when <paramref name="values"/> is <see langword="null"/>.</returns>
        public static ImmutableArray<string> TrimNonEmpty(IEnumerable<string>? values)
        {
            if (values is null)
                return [];

            return [.. values.Where(static v => !string.IsNullOrWhiteSpace(v)).Select(static v => v.Trim())];
        }

        /// <summary>
        /// Splits a <c>;</c>-delimited list into its trimmed, non-empty parts.
        /// </summary>
        /// <param name="joined">Delimited text (for example <c>Alice; Bob</c>).</param>
        /// <returns>List parts in source order; empty when <paramref name="joined"/> is blank.</returns>
        public static ImmutableArray<string> Split(string? joined)
        {
            if (string.IsNullOrWhiteSpace(joined))
                return [];

            return
            [
                .. joined
                    .Split(_ListSeparators, StringSplitOptions.TrimEntries)
                    .Where(static part => !string.IsNullOrEmpty(part)),
            ];
        }

        /// <summary>
        /// Joins values into <c>"; "</c>-delimited text, the inverse of <see cref="Split"/>.
        /// </summary>
        /// <param name="values">Values to join; a default or empty array is allowed.</param>
        /// <returns>Delimited text, or <see cref="string.Empty"/> when no value survives trimming.</returns>
        public static string Join(ImmutableArray<string> values)
        {
            return JoinOrNull(values) ?? string.Empty;
        }

        /// <summary>
        /// Joins values into <c>"; "</c>-delimited text, reporting an all-blank list as absent.
        /// </summary>
        /// <param name="values">Values to join; a default or empty array is allowed.</param>
        /// <returns>Delimited text, or <see langword="null"/> when no value survives trimming.</returns>
        public static string? JoinOrNull(ImmutableArray<string> values)
        {
            var span = values.AsSpan();
            if (span.IsEmpty)
                return null;

            var parts = new List<string>(span.Length);
            foreach (var value in span)
            {
                if (string.IsNullOrWhiteSpace(value))
                    continue;

                parts.Add(value.Trim());
            }

            return parts.Count == 0 ? null : string.Join(_JoinSeparator, parts);
        }

        /// <summary>
        /// Joins values into <c>"; "</c>-delimited text, reporting an all-blank sequence as absent.
        /// </summary>
        /// <param name="values">Values to join.</param>
        /// <returns>Delimited text, or <see langword="null"/> when no value survives trimming.</returns>
        public static string? JoinOrNull(IEnumerable<string>? values)
        {
            return JoinOrNull(TrimNonEmpty(values));
        }
    }
}
