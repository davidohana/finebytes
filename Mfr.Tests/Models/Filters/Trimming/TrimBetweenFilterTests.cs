using Mfr.Filters.Trimming;

namespace Mfr.Tests.Models.Filters.Trimming
{
    /// <summary>
    /// Tests for <see cref="TrimBetweenFilter"/>.
    /// </summary>
    public class TrimBetweenFilterTests
    {
        private static readonly FilePrefixTarget _target = new();

        /// <summary>
        /// Verifies the MFR7 help example:
        /// Portishead - Glory Box → Portishead - Box
        /// (left 13 incl. through right 5 incl.).
        /// </summary>
        [Fact]
        public void Apply_IssueExample()
        {
            // "Portishead - Glory Box" (len 22): left 13 is the space before 'G';
            // right 5 is 'y'. Removal is " Glory" → "Portishead - Box".
            var options = new TrimBetweenFilterOptions(new Position(13, Side.Left), new Position(5, Side.Right));
            var f = new TrimBetweenFilter(_target, options);

            Assert.Equal("Portishead - Box", FilterTestHelpers.ApplyToPrefix(f, "Portishead - Glory Box"));
        }

        [Fact]
        public void Apply_LeftToLeft()
        {
            // Remove from 2 to 4 (incl): "abcd" -> "a"
            var options = new TrimBetweenFilterOptions(new Position(2, Side.Left), new Position(4, Side.Left));
            var f = new TrimBetweenFilter(_target, options);
            Assert.Equal("a", FilterTestHelpers.ApplyToPrefix(f, "abcd"));
        }

        [Fact]
        public void Apply_RightToRight()
        {
            // "abcd", pos 1 Right is 'd', pos 3 Right is 'b'.
            // Remove 'b', 'c', 'd' -> "a"
            var options = new TrimBetweenFilterOptions(new Position(3, Side.Right), new Position(1, Side.Right));
            var f = new TrimBetweenFilter(_target, options);
            Assert.Equal("a", FilterTestHelpers.ApplyToPrefix(f, "abcd"));
        }

        [Fact]
        public void Apply_FullTrim()
        {
            var options = new TrimBetweenFilterOptions(new Position(1, Side.Left), new Position(1, Side.Right));
            var f = new TrimBetweenFilter(_target, options);
            Assert.Equal("", FilterTestHelpers.ApplyToPrefix(f, "anything"));
        }

        [Fact]
        public void Apply_ReorderedPositions()
        {
            // Start at 4, End at 2 -> should be same as 2 to 4
            var options = new TrimBetweenFilterOptions(new Position(4, Side.Left), new Position(2, Side.Left));
            var f = new TrimBetweenFilter(_target, options);
            Assert.Equal("ae", FilterTestHelpers.ApplyToPrefix(f, "abcde"));
        }

        [Fact]
        public void Apply_ClampedPositions()
        {
            var options = new TrimBetweenFilterOptions(
                new Position(0, Side.Left), // Clamps to index 0
                new Position(100, Side.Left) // Clamps to length-1
            );
            var f = new TrimBetweenFilter(_target, options);
            Assert.Equal("", FilterTestHelpers.ApplyToPrefix(f, "abc"));
        }

        [Fact]
        public void Apply_Empty_ReturnsEmpty()
        {
            var options = new TrimBetweenFilterOptions(new Position(1, Side.Left), new Position(1, Side.Right));
            var f = new TrimBetweenFilter(_target, options);
            Assert.Equal("", FilterTestHelpers.ApplyToPrefix(f, ""));
        }

        [Fact]
        public void Apply_SameStartAndEnd_RemovesOneCharacter()
        {
            var options = new TrimBetweenFilterOptions(new Position(2, Side.Left), new Position(2, Side.Left));
            var f = new TrimBetweenFilter(_target, options);
            Assert.Equal("ac", FilterTestHelpers.ApplyToPrefix(f, "abc"));
        }
    }
}
