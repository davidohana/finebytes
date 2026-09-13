using Mfr.Utils;

namespace Mfr.Filters.Formatting
{
    /// <summary>
    /// Parses Filter Configuration editor text and validates embedded name-list entries.
    /// </summary>
    public static class NameListParser
    {
        /// <summary>
        /// Formats stored entries as line-separated names.
        /// </summary>
        /// <param name="entries">Names in rename-list index order.</param>
        /// <returns>Editor text; a trailing empty entry is preserved with an extra newline.</returns>
        public static string FormatEditorText(IReadOnlyList<string> entries)
        {
            ArgumentNullException.ThrowIfNull(entries);

            if (entries.Count == 0)
            {
                return string.Empty;
            }

            var text = string.Join('\n', entries);
            if (string.IsNullOrEmpty(entries[^1]))
            {
                return text + "\n";
            }

            return text;
        }

        /// <summary>
        /// Parses line-separated names (one entry per line, including blank lines).
        /// </summary>
        /// <remarks>
        /// A trailing newline after the last non-empty line does not add an extra entry (same as
        /// reading lines from a file). Interior blank lines are kept. Does not skip comment-like
        /// lines; those are names. Does not throw; length and count limits are enforced when the
        /// filter is applied.
        /// </remarks>
        /// <param name="text">Multiline editor text.</param>
        /// <returns>Parsed names in line order.</returns>
        public static IReadOnlyList<string> ParseEditorText(string? text)
        {
            return [.. MultilineText.EnumerateLines(text)];
        }

        /// <summary>
        /// Validates embedded entries for apply-time use.
        /// </summary>
        /// <remarks>
        /// Empty list is allowed (no-op). Blank lines are kept as empty names. Null elements are
        /// treated as empty for length checks (apply coalesces them the same way). Entry count is
        /// capped at <see cref="ListEntryLength.DefaultMaxEntryCount"/> (or
        /// <paramref name="maxEntryCount"/>). Each entry is limited to
        /// <paramref name="maxEntryLength"/> (or <see cref="ListEntryLength.DefaultMaxLength"/>).
        /// </remarks>
        /// <param name="entries">Configured names in rename-list index order. Null is treated as empty.</param>
        /// <param name="maxEntryLength">
        /// Maximum characters per entry; defaults to <see cref="ListEntryLength.DefaultMaxLength"/>.
        /// </param>
        /// <param name="maxEntryCount">
        /// Maximum number of entries; defaults to <see cref="ListEntryLength.DefaultMaxEntryCount"/>.
        /// </param>
        /// <returns>The same <paramref name="entries"/> list after checks succeed, or empty when null.</returns>
        internal static IReadOnlyList<string> Validate(
            IReadOnlyList<string>? entries,
            int? maxEntryLength = null,
            int? maxEntryCount = null
        )
        {
            entries ??= [];
            var maxLen = maxEntryLength ?? ListEntryLength.DefaultMaxLength;
            var maxCount = maxEntryCount ?? ListEntryLength.DefaultMaxEntryCount;
            ListEntryLength.ThrowIfTooManyEntries(entries.Count, "Name-list", maxCount);

            for (var i = 0; i < entries.Count; i++)
            {
                ListEntryLength.ThrowIfTooLong(entries[i] ?? string.Empty, $"Name-list entry {i + 1}", maxLen);
            }

            return entries;
        }
    }
}
