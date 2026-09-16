using Mfr.Filters;
using Mfr.Filters.Trimming;

namespace Mfr.Tests.Models.Filters.Trimming
{
    /// <summary>
    /// Tests for <see cref="ExtractLeftFilter"/>.
    /// </summary>
    public class ExtractLeftFilterTests
    {
        private static readonly FileNameTarget _target = new();

        /// <summary>
        /// Verifies left extract clamps count to <c>[0, length]</c> then keeps that many characters.
        /// </summary>
        /// <param name="count">Requested keep length (may be negative or past the segment).</param>
        /// <param name="input">File name under test.</param>
        /// <param name="expected">File name after extract.</param>
        [Theory]
        [InlineData(3, "abcdef", "abc")]
        [InlineData(0, "abc", "")]
        [InlineData(-1, "abc", "")]
        [InlineData(100, "ab", "ab")]
        public void Apply_TakesLeftSubstring_ClampingCount(int count, string input, string expected)
        {
            var f = new ExtractLeftFilter(_target, new CountFilterOptions(count));
            Assert.Equal(expected, FilterTestHelpers.ApplyToFileName(f, input));
        }
    }
}
