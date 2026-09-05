using Mfr.Filters.Case;

namespace Mfr.Tests.Models.Filters.Case
{
    /// <summary>
    /// Tests for <see cref="CasingListParser"/>.
    /// </summary>
    public sealed class CasingListParserTests
    {
        /// <summary>
        /// Verifies whitespace-separated editor text parses into words.
        /// </summary>
        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("   ")]
        public void ParseEditorText_Blank_ReturnsEmpty(string? text)
        {
            Assert.Empty(CasingListParser.ParseEditorText(text));
        }

        /// <summary>
        /// Verifies editor text splits on any whitespace and trims entries.
        /// </summary>
        [Fact]
        public void ParseEditorText_SplitsOnWhitespace()
        {
            Assert.Equal(["and", "or", "RMX"], CasingListParser.ParseEditorText("  and   or\tRMX  "));
        }

        /// <summary>
        /// Verifies stored words round-trip through space-separated editor text.
        /// </summary>
        [Fact]
        public void FormatEditorText_JoinsWithSpaces()
        {
            Assert.Equal(string.Empty, CasingListParser.FormatEditorText([]));
            Assert.Equal("and or RMX", CasingListParser.FormatEditorText(["and", "or", "RMX"]));
        }

        /// <summary>
        /// Verifies an empty word list yields an empty map.
        /// </summary>
        [Fact]
        public void BuildMap_Empty_ReturnsEmpty()
        {
            Assert.Empty(CasingListParser.BuildMap([]));
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
        /// Verifies BuildMap rejects empty or whitespace-only entries.
        /// </summary>
        [Theory]
        [InlineData("")]
        [InlineData("   ")]
        public void BuildMap_EmptyWord_Throws(string word)
        {
            var ex = Assert.Throws<UserException>(() => CasingListParser.BuildMap(["ok", word]));
            Assert.Contains("word 2 cannot be empty", ex.Message, StringComparison.Ordinal);
        }

        /// <summary>
        /// Verifies BuildMap rejects words that contain whitespace.
        /// </summary>
        [Theory]
        [InlineData("not ok")]
        [InlineData("not\tok")]
        public void BuildMap_WordWithWhitespace_Throws(string word)
        {
            var ex = Assert.Throws<UserException>(() => CasingListParser.BuildMap(["ok", word]));
            Assert.Contains("word 2", ex.Message, StringComparison.Ordinal);
            Assert.Contains("single word", ex.Message, StringComparison.Ordinal);
        }

        /// <summary>
        /// Verifies overly long words are rejected.
        /// </summary>
        [Fact]
        public void BuildMap_WordTooLong_Throws()
        {
            var longWord = new string('x', ConfigStore.Config.Filters.MaxListFileLineLength + 1);

            var ex = Assert.Throws<UserException>(() => CasingListParser.BuildMap([longWord]));
            Assert.Contains("exceeds maximum length", ex.Message, StringComparison.Ordinal);
        }
    }
}
