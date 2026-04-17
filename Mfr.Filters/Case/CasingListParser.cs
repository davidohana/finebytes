using Mfr.Models;

namespace Mfr.Filters.Case
{
    /// <summary>
    /// Parses and validates casing-list files.
    /// </summary>
    internal static class CasingListParser
    {
        /// <summary>
        /// Parses one-word-per-line casing entries from a text file.
        /// </summary>
        /// <param name="filePath">Casing-list file path.</param>
        /// <returns>Case-insensitive map from lowercased word to canonical cased form.</returns>
        internal static Dictionary<string, string> ParseFile(string filePath)
        {
            if (string.IsNullOrWhiteSpace(filePath))
            {
                throw new UserException("Casing-list file path cannot be empty.");
            }

            if (!File.Exists(filePath))
            {
                throw new UserException($"Casing-list file not found: '{filePath}'.");
            }

            var lowerWordToCasing = new Dictionary<string, string>(StringComparer.Ordinal);
            var lines = File.ReadAllLines(filePath);
            for (var i = 0; i < lines.Length; i++)
            {
                var lineNumber = i + 1;
                var rawLine = lines[i];
                var trimmed = rawLine.Trim();
                // Allow empty lines and comments so list files stay easy to maintain.
                if (trimmed.Length == 0 || _IsCommentLine(trimmed))
                {
                    continue;
                }

                if (trimmed.Contains(' '))
                {
                    throw new UserException($"Invalid casing-list format at line {lineNumber}: line must contain exactly one word.");
                }

                var lowerWord = trimmed.ToLowerInvariant();
                // Last duplicate wins, matching "reload from file" expectations.
                lowerWordToCasing[lowerWord] = trimmed;
            }

            if (lowerWordToCasing.Count == 0)
            {
                throw new UserException("Casing-list file must contain at least one word.");
            }

            return lowerWordToCasing;
        }

        /// <summary>
        /// Returns whether the line should be treated as a comment.
        /// </summary>
        /// <param name="trimmedLine">Trimmed line content from the casing-list file.</param>
        /// <returns><c>true</c> when the line starts with a supported comment marker.</returns>
        private static bool _IsCommentLine(string trimmedLine)
        {
            return trimmedLine.StartsWith("//", StringComparison.Ordinal)
                || trimmedLine.StartsWith(@"\\", StringComparison.Ordinal)
                || trimmedLine.StartsWith("# ", StringComparison.Ordinal);
        }
    }
}
