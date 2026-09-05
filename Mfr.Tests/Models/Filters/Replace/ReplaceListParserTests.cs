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
        /// Verifies whitespace in search or replacement is allowed, including <c>=&gt;</c> in search.
        /// </summary>
        [Fact]
        public void Validate_WhitespaceAndSeparatorInSearch_Allowed()
        {
            var entries = ReplaceListParser.Validate([
                new ReplaceListEntry("Blue Train", "Blue Train Live"),
                new ReplaceListEntry("a=>b", "x"),
            ]);

            Assert.Equal(2, entries.Count);
            Assert.Equal("Blue Train", entries[0].Search);
            Assert.Equal("Blue Train Live", entries[0].Replacement);
            Assert.Equal("a=>b", entries[1].Search);
            Assert.Equal("x", entries[1].Replacement);
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

        /// <summary>
        /// Verifies editor text parses into search/replace pairs.
        /// </summary>
        [Theory]
        [InlineData("a => b", "a", "b")]
        [InlineData("a=>b", "a", "b")]
        [InlineData("Blue Train => Blue_Train", "Blue Train", "Blue_Train")]
        [InlineData("x", "x", "")]
        [InlineData("a =>", "a", "")]
        [InlineData("a => b => c", "a", "b => c")]
        public void ParseEditorText_Line_YieldsPair(string text, string search, string replacement)
        {
            var entries = ReplaceListParser.ParseEditorText(text);

            Assert.Single(entries);
            Assert.Equal(search, entries[0].Search);
            Assert.Equal(replacement, entries[0].Replacement);
        }

        /// <summary>
        /// Verifies blank lines, empty search, and CRLF are handled.
        /// </summary>
        [Fact]
        public void ParseEditorText_SkipsBlankAndEmptySearch_AcceptsCrlf()
        {
            var entries = ReplaceListParser.ParseEditorText("a => b\r\n\r\n=> skip\nx\n");

            Assert.Equal(2, entries.Count);
            Assert.Equal("a", entries[0].Search);
            Assert.Equal("b", entries[0].Replacement);
            Assert.Equal("x", entries[1].Search);
            Assert.Equal("", entries[1].Replacement);
        }

        /// <summary>
        /// Verifies empty editor text parses to no entries.
        /// </summary>
        [Theory]
        [InlineData("")]
        [InlineData("   ")]
        [InlineData("\n\n")]
        public void ParseEditorText_Empty_ReturnsEmpty(string text)
        {
            Assert.Empty(ReplaceListParser.ParseEditorText(text));
        }

        /// <summary>
        /// Verifies format/parse round-trips structured entries.
        /// </summary>
        [Fact]
        public void FormatEditorText_RoundTripsThroughParse()
        {
            ReplaceListEntry[] entries = [new("a", "b"), new("Blue Train", "Blue_Train"), new("x", "")];

            var text = ReplaceListParser.FormatEditorText(entries);
            var parsed = ReplaceListParser.ParseEditorText(text);

            Assert.Equal("a => b\nBlue Train => Blue_Train\nx", text);
            Assert.Equal(entries, parsed);
        }
    }
}
