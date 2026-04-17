using System.Text;
using System.Text.RegularExpressions;
using Mfr.Models;

namespace Mfr.Filters.Replace
{
    /// <summary>
    /// Matching mode for replacer patterns.
    /// </summary>
    public enum ReplacerMode
    {
        /// <summary>
        /// Pattern is treated as literal text.
        /// </summary>
        Literal,

        /// <summary>
        /// Pattern uses '*' (any characters) and '?' (single character) wildcards.
        /// </summary>
        Wildcard,

        /// <summary>
        /// Pattern is a regular expression.
        /// </summary>
        Regex
    }

    /// <summary>
    /// Options for replacer transformations.
    /// </summary>
    /// <param name="Find">Search pattern.</param>
    /// <param name="Replacement">Replacement value.</param>
    /// <param name="Mode">Pattern interpretation mode.</param>
    /// <param name="CaseSensitive">Whether matching is case-sensitive.</param>
    /// <param name="ReplaceAll">Whether all matches are replaced.</param>
    /// <param name="WholeWord">Whether matching is constrained to whole words.</param>
    public sealed record ReplacerOptions(
        string Find,
        string Replacement,
        ReplacerMode Mode,
        bool CaseSensitive,
        bool ReplaceAll,
        bool WholeWord);

    /// <summary>
    /// Replaces text according to search options.
    /// </summary>
    /// <param name="Target">The target that this filter applies to.</param>
    /// <param name="Options">Replacement options.</param>
    public sealed record ReplacerFilter(
        FilterTarget Target,
        ReplacerOptions Options) : BaseFilter(Target)
    {
        /// <summary>
        /// Gets the filter type discriminator.
        /// </summary>
        public override string Type => "Replacer";

        protected override string _TransformSegment(string segment, RenameItem item)
        {
            return _ReplaceSegment(segment, Options);
        }

        internal static string ReplaceSegment(string segment, ReplacerOptions options)
        {
            return _ReplaceSegment(segment, options);
        }

        private static string _ReplaceSegment(string segment, ReplacerOptions options)
        {
            var regexOptions = options.CaseSensitive ? RegexOptions.None : RegexOptions.IgnoreCase;
            var pattern = options.Mode switch
            {
                ReplacerMode.Literal => Regex.Escape(options.Find),
                ReplacerMode.Wildcard => _WildcardToRegex(options.Find),
                ReplacerMode.Regex => options.Find,
                _ => options.Find
            };

            if (options.WholeWord)
            {
                pattern = $@"\b(?:{pattern})\b";
            }

            if (options.ReplaceAll)
            {
                return Regex.Replace(segment, pattern, options.Replacement, regexOptions);
            }

            var regex = new Regex(pattern, regexOptions);
            return regex.Replace(segment, options.Replacement, 1);
        }

        private static string _WildcardToRegex(string wildcard)
        {
            var sb = new StringBuilder();
            foreach (var ch in wildcard)
            {
                sb.Append(ch switch
                {
                    '*' => ".*",
                    '?' => ".",
                    _ => Regex.Escape(ch.ToString())
                });
            }

            return sb.ToString();
        }
    }
}
