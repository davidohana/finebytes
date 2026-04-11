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
        /// Verifies runs of whitespace collapse to a single space.
        /// </summary>
        [Fact]
        public void Apply_CollapsesWhitespaceRunsToSingleSpace()
        {
            var f = new ShrinkSpacesFilter(true, _target);
            Assert.Equal("a b c", FilterTestHelpers.ApplyToPrefix(f, "a  \t b \n c"));
        }
    }
}
