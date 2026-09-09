using Mfr.Filters.Trimming;

namespace Mfr.Tests.Models.Filters.Trimming
{
    /// <summary>
    /// Tests for <see cref="TrimBetweenFilter.TryGetSelectionRange"/>.
    /// </summary>
    public sealed class TrimBetweenSelectionRangeTests
    {
        /// <summary>
        /// Verifies left-anchored positions map to a 0-based inclusive selection.
        /// </summary>
        [Fact]
        public void TryGetSelectionRange_left_anchored()
        {
            Assert.True(
                TrimBetweenFilter.TryGetSelectionRange(
                    "abcde",
                    new Position(2, Side.Left),
                    new Position(4, Side.Left),
                    out var start,
                    out var length
                )
            );
            Assert.Equal(1, start);
            Assert.Equal(3, length);
        }

        /// <summary>
        /// Verifies mixed anchors and swap when start lies after end.
        /// </summary>
        [Fact]
        public void TryGetSelectionRange_swaps_when_start_after_end()
        {
            Assert.True(
                TrimBetweenFilter.TryGetSelectionRange(
                    "abcde",
                    new Position(4, Side.Left),
                    new Position(2, Side.Left),
                    out var start,
                    out var length
                )
            );
            Assert.Equal(1, start);
            Assert.Equal(3, length);
        }

        /// <summary>
        /// Verifies right-anchored endpoints.
        /// </summary>
        [Fact]
        public void TryGetSelectionRange_right_anchored()
        {
            Assert.True(
                TrimBetweenFilter.TryGetSelectionRange(
                    "abcd",
                    new Position(3, Side.Right),
                    new Position(1, Side.Right),
                    out var start,
                    out var length
                )
            );
            Assert.Equal(1, start);
            Assert.Equal(3, length);
        }

        /// <summary>
        /// Verifies empty text cannot produce a selection.
        /// </summary>
        [Fact]
        public void TryGetSelectionRange_empty_text_returns_false()
        {
            Assert.False(
                TrimBetweenFilter.TryGetSelectionRange(
                    "",
                    new Position(1, Side.Left),
                    new Position(1, Side.Left),
                    out _,
                    out _
                )
            );
        }
    }
}
