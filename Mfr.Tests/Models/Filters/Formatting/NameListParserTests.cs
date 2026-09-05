using Mfr.Filters.Formatting;

namespace Mfr.Tests.Models.Filters.Formatting
{
    /// <summary>
    /// Tests for <see cref="NameListParser"/>.
    /// </summary>
    public sealed class NameListParserTests
    {
        /// <summary>
        /// Verifies an empty entry list validates to empty.
        /// </summary>
        [Fact]
        public void Validate_Empty_ReturnsEmpty()
        {
            Assert.Empty(NameListParser.Validate([]));
        }

        /// <summary>
        /// Verifies blank-line entries are kept.
        /// </summary>
        [Fact]
        public void Validate_BlankEntries_Kept()
        {
            var entries = NameListParser.Validate(["A", "", "B"]);

            Assert.Equal(3, entries.Count);
            Assert.Equal("A", entries[0]);
            Assert.Equal(string.Empty, entries[1]);
            Assert.Equal("B", entries[2]);
        }

        /// <summary>
        /// Verifies overly long entries are rejected.
        /// </summary>
        [Fact]
        public void Validate_EntryTooLong_Throws()
        {
            var maxLen = ConfigStore.Config.Filters.MaxListFileLineLength;
            var tooLong = new string('x', maxLen + 1);

            var ex = Assert.Throws<UserException>(() => NameListParser.Validate([tooLong]));
            Assert.Contains("exceeds maximum length", ex.Message, StringComparison.Ordinal);
        }

        /// <summary>
        /// Verifies editor text parses one name per line, keeping blanks and CRLF.
        /// </summary>
        [Fact]
        public void ParseEditorText_PreservesLinesAndBlanks_AcceptsCrlf()
        {
            var entries = NameListParser.ParseEditorText("A\r\n\r\nB\n");

            Assert.Equal(3, entries.Count);
            Assert.Equal("A", entries[0]);
            Assert.Equal(string.Empty, entries[1]);
            Assert.Equal("B", entries[2]);
        }

        /// <summary>
        /// Verifies a trailing newline after the last name does not add an extra entry.
        /// </summary>
        [Fact]
        public void ParseEditorText_TrailingNewline_DoesNotAddEntry()
        {
            var entries = NameListParser.ParseEditorText("Alpha\nBeta\n");

            Assert.Equal(["Alpha", "Beta"], entries);
        }

        /// <summary>
        /// Verifies comment-like lines are kept as names in editor text.
        /// </summary>
        [Fact]
        public void ParseEditorText_CommentLikeLines_Kept()
        {
            var entries = NameListParser.ParseEditorText("// header\nReal1\n# also a comment");

            Assert.Equal(["// header", "Real1", "# also a comment"], entries);
        }

        /// <summary>
        /// Verifies empty editor text parses to no entries.
        /// </summary>
        [Theory]
        [InlineData(null)]
        [InlineData("")]
        public void ParseEditorText_Empty_ReturnsEmpty(string? text)
        {
            Assert.Empty(NameListParser.ParseEditorText(text));
        }

        /// <summary>
        /// Verifies format/parse round-trips structured entries, including a trailing blank.
        /// </summary>
        [Fact]
        public void FormatEditorText_RoundTripsThroughParse()
        {
            string[] entries = ["Alpha", "", "Beta", ""];

            var text = NameListParser.FormatEditorText(entries);
            var parsed = NameListParser.ParseEditorText(text);

            Assert.Equal("Alpha\n\nBeta\n\n", text);
            Assert.Equal(entries, parsed);
        }
    }
}
