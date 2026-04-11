using Mfr.Filters.Space;
using Mfr.Models;

namespace Mfr.Tests.Models.Filters.Space
{
    /// <summary>
    /// Tests for <see cref="RemoveSpacesFilter"/>.
    /// </summary>
    public class RemoveSpacesFilterTests
    {
        private static readonly FileNameTarget _target = new(FileNamePart.Prefix);

        /// <summary>
        /// Verifies all whitespace is removed.
        /// </summary>
        [Fact]
        public void Apply_StripsAllWhitespace()
        {
            var f = new RemoveSpacesFilter(true, _target);
            Assert.Equal("ab", FilterTestHelpers.ApplyToPrefix(f, "a \t\r\nb"));
        }
    }
}
