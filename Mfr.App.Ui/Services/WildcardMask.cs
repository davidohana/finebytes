using System.Collections.Immutable;
using System.Text.RegularExpressions;

namespace Mfr.App.Ui.Services
{
    /// <summary>
    /// Matches file names against Explorer-style wildcard masks (<c>*</c> / <c>?</c>).
    /// </summary>
    internal static class WildcardMask
    {
        /// <summary>
        /// Whether <paramref name="fileName"/> matches <paramref name="pattern"/>.
        /// </summary>
        /// <param name="fileName">File name only, not a full path.</param>
        /// <param name="pattern">Mask such as <c>*</c> or <c>*.mp3</c>. Blank matches every name.</param>
        /// <returns><see langword="true"/> when the name matches.</returns>
        public static bool IsMatch(string fileName, string? pattern)
        {
            if (string.IsNullOrEmpty(pattern))
                return true;

            var regexPattern = _ToAnchoredRegex(pattern);
            try
            {
                return Regex.IsMatch(fileName, regexPattern, RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);
            }
            catch (ArgumentException)
            {
                return false;
            }
        }

        /// <summary>
        /// Whether <paramref name="fileName"/> matches any mask in a <c>:</c>- or <c>;</c>-delimited list.
        /// </summary>
        /// <param name="fileName">File name only, not a full path.</param>
        /// <param name="joinedPatterns">Exclude masks typed in the explorer pane.</param>
        /// <returns><see langword="true"/> when at least one mask matches.</returns>
        public static bool MatchesAny(string fileName, string? joinedPatterns)
        {
            foreach (var pattern in SplitPatterns(joinedPatterns))
            {
                if (IsMatch(fileName, pattern))
                    return true;
            }

            return false;
        }

        /// <summary>
        /// Splits a combined mask list into individual patterns.
        /// </summary>
        /// <param name="joinedPatterns"><c>:</c>- or <c>;</c>-delimited masks.</param>
        /// <returns>Trimmed non-empty patterns in source order.</returns>
        public static ImmutableArray<string> SplitPatterns(string? joinedPatterns)
        {
            if (string.IsNullOrWhiteSpace(joinedPatterns))
                return [];

            return [.. joinedPatterns.Split([':', ';'], StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries)];
        }

        private static string _ToAnchoredRegex(string pattern)
        {
            return "^" + Regex.Escape(pattern).Replace("\\*", ".*", StringComparison.Ordinal).Replace("\\?", ".", StringComparison.Ordinal) + "$";
        }
    }
}
