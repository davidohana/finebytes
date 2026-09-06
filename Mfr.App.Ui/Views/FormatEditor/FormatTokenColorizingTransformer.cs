using Avalonia.Media;
using AvaloniaEdit.Document;
using AvaloniaEdit.Rendering;
using Mfr.Filters.Formatting.FormatString;

namespace Mfr.App.Ui.Views.FormatEditor
{
    /// <summary>
    /// Colors validated format tokens and washes the current validation error span.
    /// </summary>
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
        /// Gets or sets the accent brush for valid tokens.
        /// </summary>
        public IBrush? TokenForeground { get; set; }

        /// <summary>
        /// Gets or sets the soft background wash for the error span.
        /// </summary>
        public IBrush? ErrorBackground { get; set; }

        /// <inheritdoc />
        protected override void ColorizeLine(DocumentLine line)
        {
            var lineStart = line.Offset;
            var lineEnd = line.EndOffset;

            if (TokenForeground is not null)
            {
                foreach (var token in Tokens)
                {
                    _ColorizeOverlap(
                        lineStart,
                        lineEnd,
                        token.Start,
                        token.Start + token.Length,
                        element => element.TextRunProperties.SetForegroundBrush(TokenForeground)
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
