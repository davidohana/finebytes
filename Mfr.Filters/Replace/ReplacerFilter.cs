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
            if (Options.Match.Mode != ReplacerMode.Regex)
            {
                return;
            }

            ReplacerMatching.ValidateRegexPattern(Options.Find, nameof(Options));
        }

        protected override string _TransformValue(string value, RenameItem item)
        {
            return ReplacerMatching.ReplaceSegment(value, Options);
        }
    }
}
