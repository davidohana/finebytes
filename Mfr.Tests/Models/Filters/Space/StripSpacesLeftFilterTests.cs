using Mfr.Filters.Space;

namespace Mfr.Tests.Models.Filters.Space
{
    /// <summary>
    /// Tests for <see cref="StripSpacesLeftFilter"/>.
    /// </summary>
    public class StripSpacesLeftFilterTests
    {
        private static readonly FileNameTarget _target = new();

        /// <summary>
        /// Verifies default trimming (space character) from the left.
        /// </summary>
        [Fact]
        public void Apply_RemovesLeadingSpaces()
        {
            var f = new StripSpacesLeftFilter(_target);
            Assert.Equal("New_York__.jpg", FilterTestHelpers.ApplyToFileName(f, "   New_York__.jpg"));
        }

        /// <summary>
        /// Verifies trimming when custom space character (underscore) is set.
        /// </summary>
        [Fact]
        public void Apply_RemovesLeadingCustomSpaceCharacters()
        {
            var spaceFilter = new SpaceCharacterFilter(
                _target,
                new SpaceCharacterOptions(SpaceCharacter: '_', Replacements: [])
            );

            var trimFilter = new StripSpacesLeftFilter(_target);

            var item = FilterTestHelpers.CreateRenameItem(fileName: "__New_York__.jpg");

            // In a real scenario, filters are applied in sequence.
            spaceFilter.Setup();
            trimFilter.Setup();
            spaceFilter.Apply(item);
            trimFilter.Apply(item);

            Assert.Equal("New_York__.jpg", item.Preview.FileName);
        }

        /// <summary>
        /// Verifies that only leading characters are removed.
        /// </summary>
        [Fact]
        public void Apply_OnlyRemovesLeadingCharacters()
        {
            var f = new StripSpacesLeftFilter(_target);
            Assert.Equal("a b ", FilterTestHelpers.ApplyToFileName(f, "  a b "));
        }

        /// <summary>
        /// Verifies that it returns empty string if all characters are trimmed.
        /// </summary>
        [Fact]
        public void Apply_AllSpaces_ReturnsEmpty()
        {
            var f = new StripSpacesLeftFilter(_target);
            Assert.Equal("", FilterTestHelpers.ApplyToFileName(f, "    "));
        }
    }
}
