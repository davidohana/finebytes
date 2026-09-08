using Avalonia;
using Avalonia.Media;
using Avalonia.VisualTree;
using AvaloniaEdit.Document;
using AvaloniaEdit.Editing;
using AvaloniaEdit.Rendering;
using Mfr.Filters.Formatting.FormatString;

namespace Mfr.App.Ui.Views.FormatEditor
{
    /// <summary>
    /// Draws soft alternating token and error washes under AvaloniaEdit selection (so mouse selection stays visible).
    /// </summary>
    /// <remarks>
    /// Uses <see cref="KnownLayer.Background"/> and skips ranges covered by the current selection so the
    /// selection layer is not fighting a same-luminance chip. Even-index tokens use
    /// <see cref="TokenBackground"/>; odd-index tokens use <see cref="TokenAltBackground"/>.
    /// </remarks>
    internal sealed class FormatTokenBackgroundRenderer : IBackgroundRenderer
    {
        /// <summary>
        /// Gets or sets validated token spans to wash.
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
        /// Gets or sets the soft background wash for even-index valid tokens.
        /// </summary>
        public IBrush? TokenBackground { get; set; }

        /// <summary>
        /// Gets or sets the soft background wash for odd-index valid tokens.
        /// </summary>
        public IBrush? TokenAltBackground { get; set; }

        /// <summary>
        /// Gets or sets the soft background wash for the error span.
        /// </summary>
        public IBrush? ErrorBackground { get; set; }

        /// <inheritdoc />
        public KnownLayer Layer => KnownLayer.Background;

        /// <inheritdoc />
        public void Draw(TextView textView, DrawingContext drawingContext)
        {
            ArgumentNullException.ThrowIfNull(textView);
            ArgumentNullException.ThrowIfNull(drawingContext);

            var selection = _TryGetTextArea(textView)?.Selection;
            for (var i = 0; i < Tokens.Count; i++)
            {
                var token = Tokens[i];
                if (token.Length < 2)
                {
                    continue;
                }

                var brush = i % 2 == 0 ? TokenBackground : TokenAltBackground ?? TokenBackground;
                if (brush is null)
                {
                    continue;
                }

                _DrawSegmentMinusSelection(textView, drawingContext, token.Start, token.Length, brush, selection);
            }

            if (ErrorBackground is null || ErrorPosition < 0 || ErrorLength <= 0)
            {
                return;
            }

            _DrawSegmentMinusSelection(
                textView,
                drawingContext,
                ErrorPosition,
                ErrorLength,
                ErrorBackground,
                selection
            );
        }

        /// <summary>
        /// Fills <paramref name="length"/> characters from <paramref name="start"/>, omitting selection overlap.
        /// </summary>
        private static void _DrawSegmentMinusSelection(
            TextView textView,
            DrawingContext drawingContext,
            int start,
            int length,
            IBrush brush,
            Selection? selection
        )
        {
            var end = start + length;
            if (selection is null || selection.IsEmpty)
            {
                _DrawSegment(textView, drawingContext, start, length, brush);
                return;
            }

            var cursor = start;
            foreach (var segment in selection.Segments.OrderBy(s => s.StartOffset))
            {
                var selStart = Math.Max(cursor, segment.StartOffset);
                var selEnd = Math.Min(end, segment.EndOffset);
                if (selStart >= selEnd)
                {
                    continue;
                }

                if (cursor < selStart)
                {
                    _DrawSegment(textView, drawingContext, cursor, selStart - cursor, brush);
                }

                cursor = selEnd;
            }

            if (cursor < end)
            {
                _DrawSegment(textView, drawingContext, cursor, end - cursor, brush);
            }
        }

        /// <summary>
        /// Fills the visual geometry for a document segment.
        /// </summary>
        private static void _DrawSegment(
            TextView textView,
            DrawingContext drawingContext,
            int start,
            int length,
            IBrush brush
        )
        {
            if (length <= 0)
            {
                return;
            }

            var builder = new BackgroundGeometryBuilder { AlignToWholePixels = true, CornerRadius = 1 };
            builder.AddSegment(textView, new SimpleSegment(start, length));
            var geometry = builder.CreateGeometry();
            if (geometry is null)
            {
                return;
            }

            drawingContext.DrawGeometry(brush, null, geometry);
        }

        /// <summary>
        /// Resolves the hosting <see cref="TextArea"/> from the text view ancestry.
        /// </summary>
        private static TextArea? _TryGetTextArea(TextView textView)
        {
            for (var visual = textView as Visual; visual is not null; visual = visual.GetVisualParent())
            {
                if (visual is TextArea textArea)
                {
                    return textArea;
                }
            }

            return null;
        }
    }
}
