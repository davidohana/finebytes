using Mfr.App.Ui.ViewModels.FilterEditors.Trimming;

namespace Mfr.Tests.Ui.FilterEditors.Trimming
{
    /// <summary>
    /// Unit tests for <see cref="VisualTrimHelperMapping"/> count-edge formulas.
    /// </summary>
    public sealed class VisualTrimHelperMappingTests
    {
        /// <summary>
        /// Verifies left-edge selection expands to the start and uses end index as count.
        /// </summary>
        [Fact]
        public void SelectionToLeftCount_forces_start_at_zero()
        {
            var count = VisualTrimHelperMapping.SelectionToLeftCount(
                selectionStart: 2,
                selectionLength: 3,
                out var highlightStart,
                out var highlightLength
            );
            Assert.Equal(5, count);
            Assert.Equal(0, highlightStart);
            Assert.Equal(5, highlightLength);
        }

        /// <summary>
        /// Verifies right-edge selection expands to the end.
        /// </summary>
        [Fact]
        public void SelectionToRightCount_forces_end_at_text_length()
        {
            var count = VisualTrimHelperMapping.SelectionToRightCount(
                selectionStart: 4,
                textLength: 10,
                out var highlightStart,
                out var highlightLength
            );
            Assert.Equal(6, count);
            Assert.Equal(4, highlightStart);
            Assert.Equal(6, highlightLength);
        }

        /// <summary>
        /// Verifies count-to-highlight for left and right edges.
        /// </summary>
        [Fact]
        public void CountToHighlight_clamps_to_text_length()
        {
            VisualTrimHelperMapping.CountToLeftHighlight(100, 5, out var leftStart, out var leftLength);
            Assert.Equal(0, leftStart);
            Assert.Equal(5, leftLength);

            VisualTrimHelperMapping.CountToRightHighlight(3, 10, out var rightStart, out var rightLength);
            Assert.Equal(7, rightStart);
            Assert.Equal(3, rightLength);
        }
    }
}
