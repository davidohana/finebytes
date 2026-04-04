using Mfr.Models;
using Mfr.Models.Filters;
using Mfr.Models.Filters.Text;

namespace Mfr.Tests.Models.Filters.Text
{
    /// <summary>
    /// Tests for <see cref="TrimRightFilter"/>.
    /// </summary>
    public class TrimRightFilterTests
    {
        private static readonly FileNameTarget _Target = new(FileNamePart.Prefix);

        /// <summary>
        /// Verifies trimming from the right by count.
        /// </summary>
        [Fact]
        public void Apply_RemovesRightCharacters()
        {
            var f = new TrimRightFilter(true, _Target, new CountFilterOptions(2));
            Assert.Equal("ab", FilterTestHelpers.ApplyToPrefix(f, "abcd"));
        }

        /// <summary>
        /// Verifies non-positive count leaves the segment unchanged.
        /// </summary>
        [Fact]
        public void Apply_NonPositiveCount_ReturnsOriginal()
        {
            var f = new TrimRightFilter(true, _Target, new CountFilterOptions(0));
            Assert.Equal("ab", FilterTestHelpers.ApplyToPrefix(f, "ab"));
        }

        /// <summary>
        /// Verifies over-trim yields empty string.
        /// </summary>
        [Fact]
        public void Apply_CountExceedsLength_ReturnsEmpty()
        {
            var f = new TrimRightFilter(true, _Target, new CountFilterOptions(10));
            Assert.Equal("", FilterTestHelpers.ApplyToPrefix(f, "hi"));
        }
    }
}
