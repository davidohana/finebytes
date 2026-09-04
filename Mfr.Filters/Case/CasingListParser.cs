namespace Mfr.Filters.Case
{
    /// <summary>
    /// Parses and validates casing-list words.
    /// </summary>
    internal static class CasingListParser
    {
        /// <summary>
        /// Parses editor / freeform text into a word list (one word per line).
        /// </summary>
        /// <param name="wordsText">Line-separated words; blank lines and list-file comments are ignored.</param>
        /// <returns>Canonical word spellings in order; empty when there are no non-comment words.</returns>
        internal static IReadOnlyList<string> ParseWordLines(string wordsText)
        {
            if (string.IsNullOrWhiteSpace(wordsText))
            {
                return [];
            }

            var words = new List<string>();
            var lines = wordsText.ReplaceLineEndings("\n").Split('\n');
            var maxLineLen = ConfigStore.Config.Filters.MaxListFileLineLength;
            for (var i = 0; i < lines.Length; i++)
            {
                var lineNumber = i + 1;
                var rawLine = lines[i];
                if (rawLine.Length > maxLineLen)
                {
                    throw new UserException($"Casing-list line {lineNumber} exceeds maximum length ({maxLineLen}).");
                }

                var trimmed = rawLine.Trim();
                if (trimmed.Length == 0 || ListFileParseHelpers.IsListFileCommentLine(rawLine))
                {
                    continue;
                }

                if (trimmed.Contains(' '))
                {
                    throw new UserException(
                        $"Invalid casing-list format at line {lineNumber}: line must contain exactly one word."
                    );
                }

                words.Add(trimmed);
            }

            return words;
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
            var maxLineLen = ConfigStore.Config.Filters.MaxListFileLineLength;
            for (var i = 0; i < words.Count; i++)
            {
                var word = words[i];
                var index = i + 1;
                if (string.IsNullOrWhiteSpace(word))
                {
                    throw new UserException($"Casing-list word {index} cannot be empty.");
                }

                if (word.Length > maxLineLen)
                {
                    throw new UserException($"Casing-list word {index} exceeds maximum length ({maxLineLen}).");
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
