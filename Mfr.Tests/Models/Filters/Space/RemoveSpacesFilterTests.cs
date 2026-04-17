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
        /// Verifies all occurrences of the default word separator (U+0020 SPACE) are removed.
        /// </summary>
        [Fact]
        public void Apply_StripsSeparatorChar()
        {
            var f = new RemoveSpacesFilter(_target);
            Assert.Equal("ab", FilterTestHelpers.ApplyToPrefix(f, "a b"));
            Assert.Equal("a\t\r\nb", FilterTestHelpers.ApplyToPrefix(f, "a \t\r\nb"));
        }
    }
}
