using Mfr.Filters.Case;

namespace Mfr.Tests.Models.Filters.Case
{
    /// <summary>
    /// Tests for <see cref="CasingListParser"/>.
    /// </summary>
    public sealed class CasingListParserTests
    {
        /// <summary>
        /// Verifies space-separated text parse keeps word order.
        /// </summary>
        [Fact]
        public void ParseWordsText_ValidWords_ReturnsWords()
        {
            var words = CasingListParser.ParseWordsText("  and   RMX  ");

            Assert.Equal(["and", "RMX"], words);
        }

        /// <summary>
        /// Verifies newlines and tabs also act as separators.
        /// </summary>
        [Fact]
        public void ParseWordsText_WhitespaceVariants_Split()
        {
            var words = CasingListParser.ParseWordsText("and\tor\nwith");

            Assert.Equal(["and", "or", "with"], words);
        }

        /// <summary>
        /// Verifies empty text yields an empty list.
        /// </summary>
        [Theory]
        [InlineData("")]
        [InlineData("   ")]
        public void ParseWordsText_Blank_ReturnsEmpty(string content)
        {
            Assert.Empty(CasingListParser.ParseWordsText(content));
        }

        /// <summary>
        /// Verifies map build is case-insensitive and last duplicate wins.
        /// </summary>
        [Fact]
        public void BuildMap_DuplicateWords_LastWins()
        {
            var map = CasingListParser.BuildMap(["foo", "Foo", "FOO"]);

            Assert.Single(map);
            Assert.Equal("FOO", map["foo"]);
        }

        /// <summary>
        /// Verifies BuildMap rejects words that contain spaces.
        /// </summary>
        [Fact]
        public void BuildMap_WordWithSpace_Throws()
        {
            var ex = Assert.Throws<UserException>(() => CasingListParser.BuildMap(["ok", "not ok"]));
            Assert.Contains("word 2", ex.Message, StringComparison.Ordinal);
        }

        /// <summary>
        /// Verifies overly long words are rejected when parsing editor text.
        /// </summary>
        [Fact]
        public void ParseWordsText_WordTooLong_Throws()
        {
            var longWord = new string('x', ConfigStore.Config.Filters.MaxListFileLineLength + 1);

            var ex = Assert.Throws<UserException>(() => CasingListParser.ParseWordsText(longWord));
            Assert.Contains("exceeds maximum length", ex.Message, StringComparison.Ordinal);
        }
    }
}
