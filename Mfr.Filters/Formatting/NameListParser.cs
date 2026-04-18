using Mfr.Models;

namespace Mfr.Filters.Formatting
{
    /// <summary>
    /// Parses name-list files (one entry per line) used by <see cref="NameListFilter"/>.
    /// </summary>
    internal static class NameListParser
    {
        /// <summary>
        /// Parses name-list entries from a text file (UTF-8, same as other list file parsers).
        /// </summary>
        /// <param name="filePath">Path to the name-list file.</param>
        /// <param name="skipEmptyLines">When <c>true</c>, lines that are empty or whitespace-only are omitted (they do not consume an index).</param>
        /// <returns>Ordered entries; index <c>k</c> maps to rename item <see cref="FileMeta.GlobalIndex"/> <c>k</c>.</returns>
        internal static IReadOnlyList<string> ParseFile(string filePath, bool skipEmptyLines)
        {
            ListFileParseHelpers.ValidateListFilePath(filePath, listKindLabel: "Name-list");

            var rawLines = File.ReadAllLines(filePath);
            var entries = new List<string>(rawLines.Length);
            var maxLineLen = ConfigLoader.Settings.Filters.MaxListFileLineLength;
            for (var i = 0; i < rawLines.Length; i++)
            {
                var lineNumber = i + 1;
                var line = rawLines[i];
                if (line.Length > maxLineLen)
                {
                    throw new UserException(
                        $"Name-list line {lineNumber} exceeds maximum length ({maxLineLen}).");
                }

                if (ListFileParseHelpers.IsListFileCommentLine(line))
                {
                    continue;
                }

                if (skipEmptyLines)
                {
                    if (string.IsNullOrWhiteSpace(line))
                    {
                        continue;
                    }

                    entries.Add(line.TrimEnd());
                    continue;
                }

                entries.Add(line);
            }

            if (entries.Count == 0)
            {
                throw new UserException("Name-list file must contain at least one name entry.");
            }

            return entries;
        }
    }
}
