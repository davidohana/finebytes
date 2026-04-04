using Mfr.Models;
using Mfr.Models.Filters.Text;

namespace Mfr.Tests.Models.Filters.Text
{
    /// <summary>
    /// Tests for <see cref="ShrinkSpacesFilter"/>.
    /// </summary>
    public class ShrinkSpacesFilterTests
    {
        private static readonly FileNameTarget _Target = new(FileNameTargetMode.Prefix);

        /// <summary>
        /// Verifies runs of whitespace collapse to a single space.
        /// </summary>
        [Fact]
        public void Apply_CollapsesWhitespaceRunsToSingleSpace()
        {
            var f = new ShrinkSpacesFilter(true, _Target);
            var file = FilterTestHelpers.CreateFile();
            Assert.Equal("a b c", f.Apply("a  \t b \n c", file));
        }
    }
}
