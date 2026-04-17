using Mfr.Filters;
using Mfr.Filters.Formatting;
using Mfr.Models;
using Mfr.Tests.TestSupport;

namespace Mfr.Tests.Models.Filters.Formatting
{
    /// <summary>
    /// Tests for <see cref="NameListParser"/>.
    /// </summary>
    public sealed class NameListParserTests : IDisposable
    {
        private readonly TempDirectoryFixture _tempDir = new();

        /// <inheritdoc />
        public void Dispose()
        {
            _tempDir.Dispose();
        }

        /// <summary>
        /// Verifies ordered entries are returned when empty lines are not skipped.
        /// </summary>
        [Fact]
        public void ParseFile_SkipEmptyLinesFalse_PreservesLinesAndBlanks()
        {
            var path = _CreateFile(
                """
                A

                B
                """);

            var entries = NameListParser.ParseFile(path, skipEmptyLines: false);

            Assert.Equal(3, entries.Count);
            Assert.Equal("A", entries[0]);
            Assert.Equal(string.Empty, entries[1]);
            Assert.Equal("B", entries[2]);
        }

        /// <summary>
        /// Verifies blank lines are omitted when requested.
        /// </summary>
        [Fact]
        public void ParseFile_SkipEmptyLinesTrue_OmitsBlankLines()
        {
            var path = _CreateFile(
                """
                First


                Second
                """);

            var entries = NameListParser.ParseFile(path, skipEmptyLines: true);

            Assert.Equal(2, entries.Count);
            Assert.Equal("First", entries[0]);
            Assert.Equal("Second", entries[1]);
        }

        /// <summary>
        /// Verifies comment lines are skipped and do not produce entries.
        /// </summary>
        [Fact]
        public void ParseFile_CommentLines_Skipped()
        {
            var path = _CreateFile(
                """
                // header
                Real1
                # also a comment
                Real2
                """);

            var entries = NameListParser.ParseFile(path, skipEmptyLines: false);

            Assert.Equal(2, entries.Count);
            Assert.Equal("Real1", entries[0]);
            Assert.Equal("Real2", entries[1]);
        }

        /// <summary>
        /// Verifies empty path throws.
        /// </summary>
        [Theory]
        [InlineData("")]
        [InlineData("   ")]
        public void ParseFile_EmptyPath_Throws(string filePath)
        {
            var ex = Assert.Throws<UserException>(() => NameListParser.ParseFile(filePath, skipEmptyLines: true));
            Assert.Contains("cannot be empty", ex.Message, StringComparison.Ordinal);
        }

        /// <summary>
        /// Verifies missing file throws.
        /// </summary>
        [Fact]
        public void ParseFile_MissingFile_Throws()
        {
            var path = Path.Combine(_tempDir.TempDir, "does-not-exist.txt");

            var ex = Assert.Throws<UserException>(() => NameListParser.ParseFile(path, skipEmptyLines: true));
            Assert.Contains("not found", ex.Message, StringComparison.Ordinal);
        }

        /// <summary>
        /// Verifies at least one non-comment entry is required.
        /// </summary>
        [Theory]
        [InlineData("")]
        [InlineData("// only\n")]
        [InlineData("# comment only")]
        public void ParseFile_NoEntries_Throws(string content)
        {
            var path = _CreateFile(content);

            var ex = Assert.Throws<UserException>(() => NameListParser.ParseFile(path, skipEmptyLines: true));
            Assert.Contains("at least one name entry", ex.Message, StringComparison.Ordinal);
        }

        /// <summary>
        /// Verifies overly long lines are rejected.
        /// </summary>
        [Fact]
        public void ParseFile_LineTooLong_Throws()
        {
            var longLine = new string('x', ListFileParseHelpers.MaxListFileLineLength + 1);
            var path = _CreateFile(longLine);

            var ex = Assert.Throws<UserException>(() => NameListParser.ParseFile(path, skipEmptyLines: true));
            Assert.Contains("exceeds maximum length", ex.Message, StringComparison.Ordinal);
        }

        private string _CreateFile(string content)
        {
            var path = Path.Combine(_tempDir.TempDir, $"name-list-{Guid.NewGuid():N}.txt");
            File.WriteAllText(path, content.ReplaceLineEndings(Environment.NewLine));
            return path;
        }
    }
}
