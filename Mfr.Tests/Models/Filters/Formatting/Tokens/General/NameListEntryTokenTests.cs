using Mfr.Filters.Formatting;
using Mfr.Filters.Formatting.Tokens.General;
using Mfr.Models;
using Mfr.Tests.TestSupport;

namespace Mfr.Tests.Models.Filters.Formatting.Tokens.General
{
    /// <summary>
    /// Tests for <see cref="NameListEntryToken"/>.
    /// </summary>
    public sealed class NameListEntryTokenTests : IDisposable
    {
        private readonly TempDirectoryFixture _tempDir = new();

        /// <inheritdoc />
        public void Dispose()
        {
            _tempDir.Dispose();
        }

        /// <summary>
        /// Verifies the token returns the line at the item's global index.
        /// </summary>
        [Fact]
        public void Resolve_LineAtGlobalIndex_ReturnsMatchingEntry()
        {
            var token = new NameListEntryToken();
            var filePath = _CreateFile(
                """
                First
                Second
                Third
                """);
            var item = FilterTestHelpers.CreateRenameItem(globalIndex: 1);

            var result = token.Resolve(arg: filePath, item: item);

            Assert.Equal("Second", result);
        }

        /// <summary>
        /// Verifies empty lines are preserved as entries for index mapping.
        /// </summary>
        [Fact]
        public void Resolve_EmptyLineAtIndex_ReturnsEmptyString()
        {
            var token = new NameListEntryToken();
            var filePath = _CreateFile(
                """
                Alpha

                Gamma
                """);
            var item = FilterTestHelpers.CreateRenameItem(globalIndex: 1);

            var result = token.Resolve(arg: filePath, item: item);

            Assert.Equal(string.Empty, result);
        }

        /// <summary>
        /// Verifies index values beyond file line count return empty text.
        /// </summary>
        [Fact]
        public void Resolve_IndexBeyondLineCount_ReturnsEmptyString()
        {
            var token = new NameListEntryToken();
            var filePath = _CreateFile(
                """
                A
                B
                """);
            var item = FilterTestHelpers.CreateRenameItem(globalIndex: 4);

            var result = token.Resolve(arg: filePath, item: item);

            Assert.Equal(string.Empty, result);
        }

        /// <summary>
        /// Verifies an empty file is rejected (same default requirement as NameListFilter).
        /// </summary>
        [Fact]
        public void Resolve_EmptyFile_ThrowsUserException()
        {
            var token = new NameListEntryToken();
            var filePath = _CreateFile(string.Empty);
            var item = FilterTestHelpers.CreateRenameItem(globalIndex: 0);

            var ex = Assert.Throws<UserException>(() => token.Resolve(arg: filePath, item: item));

            Assert.Contains("at least one name entry", ex.Message, StringComparison.Ordinal);
        }

        /// <summary>
        /// Verifies comment-like lines are skipped using default list parsing behavior.
        /// </summary>
        [Fact]
        public void Resolve_CommentLikeLine_Skipped()
        {
            var token = new NameListEntryToken();
            var filePath = _CreateFile(
                """
                //first
                second
                """);
            var item = FilterTestHelpers.CreateRenameItem(globalIndex: 0);

            var result = token.Resolve(arg: filePath, item: item);

            Assert.Equal("second", result);
        }

        /// <summary>
        /// Verifies the argument is required.
        /// </summary>
        [Theory]
        [InlineData("")]
        [InlineData(" ")]
        public void Resolve_EmptyArgument_Throws(string arg)
        {
            var token = new NameListEntryToken();
            var item = FilterTestHelpers.CreateRenameItem();

            var ex = Assert.Throws<InvalidOperationException>(() => token.Resolve(arg: arg, item: item));

            Assert.Contains("name-list-entry", ex.Message, StringComparison.Ordinal);
        }

        /// <summary>
        /// Verifies missing files produce a user-facing validation error.
        /// </summary>
        [Fact]
        public void Resolve_MissingFile_ThrowsUserException()
        {
            var token = new NameListEntryToken();
            var item = FilterTestHelpers.CreateRenameItem();
            var missingPath = Path.Combine(_tempDir.TempDir, "missing.txt");

            var ex = Assert.Throws<UserException>(() => token.Resolve(arg: missingPath, item: item));

            Assert.Contains("not found", ex.Message, StringComparison.Ordinal);
        }

        /// <summary>
        /// Verifies resolver wiring discovers and executes the token.
        /// </summary>
        [Fact]
        public void ResolveTemplate_NameListEntryToken_Resolves()
        {
            var filePath = _CreateFile(
                """
                One
                Two
                """);
            var item = FilterTestHelpers.CreateRenameItem(globalIndex: 1);

            var result = FormatStringResolver.ResolveTemplate(
                template: $"<name-list-entry:{filePath}>",
                item: item);

            Assert.Equal("Two", result);
        }

        private string _CreateFile(string content)
        {
            var path = Path.Combine(_tempDir.TempDir, $"name-list-entry-{Guid.NewGuid():N}.txt");
            File.WriteAllText(path, content.ReplaceLineEndings(Environment.NewLine));
            return path;
        }
    }
}
