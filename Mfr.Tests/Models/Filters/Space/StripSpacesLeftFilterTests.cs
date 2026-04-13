using Mfr.Filters.Space;
using Mfr.Filters.Trimming;
using Mfr.Models;

namespace Mfr.Tests.Models.Filters.Space
{
    /// <summary>
    /// Tests for <see cref="StripSpacesLeftFilter"/>.
    /// </summary>
    public class StripSpacesLeftFilterTests
    {
        private static readonly FileNameTarget _target = new(FileNamePart.Prefix);

        /// <summary>
        /// Verifies default trimming (space character) from the left.
        /// </summary>
        [Fact]
        public void Apply_RemovesLeadingSpaces()
        {
            var f = new StripSpacesLeftFilter(true, _target);
            Assert.Equal("New_York__.jpg", FilterTestHelpers.ApplyToPrefix(f, "   New_York__.jpg"));
        }

        /// <summary>
        /// Verifies trimming when custom space character (underscore) is set.
        /// </summary>
        [Fact]
        public void Apply_RemovesLeadingCustomSpaceCharacters()
        {
            var spaceFilter = new SpaceCharacterFilter(
                true,
                _target,
                new SpaceCharacterOptions(
                    SpaceCharacter: '_',
                    ReplaceSpaces: false,
                    ReplaceUnderscores: false,
                    ReplacePercent20: false,
                    CustomText: ""));

            var trimFilter = new StripSpacesLeftFilter(true, _target);

            var file = FilterTestHelpers.CreateFile(prefix: "__New_York__.jpg");

            // In a real scenario, filters are applied in sequence.
            spaceFilter.Apply(file);
            trimFilter.Apply(file);

            Assert.Equal("New_York__.jpg", file.Preview.Prefix);
        }

        /// <summary>
        /// Verifies that only leading characters are removed.
        /// </summary>
        [Fact]
        public void Apply_OnlyRemovesLeadingCharacters()
        {
            var f = new StripSpacesLeftFilter(true, _target);
            Assert.Equal("a b ", FilterTestHelpers.ApplyToPrefix(f, "  a b "));
        }

        /// <summary>
        /// Verifies that it returns empty string if all characters are trimmed.
        /// </summary>
        [Fact]
        public void Apply_AllSpaces_ReturnsEmpty()
        {
            var f = new StripSpacesLeftFilter(true, _target);
            Assert.Equal("", FilterTestHelpers.ApplyToPrefix(f, "    "));
        }
    }
}
