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
        /// Whether <paramref name="fileName"/> matches any mask in <paramref name="patterns"/>.
        /// </summary>
        /// <param name="fileName">File name only, not a full path.</param>
        /// <param name="patterns">Exclude masks from session or the Exclude Masks dialog.</param>
        /// <returns><see langword="true"/> when at least one mask matches.</returns>
        public static bool MatchesAny(string fileName, IEnumerable<string>? patterns)
        {
            if (patterns is null)
            {
                return false;
            }

            foreach (var pattern in patterns)
            {
                if (string.IsNullOrWhiteSpace(pattern))
                {
                    continue;
                }

                if (IsMatch(fileName, pattern.Trim()))
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// Formats stored masks as one pattern per line for the Exclude Masks dialog.
        /// </summary>
        /// <param name="patterns">Persisted or in-memory mask list.</param>
        /// <returns>Newline-separated patterns, or empty when none.</returns>
        public static string FormatForEditor(IEnumerable<string>? patterns)
        {
            if (patterns is null)
            {
                return string.Empty;
            }

            var lines = patterns
                .Where(static p => !string.IsNullOrWhiteSpace(p))
                .Select(static p => p.Trim())
                .ToArray();
            if (lines.Length == 0)
            {
                return string.Empty;
            }

            return string.Join(Environment.NewLine, lines);
        }

        /// <summary>
        /// Parses Exclude Masks dialog text into a trimmed pattern list (one mask per line).
        /// </summary>
        /// <param name="editorText">Multiline masks from the dialog.</param>
        /// <returns>Trimmed patterns, or empty when none.</returns>
        public static IReadOnlyList<string> NormalizeForStorage(string? editorText)
        {
            if (string.IsNullOrWhiteSpace(editorText))
            {
                return [];
            }

            return
            [
                .. editorText.Split(
                    ['\r', '\n'],
                    StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries
                ),
            ];
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
