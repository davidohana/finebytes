using Mfr.Filters.Trimming;

namespace Mfr.Tests.Models.Filters.Trimming
{
    /// <summary>
    /// Tests for Trim Between selection ↔ position mapping.
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

        /// <summary>
        /// Verifies a non-empty selection maps to left-anchored inclusive positions.
        /// </summary>
        [Fact]
        public void TryGetPositionsFromSelection_maps_inclusive_left_positions()
        {
            Assert.True(
                TrimBetweenFilter.TryGetPositionsFromSelection(
                    selectionStart: 1,
                    selectionLength: 3,
                    out var start,
                    out var end
                )
            );
            Assert.Equal(new Position(2, Side.Left), start);
            Assert.Equal(new Position(4, Side.Left), end);
        }

        /// <summary>
        /// Verifies an empty selection is rejected.
        /// </summary>
        [Fact]
        public void TryGetPositionsFromSelection_rejects_empty_selection()
        {
            Assert.False(
                TrimBetweenFilter.TryGetPositionsFromSelection(
                    selectionStart: 2,
                    selectionLength: 0,
                    out _,
                    out _
                )
            );
        }

        /// <summary>
        /// Verifies selection → positions → selection round-trips for left-anchored ranges.
        /// </summary>
        [Fact]
        public void Selection_and_positions_round_trip()
        {
            const string text = "abcdef";
            const int selectionStart = 1;
            const int selectionLength = 3;

            Assert.True(
                TrimBetweenFilter.TryGetPositionsFromSelection(
                    selectionStart,
                    selectionLength,
                    out var start,
                    out var end
                )
            );
            Assert.True(
                TrimBetweenFilter.TryGetSelectionRange(text, start, end, out var roundTripStart, out var roundTripLength)
            );
            Assert.Equal(selectionStart, roundTripStart);
            Assert.Equal(selectionLength, roundTripLength);
        }
    }
}
