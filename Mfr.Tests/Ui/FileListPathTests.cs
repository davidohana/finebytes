using Mfr.App.Ui.Services.FileList;

namespace Mfr.Tests.Ui
{
    /// <summary>
    /// Tests UNC and Network location rules used by the File List pane.
    /// </summary>
    public sealed class FileListPathTests
    {
        /// <summary>
        /// Verifies a typed drive letter such as <c>D:</c> is the drive root, not the current folder on that drive.
        /// </summary>
        [Fact]
        public void TryGetDriveRoot_Expands_Bare_Drive_Spec()
        {
            if (!OperatingSystem.IsWindows())
            {
                Assert.False(FileListPath.TryGetDriveRoot("d:", out _));
                return;
            }

            Assert.True(FileListPath.TryGetDriveRoot("d:", out var root));
            Assert.Equal(@"D:\", root);
            Assert.True(FileListPath.TryGetDriveRoot(" D: ", out root));
            Assert.Equal(@"D:\", root);
            Assert.False(FileListPath.TryGetDriveRoot(@"d:\", out _));
            Assert.False(FileListPath.TryGetDriveRoot(@"d:\music", out _));
            Assert.False(FileListPath.TryGetDriveRoot("This PC", out _));
        }

        /// <summary>
        /// Verifies Network, <c>\\</c>, and <c>//</c> are the Network sentinel on Windows.
        /// </summary>
        [Fact]
        public void IsNetworkPath_Accepts_Aliases_On_Windows()
        {
            if (!OperatingSystem.IsWindows())
            {
                return;
            }

            Assert.True(FileListPath.IsNetworkPath(FileListPath.NetworkDisplayName));
            Assert.True(FileListPath.IsNetworkPath("network"));
            Assert.True(FileListPath.IsNetworkPath(@"\\"));
            Assert.True(FileListPath.IsNetworkPath("//"));
            Assert.False(FileListPath.IsNetworkPath(@"\\nas\music"));
            Assert.False(FileListPath.IsNetworkPath(FileListPath.ComputerDisplayName));
        }

        /// <summary>
        /// Verifies UNC shares are detected, including forward slashes and long-path form.
        /// </summary>
        [Fact]
        public void IsUncPath_Detects_Share_Paths()
        {
            Assert.True(FileListPath.IsUncPath(@"\\ohanas"));
            Assert.True(FileListPath.IsUncPath(@"\\nas\music\albums"));
            Assert.True(FileListPath.IsUncPath(@"//nas/music"));
            Assert.True(FileListPath.IsUncPath(@"\\?\UNC\nas\music"));
            Assert.False(FileListPath.IsUncPath(@"\\?\C:\Users"));
            Assert.False(FileListPath.IsUncPath(@"C:\Users"));
            Assert.False(FileListPath.IsUncPath("Network"));
        }

        /// <summary>
        /// Verifies share-root parsing stops at <c>\\server\share</c>.
        /// </summary>
        [Fact]
        public void TryGetUncShareRoot_Reads_Server_And_Share()
        {
            Assert.True(FileListPath.TryGetUncShareRoot(@"\\nas\music\albums\live", out var root));
            Assert.Equal(@"\\nas\music", root);
            Assert.True(FileListPath.IsUncShareRoot(@"\\nas\music"));
            Assert.False(FileListPath.IsUncShareRoot(@"\\nas\music\albums"));
            Assert.False(FileListPath.TryGetUncShareRoot(@"\\nas", out _));
            Assert.True(FileListPath.TryGetUncServerRoot(@"\\nas\music\albums", out var server));
            Assert.Equal(@"\\nas", server);
            Assert.True(FileListPath.IsUncServerRoot(@"\\nas"));
            Assert.True(FileListPath.IsUncServerRoot(@"//ohanas"));
            Assert.True(FileListPath.IsUncServerRoot(@"\\wsl$"));
            Assert.True(FileListPath.IsUncServerRoot(@"\\wsl.localhost"));
            Assert.False(FileListPath.IsUncServerRoot(@"\\nas\music"));
            Assert.False(FileListPath.IsUncServerRoot(@"\\wsl$\Ubuntu"));
            if (OperatingSystem.IsWindows())
            {
                Assert.True(FileListPath.IsUncServerRoot(@"\\nas\"));
            }

            if (OperatingSystem.IsWindows())
            {
                Assert.True(FileListPath.IsUncShareRoot(@"\\nas\music\"));
            }
        }

        /// <summary>
        /// Verifies Go Up from a UNC share opens the server, then Network.
        /// </summary>
        [Fact]
        public void GetParentPath_Unc_Walks_Server_Then_Network()
        {
            if (!OperatingSystem.IsWindows())
            {
                return;
            }

            Assert.Equal(@"\\nas", FileListPath.GetParentPath(@"\\nas\music"));
            Assert.Equal(@"\\nas", FileListPath.GetParentPath(@"\\nas\music\"));
            Assert.Equal(@"\\nas\music", FileListPath.GetParentPath(@"\\nas\music\albums"));
            Assert.Equal(FileListPath.NetworkPath, FileListPath.GetParentPath(@"\\nas"));
            Assert.Equal(@"\\wsl$", FileListPath.GetParentPath(@"\\wsl$\Ubuntu"));
            Assert.Equal(FileListPath.NetworkPath, FileListPath.GetParentPath(@"\\wsl$"));
            Assert.Equal(@"\\wsl.localhost", FileListPath.GetParentPath(@"\\wsl.localhost\Ubuntu"));
            Assert.Equal(FileListPath.ComputerPath, FileListPath.GetParentPath(FileListPath.NetworkPath));
            Assert.Null(FileListPath.GetParentPath(FileListPath.ComputerPath));
        }

        /// <summary>
        /// Verifies UNC breadcrumbs start at This PC, then Network, then the share.
        /// </summary>
        [Fact]
        public void Breadcrumb_Unc_Starts_At_ThisPc_Then_Network()
        {
            if (!OperatingSystem.IsWindows())
            {
                return;
            }

            var segments = FileListPath.BuildBreadcrumbSegments(@"\\nas\music\albums");
            Assert.Equal(
                [FileListPath.ComputerDisplayName, FileListPath.NetworkDisplayName, "nas", "music", "albums"],
                segments.Select(segment => segment.Label)
            );
            Assert.Equal(FileListPath.ComputerDisplayName, segments[0].TargetPath);
            Assert.Equal(FileListPath.NetworkDisplayName, segments[1].TargetPath);
            Assert.Equal(@"\\nas", segments[2].TargetPath);
            Assert.Equal(@"\\nas\music", segments[3].TargetPath);
            Assert.False(segments[0].ShowLeadingChevron);
            Assert.True(segments[1].ShowLeadingChevron);
            Assert.True(segments[^1].ShowLeadingChevron);
        }

        /// <summary>
        /// Verifies the Network sentinel sits under This PC.
        /// </summary>
        [Fact]
        public void Breadcrumb_Network_Follows_ThisPc()
        {
            if (!OperatingSystem.IsWindows())
            {
                return;
            }

            var segments = FileListPath.BuildBreadcrumbSegments(FileListPath.NetworkPath);
            Assert.Equal(
                [FileListPath.ComputerDisplayName, FileListPath.NetworkDisplayName],
                segments.Select(segment => segment.Label)
            );
            Assert.Equal(FileListPath.ComputerDisplayName, segments[0].TargetPath);
            Assert.Equal(FileListPath.NetworkDisplayName, segments[1].TargetPath);
            Assert.False(segments[0].ShowLeadingChevron);
            Assert.True(segments[1].ShowLeadingChevron);
        }

        /// <summary>
        /// Verifies a UNC server root is This PC, Network, then the computer name.
        /// </summary>
        [Fact]
        public void Breadcrumb_Unc_Server_Shows_Host_Name()
        {
            if (!OperatingSystem.IsWindows())
            {
                return;
            }

            var segments = FileListPath.BuildBreadcrumbSegments(@"\\ohanas");
            Assert.Equal(
                [FileListPath.ComputerDisplayName, FileListPath.NetworkDisplayName, "ohanas"],
                segments.Select(segment => segment.Label)
            );
            Assert.Equal(@"\\ohanas", segments[2].TargetPath);
        }

        /// <summary>
        /// Verifies WSL UNC roots use the same Network trail as other servers.
        /// </summary>
        [Fact]
        public void Breadcrumb_Wsl_Unc_Shows_Host_Then_Distro()
        {
            if (!OperatingSystem.IsWindows())
            {
                return;
            }

            var segments = FileListPath.BuildBreadcrumbSegments(@"\\wsl$\Ubuntu\home");
            Assert.Equal(
                [FileListPath.ComputerDisplayName, FileListPath.NetworkDisplayName, "wsl$", "Ubuntu", "home"],
                segments.Select(segment => segment.Label)
            );
            Assert.Equal(@"\\wsl$", segments[2].TargetPath);
            Assert.Equal(@"\\wsl$\Ubuntu", segments[3].TargetPath);
        }

        /// <summary>
        /// Verifies Go Up from Documents returns to This PC.
        /// </summary>
        [Fact]
        public void GetParentPath_Known_Place_Returns_ThisPc()
        {
            if (!OperatingSystem.IsWindows())
            {
                return;
            }

            var documents = _ExistingSpecialFolder(Environment.SpecialFolder.MyDocuments);
            if (documents is null)
            {
                return;
            }

            Assert.Equal(FileListPath.ComputerPath, FileListPath.GetParentPath(documents));
            Assert.Equal(documents, FileListPath.GetParentPath(Path.Combine(documents, "Work")));
        }

        /// <summary>
        /// Verifies Documents breadcrumbs are This PC, then Documents, then child folders.
        /// </summary>
        [Fact]
        public void Breadcrumb_Known_Place_Follows_ThisPc()
        {
            if (!OperatingSystem.IsWindows())
            {
                return;
            }

            var documents = _ExistingSpecialFolder(Environment.SpecialFolder.MyDocuments);
            if (documents is null)
            {
                return;
            }

            var atPlace = FileListPath.BuildBreadcrumbSegments(documents);
            Assert.Equal([FileListPath.ComputerDisplayName, "Documents"], atPlace.Select(segment => segment.Label));
            Assert.Equal(documents, atPlace[1].TargetPath);
            Assert.False(atPlace[0].ShowLeadingChevron);
            Assert.True(atPlace[1].ShowLeadingChevron);

            var nested = FileListPath.BuildBreadcrumbSegments(Path.Combine(documents, "Work"));
            Assert.Equal(
                [FileListPath.ComputerDisplayName, "Documents", "Work"],
                nested.Select(segment => segment.Label)
            );
        }

        /// <summary>
        /// Verifies This PC and Network are not OS folders that Explorer can open as a path.
        /// </summary>
        [Fact]
        public void IsFilesystemFolderPath_Rejects_Sentinels()
        {
            Assert.False(FileListPath.IsFilesystemFolderPath(FileListPath.ComputerPath));
            Assert.False(FileListPath.IsFilesystemFolderPath(" "));
            if (OperatingSystem.IsWindows())
            {
                Assert.False(FileListPath.IsFilesystemFolderPath(FileListPath.NetworkPath));
            }

            Assert.True(FileListPath.IsFilesystemFolderPath(@"C:\Music"));
        }

        /// <summary>
        /// Verifies listing labels use the last folder segment, including UNC shares.
        /// </summary>
        [Fact]
        public void DirectoryDisplayName_Uses_Last_Segment()
        {
            Assert.Equal("Music", FileListPath.DirectoryDisplayName(@"C:\Music"));
            Assert.Equal("share", FileListPath.LastUncSegment(@"\\server\share"));
            Assert.Equal("server", FileListPath.LastUncSegment(@"\\server"));
        }

        private static string? _ExistingSpecialFolder(Environment.SpecialFolder folder)
        {
            var path = Environment.GetFolderPath(folder);
            if (string.IsNullOrWhiteSpace(path) || !Directory.Exists(path))
            {
                return null;
            }

            return new DirectoryInfo(path).FullName;
        }
    }
}
