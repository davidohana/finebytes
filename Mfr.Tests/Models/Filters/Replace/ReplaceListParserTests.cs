using Mfr.Filters.Replace;

namespace Mfr.Tests.Models.Filters.Replace
{
    /// <summary>
    /// Tests for <see cref="ReplaceListParser"/>.
    /// </summary>
    public sealed class ReplaceListParserTests
    {
        /// <summary>
        /// Verifies an empty entry list validates to empty.
        /// </summary>
        [Fact]
        public void Validate_Empty_ReturnsEmpty()
        {
            Assert.Empty(ReplaceListParser.Validate([]));
        }

        /// <summary>
        /// Verifies empty replacement string is kept (strip).
        /// </summary>
        [Fact]
        public void Validate_EmptyReplacementString_Kept()
        {
            var entries = ReplaceListParser.Validate([new ReplaceListEntry("x", "")]);

            Assert.Single(entries);
            Assert.Equal("x", entries[0].Search);
            Assert.Equal("", entries[0].Replacement);
        }

        /// <summary>
        /// Verifies whitespace in search or replacement is allowed.
        /// </summary>
        [Fact]
        public void Validate_WhitespaceInPair_Allowed()
        {
            var entries = ReplaceListParser.Validate([new ReplaceListEntry("Blue Train", "Blue Train Live")]);

            Assert.Single(entries);
            Assert.Equal("Blue Train", entries[0].Search);
            Assert.Equal("Blue Train Live", entries[0].Replacement);
        }

        /// <summary>
        /// Verifies empty or whitespace search is rejected.
        /// </summary>
        [Theory]
        [InlineData("")]
        [InlineData("   ")]
        public void Validate_EmptySearch_Throws(string search)
        {
            var ex = Assert.Throws<UserException>(() =>
                ReplaceListParser.Validate([new ReplaceListEntry(search, "b")])
            );
            Assert.Contains("search cannot be empty", ex.Message, StringComparison.Ordinal);
        }

        /// <summary>
        /// Verifies search containing the editor separator is rejected.
        /// </summary>
        [Fact]
        public void Validate_SearchContainsEditorSeparator_Throws()
        {
            var ex = Assert.Throws<UserException>(() =>
                ReplaceListParser.Validate([new ReplaceListEntry("a => b", "c")])
            );
            Assert.Contains(ReplaceListEntry.EditorSeparator, ex.Message, StringComparison.Ordinal);
        }

        /// <summary>
        /// Verifies overly long search or replacement is rejected.
        /// </summary>
        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public void Validate_PartTooLong_Throws(bool isSearch)
        {
            var maxLen = ConfigStore.Config.Filters.MaxListFileLineLength;
            var tooLong = new string('x', maxLen + 1);
            var entry = isSearch ? new ReplaceListEntry(tooLong, "b") : new ReplaceListEntry("a", tooLong);

            var ex = Assert.Throws<UserException>(() => ReplaceListParser.Validate([entry]));
            Assert.Contains("exceeds maximum length", ex.Message, StringComparison.Ordinal);
        }
    }
}
