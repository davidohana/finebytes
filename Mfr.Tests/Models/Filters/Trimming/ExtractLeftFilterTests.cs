using Mfr.Filters;
using Mfr.Filters.Trimming;
using Mfr.Models;

namespace Mfr.Tests.Models.Filters.Trimming
{
    /// <summary>
    /// Tests for <see cref="ExtractLeftFilter"/>.
    /// </summary>
    public class ExtractLeftFilterTests
    {
        private static readonly FilePrefixTarget _target = new();

        /// <summary>
        /// Verifies left extraction by count.
        /// </summary>
        [Fact]
        public void Apply_TakesLeftSubstring()
        {
            var f = new ExtractLeftFilter(_target, new CountFilterOptions(3));
            Assert.Equal("abc", FilterTestHelpers.ApplyToPrefix(f, "abcdef"));
        }

        /// <summary>
        /// Verifies zero count returns empty.
        /// </summary>
        [Fact]
        public void Apply_ZeroCount_ReturnsEmpty()
        {
            var f = new ExtractLeftFilter(_target, new CountFilterOptions(0));
            Assert.Equal("", FilterTestHelpers.ApplyToPrefix(f, "abc"));
        }

        /// <summary>
        /// Verifies short segment returns full segment.
        /// </summary>
        [Fact]
        public void Apply_CountBeyondLength_ReturnsFullSegment()
        {
            var f = new ExtractLeftFilter(_target, new CountFilterOptions(100));
            Assert.Equal("ab", FilterTestHelpers.ApplyToPrefix(f, "ab"));
        }
    }
}
