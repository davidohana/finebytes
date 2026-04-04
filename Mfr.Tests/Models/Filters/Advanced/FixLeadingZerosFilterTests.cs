using Mfr.Models;
using Mfr.Models.Filters.Advanced;

namespace Mfr.Tests.Models.Filters.Advanced
{
    /// <summary>
    /// Tests for <see cref="FixLeadingZerosFilter"/>.
    /// </summary>
    public class FixLeadingZerosFilterTests
    {
        private static readonly FileNameTarget _Target = new(FileNameTargetMode.Prefix);

        /// <summary>
        /// Verifies non-positive width leaves segment unchanged.
        /// </summary>
        [Fact]
        public void Apply_NonPositiveWidth_ReturnsOriginal()
        {
            var f = new FixLeadingZerosFilter(true, _Target, new FixLeadingZerosOptions(Width: 0, RemoveExtraZeros: true));
            var file = FilterTestHelpers.CreateFile();
            Assert.Equal("track12", f.Apply("track12", file));
        }

        /// <summary>
        /// Verifies digit groups are padded to width.
        /// </summary>
        [Fact]
        public void Apply_PadsNumericRuns()
        {
            var f = new FixLeadingZerosFilter(true, _Target, new FixLeadingZerosOptions(Width: 4, RemoveExtraZeros: false));
            var file = FilterTestHelpers.CreateFile();
            Assert.Equal("track0009", f.Apply("track9", file));
        }

        /// <summary>
        /// Verifies extra leading zeros are trimmed before padding when requested.
        /// </summary>
        [Fact]
        public void Apply_RemoveExtraZeros_NormalizesThenPads()
        {
            var f = new FixLeadingZerosFilter(true, _Target, new FixLeadingZerosOptions(Width: 3, RemoveExtraZeros: true));
            var file = FilterTestHelpers.CreateFile();
            Assert.Equal("x007", f.Apply("x0007", file));
        }
    }
}
