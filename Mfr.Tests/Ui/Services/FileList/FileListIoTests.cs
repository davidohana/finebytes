using Mfr.App.Ui.Services.FileList;

namespace Mfr.Tests.Ui.Services.FileList
{
    /// <summary>
    /// Tests shared File List IO probes used by catalog listing and Go Up climb.
    /// </summary>
    public sealed class FileListIoTests
    {
        /// <summary>
        /// Verifies UNC paths always request a network timeout.
        /// </summary>
        [Fact]
        public void NeedsNetworkTimeout_True_For_Unc()
        {
            if (!OperatingSystem.IsWindows())
            {
                return;
            }

            Assert.True(FileListIo.NeedsNetworkTimeout(@"\\server\share"));
            Assert.True(FileListIo.NeedsNetworkTimeout(@"\\server\share\folder"));
        }

        /// <summary>
        /// Verifies a local temp folder Exists probe succeeds without a network timeout path.
        /// </summary>
        [Fact]
        public void DirectoryExists_True_For_Local_Temp()
        {
            var dir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(dir);
            try
            {
                Assert.False(FileListIo.NeedsNetworkTimeout(dir));
                Assert.True(FileListIo.DirectoryExists(dir));
            }
            finally
            {
                Directory.Delete(dir);
            }
        }

        /// <summary>
        /// Verifies missing local folders report false.
        /// </summary>
        [Fact]
        public void DirectoryExists_False_For_Missing_Local()
        {
            var missing = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"));
            Assert.False(FileListIo.DirectoryExists(missing));
        }
    }
}
