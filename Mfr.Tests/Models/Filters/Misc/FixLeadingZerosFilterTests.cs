using Mfr.Filters.Misc;

namespace Mfr.Tests.Models.Filters.Misc
{
    /// <summary>
    /// Tests for <see cref="FixLeadingZerosFilter"/>.
    /// </summary>
    public class FixLeadingZerosFilterTests
    {
        private static readonly FilePrefixTarget _target = new();

        /// <summary>
        /// Verifies non-positive width leaves segment unchanged.
        /// </summary>
        [Fact]
        public void Apply_NonPositiveWidth_ReturnsOriginal()
        {
            var f = new FixLeadingZerosFilter(_target, new FixLeadingZerosOptions(Width: 0, RemoveExtraZeros: true));
            Assert.Equal("track12", FilterTestHelpers.ApplyToPrefix(f, "track12"));
        }

        /// <summary>
        /// Verifies digit groups are padded to width.
        /// </summary>
        [Fact]
        public void Apply_PadsNumericRuns()
        {
            var f = new FixLeadingZerosFilter(
                _target,
                new FixLeadingZerosOptions(Width: 4, RemoveExtraZeros: false, WholeWordOnly: false)
            );
            Assert.Equal("track0009", FilterTestHelpers.ApplyToPrefix(f, "track9"));
        }

        /// <summary>
        /// Verifies extra leading zeros are trimmed before padding when requested.
        /// </summary>
        [Fact]
        public void Apply_RemoveExtraZeros_NormalizesThenPads()
        {
            var f = new FixLeadingZerosFilter(
                _target,
                new FixLeadingZerosOptions(Width: 3, RemoveExtraZeros: true, WholeWordOnly: false)
            );
            Assert.Equal("x007", FilterTestHelpers.ApplyToPrefix(f, "x0007"));
        }

        /// <summary>
        /// Verifies whole word only requirement.
        /// </summary>
        [Fact]
        public void Apply_WholeWordOnly_DoesNotChangePartWordNumbers()
        {
            var options = new FixLeadingZerosOptions(Width: 3, RemoveExtraZeros: false, WholeWordOnly: true);
            var f = new FixLeadingZerosFilter(_target, options);
            Assert.Equal("doc1_012", FilterTestHelpers.ApplyToPrefix(f, "doc1_12"));
            Assert.Equal("12x", FilterTestHelpers.ApplyToPrefix(f, "12x"));
        }

        /// <summary>
        /// Verifies MaxCount is not consumed by whole-word skips (MFR7 first-only).
        /// </summary>
        [Fact]
        public void Apply_MaxCount_SkipsPartWordThenFixesNext()
        {
            var f = new FixLeadingZerosFilter(
                _target,
                new FixLeadingZerosOptions(Width: 3, RemoveExtraZeros: false, MaxCount: 1, WholeWordOnly: true)
            );
            Assert.Equal("doc1_002", FilterTestHelpers.ApplyToPrefix(f, "doc1_2"));
        }

        /// <summary>
        /// Verifies palette defaults match MFR7 add-to-list (width 2, first-only, whole-word).
        /// </summary>
        [Fact]
        public void DefaultConstructor_UsesMfr7AddDefaults()
        {
            var f = new FixLeadingZerosFilter();
            Assert.IsType<FilePrefixTarget>(f.Target);
            Assert.Equal(2, f.Options.Width);
            Assert.False(f.Options.RemoveExtraZeros);
            Assert.Equal(1, f.Options.MaxCount);
            Assert.True(f.Options.WholeWordOnly);
        }

        /// <summary>
        /// Verifies maximum count of numbers to fix.
        /// </summary>
        [Fact]
        public void Apply_MaxCount_Works()
        {
            var options = new FixLeadingZerosOptions(Width: 3, RemoveExtraZeros: false, MaxCount: 1);
            var f = new FixLeadingZerosFilter(_target, options);
            Assert.Equal("005-Opus 40", FilterTestHelpers.ApplyToPrefix(f, "05-Opus 40"));

            options = new FixLeadingZerosOptions(Width: 3, RemoveExtraZeros: false, MaxCount: 2);
            f = new FixLeadingZerosFilter(_target, options);
            Assert.Equal("005-Opus 040 (1)", FilterTestHelpers.ApplyToPrefix(f, "05-Opus 40 (1)"));
        }

        /// <summary>
        /// Verifies MaxCount skips unchanged (already-wide) groups, matching MFR7 first-only.
        /// </summary>
        [Fact]
        public void Apply_MaxCount_SkipsUnchangedGroups()
        {
            var f = new FixLeadingZerosFilter(
                _target,
                new FixLeadingZerosOptions(Width: 3, RemoveExtraZeros: false, MaxCount: 1, WholeWordOnly: false)
            );
            Assert.Equal("123-045", FilterTestHelpers.ApplyToPrefix(f, "123-45"));
        }

        /// <summary>
        /// Verifies MaxCount still applies after an already-normalized over-padded group when shrinking.
        /// </summary>
        [Fact]
        public void Apply_MaxCount_WithRemoveExtraZeros_SkipsNoOpThenShrinks()
        {
            var f = new FixLeadingZerosFilter(
                _target,
                new FixLeadingZerosOptions(Width: 3, RemoveExtraZeros: true, MaxCount: 1, WholeWordOnly: false)
            );
            Assert.Equal("123-007-5", FilterTestHelpers.ApplyToPrefix(f, "123-0007-5"));
        }

        /// <summary>
        /// Verifies all-zero digit groups normalize to a single zero before padding.
        /// </summary>
        [Fact]
        public void Apply_RemoveExtraZeros_AllZeros_PadsSingleZero()
        {
            var f = new FixLeadingZerosFilter(
                _target,
                new FixLeadingZerosOptions(Width: 3, RemoveExtraZeros: true, WholeWordOnly: false)
            );
            Assert.Equal("x000", FilterTestHelpers.ApplyToPrefix(f, "x000"));
        }
    }
}
