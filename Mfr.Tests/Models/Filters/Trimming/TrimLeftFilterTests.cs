using Mfr.Filters;
using Mfr.Filters.Trimming;
using Mfr.Models;

namespace Mfr.Tests.Models.Filters.Trimming
{
    /// <summary>
    /// Tests for <see cref="TrimLeftFilter"/>.
    /// </summary>
    public class TrimLeftFilterTests
    {
        private static readonly FilePrefixTarget _target = new();

        /// <summary>
        /// Verifies trimming from the left by count.
        /// </summary>
        [Fact]
        public void Apply_RemovesLeftCharacters()
        {
            var f = new TrimLeftFilter(_target, new CountFilterOptions(2));
            Assert.Equal("cd", FilterTestHelpers.ApplyToPrefix(f, "abcd"));
        }

        /// <summary>
        /// Verifies non-positive count leaves the segment unchanged.
        /// </summary>
        [Fact]
        public void Apply_NonPositiveCount_ReturnsOriginal()
        {
            var f = new TrimLeftFilter(_target, new CountFilterOptions(0));
            Assert.Equal("ab", FilterTestHelpers.ApplyToPrefix(f, "ab"));
        }

        /// <summary>
        /// Verifies over-trim yields empty string.
        /// </summary>
        [Fact]
        public void Apply_CountExceedsLength_ReturnsEmpty()
        {
            var f = new TrimLeftFilter(_target, new CountFilterOptions(10));
            Assert.Equal("", FilterTestHelpers.ApplyToPrefix(f, "hi"));
        }
    }
}
