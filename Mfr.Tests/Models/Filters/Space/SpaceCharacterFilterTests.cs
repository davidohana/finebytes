using Mfr.Filters.Case;
using Mfr.Filters.Space;

namespace Mfr.Tests.Models.Filters.Space
{
    /// <summary>
    /// Tests for <see cref="SpaceCharacterFilter"/>.
    /// </summary>
    public class SpaceCharacterFilterTests
    {
        private static readonly FilePrefixTarget _target = new();

        /// <summary>
        /// Verifies %20 is replaced with the defined underscore separator (MFR7-style example).
        /// </summary>
        [Fact]
        public void Apply_Percent20WithUnderscoreSeparator_ReplacesEncodedSpaces()
        {
            var f = new SpaceCharacterFilter(
                _target,
                new SpaceCharacterOptions(SpaceCharacter: '_', Replacements: ["%20"])
            );
            Assert.Equal("Gone_With_The_Wind", FilterTestHelpers.ApplyToPrefix(f, "Gone%20With%20The%20Wind"));
        }

        /// <summary>
        /// Verifies multiple replacement flags combine toward the defined character.
        /// </summary>
        [Fact]
        public void Apply_MultipleReplaceFlags_NormalizesToSeparator()
        {
            var f = new SpaceCharacterFilter(
                _target,
                new SpaceCharacterOptions(' ', SpaceCharacterOptions.DefaultReplacements)
            );
            Assert.Equal("a b c d", FilterTestHelpers.ApplyToPrefix(f, "a_b c%20d"));
        }

        /// <summary>
        /// Verifies custom text is replaced when enabled.
        /// </summary>
        [Fact]
        public void Apply_CustomReplacement_ReplacesCustomText()
        {
            var f = new SpaceCharacterFilter(
                _target,
                new SpaceCharacterOptions(SpaceCharacter: '-', Replacements: ["++"])
            );
            Assert.Equal("a-b", FilterTestHelpers.ApplyToPrefix(f, "a++b"));
        }

        /// <summary>
        /// Verifies SpaceCharacter then LettersCase Capitalize uses underscore as word boundary.
        /// </summary>
        [Fact]
        public void ApplyFilters_AfterSpaceCharacter_CapitalizeRespectsWordSeparator()
        {
            var spaceFilter = new SpaceCharacterFilter(
                _target,
                new SpaceCharacterOptions(SpaceCharacter: '_', Replacements: ["%20"])
            );
            var capitalizeFilter = new LettersCaseFilter(
                _target,
                new LettersCaseOptions(LettersCaseMode.Capitalize, ["the"])
            );

            var item = FilterTestHelpers.CreateRenameItem(prefix: "gone%20with%20the%20wind");
            var chain = FilterChain.CreateAllEnabled([spaceFilter, capitalizeFilter]);
            chain.SetupFilters();
            chain.ApplyFilters(item);

            Assert.Equal("Gone_With_the_Wind", item.Preview.Prefix);
        }
    }
}
