namespace Mfr.Filters.Case
{
    /// <summary>
    /// Parses Filter Configuration editor text and builds the casing-list lookup used at apply time.
    /// </summary>
    public static class CasingListParser
    {
        /// <summary>
        /// Formats stored words as space-separated editor text.
        /// </summary>
        /// <param name="words">Canonical word spellings.</param>
        /// <returns>Editor text; empty when <paramref name="words"/> is empty.</returns>
        public static string FormatEditorText(IReadOnlyList<string> words)
        {
            ArgumentNullException.ThrowIfNull(words);

            return string.Join(' ', words);
        }

        /// <summary>
        /// Parses space-separated (any whitespace) words from the Filter Configuration editor.
        /// </summary>
        /// <remarks>
        /// Does not throw; empty/whitespace-only text yields an empty list. Length and single-word
        /// rules are enforced when the filter is set up.
        /// </remarks>
        /// <param name="text">Space-separated editor text.</param>
        /// <returns>Parsed words in order.</returns>
        public static IReadOnlyList<string> ParseEditorText(string? text)
        {
            if (string.IsNullOrWhiteSpace(text))
            {
                return [];
            }

            return text.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        }

        /// <summary>
        /// Builds a case-insensitive map from configured words (last duplicate wins).
        /// </summary>
        /// <param name="words">Canonical word spellings.</param>
        /// <returns>Map from lowercased word to canonical form; empty when <paramref name="words"/> is empty.</returns>
        internal static Dictionary<string, string> BuildMap(IReadOnlyList<string> words)
        {
            ArgumentNullException.ThrowIfNull(words);

            var lowerWordToCasing = new Dictionary<string, string>(StringComparer.Ordinal);
            for (var i = 0; i < words.Count; i++)
            {
                var word = words[i];
                var index = i + 1;
                if (string.IsNullOrWhiteSpace(word))
                {
                    throw new UserException($"Casing-list word {index} cannot be empty.");
                }

                ListEntryLength.ThrowIfTooLong(word, $"Casing-list word {index}");

                if (word.Any(char.IsWhiteSpace))
                {
                    throw new UserException($"Casing-list word {index} must be a single word (no whitespace).");
                }

                lowerWordToCasing[word.ToLowerInvariant()] = word;
            }

            return lowerWordToCasing;
        }
    }
}
