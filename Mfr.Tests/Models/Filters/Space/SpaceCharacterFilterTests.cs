using Mfr.Core;
using Mfr.Filters;
using Mfr.Filters.Case;
using Mfr.Filters.Space;
using Mfr.Models;

namespace Mfr.Tests.Models.Filters.Space
{
    /// <summary>
    /// Tests for <see cref="SpaceCharacterFilter"/>.
    /// </summary>
    public class SpaceCharacterFilterTests
    {
        private static readonly FileNameTarget _target = new(FileNamePart.Prefix);

        /// <summary>
        /// Verifies %20 is replaced with the defined underscore separator (MFR7-style example).
        /// </summary>
        [Fact]
        public void Apply_Percent20WithUnderscoreSeparator_ReplacesEncodedSpaces()
        {
            var f = new SpaceCharacterFilter(
                true,
                _target,
                new SpaceCharacterOptions(
                    SpaceCharacter: '_',
                    ReplaceSpaces: false,
                    ReplaceUnderscores: false,
                    ReplacePercent20: true,
                    CustomText: ""));
            Assert.Equal("Gone_With_The_Wind", FilterTestHelpers.ApplyToPrefix(f, "Gone%20With%20The%20Wind"));
        }

        /// <summary>
        /// Verifies multiple replacement flags combine toward the defined character.
        /// </summary>
        [Fact]
        public void Apply_MultipleReplaceFlags_NormalizesToSeparator()
        {
            var f = new SpaceCharacterFilter(
                true,
                _target,
                new SpaceCharacterOptions(
                    SpaceCharacter: ' ',
                    ReplaceSpaces: true,
                    ReplaceUnderscores: true,
                    ReplacePercent20: true,
                    CustomText: ""));
            Assert.Equal("a b c d", FilterTestHelpers.ApplyToPrefix(f, "a_b c%20d"));
        }

        /// <summary>
        /// Verifies custom text is replaced when enabled.
        /// </summary>
        [Fact]
        public void Apply_CustomReplacement_ReplacesCustomText()
        {
            var f = new SpaceCharacterFilter(
                true,
                _target,
                new SpaceCharacterOptions(
                    SpaceCharacter: '-',
                    ReplaceSpaces: false,
                    ReplaceUnderscores: false,
                    ReplacePercent20: false,
                    CustomText: "++"));
            Assert.Equal("a-b", FilterTestHelpers.ApplyToPrefix(f, "a++b"));
        }

        /// <summary>
        /// Verifies SpaceCharacter then LettersCase TitleCase uses underscore as word boundary.
        /// </summary>
        [Fact]
        public void ApplyFilters_AfterSpaceCharacter_TitleCaseRespectsWordSeparator()
        {
            var spaceFilter = new SpaceCharacterFilter(
                true,
                _target,
                new SpaceCharacterOptions(
                    SpaceCharacter: '_',
                    ReplaceSpaces: false,
                    ReplaceUnderscores: false,
                    ReplacePercent20: true,
                    CustomText: ""));
            var titleFilter = new LettersCaseFilter(
                true,
                _target,
                new LettersCaseOptions(LettersCaseMode.TitleCase, ["the"]));

            var file = FilterTestHelpers.CreateFile(prefix: "gone%20with%20the%20wind");
            new BaseFilter[] { spaceFilter, titleFilter }.SetupFilters();
            file.ApplyFilters([spaceFilter, titleFilter]);

            Assert.Equal("Gone_With_the_Wind", file.Preview.Prefix);
        }
    }
}
