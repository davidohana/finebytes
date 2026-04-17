using Mfr.Filters;
using Mfr.Filters.Case;
using Mfr.Models;
using Mfr.Tests.TestSupport;

namespace Mfr.Tests.Models.Filters.Case
{
    /// <summary>
    /// Tests for <see cref="CasingListParser"/>.
    /// </summary>
    public sealed class CasingListParserTests : IDisposable
    {
        private readonly TempDirectoryFixture _tempDir = new();

        /// <summary>
        /// Verifies successful parse builds lower-key to canonical-casing map.
        /// </summary>
        [Fact]
        public void ParseFile_ValidWords_MapsLowerKeyToCanonicalCasing()
        {
            var path = _CreateFile(
                """
                and
                RMX
                """);

            var map = CasingListParser.ParseFile(path);

            Assert.Equal(2, map.Count);
            Assert.Equal("and", map["and"]);
            Assert.Equal("RMX", map["rmx"]);
        }

        /// <summary>
        /// Verifies comment lines and blank lines are skipped.
        /// </summary>
        [Theory]
        [InlineData("// note")]
        [InlineData(@"\\ note")]
        [InlineData("  # comment")]
        public void ParseFile_Comments_AreIgnored(string commentLine)
        {
            var path = _CreateFile(
                $"""
                {commentLine}

                hello
                """);

            var map = CasingListParser.ParseFile(path);

            Assert.Single(map);
            Assert.Equal("hello", map["hello"]);
        }

        /// <summary>
        /// Verifies <c>#</c> without a following space is content, not a comment.
        /// </summary>
        [Fact]
        public void ParseFile_HashWithoutSpace_IsNotComment()
        {
            var path = _CreateFile("#tag");

            var map = CasingListParser.ParseFile(path);

            Assert.Single(map);
            Assert.Equal("#tag", map["#tag"]);
        }

        /// <summary>
        /// Verifies the last occurrence of a duplicate word wins.
        /// </summary>
        [Fact]
        public void ParseFile_DuplicateWords_LastWins()
        {
            var path = _CreateFile(
                """
                foo
                Foo
                FOO
                """);

            var map = CasingListParser.ParseFile(path);

            Assert.Single(map);
            Assert.Equal("FOO", map["foo"]);
        }

        /// <summary>
        /// Verifies empty path throws.
        /// </summary>
        [Theory]
        [InlineData("")]
        [InlineData("   ")]
        public void ParseFile_EmptyPath_Throws(string filePath)
        {
            var ex = Assert.Throws<UserException>(() => CasingListParser.ParseFile(filePath));
            Assert.Contains("cannot be empty", ex.Message, StringComparison.Ordinal);
        }

        /// <summary>
        /// Verifies missing file throws.
        /// </summary>
        [Fact]
        public void ParseFile_MissingFile_Throws()
        {
            var path = Path.Combine(_tempDir.TempDir, "does-not-exist.txt");

            var ex = Assert.Throws<UserException>(() => CasingListParser.ParseFile(path));
            Assert.Contains("not found", ex.Message, StringComparison.Ordinal);
        }

        /// <summary>
        /// Verifies at least one word is required.
        /// </summary>
        [Theory]
        [InlineData("")]
        [InlineData("   \n  \n")]
        [InlineData("// only")]
        [InlineData("# comment")]
        public void ParseFile_NoWords_Throws(string content)
        {
            var path = _CreateFile(content);

            var ex = Assert.Throws<UserException>(() => CasingListParser.ParseFile(path));
            Assert.Contains("at least one word", ex.Message, StringComparison.Ordinal);
        }

        /// <summary>
        /// Verifies a line containing a space is rejected with line number.
        /// </summary>
        [Fact]
        public void ParseFile_LineWithMultipleWords_Throws()
        {
            var path = _CreateFile(
                """
                ok
                not ok
                """);

            var ex = Assert.Throws<UserException>(() => CasingListParser.ParseFile(path));
            Assert.Contains("line 2", ex.Message, StringComparison.Ordinal);
            Assert.Contains("exactly one word", ex.Message, StringComparison.Ordinal);
        }

        /// <summary>
        /// Verifies overly long lines are rejected.
        /// </summary>
        [Fact]
        public void ParseFile_LineTooLong_Throws()
        {
            var longWord = new string('x', ListFileParseHelpers.MaxListFileLineLength + 1);
            var path = _CreateFile(longWord);

            var ex = Assert.Throws<UserException>(() => CasingListParser.ParseFile(path));
            Assert.Contains("exceeds maximum length", ex.Message, StringComparison.Ordinal);
        }

        public void Dispose()
        {
            _tempDir.Dispose();
        }

        private string _CreateFile(string content)
        {
            var path = Path.Combine(_tempDir.TempDir, $"casing-list-{Guid.NewGuid():N}.txt");
            File.WriteAllText(path, content.ReplaceLineEndings(Environment.NewLine));
            return path;
        }
    }
}
