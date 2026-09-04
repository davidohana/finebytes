namespace Mfr.Filters.Case
{
    /// <summary>
    /// Parses and validates casing-list words.
    /// </summary>
    internal static class CasingListParser
    {
        /// <summary>
        /// Parses space-separated editor text into a word list.
        /// </summary>
        /// <param name="wordsText">Whitespace-separated words.</param>
        /// <returns>Canonical word spellings in order; empty when there are no words.</returns>
        internal static IReadOnlyList<string> ParseWordsText(string wordsText)
        {
            if (string.IsNullOrWhiteSpace(wordsText))
            {
                return [];
            }

            var tokens = wordsText.Split(
                (char[]?)null,
                StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries
            );
            var maxWordLen = ConfigStore.Config.Filters.MaxListFileLineLength;
            for (var i = 0; i < tokens.Length; i++)
            {
                if (tokens[i].Length > maxWordLen)
                {
                    throw new UserException(
                        $"Casing-list word {i + 1} exceeds maximum length ({maxWordLen})."
                    );
                }
            }

            return tokens;
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
            var maxWordLen = ConfigStore.Config.Filters.MaxListFileLineLength;
            for (var i = 0; i < words.Count; i++)
            {
                var word = words[i];
                var index = i + 1;
                if (string.IsNullOrWhiteSpace(word))
                {
                    throw new UserException($"Casing-list word {index} cannot be empty.");
                }

                if (word.Length > maxWordLen)
                {
                    throw new UserException($"Casing-list word {index} exceeds maximum length ({maxWordLen}).");
                }

                if (word.Contains(' '))
                {
                    throw new UserException($"Casing-list word {index} must be a single word (no spaces).");
                }

                lowerWordToCasing[word.ToLowerInvariant()] = word;
            }

            return lowerWordToCasing;
        }
    }
}
