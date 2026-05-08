using Mfr.Filters.Space;
using Mfr.Models;

namespace Mfr.Tests.Models.Filters.Space
{
    /// <summary>
    /// Tests for <see cref="SpaceAfterFilter"/>.
    /// </summary>
    public sealed class SpaceAfterFilterTests
    {
        private static readonly FilePrefixTarget _target = new();

        private static SpaceAfterFilter _CreateFilter(
            string afterChars,
            bool onlyWhenNextIsLetterOrDigit = false)
        {
            return new SpaceAfterFilter(
                _target,
                new SpaceAfterOptions(
                    AfterChars: afterChars,
                    OnlyWhenNextIsLetterOrDigit: onlyWhenNextIsLetterOrDigit));
        }

        /// <summary>
        /// Verifies examples from the product spec when only letter/digit qualifies.
        /// </summary>
        [Fact]
        public void Apply_OnlyWhenNextIsLetterOrDigit_MatchesSpecExamples()
        {
            var f = _CreateFilter(",;!", onlyWhenNextIsLetterOrDigit: true);
            Assert.Equal("one, two, three", FilterTestHelpers.ApplyToPrefix(f, "one,two,three"));
            Assert.Equal("one, two, three", FilterTestHelpers.ApplyToPrefix(f, "one, two,three"));
            Assert.Equal("Blaaa! blaaa!!", FilterTestHelpers.ApplyToPrefix(f, "Blaaa!blaaa!!"));
        }

        /// <summary>
        /// Verifies an existing separator is not duplicated.
        /// </summary>
        [Fact]
        public void Apply_SkipsWhenSeparatorAlreadyPresent()
        {
            var f = _CreateFilter(",", onlyWhenNextIsLetterOrDigit: true);
            Assert.Equal("one, two", FilterTestHelpers.ApplyToPrefix(f, "one, two"));
        }

        /// <summary>
        /// Verifies empty trigger list leaves the value unchanged.
        /// </summary>
        [Fact]
        public void Apply_EmptyAfterChars_IsNoOp()
        {
            var f = _CreateFilter("");
            Assert.Equal("a,b", FilterTestHelpers.ApplyToPrefix(f, "a,b"));
        }

        /// <summary>
        /// Verifies all non-ending triggers get a separator when the conditional is off.
        /// </summary>
        [Fact]
        public void Apply_WhenNotConditional_InsertsBeforeAnyFollowingCharacter()
        {
            var f = _CreateFilter("!", onlyWhenNextIsLetterOrDigit: false);
            Assert.Equal("hi! there", FilterTestHelpers.ApplyToPrefix(f, "hi!there"));
            Assert.Equal("x! y", FilterTestHelpers.ApplyToPrefix(f, "x!y"));
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
            var afterFilter = _CreateFilter(",", onlyWhenNextIsLetterOrDigit: true);
            var item = FilterTestHelpers.CreateRenameItem(prefix: "x,y");
            spaceFilter.Setup();
            afterFilter.Setup();
            spaceFilter.Apply(item);
            afterFilter.Apply(item);
            Assert.Equal("x,_y", item.Preview.Prefix);
        }
    }
}
