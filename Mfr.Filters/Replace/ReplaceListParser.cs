namespace Mfr.Filters.Replace
{
    /// <summary>
    /// Validates replace-list entries embedded in filter options.
    /// </summary>
    internal static class ReplaceListParser
    {
        /// <summary>
        /// Validates entries for apply-time use.
        /// </summary>
        /// <remarks>
        /// Empty list is allowed (no-op). Each search must be non-empty after trim rules at the editor.
        /// Search and replacement may contain whitespace. Search must not contain
        /// <see cref="ReplaceListEntry.EditorSeparator"/> so editor text can round-trip.
        /// Replacement may be empty (strip).
        /// </remarks>
        /// <param name="entries">Configured search/replace pairs in apply order.</param>
        /// <returns>Validated entries in the same order.</returns>
        internal static List<ReplaceListEntry> Validate(IReadOnlyList<ReplaceListEntry> entries)
        {
            ArgumentNullException.ThrowIfNull(entries);

            var maxLen = ConfigStore.Config.Filters.MaxListFileLineLength;
            var validated = new List<ReplaceListEntry>(entries.Count);
            for (var i = 0; i < entries.Count; i++)
            {
                var index = i + 1;
                var entry = entries[i];
                var search = entry.Search;
                var replacement = entry.Replacement;

                if (string.IsNullOrWhiteSpace(search))
                {
                    throw new UserException($"Replace-list entry {index}: search cannot be empty.");
                }

                if (search.Contains(ReplaceListEntry.EditorSeparator, StringComparison.Ordinal))
                {
                    throw new UserException(
                        $"Replace-list entry {index}: search must not contain '{ReplaceListEntry.EditorSeparator}'."
                    );
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

                validated.Add(entry);
            }

            return validated;
        }
    }
}
