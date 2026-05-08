using System.Globalization;
using Mfr.Models;

namespace Mfr.Filters.Formatting.Tokens.General
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
    internal sealed record SubstringFormatOptions(
        int StartPosition,
        int EndPosition,
        string SourceFormatString)
    {
        /// <summary>
        /// Parses the three comma-separated arguments for <c>&lt;substr&gt;</c>.
        /// </summary>
        /// <param name="arg">Raw argument text from the template.</param>
        /// <returns>Parsed options.</returns>
        /// <exception cref="InvalidOperationException">Thrown when the argument shape or values are invalid.</exception>
        internal static SubstringFormatOptions Parse(string arg)
        {
            if (string.IsNullOrEmpty(arg))
                throw new InvalidOperationException(
                    "<substr> requires 3 arguments: start-position,end-position,source-format-string.");

            var parts = arg.Split(',', 3);
            if (parts.Length != 3)
                throw new InvalidOperationException(
                    $"<substr> requires exactly 3 comma-separated arguments (got {parts.Length}): '{arg}'.");

            if (!int.TryParse(parts[0].Trim(), NumberStyles.Integer, CultureInfo.InvariantCulture, out var startPosition))
                throw new InvalidOperationException(
                    $"<substr> start-position must be a non-zero integer (got '{parts[0].Trim()}').");

            if (!int.TryParse(parts[1].Trim(), NumberStyles.Integer, CultureInfo.InvariantCulture, out var endPosition))
                throw new InvalidOperationException(
                    $"<substr> end-position must be a non-zero integer (got '{parts[1].Trim()}').");

            if (startPosition == 0)
                throw new InvalidOperationException("<substr> start-position must not be zero.");

            if (endPosition == 0)
                throw new InvalidOperationException("<substr> end-position must not be zero.");

            return new SubstringFormatOptions(
                StartPosition: startPosition,
                EndPosition: endPosition,
                SourceFormatString: parts[2]);
        }
    }

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
        /// <inheritdoc />
        public IReadOnlyList<string> Names { get; } = ["substr"];

        /// <inheritdoc />
        public string Resolve(string arg, RenameItem item)
        {
            var options = SubstringFormatOptions.Parse(arg);
            var source = FormatStringResolver.ResolveTemplate(options.SourceFormatString, item);

            if (source.Length == 0)
                return string.Empty;

            var start = _ResolvePosition(options.StartPosition, source.Length);
            var end = _ResolvePosition(options.EndPosition, source.Length);

            if (start <= end)
                return source[(start - 1)..end];

            // Crossed positions: return (end, start] — the range between them, end excluded.
            return source[end..start];
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
