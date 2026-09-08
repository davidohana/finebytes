using Avalonia.Media;
using AvaloniaEdit.Document;
using AvaloniaEdit.Rendering;
using Mfr.Filters.Formatting.FormatString;

namespace Mfr.App.Ui.Views.FormatEditor
{
    /// <summary>
    /// Accents the written token name inside validated format tokens.
    /// </summary>
    /// <remarks>
    /// Token and error backgrounds are drawn by <see cref="FormatTokenBackgroundRenderer"/> on the
    /// Background layer so selection stays visible. This transformer only sets name foreground.
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
            if (TokenNameForeground is null)
            {
                return;
            }

            var lineStart = line.Offset;
            var lineEnd = line.EndOffset;

            foreach (var token in Tokens)
            {
                if (!TryGetNameRange(token, out var nameStart, out var nameEnd) || nameEnd <= nameStart)
                {
                    continue;
                }

                _ColorizeOverlap(
                    lineStart,
                    lineEnd,
                    nameStart,
                    nameEnd,
                    element => element.TextRunProperties.SetForegroundBrush(TokenNameForeground)
                );
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
