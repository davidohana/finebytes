using Mfr.Filters.Formatting.Tokens.FileName;

namespace Mfr.Tests.Models.Filters.Formatting.Tokens.FileName
{
    /// <summary>
    /// Tests for <see cref="ParentFolderToken"/>.
    /// </summary>
    public sealed class ParentFolderTokenTests
    {
        /// <summary>
        /// Verifies the default level (no argument) returns the immediate parent folder.
        /// </summary>
        [Fact]
        public void Resolve_NoArg_ReturnsImmediateParent()
        {
            var token = new ParentFolderToken();
            var item = FilterTestHelpers.CreateRenameItem(directory: @"C:\Music\My Album");

            Assert.Equal("My Album", token.Resolve(arg: "", item: item));
        }

        /// <summary>
        /// Verifies an explicit level of <c>1</c> matches the no-arg default.
        /// </summary>
        [Fact]
        public void Resolve_LevelOne_MatchesNoArg()
        {
            var token = new ParentFolderToken();
            var item = FilterTestHelpers.CreateRenameItem(directory: @"C:\Music\My Album");

            Assert.Equal("My Album", token.Resolve(arg: "1", item: item));
        }

        /// <summary>
        /// Verifies <c>level=2</c> walks up to the grandparent folder.
        /// </summary>
        [Fact]
        public void Resolve_LevelTwo_ReturnsGrandparent()
        {
            var token = new ParentFolderToken();
            var item = FilterTestHelpers.CreateRenameItem(directory: @"C:\Medical Data\apr03\patients");

            Assert.Equal("apr03", token.Resolve(arg: "2", item: item));
        }

        /// <summary>
        /// Verifies a level deeper than the path returns an empty string instead of throwing.
        /// </summary>
        [Fact]
        public void Resolve_LevelTooHigh_ReturnsEmpty()
        {
            var token = new ParentFolderToken();
            var item = FilterTestHelpers.CreateRenameItem(directory: @"C:\Medical Data\apr03\patients");

            Assert.Equal(string.Empty, token.Resolve(arg: "5", item: item));
        }
    }
}
