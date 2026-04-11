using Mfr.Filters;
using Mfr.Filters.Text;
using Mfr.Models;

namespace Mfr.Tests.Models.Filters.Text
{
    /// <summary>
    /// Tests for <see cref="TrimRightFilter"/>.
    /// </summary>
    public class TrimRightFilterTests
    {
        private static readonly FileNameTarget _target = new(FileNamePart.Prefix);

        /// <summary>
        /// Verifies trimming from the right by count.
        /// </summary>
        [Fact]
        public void Apply_RemovesRightCharacters()
        {
            var f = new TrimRightFilter(true, _target, new CountFilterOptions(2));
            Assert.Equal("ab", FilterTestHelpers.ApplyToPrefix(f, "abcd"));
        }

        /// <summary>
        /// Verifies non-positive count leaves the segment unchanged.
        /// </summary>
        [Fact]
        public void Apply_NonPositiveCount_ReturnsOriginal()
        {
            var f = new TrimRightFilter(true, _target, new CountFilterOptions(0));
            Assert.Equal("ab", FilterTestHelpers.ApplyToPrefix(f, "ab"));
        }

        /// <summary>
        /// Verifies over-trim yields empty string.
        /// </summary>
        [Fact]
        public void Apply_CountExceedsLength_ReturnsEmpty()
        {
            var f = new TrimRightFilter(true, _target, new CountFilterOptions(10));
            Assert.Equal("", FilterTestHelpers.ApplyToPrefix(f, "hi"));
        }
    }
}
