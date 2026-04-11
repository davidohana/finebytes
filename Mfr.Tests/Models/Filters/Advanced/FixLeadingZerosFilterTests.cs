using Mfr.Filters.Advanced;
using Mfr.Models;
using FixLeadingZerosFilter = Mfr.Filters.Advanced.FixLeadingZerosFilter;

namespace Mfr.Tests.Models.Filters.Advanced
{
    /// <summary>
    /// Tests for <see cref="FixLeadingZerosFilter"/>.
    /// </summary>
    public class FixLeadingZerosFilterTests
    {
        private static readonly FileNameTarget _target = new(FileNamePart.Prefix);

        /// <summary>
        /// Verifies non-positive width leaves segment unchanged.
        /// </summary>
        [Fact]
        public void Apply_NonPositiveWidth_ReturnsOriginal()
        {
            var f = new FixLeadingZerosFilter(true, _target, new FixLeadingZerosOptions(Width: 0, RemoveExtraZeros: true));
            Assert.Equal("track12", FilterTestHelpers.ApplyToPrefix(f, "track12"));
        }

        /// <summary>
        /// Verifies digit groups are padded to width.
        /// </summary>
        [Fact]
        public void Apply_PadsNumericRuns()
        {
            var f = new FixLeadingZerosFilter(true, _target, new FixLeadingZerosOptions(Width: 4, RemoveExtraZeros: false));
            Assert.Equal("track0009", FilterTestHelpers.ApplyToPrefix(f, "track9"));
        }

        /// <summary>
        /// Verifies extra leading zeros are trimmed before padding when requested.
        /// </summary>
        [Fact]
        public void Apply_RemoveExtraZeros_NormalizesThenPads()
        {
            var f = new FixLeadingZerosFilter(true, _target, new FixLeadingZerosOptions(Width: 3, RemoveExtraZeros: true));
            Assert.Equal("x007", FilterTestHelpers.ApplyToPrefix(f, "x0007"));
        }
    }
}
