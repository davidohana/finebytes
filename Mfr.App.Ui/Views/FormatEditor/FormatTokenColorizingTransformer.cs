using Avalonia.Media;
using AvaloniaEdit.Document;
using AvaloniaEdit.Rendering;
using Mfr.Filters.Formatting.FormatString;

namespace Mfr.App.Ui.Views.FormatEditor
{
    /// <summary>
    /// Colors validated format tokens and washes the current validation error span.
    /// </summary>
    /// <remarks>
    /// Delimiters (<c>&lt;</c>/<c>&gt;</c>) and the written token name get distinct brushes; args
    /// (from the first <c>:</c> through the character before <c>&gt;</c>) keep the editor default
    /// foreground. Nested tokens inside args are not colored separately.
    /// </remarks>
    internal sealed class FormatTokenColorizingTransformer : DocumentColorizingTransformer
    {
        /// <summary>
        /// Gets or sets validated token spans to accent.
        /// </summary>
        public IReadOnlyList<FormatTokenSpan> Tokens { get; set; } = [];

        /// <summary>
        /// Gets or sets the failing span start, or <c>-1</c> when none.
        /// </summary>
        public int ErrorPosition { get; set; } = -1;

        /// <summary>
        /// Gets or sets the failing span length.
        /// </summary>
        public int ErrorLength { get; set; }

        /// <summary>
        /// Gets or sets the brush for <c>&lt;</c> and <c>&gt;</c> delimiters.
        /// </summary>
        public IBrush? TokenDelimiterForeground { get; set; }

        /// <summary>
        /// Gets or sets the brush for the written token name (text between <c>&lt;</c> and <c>:</c>/<c>&gt;</c>).
        /// </summary>
        public IBrush? TokenNameForeground { get; set; }

        /// <summary>
        /// Gets or sets the soft background wash for the error span.
        /// </summary>
        public IBrush? ErrorBackground { get; set; }

        /// <summary>
        /// Computes delimiter and written-name ranges for a validated token span.
        /// </summary>
        /// <param name="token">Token span from <see cref="FormatStringSyntax"/> validation.</param>
        /// <param name="openStart">Start of the opening <c>&lt;</c>.</param>
        /// <param name="openEnd">Exclusive end of the opening <c>&lt;</c>.</param>
        /// <param name="nameStart">Start of the written name (may equal <paramref name="nameEnd"/>).</param>
        /// <param name="nameEnd">
        /// Exclusive end of the written name, clamped so it never reaches the closing <c>&gt;</c>.
        /// </param>
        /// <param name="closeStart">Start of the closing <c>&gt;</c>.</param>
        /// <param name="closeEnd">Exclusive end of the closing <c>&gt;</c>.</param>
        /// <returns>
        /// <see langword="true"/> when <paramref name="token"/> is at least <c>&lt;&gt;</c>-sized;
        /// otherwise <see langword="false"/> and the out ranges are undefined.
        /// </returns>
        internal static bool TryGetColorRanges(
            FormatTokenSpan token,
            out int openStart,
            out int openEnd,
            out int nameStart,
            out int nameEnd,
            out int closeStart,
            out int closeEnd
        )
        {
            openStart = 0;
            openEnd = 0;
            nameStart = 0;
            nameEnd = 0;
            closeStart = 0;
            closeEnd = 0;

            if (token.Length < 2)
            {
                return false;
            }

            openStart = token.Start;
            openEnd = token.Start + 1;
            closeEnd = token.Start + token.Length;
            closeStart = closeEnd - 1;
            nameStart = openEnd;
            // Clamp: WrittenName must never paint into '>' (aliases must use written length, not canonical).
            nameEnd = Math.Min(nameStart + token.WrittenName.Length, closeStart);
            return true;
        }

        /// <inheritdoc />
        protected override void ColorizeLine(DocumentLine line)
        {
            var lineStart = line.Offset;
            var lineEnd = line.EndOffset;

            foreach (var token in Tokens)
            {
                if (
                    !TryGetColorRanges(
                        token,
                        out var openStart,
                        out var openEnd,
                        out var nameStart,
                        out var nameEnd,
                        out var closeStart,
                        out var closeEnd
                    )
                )
                {
                    continue;
                }

                if (TokenDelimiterForeground is not null)
                {
                    _ColorizeOverlap(
                        lineStart,
                        lineEnd,
                        openStart,
                        openEnd,
                        element => element.TextRunProperties.SetForegroundBrush(TokenDelimiterForeground)
                    );
                    _ColorizeOverlap(
                        lineStart,
                        lineEnd,
                        closeStart,
                        closeEnd,
                        element => element.TextRunProperties.SetForegroundBrush(TokenDelimiterForeground)
                    );
                }

                if (TokenNameForeground is not null && nameEnd > nameStart)
                {
                    _ColorizeOverlap(
                        lineStart,
                        lineEnd,
                        nameStart,
                        nameEnd,
                        element => element.TextRunProperties.SetForegroundBrush(TokenNameForeground)
                    );
                }
            }

            if (ErrorBackground is null || ErrorPosition < 0 || ErrorLength <= 0)
            {
                return;
            }

            _ColorizeOverlap(
                lineStart,
                lineEnd,
                ErrorPosition,
                ErrorPosition + ErrorLength,
                element => element.TextRunProperties.SetBackgroundBrush(ErrorBackground)
            );
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
