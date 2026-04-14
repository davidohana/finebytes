using System.Diagnostics.CodeAnalysis;
using Mfr.Core;

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
        /// <description>Each replace entry is two lines: first line is search text, second line is replacement text.</description>
        /// </item>
        /// <item>
        /// <description>Search lines cannot be empty.</description>
        /// </item>
        /// <item>
        /// <description>Exactly one empty line must separate consecutive entry pairs.</description>
        /// </item>
        /// <item>
        /// <description>Replacement lines cannot be empty. Use <c>&lt;EMPTY&gt;</c> to strip the matched search text.</description>
        /// </item>
        /// <item>
        /// <description>Search and replacement lines must be at most 1000 characters each.</description>
        /// </item>
        /// </list>
        /// Example file content:
        /// <code>
        /// # START OF REPLACE LIST
        /// a
        /// b
        ///
        /// .
        /// _
        /// # END OF REPLACE LIST
        /// </code>
        /// </remarks>
        /// <param name="filePath">Replace-list file path.</param>
        /// <returns>Parsed replace-list entries in file order.</returns>
        internal static List<ReplaceListEntry> ParseFile(string filePath)
        {
            if (string.IsNullOrWhiteSpace(filePath))
            {
                throw new UserException("Replace-list file path cannot be empty.");
            }

            if (!File.Exists(filePath))
            {
                throw new UserException($"Replace-list file not found: '{filePath}'.");
            }

            var lines = _ReadNonCommentLines(filePath);
            _ValidateLineLength(lines);

            var entries = new List<ReplaceListEntry>();
            var i = 0;
            ReplaceListFileLine? _ReadNextLine()
            {
                i++;
                if (i >= lines.Count)
                {
                    return null;
                }

                return lines[i];
            }

            ReplaceListFileLine _ReadRequiredNextLineOrThrow(int lineNumber, string detail)
            {
                var line = _ReadNextLine();
                if (line is { } lineValue)
                {
                    return lineValue;
                }

                _ThrowInvalidFormat(lineNumber: lineNumber, detail: detail);
                return default;
            }

            while (i < lines.Count)
            {
                var searchLine = lines[i];
                if (string.IsNullOrEmpty(searchLine.Text))
                {
                    _ThrowInvalidFormat(lineNumber: searchLine.LineNumber, detail: "search line cannot be empty.");
                }

                var replaceLineValue = _ReadRequiredNextLineOrThrow(
                    lineNumber: searchLine.LineNumber,
                    detail: "found a search line without a corresponding replace line.");

                if (string.IsNullOrEmpty(replaceLineValue.Text))
                {
                    _ThrowInvalidFormat(
                        lineNumber: replaceLineValue.LineNumber,
                        detail: $"replace line cannot be empty. Use '{EmptyReplacementToken}' to strip matches.");
                }

                entries.Add(new ReplaceListEntry(
                    Search: searchLine.Text,
                    Replacement: _ResolveEmptyReplacementToken(replaceLineValue.Text)));
                var separatorLine = _ReadNextLine();
                if (separatorLine is not { } separatorLineValue)
                {
                    break;
                }

                if (!string.IsNullOrEmpty(separatorLineValue.Text))
                {
                    _ThrowInvalidFormat(
                        lineNumber: separatorLineValue.LineNumber,
                        detail: "expected exactly one empty separator line between entry pairs.");
                }

                var lineAfterSeparator = _ReadNextLine();
                if (lineAfterSeparator is not { } lineAfterSeparatorValue)
                {
                    break;
                }

                if (!string.IsNullOrEmpty(lineAfterSeparatorValue.Text))
                {
                    continue;
                }

                var firstExtraSeparatorLineNumber = lineAfterSeparatorValue.LineNumber;
                while (lineAfterSeparator is { } currentLine && string.IsNullOrEmpty(currentLine.Text))
                {
                    lineAfterSeparator = _ReadNextLine();
                }

                if (lineAfterSeparator is not null)
                {
                    _ThrowInvalidFormat(
                        lineNumber: firstExtraSeparatorLineNumber,
                        detail: "expected exactly one empty separator line between entry pairs.");
                }
            }

            return entries;
        }

        private static List<ReplaceListFileLine> _ReadNonCommentLines(string filePath)
        {
            var lines = File.ReadAllLines(filePath);
            return
            [
                .. lines
                .Select((text, index) => new ReplaceListFileLine(Text: text, LineNumber: index + 1))
                .Where(line => !_IsCommentLine(line.Text))
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

        private static bool _IsCommentLine(string line)
        {
            var trimmed = line.TrimStart();
            return trimmed.StartsWith("//", StringComparison.Ordinal)
                || trimmed.StartsWith(@"\\", StringComparison.Ordinal)
                || trimmed.StartsWith("# ", StringComparison.Ordinal);
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
