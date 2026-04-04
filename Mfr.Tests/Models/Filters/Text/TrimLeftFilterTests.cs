using Mfr.Models;
using Mfr.Models.Filters;
using Mfr.Models.Filters.Text;

namespace Mfr.Tests.Models.Filters.Text
{
    /// <summary>
    /// Tests for <see cref="TrimLeftFilter"/>.
    /// </summary>
    public class TrimLeftFilterTests
    {
        private static readonly FileNameTarget _Target = new(FileNameTargetMode.Prefix);

        /// <summary>
        /// Verifies trimming from the left by count.
        /// </summary>
        [Fact]
        public void Apply_RemovesLeftCharacters()
        {
            var f = new TrimLeftFilter(true, _Target, new CountFilterOptions(2));
            var file = FilterTestHelpers.CreateFile();
            Assert.Equal("cd", f.Apply("abcd", file));
        }

        /// <summary>
        /// Verifies non-positive count leaves the segment unchanged.
        /// </summary>
        [Fact]
        public void Apply_NonPositiveCount_ReturnsOriginal()
        {
            var f = new TrimLeftFilter(true, _Target, new CountFilterOptions(0));
            var file = FilterTestHelpers.CreateFile();
            Assert.Equal("ab", f.Apply("ab", file));
        }

        /// <summary>
        /// Verifies over-trim yields empty string.
        /// </summary>
        [Fact]
        public void Apply_CountExceedsLength_ReturnsEmpty()
        {
            var f = new TrimLeftFilter(true, _Target, new CountFilterOptions(10));
            var file = FilterTestHelpers.CreateFile();
            Assert.Equal("", f.Apply("hi", file));
        }
    }
}
