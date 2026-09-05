using Mfr.Filters.Formatting;

namespace Mfr.Tests.Models.Filters.Formatting
{
    /// <summary>
    /// Tests for <see cref="InserterFilter"/>.
    /// </summary>
    public class InserterFilterTests
    {
        private static readonly FilePrefixTarget _target = new();

        /// <summary>
        /// Verifies the documented example: insert before the third character without shifting past overwrite.
        /// </summary>
        [Fact]
        public void Apply_FromBeginning_Position3_InsertsBeforeThirdCharacter()
        {
            var f = new InserterFilter(
                _target,
                new InserterOptions(Text: "_-", Position: 3, StartFrom: InserterOrigin.Beginning, Overwrite: false)
            );
            Assert.Equal("01_-_Mercury_Rave_-_Holes", FilterTestHelpers.ApplyToPrefix(f, "01_Mercury_Rave_-_Holes"));
        }

        /// <summary>
        /// Verifies beginning-origin insert index clamping and append-past-end behavior.
        /// </summary>
        [Theory]
        [InlineData("X", 99, "ab", "abX")]
        [InlineData("X", 0, "ab", "Xab")]
        [InlineData("X", 1, "", "X")]
        [InlineData("", 1, "ab", "ab")]
        public void Apply_FromBeginning_InsertsAtComputedIndex(string text, int position, string input, string expected)
        {
            var f = new InserterFilter(
                _target,
                new InserterOptions(
                    Text: text,
                    Position: position,
                    StartFrom: InserterOrigin.Beginning,
                    Overwrite: false
                )
            );
            Assert.Equal(expected, FilterTestHelpers.ApplyToPrefix(f, input));
        }

        /// <summary>
        /// Verifies MFR7 end counting: position 1 appends; larger positions walk left; oversized prepends.
        /// </summary>
        [Theory]
        [InlineData("_", 1, "ab", "ab_")]
        [InlineData("_", 2, "ab", "a_b")]
        [InlineData("^", 9, "ab", "^ab")]
        [InlineData("X", 1, "", "X")]
        public void Apply_FromEnd_InsertsAtComputedIndex(string text, int position, string input, string expected)
        {
            var f = new InserterFilter(
                _target,
                new InserterOptions(Text: text, Position: position, StartFrom: InserterOrigin.End, Overwrite: false)
            );
            Assert.Equal(expected, FilterTestHelpers.ApplyToPrefix(f, input));
        }

        /// <summary>
        /// Verifies overwrite replaces characters at the insert index, including past the segment end and from the end.
        /// </summary>
        [Theory]
        [InlineData("**", 2, InserterOrigin.Beginning, "abcd", "a**d")]
        [InlineData("YZ", 3, InserterOrigin.Beginning, "ab", "abYZ")]
        [InlineData("*", 2, InserterOrigin.End, "ab", "a*")]
        [InlineData("YZ", 1, InserterOrigin.Beginning, "", "YZ")]
        public void Apply_Overwrite_ReplacesCharactersAtIndex(
            string text,
            int position,
            InserterOrigin startFrom,
            string input,
            string expected
        )
        {
            var f = new InserterFilter(
                _target,
                new InserterOptions(Text: text, Position: position, StartFrom: startFrom, Overwrite: true)
            );
            Assert.Equal(expected, FilterTestHelpers.ApplyToPrefix(f, input));
        }

        /// <summary>
        /// Verifies comparison-like <c>&lt;</c> without a formatter token span inserts literal text.
        /// </summary>
        [Fact]
        public void Apply_TextComparisonLikeBrackets_InsertsLiteral()
        {
            var f = new InserterFilter(
                _target,
                new InserterOptions(Text: "a < b", Position: 1, StartFrom: InserterOrigin.Beginning, Overwrite: false)
            );
            Assert.Equal("a < bhello", FilterTestHelpers.ApplyToPrefix(f, "hello"));
        }

        /// <summary>
        /// Verifies formatter tokens are expanded in the insert text using preview file-name metadata.
        /// </summary>
        [Fact]
        public void Apply_TextWithToken_ResolvesTemplate()
        {
            var f = new InserterFilter(
                _target,
                new InserterOptions(
                    Text: "_<file-name>_",
                    Position: 1,
                    StartFrom: InserterOrigin.Beginning,
                    Overwrite: false
                )
            );
            Assert.Equal("_new_new", FilterTestHelpers.ApplyToPrefix(f, "new", renameListIndex: 0));
        }
    }
}
