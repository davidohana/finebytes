using System.Text;
using System.Text.RegularExpressions;
namespace Mfr8.Models.Filters.Advanced
{
    /// <summary>
    /// Matching mode for replacer patterns.
    /// </summary>
    public enum ReplacerMode
    {
        Literal,
        Wildcard,
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
        ReplacerOptions Options) : Filter(Enabled, Target)
    {
        /// <summary>
        /// Gets the filter type discriminator.
        /// </summary>
        public override string Type => "Replacer";

        internal override string Apply(string segment, FileEntryLite file)
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
                _ = sb.Append(ch switch
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
