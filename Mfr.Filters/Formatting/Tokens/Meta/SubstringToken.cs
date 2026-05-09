using System.Globalization;
using Mfr.Models;
using Mfr.Utils;

namespace Mfr.Filters.Formatting.Tokens.Meta
{
    /// <summary>
    /// Resolves the <c>&lt;substr:start-position,end-position,source-format-string&gt;</c> token.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Positions are 1-based. Negative positions count from the right: -1 is the last character,
    /// -2 is the second-to-last, and so on. Out-of-range positions are clamped to the nearest
    /// valid boundary.
    /// </para>
    /// <para>
    /// When the resolved <c>start-position</c> is less than or equal to the resolved
    /// <c>end-position</c>, the characters at positions [start, end] (inclusive) are returned.
    /// When the resolved <c>start-position</c> exceeds the resolved <c>end-position</c> (i.e. the
    /// arguments are "crossed"), the characters at positions (end, start] are returned — the range
    /// between them, exclusive of end and inclusive of start.
    /// </para>
    /// <para>
    /// <c>source-format-string</c> may itself be a nested format token such as <c>&lt;full-name&gt;</c>
    /// or a literal with embedded tokens; it is resolved before the substring is applied.
    /// </para>
    /// </remarks>
    internal sealed class SubstringToken : IFormatToken
    {
        /// <summary>
        /// Parsed arguments for <c>&lt;substr:...&gt;</c>.
        /// </summary>
        /// <param name="StartPosition">
        /// 1-based index of the first character to include. Negative values count from the right
        /// (-1 = last character, -2 = second-to-last, …). Must be non-zero.
        /// </param>
        /// <param name="EndPosition">
        /// 1-based index of the last character to include. Negative values count from the right.
        /// Must be non-zero.
        /// </param>
        /// <param name="SourceFormatString">Raw format string (may contain nested tokens) to extract from.</param>
        private sealed record Options(
            int StartPosition,
            int EndPosition,
            string SourceFormatString);

        /// <inheritdoc />
        public IReadOnlyList<string> Names { get; } = ["substr"];

        /// <inheritdoc />
        /// <exception cref="ArgumentException">Thrown when the format argument is malformed.</exception>
        public Func<RenameItem, string> Compile(string arg)
        {
            var options = _ParseOptions(arg);
            var compiledSource = FormatStringCompiler.Compile(options.SourceFormatString);
            return item =>
            {
                var source = compiledSource(item);
                if (source.Length == 0)
                    return string.Empty;

                var start = _ResolvePosition(options.StartPosition, source.Length);
                var end = _ResolvePosition(options.EndPosition, source.Length);

                if (start <= end)
                    return source[(start - 1)..end];

                // Crossed positions: return (end, start] — the range between them, end excluded.
                return source[end..start];
            };
        }

        private Options _ParseOptions(string arg)
        {
            var tokenDisplayName = FormatOptionsParsing.TokenDisplayName(this);
            Require.That(!string.IsNullOrEmpty(arg), $"{tokenDisplayName} requires 3 arguments: start-position,end-position,source-format-string.", nameof(arg));

            var parts = arg.Split(',', 3);
            Require.That(
                parts.Length == 3,
                $"{tokenDisplayName} requires exactly 3 comma-separated arguments (got {parts.Length}): '{arg}'.",
                nameof(arg));

            Require.That(
                int.TryParse(parts[0].Trim(), NumberStyles.Integer, CultureInfo.InvariantCulture, out var startPosition),
                $"{tokenDisplayName} start-position must be a non-zero integer (got '{parts[0].Trim()}').",
                nameof(arg));

            Require.That(
                int.TryParse(parts[1].Trim(), NumberStyles.Integer, CultureInfo.InvariantCulture, out var endPosition),
                $"{tokenDisplayName} end-position must be a non-zero integer (got '{parts[1].Trim()}').",
                nameof(arg));

            Require.That(startPosition != 0, $"{tokenDisplayName} start-position must not be zero.", nameof(arg));

            Require.That(endPosition != 0, $"{tokenDisplayName} end-position must not be zero.", nameof(arg));
            return new Options(
                StartPosition: startPosition,
                EndPosition: endPosition,
                SourceFormatString: parts[2]);
        }

        /// <summary>
        /// Converts a 1-based or right-relative position to a clamped 1-based index.
        /// </summary>
        /// <param name="position">Raw position (positive = from left, negative = from right).</param>
        /// <param name="length">Length of the source string.</param>
        /// <returns>Clamped 1-based position in [1, length].</returns>
        private static int _ResolvePosition(int position, int length)
        {
            var resolved = position < 0 ? length + position + 1 : position;
            return Math.Clamp(resolved, 1, length);
        }
    }
}
