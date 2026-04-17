using Mfr.Filters.Replace;
using Mfr.Models;
using Mfr.Tests.TestSupport;

namespace Mfr.Tests.Models.Filters.Replace
{
    /// <summary>
    /// Tests for <see cref="ReplaceListParser"/>.
    /// </summary>
    public sealed class ReplaceListParserTests : IDisposable
    {
        private readonly TempDirectoryFixture _tempDir = new();

        public void Dispose()
        {
            _tempDir.Dispose();
        }

        /// <summary>
        /// Verifies parser ignores various comment styles.
        /// </summary>
        [Theory]
        [InlineData("// comment")]
        [InlineData(@"\\ comment")]
        [InlineData("  # comment")]
        public void ParseFile_Comments_AreIgnored(string commentLine)
        {
            var path = _CreateFile(
                $"""
                {commentLine}
                S:a
                R:b
                """);

            var entries = ReplaceListParser.ParseFile(path);

            Assert.Single(entries);
            Assert.Equal("a", entries[0].Search);
            Assert.Equal("b", entries[0].Replacement);
        }

        /// <summary>
        /// Verifies '#'-prefixed text without following space is treated as data, not comment.
        /// </summary>
        [Fact]
        public void ParseFile_HashWithoutSpace_IsNotComment()
        {
            var path = _CreateFile(
                """
                S:#a
                R:b
                """);

            var entries = ReplaceListParser.ParseFile(path);

            Assert.Single(entries);
            Assert.Equal("#a", entries[0].Search);
            Assert.Equal("b", entries[0].Replacement);
        }

        /// <summary>
        /// Verifies parser requires at least one replacement entry.
        /// </summary>
        [Theory]
        [InlineData("")]
        [InlineData("   ")]
        [InlineData("// comment only")]
        [InlineData("# comment only")]
        public void ParseFile_NoEntries_Throws(string content)
        {
            var path = _CreateFile(content);

            var ex = Assert.Throws<UserException>(() => ReplaceListParser.ParseFile(path));
            Assert.Contains("must contain at least one replacement entry", ex.Message);
        }

        /// <summary>
        /// Verifies parser rejects empty entries or missing prefixes.
        /// </summary>
        [Theory]
        [InlineData("S:x\nR:", "replace line cannot be empty")]
        [InlineData("S:\nR:b", "search line cannot be empty")]
        [InlineData("a\nR:b", "search line must start with 'S:'")]
        [InlineData("S:a", "found a search line without a corresponding replace line")]
        public void ParseFile_InvalidFormat_Throws(string content, string expectedError)
        {
            var path = _CreateFile(content);

            var ex = Assert.Throws<UserException>(() => ReplaceListParser.ParseFile(path));
            Assert.Contains(expectedError, ex.Message);
        }

        /// <summary>
        /// Verifies parser maps &lt;EMPTY&gt; token to empty replacement.
        /// </summary>
        [Fact]
        public void ParseFile_EmptyReplacementToken_MapsToEmptyString()
        {
            var path = _CreateFile(
                """
                S:x
                R:<EMPTY>
                """);

            var entries = ReplaceListParser.ParseFile(path);

            Assert.Single(entries);
            Assert.Equal("x", entries[0].Search);
            Assert.Equal("", entries[0].Replacement);
        }

        /// <summary>
        /// Verifies parser ignores blank lines between pairs.
        /// </summary>
        [Theory]
        [InlineData("S:a\nR:b\n\n\nS:c\nR:d")]
        public void ParseFile_BlankLinesBetweenPairs_AreAllowed(string content)
        {
            var path = _CreateFile(content);

            var entries = ReplaceListParser.ParseFile(path);

            Assert.Equal(2, entries.Count);
            Assert.Equal("a", entries[0].Search);
            Assert.Equal("c", entries[1].Search);
        }

        /// <summary>
        /// Verifies parser rejects lines over maximum length.
        /// </summary>
        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public void ParseFile_LineTooLong_Throws(bool isSearch)
        {
            var tooLong = new string('x', 1001);
            var content = isSearch ? $"S:{tooLong}\nR:b" : $"S:a\nR:{tooLong}";
            var path = _CreateFile(content);

            var ex = Assert.Throws<UserException>(() => ReplaceListParser.ParseFile(path));
            Assert.Contains("line length exceeds 1000", ex.Message);
        }

        private string _CreateFile(string content)
        {
            var path = Path.Combine(_tempDir.TempDir, $"test-{Guid.NewGuid():N}.txt");
            File.WriteAllText(path, content.ReplaceLineEndings(Environment.NewLine));
            return path;
        }
    }
}
