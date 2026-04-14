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
    /// <param name="Enabled">Whether the filter is enabled.</param>
    /// <param name="Target">The target that this filter applies to.</param>
    /// <param name="Options">Replacement options.</param>
    public sealed record ReplacerFilter(
        bool Enabled,
        FilterTarget Target,
        ReplacerOptions Options) : BaseFilter(Enabled, Target)
    {
        /// <summary>
        /// Gets the filter type discriminator.
        /// </summary>
        public override string Type => "Replacer";

        internal override string TransformSegment(string segment, RenameItem item, FilterChainContext context)
        {
            var regexOptions = Options.CaseSensitive ? RegexOptions.None : RegexOptions.IgnoreCase;
            var pattern = Options.Mode switch
            {
                ReplacerMode.Literal => Regex.Escape(Options.Find),
                ReplacerMode.Wildcard => _WildcardToRegex(Options.Find),
                ReplacerMode.Regex => Options.Find,
                _ => Options.Find
            };

            if (Options.WholeWord)
            {
                pattern = $@"\b(?:{pattern})\b";
            }

            if (Options.ReplaceAll)
            {
                return Regex.Replace(segment, pattern, Options.Replacement, regexOptions);
            }

            var regex = new Regex(pattern, regexOptions);
            return regex.Replace(segment, Options.Replacement, 1);
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
