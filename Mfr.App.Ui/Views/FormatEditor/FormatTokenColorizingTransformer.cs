using Avalonia.Media;
using AvaloniaEdit.Document;
using AvaloniaEdit.Rendering;
using Mfr.Filters.Formatting.FormatString;

namespace Mfr.App.Ui.Views.FormatEditor
{
    /// <summary>
    /// Accents the written token name and numeric literals inside validated format tokens.
    /// </summary>
    /// <remarks>
    /// Token and error backgrounds are drawn by <see cref="FormatTokenBackgroundRenderer"/> on the
    /// Background layer so selection stays visible. This transformer only sets name and number
    /// foregrounds. Numbers are scanned in <see cref="FormatTokenSpan.Args"/> (optional leading
    /// <c>-</c> plus ASCII digits), including nested token args; nested token names are skipped so
    /// digits in names like <c>id3v2</c> stay unaccented.
    /// </remarks>
    internal sealed class FormatTokenColorizingTransformer : DocumentColorizingTransformer
    {
        /// <summary>
        /// Gets or sets validated token spans to style.
        /// </summary>
        public IReadOnlyList<FormatTokenSpan> Tokens { get; set; } = [];

        /// <summary>
        /// Gets or sets the foreground for the written token name.
        /// </summary>
        public IBrush? TokenNameForeground { get; set; }

        /// <summary>
        /// Gets or sets the foreground for numeric literals in token args.
        /// </summary>
        public IBrush? TokenNumberForeground { get; set; }

        /// <summary>
        /// Computes the written-name range inside a validated token span.
        /// </summary>
        /// <param name="token">Token span from <see cref="FormatStringSyntax"/> validation.</param>
        /// <param name="nameStart">Start of the written name (may equal <paramref name="nameEnd"/>).</param>
        /// <param name="nameEnd">
        /// Exclusive end of the written name, clamped so it never reaches the closing <c>&gt;</c>.
        /// </param>
        /// <returns>
        /// <see langword="true"/> when <paramref name="token"/> is at least <c>&lt;&gt;</c>-sized;
        /// otherwise <see langword="false"/>.
        /// </returns>
        internal static bool TryGetNameRange(FormatTokenSpan token, out int nameStart, out int nameEnd)
        {
            nameStart = 0;
            nameEnd = 0;
            if (token.Length < 2)
            {
                return false;
            }

            var openEnd = token.Start + 1;
            var closeStart = token.Start + token.Length - 1;
            nameStart = openEnd;
            // Clamp: WrittenName must never paint into '>' (aliases must use written length, not canonical).
            nameEnd = Math.Min(nameStart + token.WrittenName.Length, closeStart);
            return true;
        }

        /// <summary>
        /// Enumerates absolute document ranges for numeric literals in <paramref name="token"/> args.
        /// </summary>
        /// <param name="token">Validated token span (args may contain nested <c>&lt;…&gt;</c>).</param>
        /// <returns>
        /// Half-open <c>[start, end)</c> ranges for optional <c>-</c> plus one or more ASCII digits.
        /// Nested token names are skipped; numbers in nested args are included.
        /// </returns>
        internal static IEnumerable<(int Start, int End)> EnumerateNumberRanges(FormatTokenSpan token)
        {
            if (token.Args.Length == 0 || token.Length < 2)
            {
                yield break;
            }

            var args = token.Args;
            var argsStart = token.Start + token.Length - 1 - args.Length;
            var inNestedName = false;

            var i = 0;
            while (i < args.Length)
            {
                var c = args[i];
                if (inNestedName)
                {
                    if (c == ':')
                    {
                        inNestedName = false;
                        i++;
                        continue;
                    }

                    if (c == '>')
                    {
                        inNestedName = false;
                        i++;
                        continue;
                    }

                    i++;
                    continue;
                }

                if (c == '<')
                {
                    inNestedName = true;
                    i++;
                    continue;
                }

                if (c == '>')
                {
                    i++;
                    continue;
                }

                if (c == '-' && i + 1 < args.Length && char.IsAsciiDigit(args[i + 1]))
                {
                    var numberStart = i;
                    i += 2;
                    while (i < args.Length && char.IsAsciiDigit(args[i]))
                    {
                        i++;
                    }

                    yield return (argsStart + numberStart, argsStart + i);
                    continue;
                }

                if (char.IsAsciiDigit(c))
                {
                    var numberStart = i;
                    i++;
                    while (i < args.Length && char.IsAsciiDigit(args[i]))
                    {
                        i++;
                    }

                    yield return (argsStart + numberStart, argsStart + i);
                    continue;
                }

                i++;
            }
        }

        /// <inheritdoc />
        protected override void ColorizeLine(DocumentLine line)
        {
            var lineStart = line.Offset;
            var lineEnd = line.EndOffset;

            foreach (var token in Tokens)
            {
                if (
                    TokenNameForeground is not null
                    && TryGetNameRange(token, out var nameStart, out var nameEnd)
                    && nameEnd > nameStart
                )
                {
                    _ColorizeOverlap(
                        lineStart,
                        lineEnd,
                        nameStart,
                        nameEnd,
                        element => element.TextRunProperties.SetForegroundBrush(TokenNameForeground)
                    );
                }

                if (TokenNumberForeground is null)
                {
                    continue;
                }

                foreach (var (numberStart, numberEnd) in EnumerateNumberRanges(token))
                {
                    _ColorizeOverlap(
                        lineStart,
                        lineEnd,
                        numberStart,
                        numberEnd,
                        element => element.TextRunProperties.SetForegroundBrush(TokenNumberForeground)
                    );
                }
            }
        }

        /// <summary>
        /// Applies <paramref name="colorize"/> to the overlap of a document span with the current line.
        /// </summary>
        private void _ColorizeOverlap(
            int lineStart,
            int lineEnd,
            int spanStart,
            int spanEnd,
            Action<VisualLineElement> colorize
        )
        {
            var start = Math.Max(lineStart, spanStart);
            var end = Math.Min(lineEnd, spanEnd);
            if (start >= end)
            {
                return;
            }

            ChangeLinePart(start, end, colorize);
        }
    }
}
