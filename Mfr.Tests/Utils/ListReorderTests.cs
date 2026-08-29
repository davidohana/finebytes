using Mfr.Utils;

namespace Mfr.Tests.Utils
{
    /// <summary>
    /// Tests for <see cref="ListReorder"/>.
    /// </summary>
    public sealed class ListReorderTests
    {
        /// <summary>
        /// Verifies invalid offset and empty inputs cannot move.
        /// </summary>
        [Fact]
        public void CanMove_returns_false_for_invalid_or_empty_inputs()
        {
            var items = new List<string> { "a", "b" };
            var selected = new HashSet<string> { "a" };

            Assert.False(ListReorder.CanMoveSelectedTowardNeighbor(items, selected, offset: 0));
            Assert.False(ListReorder.CanMoveSelectedTowardNeighbor(items, selected, offset: 2));
            Assert.False(ListReorder.CanMoveSelectedTowardNeighbor([], selected, offset: -1));
            Assert.False(ListReorder.CanMoveSelectedTowardNeighbor(items, new HashSet<string>(), offset: -1));
        }

        /// <summary>
        /// Verifies a single item at the top cannot move up.
        /// </summary>
        [Fact]
        public void TryMove_respects_single_item_bounds()
        {
            var items = new List<string> { "a", "b", "c" };
            var selected = new HashSet<string> { "a" };

            Assert.False(ListReorder.TryMoveSelectedTowardNeighbor(items, selected, offset: -1));
            Assert.Equal(["a", "b", "c"], items);

            selected = ["b"];
            Assert.True(ListReorder.TryMoveSelectedTowardNeighbor(items, selected, offset: -1));
            Assert.Equal(["b", "a", "c"], items);

            selected = ["b"];
            Assert.False(ListReorder.TryMoveSelectedTowardNeighbor(items, selected, offset: -1));

            selected = ["c"];
            Assert.False(ListReorder.TryMoveSelectedTowardNeighbor(items, selected, offset: 1));
            Assert.True(ListReorder.TryMoveSelectedTowardNeighbor(items, selected, offset: -1));
            Assert.Equal(["b", "c", "a"], items);
        }

        /// <summary>
        /// Verifies a contiguous selection moves as a block.
        /// </summary>
        [Fact]
        public void TryMove_moves_contiguous_selection_as_block()
        {
            var items = new List<string> { "a", "b", "c", "d" };
            var selected = new HashSet<string> { "b", "c" };

            Assert.True(ListReorder.TryMoveSelectedTowardNeighbor(items, selected, offset: 1));
            Assert.Equal(["a", "d", "b", "c"], items);

            selected = ["b", "c"];
            Assert.True(ListReorder.TryMoveSelectedTowardNeighbor(items, selected, offset: -1));
            Assert.Equal(["a", "b", "c", "d"], items);
        }

        /// <summary>
        /// Verifies non-contiguous selections only advance items with a free slot.
        /// </summary>
        [Fact]
        public void TryMove_moves_non_contiguous_items_independently()
        {
            var items = new List<string> { "a", "b", "c" };
            var selected = new HashSet<string> { "a", "c" };

            Assert.True(ListReorder.TryMoveSelectedTowardNeighbor(items, selected, offset: -1));
            Assert.Equal(["a", "c", "b"], items);
        }
    }
}
