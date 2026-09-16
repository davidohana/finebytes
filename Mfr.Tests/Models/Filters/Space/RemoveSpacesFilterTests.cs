using Mfr.Filters.Space;

namespace Mfr.Tests.Models.Filters.Space
{
    /// <summary>
    /// Tests for <see cref="RemoveSpacesFilter"/>.
    /// </summary>
    public class RemoveSpacesFilterTests
    {
        private static readonly FileNameTarget _target = new();

        /// <summary>
        /// Verifies all occurrences of the default word separator (U+0020 SPACE) are removed.
        /// </summary>
        [Fact]
        public void Apply_StripsSeparatorChar()
        {
            var f = new RemoveSpacesFilter(_target);
            Assert.Equal("ab", FilterTestHelpers.ApplyToFileName(f, "a b"));
            Assert.Equal("a\t\r\nb", FilterTestHelpers.ApplyToFileName(f, "a \t\r\nb"));
        }
    }
}
