using System.Collections.Immutable;
using System.Text.RegularExpressions;

namespace Mfr.App.Ui.Services.FileList
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
            {
                return true;
            }

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
        /// Whether <paramref name="fileName"/> matches any mask in a delimited list.
        /// </summary>
        /// <param name="fileName">File name only, not a full path.</param>
        /// <param name="joinedPatterns">Exclude masks from session or the Exclude Masks dialog.</param>
        /// <returns><see langword="true"/> when at least one mask matches.</returns>
        public static bool MatchesAny(string fileName, string? joinedPatterns)
        {
            foreach (var pattern in SplitPatterns(joinedPatterns))
            {
                if (IsMatch(fileName, pattern))
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// Splits a combined mask list into individual patterns.
        /// </summary>
        /// <param name="joinedPatterns">
        /// Masks separated by <c>:</c>, <c>;</c>, <c>|</c>, or newlines (MFR 7 dialog / config forms).
        /// </param>
        /// <returns>Trimmed non-empty patterns in source order.</returns>
        public static ImmutableArray<string> SplitPatterns(string? joinedPatterns)
        {
            if (string.IsNullOrWhiteSpace(joinedPatterns))
            {
                return [];
            }

            return
            [
                .. joinedPatterns.Split(
                    [':', ';', '|', '\r', '\n'],
                    StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries
                ),
            ];
        }

        /// <summary>
        /// Formats stored masks as one pattern per line for the Exclude Masks dialog.
        /// </summary>
        /// <param name="joinedPatterns">Persisted or in-memory mask list.</param>
        /// <returns>Newline-separated patterns, or empty when none.</returns>
        public static string FormatForEditor(string? joinedPatterns)
        {
            var patterns = SplitPatterns(joinedPatterns);
            if (patterns.IsDefaultOrEmpty)
            {
                return string.Empty;
            }

            return string.Join(Environment.NewLine, patterns);
        }

        /// <summary>
        /// Normalizes editor text into a <c>;</c>-delimited list for persistence.
        /// </summary>
        /// <param name="editorText">Multiline or delimited masks from the dialog.</param>
        /// <returns><c>;</c>-joined patterns, or empty when none.</returns>
        public static string NormalizeForStorage(string? editorText)
        {
            var patterns = SplitPatterns(editorText);
            if (patterns.IsDefaultOrEmpty)
            {
                return string.Empty;
            }

            return string.Join(';', patterns);
        }

        private static string _ToAnchoredRegex(string pattern)
        {
            return "^"
                + Regex
                    .Escape(pattern)
                    .Replace("\\*", ".*", StringComparison.Ordinal)
                    .Replace("\\?", ".", StringComparison.Ordinal)
                + "$";
        }
    }
}
