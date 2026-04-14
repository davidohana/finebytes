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
    /// <param name="Enabled">Whether the filter is enabled.</param>
    /// <param name="Target">The target that this filter applies to.</param>
    /// <param name="Options">Replace-list options.</param>
    public sealed record ReplaceListFilter(
        bool Enabled,
        FilterTarget Target,
        ReplaceListOptions Options) : BaseFilter(Enabled, Target)
    {
        /// <summary>
        /// Gets the filter type discriminator.
        /// </summary>
        public override string Type => "ReplaceList";

        internal override string TransformSegment(string segment, RenameItem item, FilterChainContext context)
        {
            var searchToReplace = _GetOrLoadReplaceEntries(context);
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
                var replacerFilter = new ReplacerFilter(
                    Enabled: true,
                    Target: Target,
                    Options: replacerOptions);
                transformed = replacerFilter.TransformSegment(transformed, item, context);
            }

            return transformed;
        }

        private List<ReplaceListEntry> _GetOrLoadReplaceEntries(FilterChainContext context)
        {
            if (string.IsNullOrWhiteSpace(Options.FilePath))
            {
                throw new InvalidOperationException("Replace-list file path cannot be empty.");
            }

            var normalizedFilePath = Path.GetFullPath(Options.FilePath);
            var cacheKey = new FilterCacheKey(
                Scope: FilterCacheScope.ReplaceListEntries,
                Id: normalizedFilePath);
            return context.GetOrAdd(
                key: cacheKey,
                factory: () => ReplaceListParser.ParseFile(filePath: Options.FilePath));
        }
    }
}
