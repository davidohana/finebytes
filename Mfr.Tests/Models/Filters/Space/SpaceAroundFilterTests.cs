using Mfr.Filters.Space;
using Mfr.Models;

namespace Mfr.Tests.Models.Filters.Space
{
    /// <summary>
    /// Tests for <see cref="SpaceAroundFilter"/>.
    /// </summary>
    public sealed class SpaceAroundFilterTests
    {
        private static readonly FilePrefixTarget _target = new();

        private static SpaceAroundFilter _CreateFilter(
            string aroundChars,
            bool onlyWhenNeighboringAreLettersOrDigits = false)
        {
            return new SpaceAroundFilter(
                _target,
                new SpaceAroundOptions(
                    AroundChars: aroundChars,
                    OnlyWhenNeighboringAreLettersOrDigits: onlyWhenNeighboringAreLettersOrDigits));
        }

        /// <summary>
        /// Verifies documentation examples for hyphen with neighbor letter/digit rule.
        /// </summary>
        [Fact]
        public void Apply_OnlyWhenNeighboringAreLettersOrDigits_HyphenExamples()
        {
            var f = _CreateFilter("-", onlyWhenNeighboringAreLettersOrDigits: true);
            Assert.Equal(
                "Aimee Mann - Stupid Thing.mp3",
                FilterTestHelpers.ApplyToPrefix(f, "Aimee Mann-Stupid Thing.mp3"));
            Assert.Equal(
                "Aimee Mann - Stupid Thing.mp3",
                FilterTestHelpers.ApplyToPrefix(f, "Aimee Mann- Stupid Thing.mp3"));
            Assert.Equal(
                "Aimee Mann - Stupid Thing.mp3",
                FilterTestHelpers.ApplyToPrefix(f, "Aimee Mann - Stupid Thing.mp3"));
            Assert.Equal(
                "Aimee Mann -- Stupid Thing.mp3",
                FilterTestHelpers.ApplyToPrefix(f, "Aimee Mann--Stupid Thing.mp3"));
        }

        /// <summary>
        /// Verifies empty trigger list leaves the value unchanged.
        /// </summary>
        [Fact]
        public void Apply_EmptyAroundChars_IsNoOp()
        {
            var f = _CreateFilter("");
            Assert.Equal("a-b", FilterTestHelpers.ApplyToPrefix(f, "a-b"));
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
            var aroundFilter = _CreateFilter("-", onlyWhenNeighboringAreLettersOrDigits: true);
            var item = FilterTestHelpers.CreateRenameItem(prefix: "a-b");
            spaceFilter.Setup();
            aroundFilter.Setup();
            spaceFilter.Apply(item);
            aroundFilter.Apply(item);
            Assert.Equal("a_-_b", item.Preview.Prefix);
        }
    }
}
