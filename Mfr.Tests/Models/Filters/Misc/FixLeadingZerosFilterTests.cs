using Mfr.Filters.Misc;
using Mfr.Models;

namespace Mfr.Tests.Models.Filters.Misc
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

        /// <summary>
        /// Verifies whole word only requirement.
        /// </summary>
        [Fact]
        public void Apply_WholeWordOnly_DoesNotChangePartWordNumbers()
        {
            var options = new FixLeadingZerosOptions(Width: 3, RemoveExtraZeros: false, WholeWordOnly: true);
            var f = new FixLeadingZerosFilter(true, _target, options);
            Assert.Equal("doc1_012", FilterTestHelpers.ApplyToPrefix(f, "doc1_12"));
        }

        /// <summary>
        /// Verifies maximum count of numbers to fix.
        /// </summary>
        [Fact]
        public void Apply_MaxCount_Works()
        {
            var options = new FixLeadingZerosOptions(Width: 3, RemoveExtraZeros: false, MaxCount: 1);
            var f = new FixLeadingZerosFilter(true, _target, options);
            Assert.Equal("005-Opus 40", FilterTestHelpers.ApplyToPrefix(f, "05-Opus 40"));

            options = new FixLeadingZerosOptions(Width: 3, RemoveExtraZeros: false, MaxCount: 2);
            f = new FixLeadingZerosFilter(true, _target, options);
            Assert.Equal("005-Opus 040 (1)", FilterTestHelpers.ApplyToPrefix(f, "05-Opus 40 (1)"));
        }
    }
}
