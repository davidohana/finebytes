using Mfr.Filters;
using Mfr.Models;
using Mfr.Tests.TestSupport;

namespace Mfr.Tests.Models.Filters
{
    /// <summary>
    /// Tests for <see cref="ListFileParseHelpers"/>.
    /// </summary>
    public sealed class ListFileParseHelpersTests : IDisposable
    {
        private const string _kind = "Test-list";

        private readonly TempDirectoryFixture _tempDir = new();

        /// <inheritdoc />
        public void Dispose()
        {
            _tempDir.Dispose();
        }

        /// <summary>
        /// Verifies empty or whitespace path throws with expected wording.
        /// </summary>
        [Theory]
        [InlineData("")]
        [InlineData("   ")]
        public void ValidateListFilePath_EmptyOrWhitespace_Throws(string filePath)
        {
            var ex = Assert.Throws<UserException>(() => ListFileParseHelpers.ValidateListFilePath(filePath, _kind));
            Assert.Contains("cannot be empty", ex.Message, StringComparison.Ordinal);
            Assert.Contains(_kind, ex.Message, StringComparison.Ordinal);
        }

        /// <summary>
        /// Verifies missing file throws.
        /// </summary>
        [Fact]
        public void ValidateListFilePath_MissingFile_Throws()
        {
            var path = Path.Combine(_tempDir.TempDir, "missing.txt");

            var ex = Assert.Throws<UserException>(() => ListFileParseHelpers.ValidateListFilePath(path, _kind));

            Assert.Contains("not found", ex.Message, StringComparison.Ordinal);
            Assert.Contains(path, ex.Message, StringComparison.Ordinal);
        }

        /// <summary>
        /// Verifies comment markers after optional leading whitespace.
        /// </summary>
        [Theory]
        [InlineData("// note")]
        [InlineData(@"\\ note")]
        [InlineData("  # comment")]
        public void IsListFileCommentLine_Markers_ReturnTrue(string line)
        {
            Assert.True(ListFileParseHelpers.IsListFileCommentLine(line));
        }

        /// <summary>
        /// Verifies <c>#</c> without a following space is not a comment.
        /// </summary>
        [Fact]
        public void IsListFileCommentLine_HashWithoutFollowingSpace_ReturnFalse()
        {
            Assert.False(ListFileParseHelpers.IsListFileCommentLine("#tag"));
        }

        /// <summary>
        /// Verifies empty or whitespace-only lines are not comments.
        /// </summary>
        [Theory]
        [InlineData("")]
        [InlineData("   ")]
        [InlineData("\t")]
        public void IsListFileCommentLine_Blank_ReturnFalse(string line)
        {
            Assert.False(ListFileParseHelpers.IsListFileCommentLine(line));
        }

        /// <summary>
        /// Verifies ordinary content is not a comment.
        /// </summary>
        [Fact]
        public void IsListFileCommentLine_Content_ReturnFalse()
        {
            Assert.False(ListFileParseHelpers.IsListFileCommentLine("hello"));
        }
    }
}
