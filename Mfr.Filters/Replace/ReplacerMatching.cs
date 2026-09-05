using System.Text;
using System.Text.RegularExpressions;

namespace Mfr.Filters.Replace
{
    /// <summary>
    /// Shared find/replace matching for <see cref="ReplacerFilter"/> and <see cref="ReplaceListFilter"/>.
    /// </summary>
    internal static class ReplacerMatching
    {
        /// <summary>
        /// Compiles <paramref name="pattern"/> when non-empty so invalid regex fails at Setup.
        /// </summary>
        /// <param name="pattern">Regular expression text (empty is a no-op).</param>
        /// <param name="paramName">Optional argument name for <see cref="ArgumentException"/>.</param>
        /// <exception cref="ArgumentException">When <paramref name="pattern"/> is not a valid regular expression.</exception>
        internal static void ValidateRegexPattern(string pattern, string? paramName = null)
        {
            if (pattern.Length == 0)
            {
                return;
            }

            try
            {
                _ = new Regex(pattern);
            }
            catch (ArgumentException ex)
            {
                throw new ArgumentException($"Invalid regular expression: {ex.Message}", paramName, ex);
            }
        }

        /// <summary>
        /// Applies one find/replace pass to <paramref name="segment"/>.
        /// </summary>
        /// <param name="segment">Text to transform.</param>
        /// <param name="options">Find/replace options for this pass.</param>
        /// <returns>Transformed text; unchanged when <see cref="ReplacerOptions.Find"/> is empty.</returns>
        internal static string ReplaceSegment(string segment, ReplacerOptions options)
        {
            if (options.Find.Length == 0)
            {
                return segment;
            }

            var match = options.Match;
            var regexOptions = match.CaseSensitive ? RegexOptions.None : RegexOptions.IgnoreCase;
            var pattern = match.Mode switch
            {
                ReplacerMode.Literal => Regex.Escape(options.Find),
                ReplacerMode.Wildcard => _WildcardToRegex(options.Find),
                ReplacerMode.Regex => options.Find,
                _ => throw new ArgumentOutOfRangeException(nameof(options), match.Mode, null),
            };

            if (match.WholeWord)
            {
                pattern = $@"\b(?:{pattern})\b";
            }

            var regex = new Regex(pattern, regexOptions);
            var count = match.ReplaceAll ? int.MaxValue : 1;

            // Literal/Wildcard must insert Replacement as plain text. Regex.Replace's string overload
            // treats $0/$1/$$ as substitutions (MFR7 uses MatchEvaluator / String.Replace for the same reason).
            if (match.Mode == ReplacerMode.Regex)
            {
                return regex.Replace(segment, options.Replacement, count);
            }

            return regex.Replace(segment, _ => options.Replacement, count);
        }

        /// <summary>
        /// Converts a wildcard pattern (<c>*</c> / <c>?</c>) into an equivalent regex pattern.
        /// </summary>
        /// <param name="wildcard">Wildcard search text.</param>
        /// <returns>Regex pattern with other characters escaped.</returns>
        private static string _WildcardToRegex(string wildcard)
        {
            var sb = new StringBuilder();
            foreach (var ch in wildcard)
            {
                sb.Append(
                    ch switch
                    {
                        '*' => ".*",
                        '?' => ".",
                        _ => Regex.Escape(ch.ToString()),
                    }
                );
            }

            return sb.ToString();
        }
    }
}
