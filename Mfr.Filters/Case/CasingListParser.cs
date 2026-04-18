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
            ListFileParseHelpers.ValidateListFilePath(filePath, listKindLabel: "Casing-list");

            var lowerWordToCasing = new Dictionary<string, string>(StringComparer.Ordinal);
            var lines = File.ReadAllLines(filePath);
            var maxLineLen = ConfigLoader.Settings.Filters.MaxListFileLineLength;
            for (var i = 0; i < lines.Length; i++)
            {
                var lineNumber = i + 1;
                var rawLine = lines[i];
                if (rawLine.Length > maxLineLen)
                {
                    throw new UserException(
                        $"Casing-list line {lineNumber} exceeds maximum length ({maxLineLen}).");
                }

                var trimmed = rawLine.Trim();
                // Allow empty lines and comments so list files stay easy to maintain.
                if (trimmed.Length == 0 || ListFileParseHelpers.IsListFileCommentLine(rawLine))
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
    }
}
