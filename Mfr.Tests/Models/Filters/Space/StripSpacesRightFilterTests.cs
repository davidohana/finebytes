using Mfr.Filters.Space;

namespace Mfr.Tests.Models.Filters.Space
{
    /// <summary>
    /// Tests for <see cref="StripSpacesRightFilter"/>.
    /// </summary>
    public class StripSpacesRightFilterTests
    {
        private static readonly FileNameTarget _target = new();

        /// <summary>
        /// Verifies default trimming (space character) from the right.
        /// </summary>
        [Fact]
        public void Apply_RemovesTrailingSpaces()
        {
            var f = new StripSpacesRightFilter(_target);
            Assert.Equal("New_York__", FilterTestHelpers.ApplyToFileName(f, "New_York__   "));
        }

        /// <summary>
        /// Verifies trimming when custom space character (underscore) is set.
        /// </summary>
        [Fact]
        public void Apply_RemovesTrailingCustomSpaceCharacters()
        {
            var spaceFilter = new SpaceCharacterFilter(
                _target,
                new SpaceCharacterOptions(SpaceCharacter: '_', Replacements: [])
            );

            var trimFilter = new StripSpacesRightFilter(_target);

            var item = FilterTestHelpers.CreateRenameItem(fileName: "__New_York__");

            // In a real scenario, filters are applied in sequence.
            spaceFilter.Setup();
            trimFilter.Setup();
            spaceFilter.Apply(item);
            trimFilter.Apply(item);

            Assert.Equal("__New_York", item.Preview.FileName);
        }

        /// <summary>
        /// Verifies that only trailing characters are removed.
        /// </summary>
        [Fact]
        public void Apply_OnlyRemovesTrailingCharacters()
        {
            var f = new StripSpacesRightFilter(_target);
            Assert.Equal("  a b", FilterTestHelpers.ApplyToFileName(f, "  a b "));
        }

        /// <summary>
        /// Verifies that it returns empty string if all characters are trimmed.
        /// </summary>
        [Fact]
        public void Apply_AllSpaces_ReturnsEmpty()
        {
            var f = new StripSpacesRightFilter(_target);
            Assert.Equal("", FilterTestHelpers.ApplyToFileName(f, "    "));
        }
    }
}
