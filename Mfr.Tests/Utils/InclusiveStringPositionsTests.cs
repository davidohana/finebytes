using Mfr.Utils;

namespace Mfr.Tests.Utils
{
    /// <summary>
    /// Tests for <see cref="InclusiveStringPositions"/>.
    /// </summary>
    public sealed class InclusiveStringPositionsTests
    {
        /// <summary>
        /// Verifies left-anchored inclusive positions map and clamp to <c>[0, length - 1]</c>.
        /// </summary>
        /// <param name="oneBased">1-based position.</param>
        /// <param name="length">String length.</param>
        /// <param name="expected">Expected 0-based index.</param>
        [Theory]
        [InlineData(1, 5, 0)]
        [InlineData(5, 5, 4)]
        [InlineData(0, 5, 0)]
        [InlineData(-3, 5, 0)]
        [InlineData(6, 5, 4)]
        [InlineData(99, 5, 4)]
        public void ToZeroBasedIndex_from_left_clamps(int oneBased, int length, int expected)
        {
            Assert.Equal(expected, InclusiveStringPositions.ToZeroBasedIndex(oneBased, fromLeft: true, length));
        }

        /// <summary>
        /// Verifies right-anchored inclusive positions map and clamp to <c>[0, length - 1]</c>.
        /// </summary>
        /// <param name="oneBased">1-based position from the end.</param>
        /// <param name="length">String length.</param>
        /// <param name="expected">Expected 0-based index.</param>
        [Theory]
        [InlineData(1, 5, 4)]
        [InlineData(5, 5, 0)]
        [InlineData(0, 5, 4)]
        [InlineData(-3, 5, 4)]
        [InlineData(6, 5, 0)]
        [InlineData(99, 5, 0)]
        public void ToZeroBasedIndex_from_right_clamps(int oneBased, int length, int expected)
        {
            Assert.Equal(expected, InclusiveStringPositions.ToZeroBasedIndex(oneBased, fromLeft: false, length));
        }

        /// <summary>
        /// Verifies a single-character string always resolves to index 0.
        /// </summary>
        [Theory]
        [InlineData(1, true)]
        [InlineData(1, false)]
        [InlineData(0, true)]
        [InlineData(0, false)]
        [InlineData(2, true)]
        [InlineData(2, false)]
        public void ToZeroBasedIndex_length_one_always_zero(int oneBased, bool fromLeft)
        {
            Assert.Equal(0, InclusiveStringPositions.ToZeroBasedIndex(oneBased, fromLeft, length: 1));
        }

        /// <summary>
        /// Verifies non-positive length is rejected.
        /// </summary>
        [Theory]
        [InlineData(0)]
        [InlineData(-1)]
        public void ToZeroBasedIndex_rejects_non_positive_length(int length)
        {
            Assert.Throws<ArgumentException>(() =>
                InclusiveStringPositions.ToZeroBasedIndex(1, fromLeft: true, length)
            );
        }
    }
}
