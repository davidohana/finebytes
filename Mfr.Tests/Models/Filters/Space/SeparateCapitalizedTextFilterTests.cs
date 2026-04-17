using Mfr.Filters.Space;
using Mfr.Models;

namespace Mfr.Tests.Models.Filters.Space
{
    /// <summary>
    /// Tests for <see cref="SeparateCapitalizedTextFilter"/>.
    /// </summary>
    public class SeparateCapitalizedTextFilterTests
    {
        private static readonly FileNameTarget _target = new(FileNamePart.Prefix);

        /// <summary>
        /// Verifies camel-case, letter–digit, and digit–letter boundaries insert the default separator.
        /// </summary>
        [Fact]
        public void Apply_InsertsDefaultSpaceAtBoundaries()
        {
            var f = new SeparateCapitalizedTextFilter(_target);
            Assert.Equal("Dandy Worhols 01 Godless", FilterTestHelpers.ApplyToPrefix(f, "DandyWorhols01Godless"));
            Assert.Equal("song 2 remix", FilterTestHelpers.ApplyToPrefix(f, "song2remix"));
        }

        /// <summary>
        /// Verifies <see cref="SpaceCharacterFilter"/> sets the separator used for insertions.
        /// </summary>
        [Fact]
        public void Apply_AfterSpaceCharacter_UsesConfiguredWordSeparator()
        {
            var spaceFilter = new SpaceCharacterFilter(
                _target,
                new SpaceCharacterOptions(
                    SpaceCharacter: '_',
                    ReplaceSpaces: false,
                    ReplaceUnderscores: false,
                    ReplacePercent20: false,
                    CustomText: ""));
            var separateFilter = new SeparateCapitalizedTextFilter(_target);
            var item = FilterTestHelpers.CreateRenameItem(prefix: "aBc12x");
            spaceFilter.Setup();
            separateFilter.Setup();
            spaceFilter.Apply(item);
            separateFilter.Apply(item);
            Assert.Equal("a_Bc_12_x", item.Preview.Prefix);
        }
    }
}
