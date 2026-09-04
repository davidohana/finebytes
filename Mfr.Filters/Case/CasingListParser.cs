namespace Mfr.Filters.Case
{
    /// <summary>
    /// Validates casing-list words and builds the lookup used at apply time.
    /// </summary>
    internal static class CasingListParser
    {
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
