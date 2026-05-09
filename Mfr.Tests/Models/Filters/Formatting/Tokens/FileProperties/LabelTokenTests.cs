using Mfr.Filters.Formatting.Tokens.FileProperties;

namespace Mfr.Tests.Models.Filters.Formatting.Tokens.FileProperties
{
    /// <summary>
    /// Tests for <see cref="LabelToken"/>.
    /// </summary>
    public sealed class LabelTokenTests
    {
        /// <summary>
        /// Verifies a path with no resolvable root returns an empty string.
        /// </summary>
        [Fact]
        public void Resolve_NoRoot_ReturnsEmpty()
        {
            var token = new LabelToken();
            var item = FilterTestHelpers.CreateRenameItem(directory: "");

            Assert.Equal(string.Empty, token.Compile(arg: "")(item));
        }

        /// <summary>
        /// Verifies UNC paths return an empty string instead of throwing on <see cref="DriveInfo"/>.
        /// </summary>
        [Fact]
        public void Resolve_UncPath_ReturnsEmpty()
        {
            var token = new LabelToken();
            var item = FilterTestHelpers.CreateRenameItem(directory: @"\\server\share\docs");

            Assert.Equal(string.Empty, token.Compile(arg: "")(item));
        }

        /// <summary>
        /// Verifies a local path on a drive that exists returns a non-null label string.
        /// </summary>
        /// <remarks>
        /// <para>
        /// The label value depends on the host machine; we only assert that the token
        /// returns the same string the platform would report via <see cref="DriveInfo.VolumeLabel"/>
        /// for the resolved root, so the test stays portable across machines.
        /// </para>
        /// </remarks>
        [Fact]
        public void Resolve_LocalPath_MatchesDriveInfoVolumeLabel()
        {
            var localDrive = DriveInfo.GetDrives().FirstOrDefault(d => d.IsReady);
            if (localDrive is null)
                return;

            var token = new LabelToken();
            var item = FilterTestHelpers.CreateRenameItem(directory: localDrive.RootDirectory.FullName);

            Assert.Equal(localDrive.VolumeLabel, token.Compile(arg: "")(item));
        }
    }
}
