using Mfr.Filters.Formatting;
using Mfr.Models;

namespace Mfr.Filters.Replace
{
    /// <summary>
    /// Options for replace-list transformations loaded from a file.
    /// </summary>
    /// <param name="FilePath">Path to the replace-list file.</param>
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
    /// <param name="Enabled">Whether the filter is enabled.</param>
    /// <param name="Target">The target that this filter applies to.</param>
    /// <param name="Options">Replace-list options.</param>
    public sealed record ReplaceListFilter(
        bool Enabled,
        FilterTarget Target,
        ReplaceListOptions Options) : Filter(Enabled, Target)
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
            foreach (var (search, replaceTemplate) in searchToReplace)
            {
                var replacement = FormatterTokenResolver.ResolveTemplate(replaceTemplate, item);
                var replacerOptions = new ReplacerOptions(
                    Find: search,
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

        private List<(string Search, string Replacement)> _GetOrLoadReplaceEntries(FilterChainContext context)
        {
            if (string.IsNullOrWhiteSpace(Options.FilePath))
            {
                throw new InvalidOperationException("Replace-list file path cannot be empty.");
            }

            var normalizedFilePath = Path.GetFullPath(Options.FilePath);
            var cacheId = string.Join(
                "|",
                normalizedFilePath,
                Options.Mode,
                Options.CaseSensitive,
                Options.ReplaceAll,
                Options.WholeWord);
            var cacheKey = new FilterCacheKey(
                Scope: "ReplaceListEntries",
                Id: cacheId);
            return context.GetOrAdd(
                key: cacheKey,
                factory: () => _LoadReplaceEntries(filePath: Options.FilePath));
        }

        private static List<(string Search, string Replacement)> _LoadReplaceEntries(string filePath)
        {
            if (string.IsNullOrWhiteSpace(filePath))
            {
                throw new InvalidOperationException("Replace-list file path cannot be empty.");
            }

            if (!File.Exists(filePath))
            {
                throw new FileNotFoundException($"Replace-list file not found: '{filePath}'.", filePath);
            }

            var entries = new List<(string Search, string Replacement)>();
            var lines = File.ReadAllLines(filePath);
            string? pendingSearch = null;

            foreach (var line in lines)
            {
                if (_IsCommentLine(line))
                {
                    continue;
                }

                if (pendingSearch is null)
                {
                    if (string.IsNullOrEmpty(line))
                    {
                        continue;
                    }

                    pendingSearch = line;
                    continue;
                }

                entries.Add((Search: pendingSearch, Replacement: line));
                pendingSearch = null;
            }

            if (pendingSearch is not null)
            {
                entries.Add((Search: pendingSearch, Replacement: ""));
            }

            return entries;
        }

        private static bool _IsCommentLine(string line)
        {
            return line.TrimStart().StartsWith(@"\\", StringComparison.Ordinal);
        }
    }
}
