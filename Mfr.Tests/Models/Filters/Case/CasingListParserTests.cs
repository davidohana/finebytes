using Mfr.Filters.Case;

namespace Mfr.Tests.Models.Filters.Case
{
    /// <summary>
    /// Tests for <see cref="CasingListParser"/>.
    /// </summary>
    public sealed class CasingListParserTests
    {
        /// <summary>
        /// Verifies line text parse keeps word order and trims.
        /// </summary>
        [Fact]
        public void ParseWordLines_ValidWords_ReturnsTrimmedWords()
        {
            var words = CasingListParser.ParseWordLines(
                """
                and
                  RMX
                """
            );

            Assert.Equal(["and", "RMX"], words);
        }

        /// <summary>
        /// Verifies comment lines and blank lines are skipped.
        /// </summary>
        [Theory]
        [InlineData("// note")]
        [InlineData(@"\\ note")]
        [InlineData("  # comment")]
        public void ParseWordLines_Comments_AreIgnored(string commentLine)
        {
            var words = CasingListParser.ParseWordLines(
                $"""
                {commentLine}

                hello
                """
            );

            Assert.Equal(["hello"], words);
        }

        /// <summary>
        /// Verifies <c>#</c> without a following space is content, not a comment.
        /// </summary>
        [Fact]
        public void ParseWordLines_HashWithoutSpace_IsNotComment()
        {
            var words = CasingListParser.ParseWordLines("#tag");

            Assert.Equal(["#tag"], words);
        }

        /// <summary>
        /// Verifies empty or comment-only text yields an empty list.
        /// </summary>
        [Theory]
        [InlineData("")]
        [InlineData("   ")]
        [InlineData("   \n  \n")]
        [InlineData("// only")]
        [InlineData("# comment")]
        public void ParseWordLines_NoWords_ReturnsEmpty(string content)
        {
            Assert.Empty(CasingListParser.ParseWordLines(content));
        }

        /// <summary>
        /// Verifies a line containing a space is rejected with line number.
        /// </summary>
        [Fact]
        public void ParseWordLines_LineWithMultipleWords_Throws()
        {
            var ex = Assert.Throws<UserException>(
                () =>
                    CasingListParser.ParseWordLines(
                        """
                        ok
                        not ok
                        """
                    )
            );
            Assert.Contains("line 2", ex.Message, StringComparison.Ordinal);
            Assert.Contains("exactly one word", ex.Message, StringComparison.Ordinal);
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
        /// Verifies overly long lines are rejected when parsing editor text.
        /// </summary>
        [Fact]
        public void ParseWordLines_LineTooLong_Throws()
        {
            var longWord = new string('x', ConfigStore.Config.Filters.MaxListFileLineLength + 1);

            var ex = Assert.Throws<UserException>(() => CasingListParser.ParseWordLines(longWord));
            Assert.Contains("exceeds maximum length", ex.Message, StringComparison.Ordinal);
        }
    }
}
