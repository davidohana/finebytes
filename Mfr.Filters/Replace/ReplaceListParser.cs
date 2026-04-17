using System.Diagnostics.CodeAnalysis;
using Mfr.Models;

namespace Mfr.Filters.Replace
{
    /// <summary>
    /// Represents one parsed replace-list entry.
    /// </summary>
    /// <param name="Search">Search pattern text.</param>
    /// <param name="Replacement">Replacement text.</param>
    internal readonly record struct ReplaceListEntry(string Search, string Replacement);

    /// <summary>
    /// Parses and validates replace-list files.
    /// </summary>
    internal static class ReplaceListParser
    {
        internal const int MaxEntryLineLength = 1000;
        internal const string EmptyReplacementToken = "<EMPTY>";

        /// <summary>
        /// Parses and validates replace-list entries from a file.
        /// </summary>
        /// <remarks>
        /// Expected format:
        /// <list type="bullet">
        /// <item>
        /// <description>Comment lines may appear anywhere and must start with <c>//</c>, <c>\\</c>, or <c># </c> (hash followed by a space, after optional leading whitespace).</description>
        /// </item>
        /// <item>
        /// <description>Empty lines are ignored.</description>
        /// </item>
        /// <item>
        /// <description>Each replace entry is two lines: search line starting with <c>S:</c> and replacement line starting with <c>R:</c>.</description>
        /// </item>
        /// <item>
        /// <description>Search and replacement lines cannot be empty (excluding the prefix). Use <c>&lt;EMPTY&gt;</c> on replacement lines to strip the matched search text.</description>
        /// </item>
        /// <item>
        /// <description>Search and replacement lines must be at most 1000 characters each.</description>
        /// </item>
        /// <item>
        /// <description>At least one replacement entry must be present.</description>
        /// </item>
        /// </list>
        /// Example file content:
        /// <code>
        /// # START OF REPLACE LIST
        /// S:a
        /// R:b
        ///
        /// S:.
        /// R:_
        /// # END OF REPLACE LIST
        /// </code>
        /// </remarks>
        /// <param name="filePath">Replace-list file path.</param>
        /// <returns>Parsed replace-list entries in file order.</returns>
        internal static List<ReplaceListEntry> ParseFile(string filePath)
        {
            ListFileParseHelpers.ValidateListFilePath(filePath, listKindLabel: "Replace-list");

            var lines = _ReadNonCommentAndNonEmptyLines(filePath);
            if (lines.Count == 0)
            {
                throw new UserException("Replace-list file must contain at least one replacement entry.");
            }

            _ValidateLineLength(lines);

            var entries = new List<ReplaceListEntry>();
            var i = 0;

            while (i < lines.Count)
            {
                var searchLine = lines[i++];
                if (!searchLine.Text.StartsWith("S:", StringComparison.Ordinal))
                {
                    _ThrowInvalidFormat(searchLine.LineNumber, "search line must start with 'S:'.");
                }

                if (i >= lines.Count)
                {
                    _ThrowInvalidFormat(searchLine.LineNumber, "found a search line without a corresponding replace line.");
                }

                var replaceLine = lines[i++];
                if (!replaceLine.Text.StartsWith("R:", StringComparison.Ordinal))
                {
                    _ThrowInvalidFormat(replaceLine.LineNumber, "replace line must start with 'R:'.");
                }

                var searchText = searchLine.Text[2..];
                var replaceText = replaceLine.Text[2..];

                if (string.IsNullOrEmpty(searchText))
                {
                    _ThrowInvalidFormat(searchLine.LineNumber, "search line cannot be empty.");
                }

                if (string.IsNullOrEmpty(replaceText))
                {
                    _ThrowInvalidFormat(replaceLine.LineNumber, $"replace line cannot be empty. Use '{EmptyReplacementToken}' to strip matches.");
                }

                entries.Add(new ReplaceListEntry(searchText, _ResolveEmptyReplacementToken(replaceText)));
            }

            return entries;
        }

        private static List<ReplaceListFileLine> _ReadNonCommentAndNonEmptyLines(string filePath)
        {
            var lines = File.ReadAllLines(filePath);
            return
            [
                .. lines
                .Select((text, index) => new ReplaceListFileLine(Text: text, LineNumber: index + 1))
                .Where(line => !string.IsNullOrWhiteSpace(line.Text) && !ListFileParseHelpers.IsListFileCommentLine(line.Text))
            ];
        }

        private static void _ValidateLineLength(IReadOnlyList<ReplaceListFileLine> lines)
        {
            var firstInvalidLine = lines.FirstOrDefault(line => line.Text.Length > MaxEntryLineLength);
            if (firstInvalidLine == default)
            {
                return;
            }

            _ThrowInvalidFormat(
                lineNumber: firstInvalidLine.LineNumber,
                detail: $"line length exceeds {MaxEntryLineLength} characters.");
        }

        private static string _ResolveEmptyReplacementToken(string replacement)
        {
            if (string.Equals(replacement, EmptyReplacementToken, StringComparison.Ordinal))
            {
                return string.Empty;
            }

            return replacement;
        }

        [DoesNotReturn]
        private static void _ThrowInvalidFormat(int lineNumber, string detail)
        {
            throw new UserException($"Invalid replace-list format at line {lineNumber}: {detail}");
        }

        private readonly record struct ReplaceListFileLine(string Text, int LineNumber);
    }
}
