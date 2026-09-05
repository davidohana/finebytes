using Mfr.Filters;
using Mfr.Filters.Trimming;

namespace Mfr.Tests.Models.Filters.Trimming
{
    /// <summary>
    /// Tests for <see cref="TrimLeftFilter"/>.
    /// </summary>
    public class TrimLeftFilterTests
    {
        private static readonly FilePrefixTarget _target = new();

        /// <summary>
        /// Verifies left trim clamps count to <c>[0, length]</c> then drops that many characters.
        /// </summary>
        /// <param name="count">Requested trim length (may be negative or past the segment).</param>
        /// <param name="input">Prefix under test.</param>
        /// <param name="expected">Prefix after trim.</param>
        [Theory]
        [InlineData(2, "abcd", "cd")]
        [InlineData(0, "ab", "ab")]
        [InlineData(-1, "ab", "ab")]
        [InlineData(10, "hi", "")]
        public void Apply_RemovesLeftCharacters_ClampingCount(int count, string input, string expected)
        {
            var f = new TrimLeftFilter(_target, new CountFilterOptions(count));
            Assert.Equal(expected, FilterTestHelpers.ApplyToPrefix(f, input));
        }
    }
}
