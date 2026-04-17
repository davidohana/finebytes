using Mfr.Filters.Formatting;
using Mfr.Models;

namespace Mfr.Filters.Replace
{
    /// <summary>
    /// Options for replace-list transformations loaded from a file.
    /// </summary>
    /// <param name="FilePath">Path to the replace-list file parsed by <see cref="ReplaceListParser"/>.</param>
    /// <param name="Mode">Pattern interpretation mode.</param>
    /// <param name="CaseSensitive">Whether matching is case-sensitive.</param>
    /// <param name="ReplaceAll">Whether all matches are replaced for each pair.</param>
    /// <param name="WholeWord">Whether matching is constrained to whole words.</param>
    public sealed record ReplaceListOptions(
        string FilePath,
        ReplacerMode Mode,
        bool CaseSensitive,
        bool ReplaceAll,
        bool WholeWord);

    /// <summary>
    /// Applies sequential replacements from a replace-list file.
    /// </summary>
    /// <remarks>
    /// Replace entries are applied in file order. This is equivalent to chaining multiple
    /// <see cref="ReplacerFilter"/> instances with the same mode/options.
    /// </remarks>
    /// <param name="Target">The target that this filter applies to.</param>
    /// <param name="Options">Replace-list options.</param>
    public sealed record ReplaceListFilter(
        FilterTarget Target,
        ReplaceListOptions Options) : BaseFilter(Target)
    {
        private List<ReplaceListEntry>? _replaceListEntries;

        /// <summary>
        /// Gets the filter type discriminator.
        /// </summary>
        public override string Type => "ReplaceList";

        protected override void _Setup()
        {
            _replaceListEntries = ReplaceListParser.ParseFile(filePath: Options.FilePath);
        }

        protected override string _TransformSegment(string segment, RenameItem item)
        {
            var searchToReplace = _replaceListEntries
                ?? throw new InvalidOperationException("Replace-list setup must complete before transform.");
            if (searchToReplace.Count == 0)
            {
                return segment;
            }

            var transformed = segment;
            foreach (var entry in searchToReplace)
            {
                var replacement = FormatterTokenResolver.ResolveTemplate(entry.Replacement, item);
                var replacerOptions = new ReplacerOptions(
                    Find: entry.Search,
                    Replacement: replacement,
                    Mode: Options.Mode,
                    CaseSensitive: Options.CaseSensitive,
                    ReplaceAll: Options.ReplaceAll,
                    WholeWord: Options.WholeWord);
                transformed = ReplacerFilter.ReplaceSegment(transformed, replacerOptions);
            }

            return transformed;
        }
    }
}
