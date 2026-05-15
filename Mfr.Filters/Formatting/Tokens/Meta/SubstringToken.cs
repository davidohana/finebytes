using System.Globalization;
using Mfr.Utils;

namespace Mfr.Filters.Formatting.Tokens.Meta
{
    /// <summary>
    /// Resolves the <c>&lt;substr:…&gt;</c> token (substring over resolved source text).
    /// </summary>
    /// <remarks>
    /// <para>
    /// Named arguments (order-independent), for example
    /// <c>&lt;substr:start=1, end=5, source=&lt;file-name&gt;&gt;</c>.
    /// Whitespace around <c>=</c> and commas is ignored. Commas inside balanced <c>&lt;…&gt;</c> belong to nested templates, not to option boundaries.
    /// </para>
    /// <para>
    /// Positions are 1-based. Negative positions count from the right: -1 is the last character,
    /// -2 is the second-to-last, and so on. Out-of-range positions are clamped to the nearest
    /// valid boundary.
    /// </para>
    /// <para>
    /// When the resolved <c>start</c> position is less than or equal to the resolved
    /// <c>end</c> position, the characters at positions [start, end] (inclusive) are returned.
    /// When the resolved <c>start</c> exceeds the resolved <c>end</c> (crossed arguments),
    /// the characters at positions (end, start] are returned — the range between them,
    /// exclusive of end and inclusive of start.
    /// </para>
    /// <para>
    /// <c>source</c> may be a nested format token such as <c>&lt;full-name&gt;</c>
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

        private static readonly string[] _substrOptionKeys = ["start", "end", "source"];

        /// <inheritdoc />
        public IReadOnlyList<string> Names { get; } = ["substr"];

        /// <inheritdoc />
        /// <exception cref="ArgumentException">Thrown when the format argument is malformed.</exception>
        public Formatter Compile(string tokenArgs)
        {
            var options = _ParseOptions(tokenArgs);
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

        private Options _ParseOptions(string tokenArgs)
        {
            var tokenDisplayName = FormatOptionsParsing.TokenDisplayName(this);
            Require.That(
                !string.IsNullOrEmpty(tokenArgs),
                $"{tokenDisplayName} requires named options ({FormatOptionsParsing.FormatExpectedKeywords(_substrOptionKeys)}).",
                nameof(tokenArgs));

            var map = FormatOptionsParsing.ParseNamedKeyValuePairs(tokenArgs.Trim(), tokenDisplayName);
            FormatOptionsParsing.RequireKnownOptionKeysOnly(map, tokenDisplayName, _substrOptionKeys, nameof(tokenArgs));
            FormatOptionsParsing.RequireAllOptionKeysPresent(map, tokenDisplayName, _substrOptionKeys, nameof(tokenArgs));

            var startText = map["start"].Trim();
            var startParsedOk = int.TryParse(startText, NumberStyles.Integer, CultureInfo.InvariantCulture, out var startPosition);
            Require.That(
                startParsedOk,
                $"{tokenDisplayName} start must be a non-zero integer (got '{startText}').",
                nameof(tokenArgs));

            var endText = map["end"].Trim();
            var endParsedOk = int.TryParse(endText, NumberStyles.Integer, CultureInfo.InvariantCulture, out var endPosition);
            Require.That(
                endParsedOk,
                $"{tokenDisplayName} end must be a non-zero integer (got '{endText}').",
                nameof(tokenArgs));

            Require.That(startPosition != 0, $"{tokenDisplayName} start must not be zero.", nameof(tokenArgs));

            Require.That(endPosition != 0, $"{tokenDisplayName} end must not be zero.", nameof(tokenArgs));

            return new Options(
                StartPosition: startPosition,
                EndPosition: endPosition,
                SourceFormatString: map["source"]);
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
