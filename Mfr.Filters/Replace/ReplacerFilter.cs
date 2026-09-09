using Mfr.Filters.Formatting;
using Mfr.Filters.Formatting.FormatString;

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
    /// <param name="Replacement">
    /// Replacement text, or a format string (formatter tokens such as <c>&lt;counter:…&gt;</c> expand per
    /// item before the replace runs). In <see cref="ReplacerMode.Regex"/>, <c>$0</c> / <c>$1</c>… in the
    /// expanded text are regex substitutions.
    /// </param>
    /// <param name="Match">Mode and match flags shared with <see cref="ReplaceListOptions"/>.</param>
    public sealed record ReplacerOptions(string Find, string Replacement, ReplacerMatchOptions Match);

    /// <summary>
    /// Replaces text according to search options.
    /// </summary>
    /// <remarks>
    /// <see cref="ReplacerOptions.Replacement"/> is a format string: formatter tokens expand per item
    /// before the find/replace pass (same as <see cref="ReplaceListFilter"/> and MFR7).
    /// </remarks>
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
        private Formatter _compiledReplacement = FormatStringCompiler.EmptyFormatter;

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
            // Unconditional assign (BaseFilter._Setup): `with` copies this field; clear/plain text must reset it.
            _compiledReplacement = FormatStringCompiler.Compile(Options.Replacement);

            if (Options.Match.Mode != ReplacerMode.Regex)
            {
                return;
            }

            ReplacerMatching.ValidateRegexPattern(Options.Find, nameof(Options));
        }

        protected override string _TransformValue(string value, RenameItem item)
        {
            var replacement = _compiledReplacement(item);
            var options = Options with { Replacement = replacement };
            return ReplacerMatching.ReplaceSegment(value, options);
        }
    }
}
