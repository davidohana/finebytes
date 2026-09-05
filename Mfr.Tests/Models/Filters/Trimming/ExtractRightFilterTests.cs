using Mfr.Filters;
using Mfr.Filters.Trimming;

namespace Mfr.Tests.Models.Filters.Trimming
{
    /// <summary>
    /// Tests for <see cref="ExtractRightFilter"/>.
    /// </summary>
    public class ExtractRightFilterTests
    {
        private static readonly FilePrefixTarget _target = new();

        /// <summary>
        /// Verifies right extract clamps count to <c>[0, length]</c> then keeps that many characters.
        /// </summary>
        /// <param name="count">Requested keep length (may be negative or past the segment).</param>
        /// <param name="input">Prefix under test.</param>
        /// <param name="expected">Prefix after extract.</param>
        [Theory]
        [InlineData(3, "abcdef", "def")]
        [InlineData(0, "abc", "")]
        [InlineData(-1, "abc", "")]
        [InlineData(100, "ab", "ab")]
        public void Apply_TakesRightSubstring_ClampingCount(int count, string input, string expected)
        {
            var f = new ExtractRightFilter(_target, new CountFilterOptions(count));
            Assert.Equal(expected, FilterTestHelpers.ApplyToPrefix(f, input));
        }
    }
}
