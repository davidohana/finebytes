using System.Text.RegularExpressions;
using Mfr.Filters.Formatting;
using Mfr.Filters.Formatting.FormatString;

namespace Mfr.Filters.Replace
{
    /// <summary>
    /// One search/replace pair in a replace list.
    /// </summary>
    /// <param name="Search">Search pattern text (may contain spaces).</param>
    /// <param name="Replacement">Replacement text (may contain spaces), or empty to strip matches.</param>
    public sealed record ReplaceListEntry(string Search, string Replacement);

    /// <summary>
    /// Options for replace-list transformations embedded in the filter.
    /// </summary>
    /// <param name="Entries">Search/replace pairs applied in order. Empty list is a no-op.</param>
    /// <param name="Match">Mode and match flags shared with <see cref="ReplacerOptions"/>.</param>
    public sealed record ReplaceListOptions(IReadOnlyList<ReplaceListEntry> Entries, ReplacerMatchOptions Match);

    /// <summary>
    /// Applies sequential replacements from an embedded replace list.
    /// </summary>
    /// <remarks>
    /// Replace entries are applied in list order, sharing one mode and match flags. Each replacement
    /// is a format string (formatter tokens such as <c>&lt;counter:…&gt;</c> expand per item before the
    /// replace runs), same as <see cref="ReplacerFilter"/>.
    /// </remarks>
    /// <param name="Target">The target that this filter applies to.</param>
    /// <param name="Options">Replace-list options.</param>
    /// <param name="ApplyScope">When non-null, restricts this filter to a substring or token of the target; see <see cref="StringApplyScope"/>.</param>
    [FilterPalette(FilterGroup.Replace, "Replace List")]
    public sealed record ReplaceListFilter(
        FilterTarget Target,
        ReplaceListOptions Options,
        StringApplyScope? ApplyScope = null
    ) : StringTargetFilter(Target, ApplyScope)
    {
        private List<(Regex? Search, Formatter CompiledReplacement)> _compiledEntries = [];

        /// <summary>
        /// Creates a filter with add-to-list defaults (file prefix, empty list, replace all, whole word).
        /// </summary>
        public ReplaceListFilter()
            : this(
                new FilePrefixTarget(),
                new ReplaceListOptions(Entries: [], Match: ReplacerMatchOptions.ForReplaceList)
            ) { }

        /// <summary>
        /// Gets the filter type discriminator.
        /// </summary>
        public override string Type => "ReplaceList";

        protected override void _Setup()
        {
            var entries = ReplaceListParser.Validate(Options.Entries);
            // Unconditional assign (BaseFilter._Setup): `with` copies this field; empty list must clear prior entries.
            _compiledEntries =
            [
                .. entries.Select(e =>
                    (
                        ReplacerMatching.CompileSearch(e.Search, Options.Match, nameof(Options)),
                        FormatStringCompiler.Compile(e.Replacement)
                    )
                ),
            ];
        }

        protected override string _TransformValue(string value, RenameItem item)
        {
            if (_compiledEntries.Count == 0)
            {
                return value;
            }

            var transformed = value;
            foreach (var (search, compiledReplacement) in _compiledEntries)
            {
                var replacement = compiledReplacement(item);
                transformed = ReplacerMatching.ReplaceSegment(transformed, search, replacement, Options.Match);
            }

            return transformed;
        }
    }
}
