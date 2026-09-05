namespace Mfr.Filters.Replace
{
    /// <summary>
    /// Parses Filter Configuration editor text and validates replace-list entries.
    /// </summary>
    public static class ReplaceListParser
    {
        /// <summary>
        /// Separator between search and replacement in Filter Configuration editor text.
        /// </summary>
        public const string EditorSeparator = "=>";

        /// <summary>
        /// Formats stored entries as line-separated <c>search =&gt; replacement</c> pairs.
        /// </summary>
        /// <param name="entries">Search/replace pairs in apply order.</param>
        /// <returns>Editor text; a line with no separator means strip (empty replacement).</returns>
        public static string FormatEditorText(IReadOnlyList<ReplaceListEntry> entries)
        {
            ArgumentNullException.ThrowIfNull(entries);

            return string.Join(
                '\n',
                entries.Select(e =>
                    e.Replacement.Length == 0 ? e.Search : $"{e.Search} {EditorSeparator} {e.Replacement}"
                )
            );
        }

        /// <summary>
        /// Parses line-separated pairs using <see cref="EditorSeparator"/> (first occurrence).
        /// </summary>
        /// <remarks>
        /// A line without the separator is search-only (strip). Empty search after split is skipped.
        /// Surrounding whitespace on each side is trimmed. Does not throw; empty search and length
        /// limits are enforced when the filter is applied.
        /// </remarks>
        /// <param name="text">Multiline editor text.</param>
        /// <returns>Parsed pairs in line order.</returns>
        public static IReadOnlyList<ReplaceListEntry> ParseEditorText(string text)
        {
            if (string.IsNullOrWhiteSpace(text))
            {
                return [];
            }

            var entries = new List<ReplaceListEntry>();
            foreach (var line in text.Split(['\r', '\n']))
            {
                if (string.IsNullOrWhiteSpace(line))
                {
                    continue;
                }

                var sepIndex = line.IndexOf(EditorSeparator, StringComparison.Ordinal);
                if (sepIndex < 0)
                {
                    entries.Add(new ReplaceListEntry(line.Trim(), string.Empty));
                    continue;
                }

                var search = line[..sepIndex].Trim();
                if (search.Length == 0)
                {
                    continue;
                }

                var replacement = line[(sepIndex + EditorSeparator.Length)..].Trim();
                entries.Add(new ReplaceListEntry(search, replacement));
            }

            return entries;
        }

        /// <summary>
        /// Validates entries for apply-time use.
        /// </summary>
        /// <remarks>
        /// Empty list is allowed (no-op). Each search must be non-empty. Search and replacement may
        /// contain whitespace. Replacement may be empty (strip).
        /// </remarks>
        /// <param name="entries">Configured search/replace pairs in apply order.</param>
        /// <returns>The same <paramref name="entries"/> list after checks succeed.</returns>
        internal static IReadOnlyList<ReplaceListEntry> Validate(IReadOnlyList<ReplaceListEntry> entries)
        {
            ArgumentNullException.ThrowIfNull(entries);

            var maxLen = ConfigStore.Config.Filters.MaxListFileLineLength;
            for (var i = 0; i < entries.Count; i++)
            {
                var index = i + 1;
                var search = entries[i].Search;
                var replacement = entries[i].Replacement;

                if (string.IsNullOrWhiteSpace(search))
                {
                    throw new UserException($"Replace-list entry {index}: search cannot be empty.");
                }

                if (search.Length > maxLen)
                {
                    throw new UserException($"Replace-list entry {index}: search exceeds maximum length ({maxLen}).");
                }

                if (replacement.Length > maxLen)
                {
                    throw new UserException(
                        $"Replace-list entry {index}: replacement exceeds maximum length ({maxLen})."
                    );
                }
            }

            return entries;
        }
    }
}
