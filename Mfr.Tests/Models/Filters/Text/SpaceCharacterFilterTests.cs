using Mfr.Models;
using Mfr.Models.Filters.Text;

namespace Mfr.Tests.Models.Filters.Text
{
    /// <summary>
    /// Tests for <see cref="SpaceCharacterFilter"/>.
    /// </summary>
    public class SpaceCharacterFilterTests
    {
        private static readonly FileNameTarget _Target = new(FileNamePart.Prefix);

        /// <summary>
        /// Verifies spaces are replaced and the mapped character becomes a space.
        /// </summary>
        [Fact]
        public void Apply_ReplacesSpacesAndSwapsMappedChar()
        {
            var f = new SpaceCharacterFilter(
                true,
                _Target,
                new SpaceCharacterOptions(ReplaceSpaceWith: "_", ReplaceCharWithSpace: "-"));
            Assert.Equal("a_b c", FilterTestHelpers.ApplyToPrefix(f, "a b-c"));
        }
    }
}
