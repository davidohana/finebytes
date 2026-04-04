using Mfr.Models;
using Mfr.Models.Filters.Text;

namespace Mfr.Tests.Models.Filters.Text
{
    /// <summary>
    /// Tests for <see cref="RemoveSpacesFilter"/>.
    /// </summary>
    public class RemoveSpacesFilterTests
    {
        private static readonly FileNameTarget _Target = new(FileNamePart.Prefix);

        /// <summary>
        /// Verifies all whitespace is removed.
        /// </summary>
        [Fact]
        public void Apply_StripsAllWhitespace()
        {
            var f = new RemoveSpacesFilter(true, _Target);
            Assert.Equal("ab", FilterTestHelpers.ApplyToPrefix(f, "a \t\r\nb"));
        }
    }
}
