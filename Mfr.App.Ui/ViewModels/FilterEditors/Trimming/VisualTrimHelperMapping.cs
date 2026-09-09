namespace Mfr.App.Ui.ViewModels.FilterEditors.Trimming
{
    /// <summary>
    /// Selection ↔ option formulas for the Visual Trim Helper (MFR7 parity).
    /// </summary>
    internal static class VisualTrimHelperMapping
    {
        /// <summary>
        /// How the helper maps a text selection to filter options.
        /// </summary>
        internal enum Mode
        {
            /// <summary>
            /// Force selection to start at 0; count is the selection length (Left Trim / Left Extract).
            /// </summary>
            LeftEdge,

            /// <summary>
            /// Force selection to the end; count is the selection length (Right Trim / Right Extract).
            /// </summary>
            RightEdge,

            /// <summary>
            /// Inclusive mid-string range → 1-based left-anchored start/end (Trim Between).
            /// </summary>
            Range,
        }

        /// <summary>
        /// Maps a raw text selection to a forced highlight and count for left-edge filters.
        /// </summary>
        /// <param name="selectionStart">Current selection start (0-based).</param>
        /// <param name="selectionLength">Current selection length.</param>
        /// <param name="highlightStart">Forced highlight start (always 0).</param>
        /// <param name="highlightLength">Forced highlight length (= count).</param>
        /// <returns>Character count to write to the filter.</returns>
        internal static int SelectionToLeftCount(
            int selectionStart,
            int selectionLength,
            out int highlightStart,
            out int highlightLength
        )
        {
            highlightLength = Math.Max(0, selectionStart + selectionLength);
            highlightStart = 0;
            return highlightLength;
        }

        /// <summary>
        /// Maps a raw text selection to a forced highlight and count for right-edge filters.
        /// </summary>
        /// <param name="selectionStart">Current selection start (0-based).</param>
        /// <param name="textLength">Full sample text length.</param>
        /// <param name="highlightStart">Forced highlight start.</param>
        /// <param name="highlightLength">Forced highlight length (= count).</param>
        /// <returns>Character count to write to the filter.</returns>
        internal static int SelectionToRightCount(
            int selectionStart,
            int textLength,
            out int highlightStart,
            out int highlightLength
        )
        {
            var clampedStart = Math.Clamp(selectionStart, 0, Math.Max(0, textLength));
            highlightLength = Math.Max(0, textLength - clampedStart);
            highlightStart = clampedStart;
            return highlightLength;
        }

        /// <summary>
        /// Maps a non-empty selection to 1-based inclusive left-anchored Trim Between positions.
        /// </summary>
        /// <param name="selectionStart">Selection start (0-based).</param>
        /// <param name="selectionLength">Selection length (must be &gt; 0).</param>
        /// <param name="startValue">1-based inclusive start from the left.</param>
        /// <param name="endValue">1-based inclusive end from the left.</param>
        /// <returns><see langword="true"/> when <paramref name="selectionLength"/> is positive.</returns>
        internal static bool SelectionToLeftAnchoredRange(
            int selectionStart,
            int selectionLength,
            out int startValue,
            out int endValue
        )
        {
            if (selectionLength <= 0)
            {
                startValue = 0;
                endValue = 0;
                return false;
            }

            startValue = selectionStart + 1;
            endValue = selectionStart + selectionLength;
            return true;
        }

        /// <summary>
        /// Maps a left-edge count to a highlight range.
        /// </summary>
        /// <param name="count">Trim/extract character count.</param>
        /// <param name="textLength">Sample text length.</param>
        /// <param name="highlightStart">Highlight start (0).</param>
        /// <param name="highlightLength">Clamped highlight length.</param>
        internal static void CountToLeftHighlight(
            int count,
            int textLength,
            out int highlightStart,
            out int highlightLength
        )
        {
            highlightStart = 0;
            highlightLength = Math.Clamp(count, 0, Math.Max(0, textLength));
        }

        /// <summary>
        /// Maps a right-edge count to a highlight range.
        /// </summary>
        /// <param name="count">Trim/extract character count.</param>
        /// <param name="textLength">Sample text length.</param>
        /// <param name="highlightStart">Highlight start.</param>
        /// <param name="highlightLength">Clamped highlight length.</param>
        internal static void CountToRightHighlight(
            int count,
            int textLength,
            out int highlightStart,
            out int highlightLength
        )
        {
            var clampedCount = Math.Clamp(count, 0, Math.Max(0, textLength));
            highlightStart = Math.Max(0, textLength - clampedCount);
            highlightLength = clampedCount;
        }
    }
}
