using Mfr.Filters;
using Mfr.Filters.Text;
using Mfr.Models;

namespace Mfr.Tests.Models.Filters.Text
{
    /// <summary>
    /// Tests for <see cref="TrimLeftFilter"/>.
    /// </summary>
    public class TrimLeftFilterTests
    {
        private static readonly FileNameTarget _Target = new(FileNamePart.Prefix);

        /// <summary>
        /// Verifies trimming from the left by count.
        /// </summary>
        [Fact]
        public void Apply_RemovesLeftCharacters()
        {
            var f = new TrimLeftFilter(true, _Target, new CountFilterOptions(2));
            Assert.Equal("cd", FilterTestHelpers.ApplyToPrefix(f, "abcd"));
        }

        /// <summary>
        /// Verifies non-positive count leaves the segment unchanged.
        /// </summary>
        [Fact]
        public void Apply_NonPositiveCount_ReturnsOriginal()
        {
            var f = new TrimLeftFilter(true, _Target, new CountFilterOptions(0));
            Assert.Equal("ab", FilterTestHelpers.ApplyToPrefix(f, "ab"));
        }

        /// <summary>
        /// Verifies over-trim yields empty string.
        /// </summary>
        [Fact]
        public void Apply_CountExceedsLength_ReturnsEmpty()
        {
            var f = new TrimLeftFilter(true, _Target, new CountFilterOptions(10));
            Assert.Equal("", FilterTestHelpers.ApplyToPrefix(f, "hi"));
        }
    }
}
