using Mfr.Models;
using Mfr.Models.Filters;
using Mfr.Models.Filters.Text;

namespace Mfr.Tests.Models.Filters.Text
{
    /// <summary>
    /// Tests for <see cref="ExtractRightFilter"/>.
    /// </summary>
    public class ExtractRightFilterTests
    {
        private static readonly FileNameTarget _Target = new(FileNameTargetMode.Prefix);

        /// <summary>
        /// Verifies right extraction by count.
        /// </summary>
        [Fact]
        public void Apply_TakesRightSubstring()
        {
            var f = new ExtractRightFilter(true, _Target, new CountFilterOptions(3));
            var file = FilterTestHelpers.CreateFile();
            Assert.Equal("def", f.Apply("abcdef", file));
        }

        /// <summary>
        /// Verifies zero count returns empty.
        /// </summary>
        [Fact]
        public void Apply_ZeroCount_ReturnsEmpty()
        {
            var f = new ExtractRightFilter(true, _Target, new CountFilterOptions(0));
            var file = FilterTestHelpers.CreateFile();
            Assert.Equal("", f.Apply("abc", file));
        }

        /// <summary>
        /// Verifies short segment returns full segment.
        /// </summary>
        [Fact]
        public void Apply_CountBeyondLength_ReturnsFullSegment()
        {
            var f = new ExtractRightFilter(true, _Target, new CountFilterOptions(100));
            var file = FilterTestHelpers.CreateFile();
            Assert.Equal("ab", f.Apply("ab", file));
        }
    }
}
