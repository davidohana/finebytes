using System.Text;
using System.Text.RegularExpressions;

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
        Regex,
    }

    /// <summary>
    /// Options for replacer transformations.
    /// </summary>
    /// <param name="Find">Search pattern.</param>
    /// <param name="Replacement">Replacement value.</param>
    /// <param name="Match">Mode and match flags shared with <see cref="ReplaceListOptions"/>.</param>
    public sealed record ReplacerOptions(string Find, string Replacement, ReplacerMatchOptions Match);

    /// <summary>
    /// Replaces text according to search options.
    /// </summary>
    /// <param name="Target">The target that this filter applies to.</param>
    /// <param name="Options">Replacement options.</param>
    /// <param name="ApplyScope">When non-null, restricts this filter to a substring or token of the target; see <see cref="StringApplyScope"/>.</param>
    [FilterPalette(FilterGroup.Replace, "Replacer")]
    public sealed record ReplacerFilter(
        FilterTarget Target,
        ReplacerOptions Options,
        StringApplyScope? ApplyScope = null
    ) : StringTargetFilter(Target, ApplyScope)
    {
        /// <summary>
        /// Creates a filter with MFR7 add-to-list defaults (file prefix, empty find/replace, replace all).
        /// </summary>
        public ReplacerFilter()
            : this(
                new FilePrefixTarget(),
                new ReplacerOptions(Find: "", Replacement: "", Match: ReplacerMatchOptions.ForReplacer)
            ) { }

        /// <summary>
        /// Gets the filter type discriminator.
        /// </summary>
        public override string Type => "Replacer";

        /// <inheritdoc />
        protected override void _Setup()
        {
            if (Options.Match.Mode != ReplacerMode.Regex || Options.Find.Length == 0)
            {
                return;
            }

            try
            {
                _ = new Regex(Options.Find);
            }
            catch (ArgumentException ex)
            {
                throw new ArgumentException($"Invalid regular expression: {ex.Message}", nameof(Options), ex);
            }
        }

        protected override string _TransformValue(string value, RenameItem item)
        {
            return ReplaceSegment(value, Options);
        }

        /// <summary>
        /// Applies one find/replace pass to <paramref name="segment"/> (also used by <see cref="ReplaceListFilter"/>).
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
