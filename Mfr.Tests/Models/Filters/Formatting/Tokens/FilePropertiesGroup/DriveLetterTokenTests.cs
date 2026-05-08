using Mfr.Filters.Formatting.Tokens.FilePropertiesGroup;

namespace Mfr.Tests.Models.Filters.Formatting.Tokens.FilePropertiesGroup
{
    /// <summary>
    /// Tests for <see cref="DriveLetterToken"/>.
    /// </summary>
    public sealed class DriveLetterTokenTests
    {
        /// <summary>
        /// Verifies a local path returns the drive letter without a trailing separator.
        /// </summary>
        [Fact]
        public void Resolve_LocalPath_ReturnsDriveWithoutSeparator()
        {
            var token = new DriveLetterToken();
            var item = FilterTestHelpers.CreateRenameItem(directory: @"C:\Medical Data\patients");

            Assert.Equal("C:", token.Resolve(arg: "", item: item));
        }

        /// <summary>
        /// Verifies UNC paths return <c>$</c> as documented for network shares.
        /// </summary>
        [Fact]
        public void Resolve_UncPath_ReturnsDollarSign()
        {
            var token = new DriveLetterToken();
            var item = FilterTestHelpers.CreateRenameItem(directory: @"\\server\share\docs");

            Assert.Equal("$", token.Resolve(arg: "", item: item));
        }

        /// <summary>
        /// Verifies a path with no resolvable root returns an empty string.
        /// </summary>
        [Fact]
        public void Resolve_NoRoot_ReturnsEmpty()
        {
            var token = new DriveLetterToken();
            var item = FilterTestHelpers.CreateRenameItem(directory: "");

            Assert.Equal(string.Empty, token.Resolve(arg: "", item: item));
        }
    }
}
