using Mfr.Filters.Formatting;
using Mfr.Models;

namespace Mfr.Tests.Models.Filters.Formatting
{
    /// <summary>
    /// Tests for <see cref="InserterFilter"/>.
    /// </summary>
    public class InserterFilterTests
    {
        private static readonly FileNameTarget _target = new(FileNamePart.Prefix);

        /// <summary>
        /// Verifies the documented example: insert before the third character without shifting past overwrite.
        /// </summary>
        [Fact]
        public void Apply_FromBeginning_Position3_InsertsBeforeThirdCharacter()
        {
            var f = new InserterFilter(
                _target,
                new InserterOptions(Text: "_-", Position: 3, StartFrom: InserterOrigin.Beginning, Overwrite: false));
            Assert.Equal(
                "01_-_Mercury_Rave_-_Holes",
                FilterTestHelpers.ApplyToPrefix(f, "01_Mercury_Rave_-_Holes"));
        }

        /// <summary>
        /// Verifies that a large position appends at the end of the segment.
        /// </summary>
        [Fact]
        public void Apply_FromBeginning_PositionPastLength_AppendsAtEnd()
        {
            var f = new InserterFilter(
                _target,
                new InserterOptions(Text: "X", Position: 99, StartFrom: InserterOrigin.Beginning, Overwrite: false));
            Assert.Equal("abX", FilterTestHelpers.ApplyToPrefix(f, "ab"));
        }

        /// <summary>
        /// Verifies counting from the end: position 1 inserts before the last character.
        /// </summary>
        [Fact]
        public void Apply_FromEnd_Position1_InsertsBeforeLastCharacter()
        {
            var f = new InserterFilter(
                _target,
                new InserterOptions(Text: "_", Position: 1, StartFrom: InserterOrigin.End, Overwrite: false));
            Assert.Equal("a_b", FilterTestHelpers.ApplyToPrefix(f, "ab"));
        }

        /// <summary>
        /// Verifies that an oversized position from the end inserts at the beginning.
        /// </summary>
        [Fact]
        public void Apply_FromEnd_PositionPastLength_InsertsAtStart()
        {
            var f = new InserterFilter(
                _target,
                new InserterOptions(Text: "^", Position: 9, StartFrom: InserterOrigin.End, Overwrite: false));
            Assert.Equal("^ab", FilterTestHelpers.ApplyToPrefix(f, "ab"));
        }

        /// <summary>
        /// Verifies overwrite replaces characters at the insert index.
        /// </summary>
        [Fact]
        public void Apply_Overwrite_ReplacesCharactersAtIndex()
        {
            var f = new InserterFilter(
                _target,
                new InserterOptions(Text: "**", Position: 2, StartFrom: InserterOrigin.Beginning, Overwrite: true));
            Assert.Equal("a**d", FilterTestHelpers.ApplyToPrefix(f, "abcd"));
        }

        /// <summary>
        /// Verifies formatter tokens are expanded in the insert text using original file-name metadata.
        /// </summary>
        [Fact]
        public void Apply_TextWithToken_ResolvesTemplate()
        {
            var f = new InserterFilter(
                _target,
                new InserterOptions(Text: "_<file-name>_", Position: 1, StartFrom: InserterOrigin.Beginning, Overwrite: false));
            Assert.Equal("_new_new", FilterTestHelpers.ApplyToPrefix(f, "new", globalIndex: 0));
        }
    }
}
