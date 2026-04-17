using Mfr.Filters.Space;
using Mfr.Models;

namespace Mfr.Tests.Models.Filters.Space
{
    /// <summary>
    /// Tests for <see cref="ShrinkSpacesFilter"/>.
    /// </summary>
    public class ShrinkSpacesFilterTests
    {
        private static readonly FileNameTarget _target = new(FileNamePart.Prefix);

        /// <summary>
        /// Verifies runs of the default word separator (U+0020 SPACE) collapse to a single space.
        /// </summary>
        [Fact]
        public void Apply_CollapsesSeparatorRuns()
        {
            var f = new ShrinkSpacesFilter(_target);
            Assert.Equal("a b c", FilterTestHelpers.ApplyToPrefix(f, "a   b  c"));
            Assert.Equal("a \t b", FilterTestHelpers.ApplyToPrefix(f, "a  \t b"));
        }
    }
}
