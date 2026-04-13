using Mfr.Filters.Trimming;
using Mfr.Models;

namespace Mfr.Tests.Models.Filters.Trimming
{
    /// <summary>
    /// Tests for <see cref="TrimBetweenFilter"/>.
    /// </summary>
    public class TrimBetweenFilterTests
    {
        private static readonly FileNameTarget _target = new(FileNamePart.Prefix);

        /// <summary>
        /// Verifies example from issue:
        /// Portishead - Glory Box >>> Portishead - Box
        /// Remove characters between position 13 from the left side (incl.) and position 5 from the right side (incl.)
        /// </summary>
        [Fact]
        public void Apply_IssueExample()
        {
            var options = new TrimBetweenFilterOptions(
                new Position(13, Side.Left),
                new Position(5, Side.Right)
            );
            var f = new TrimBetweenFilter(true, _target, options);

            // "Portishead - Glory Box"
            // Pos 13 (Left, 1-based) is 'G' (P-1, o-2, r-3, t-4, i-5, s-6, h-7, e-8, a-9, d-10, ' '-11, --12, ' '-13)
            // Wait, let's re-count "Portishead - Glory Box"
            // P o r t i s h e a d   -   G l o r y   B o x
            // 1 2 3 4 5 6 7 8 9 10 11 12 13 14 15 16 17 18 19 20 21 22
            // Pos 13 (Left) is 'G'
            // Pos 5 (Right) is:
            // x (1), o (2), B (3), ' ' (4), y (5)
            // So from 'G' (13) to 'y' (17)
            // "G l o r y" is 5 chars.
            // "Portishead - " + " Box" = "Portishead - Box"

            Assert.Equal("Portishead - Box", FilterTestHelpers.ApplyToPrefix(f, "Portishead - Glory Box"));
        }

        [Fact]
        public void Apply_LeftToLeft()
        {
            // Remove from 2 to 4 (incl): "abcd" -> "a"
            var options = new TrimBetweenFilterOptions(
                new Position(2, Side.Left),
                new Position(4, Side.Left)
            );
            var f = new TrimBetweenFilter(true, _target, options);
            Assert.Equal("a", FilterTestHelpers.ApplyToPrefix(f, "abcd"));
        }

        [Fact]
        public void Apply_RightToRight()
        {
            // "abcd", pos 1 Right is 'd', pos 3 Right is 'b'.
            // Remove 'b', 'c', 'd' -> "a"
            var options = new TrimBetweenFilterOptions(
                new Position(3, Side.Right),
                new Position(1, Side.Right)
            );
            var f = new TrimBetweenFilter(true, _target, options);
            Assert.Equal("a", FilterTestHelpers.ApplyToPrefix(f, "abcd"));
        }

        [Fact]
        public void Apply_FullTrim()
        {
            var options = new TrimBetweenFilterOptions(
                new Position(1, Side.Left),
                new Position(1, Side.Right)
            );
            var f = new TrimBetweenFilter(true, _target, options);
            Assert.Equal("", FilterTestHelpers.ApplyToPrefix(f, "anything"));
        }

        [Fact]
        public void Apply_ReorderedPositions()
        {
            // Start at 4, End at 2 -> should be same as 2 to 4
            var options = new TrimBetweenFilterOptions(
                new Position(4, Side.Left),
                new Position(2, Side.Left)
            );
            var f = new TrimBetweenFilter(true, _target, options);
            Assert.Equal("ae", FilterTestHelpers.ApplyToPrefix(f, "abcde"));
        }

        [Fact]
        public void Apply_ClampedPositions()
        {
            var options = new TrimBetweenFilterOptions(
                new Position(0, Side.Left), // Clamps to 1 (index 0)
                new Position(100, Side.Left) // Clamps to length (index length-1)
            );
            var f = new TrimBetweenFilter(true, _target, options);
            Assert.Equal("", FilterTestHelpers.ApplyToPrefix(f, "abc"));
        }
    }
}
