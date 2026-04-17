using Mfr.Models;

namespace Mfr.Filters
{
    /// <summary>
    /// Shared validation and comment rules for text list files loaded by filters (name list, casing list, replace list).
    /// </summary>
    internal static class ListFileParseHelpers
    {
        /// <summary>
        /// Maximum length (characters) of a single line in name-list and casing-list text files.
        /// </summary>
        internal const int MaxListFileLineLength = 1000;

        /// <summary>
        /// Validates that <paramref name="filePath"/> is non-empty and that the file exists.
        /// </summary>
        /// <param name="filePath">Path supplied by the user.</param>
        /// <param name="listKindLabel">Short label for errors, for example <c>Name-list</c> or <c>Replace-list</c>.</param>
        /// <exception cref="UserException">Thrown when the path is empty or the file is missing.</exception>
        internal static void ValidateListFilePath(string filePath, string listKindLabel)
        {
            if (string.IsNullOrWhiteSpace(filePath))
            {
                throw new UserException($"{listKindLabel} file path cannot be empty.");
            }

            if (!File.Exists(filePath))
            {
                throw new UserException($"{listKindLabel} file not found: '{filePath}'.");
            }
        }

        /// <summary>
        /// Returns whether <paramref name="line"/> is a comment line after optional leading whitespace.
        /// </summary>
        /// <param name="line">One logical line from a list file.</param>
        /// <returns>
        /// <c>true</c> when the line starts with <c>//</c>, <c>\\</c>, or <c># </c> (hash plus space) after <see cref="string.TrimStart()"/>.
        /// </returns>
        internal static bool IsListFileCommentLine(string line)
        {
            var trimmedStart = line.TrimStart();
            if (trimmedStart.Length == 0)
            {
                return false;
            }

            return trimmedStart.StartsWith("//", StringComparison.Ordinal)
                || trimmedStart.StartsWith(@"\\", StringComparison.Ordinal)
                || trimmedStart.StartsWith("# ", StringComparison.Ordinal);
        }
    }
}
