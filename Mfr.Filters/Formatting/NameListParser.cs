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
            if (entries[^1].Length == 0)
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
        /// lines; those are names. Does not throw; length limits are enforced when the filter is
        /// applied.
        /// </remarks>
        /// <param name="text">Multiline editor text.</param>
        /// <returns>Parsed names in line order.</returns>
        public static IReadOnlyList<string> ParseEditorText(string? text)
        {
            if (string.IsNullOrEmpty(text))
            {
                return [];
            }

            var entries = new List<string>();
            using var reader = new StringReader(text);
            while (reader.ReadLine() is { } line)
            {
                entries.Add(line);
            }

            return entries;
        }

        /// <summary>
        /// Validates embedded entries for apply-time use.
        /// </summary>
        /// <remarks>
        /// Empty list is allowed (no-op). Blank lines are kept as empty names. Each entry is limited
        /// to the configured list-line maximum (default 1000 characters).
        /// </remarks>
        /// <param name="entries">Configured names in rename-list index order. Null is treated as empty.</param>
        /// <returns>The same <paramref name="entries"/> list after checks succeed, or empty when null.</returns>
        internal static IReadOnlyList<string> Validate(IReadOnlyList<string>? entries)
        {
            entries ??= [];

            for (var i = 0; i < entries.Count; i++)
            {
                ListEntryLength.ThrowIfTooLong(entries[i], $"Name-list entry {i + 1}");
            }

            return entries;
        }
    }
}
