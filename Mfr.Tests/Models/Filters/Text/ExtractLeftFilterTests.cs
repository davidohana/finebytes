using Mfr.Filters;
using Mfr.Filters.Text;
using Mfr.Models;

namespace Mfr.Tests.Models.Filters.Text
{
    /// <summary>
    /// Tests for <see cref="ExtractLeftFilter"/>.
    /// </summary>
    public class ExtractLeftFilterTests
    {
        private static readonly FileNameTarget _Target = new(FileNamePart.Prefix);

        /// <summary>
        /// Verifies left extraction by count.
        /// </summary>
        [Fact]
        public void Apply_TakesLeftSubstring()
        {
            var f = new ExtractLeftFilter(true, _Target, new CountFilterOptions(3));
            Assert.Equal("abc", FilterTestHelpers.ApplyToPrefix(f, "abcdef"));
        }

        /// <summary>
        /// Verifies zero count returns empty.
        /// </summary>
        [Fact]
        public void Apply_ZeroCount_ReturnsEmpty()
        {
            var f = new ExtractLeftFilter(true, _Target, new CountFilterOptions(0));
            Assert.Equal("", FilterTestHelpers.ApplyToPrefix(f, "abc"));
        }

        /// <summary>
        /// Verifies short segment returns full segment.
        /// </summary>
        [Fact]
        public void Apply_CountBeyondLength_ReturnsFullSegment()
        {
            var f = new ExtractLeftFilter(true, _Target, new CountFilterOptions(100));
            Assert.Equal("ab", FilterTestHelpers.ApplyToPrefix(f, "ab"));
        }
    }
}
