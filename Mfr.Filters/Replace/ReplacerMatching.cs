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
        /// Builds the search regex once for a find pattern and match policy (call from filter <c>_Setup</c>).
        /// </summary>
        /// <param name="find">Search pattern text; empty yields <see langword="null"/> (no-op apply).</param>
        /// <param name="match">Mode and match flags fixed for the run.</param>
        /// <param name="paramName">Optional argument name for <see cref="ArgumentException"/>.</param>
        /// <returns>Compiled search regex, or <see langword="null"/> when <paramref name="find"/> is empty.</returns>
        /// <exception cref="ArgumentException">When <paramref name="find"/> is not a valid regular expression in Regex mode.</exception>
        /// <exception cref="ArgumentOutOfRangeException">When <paramref name="match"/> has an unknown mode.</exception>
        internal static Regex? CompileSearch(string find, ReplacerMatchOptions match, string? paramName = null)
        {
            if (find.Length == 0)
            {
                return null;
            }

            var regexOptions = match.CaseSensitive ? RegexOptions.None : RegexOptions.IgnoreCase;
            var pattern = match.Mode switch
            {
                ReplacerMode.Literal => Regex.Escape(find),
                ReplacerMode.Wildcard => _WildcardToRegex(find),
                ReplacerMode.Regex => find,
                _ => throw new ArgumentOutOfRangeException(nameof(match), match.Mode, null),
            };

            if (match.WholeWord)
            {
                pattern = $@"\b(?:{pattern})\b";
            }

            try
            {
                return new Regex(pattern, regexOptions);
            }
            catch (ArgumentException ex) when (match.Mode == ReplacerMode.Regex)
            {
                throw new ArgumentException($"Invalid regular expression: {ex.Message}", paramName, ex);
            }
        }

        /// <summary>
        /// Applies one find/replace pass to <paramref name="segment"/> using a setup-compiled search.
        /// </summary>
        /// <param name="segment">Text to transform.</param>
        /// <param name="search">Regex from <see cref="CompileSearch"/>; <see langword="null"/> leaves <paramref name="segment"/> unchanged.</param>
        /// <param name="replacement">Replacement text for this pass (may already have format tokens expanded).</param>
        /// <param name="match">Mode and replace-all flag (must match the options used to compile <paramref name="search"/>).</param>
        /// <returns>Transformed text; unchanged when <paramref name="search"/> is <see langword="null"/>.</returns>
        internal static string ReplaceSegment(
            string segment,
            Regex? search,
            string replacement,
            ReplacerMatchOptions match
        )
        {
            if (search is null)
            {
                return segment;
            }

            var count = match.ReplaceAll ? int.MaxValue : 1;

            // Literal/Wildcard must insert Replacement as plain text. Regex.Replace's string overload
            // treats $0/$1/$$ as substitutions (MFR7 uses MatchEvaluator / String.Replace for the same reason).
            if (match.Mode == ReplacerMode.Regex)
            {
                return search.Replace(segment, replacement, count);
            }

            return search.Replace(segment, _ => replacement, count);
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
