using Mfr.Filters.Case;

namespace Mfr.Tests.Models.Filters.Case
{
    /// <summary>
    /// Tests for <see cref="CasingListParser"/>.
    /// </summary>
    public sealed class CasingListParserTests
    {
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
