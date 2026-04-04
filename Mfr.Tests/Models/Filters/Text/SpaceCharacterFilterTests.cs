using Mfr.Models;
using Mfr.Models.Filters.Text;

namespace Mfr.Tests.Models.Filters.Text
{
    /// <summary>
    /// Tests for <see cref="SpaceCharacterFilter"/>.
    /// </summary>
    public class SpaceCharacterFilterTests
    {
        private static readonly FileNameTarget _Target = new(FileNameTargetMode.Prefix);

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
            var file = FilterTestHelpers.CreateFile();
            Assert.Equal("a_b c", f.Apply("a b-c", file));
        }
    }
}
