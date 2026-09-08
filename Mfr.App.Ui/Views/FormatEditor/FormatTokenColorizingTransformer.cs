using Avalonia.Media;
using AvaloniaEdit.Document;
using AvaloniaEdit.Rendering;
using Mfr.Filters.Formatting.FormatString;

namespace Mfr.App.Ui.Views.FormatEditor
{
    /// <summary>
    /// Washes validated format tokens and accents the written token name; washes the error span.
    /// </summary>
    /// <remarks>
    /// Each valid <c>&lt;…&gt;</c> gets a soft background. The written name (not aliases' canonical
    /// length) gets a distinct foreground; delimiters and args keep the editor default foreground.
    /// Nested tokens inside args are not styled separately. Error wash overlays the failing span.
    /// </remarks>
    internal sealed class FormatTokenColorizingTransformer : DocumentColorizingTransformer
    {
        /// <summary>
        /// Gets or sets validated token spans to style.
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
        /// Gets or sets the soft background wash for valid tokens.
        /// </summary>
        public IBrush? TokenBackground { get; set; }

        /// <summary>
        /// Gets or sets the foreground for the written token name.
        /// </summary>
        public IBrush? TokenNameForeground { get; set; }

        /// <summary>
        /// Gets or sets the soft background wash for the error span.
        /// </summary>
        public IBrush? ErrorBackground { get; set; }

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

        /// <inheritdoc />
        protected override void ColorizeLine(DocumentLine line)
        {
            var lineStart = line.Offset;
            var lineEnd = line.EndOffset;

            foreach (var token in Tokens)
            {
                if (token.Length < 2)
                {
                    continue;
                }

                if (TokenBackground is not null)
                {
                    _ColorizeOverlap(
                        lineStart,
                        lineEnd,
                        token.Start,
                        token.Start + token.Length,
                        element => element.TextRunProperties.SetBackgroundBrush(TokenBackground)
                    );
                }

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
