using Mfr.App.Ui.ViewModels;

namespace Mfr.Tests.Ui
{
    /// <summary>
    /// Tests UNC and Network location rules used by the File Explorer pane.
    /// </summary>
    public sealed class ExplorerPathTests
    {
        /// <summary>
        /// Verifies Network, <c>\\</c>, and <c>//</c> are the Network sentinel on Windows.
        /// </summary>
        [Fact]
        public void IsNetworkPath_Accepts_Aliases_On_Windows()
        {
            if (!OperatingSystem.IsWindows())
                return;

            Assert.True(ExplorerPath.IsNetworkPath(ExplorerPath.NetworkDisplayName));
            Assert.True(ExplorerPath.IsNetworkPath("network"));
            Assert.True(ExplorerPath.IsNetworkPath(@"\\"));
            Assert.True(ExplorerPath.IsNetworkPath("//"));
            Assert.False(ExplorerPath.IsNetworkPath(@"\\nas\music"));
            Assert.False(ExplorerPath.IsNetworkPath(ExplorerPath.ComputerDisplayName));
        }

        /// <summary>
        /// Verifies UNC shares are detected, including forward slashes and long-path form.
        /// </summary>
        [Fact]
        public void IsUncPath_Detects_Share_Paths()
        {
            Assert.True(ExplorerPath.IsUncPath(@"\\ohanas"));
            Assert.True(ExplorerPath.IsUncPath(@"\\nas\music\albums"));
            Assert.True(ExplorerPath.IsUncPath(@"//nas/music"));
            Assert.True(ExplorerPath.IsUncPath(@"\\?\UNC\nas\music"));
            Assert.False(ExplorerPath.IsUncPath(@"\\?\C:\Users"));
            Assert.False(ExplorerPath.IsUncPath(@"C:\Users"));
            Assert.False(ExplorerPath.IsUncPath("Network"));
        }

        /// <summary>
        /// Verifies share-root parsing stops at <c>\\server\share</c>.
        /// </summary>
        [Fact]
        public void TryGetUncShareRoot_Reads_Server_And_Share()
        {
            Assert.True(ExplorerPath.TryGetUncShareRoot(@"\\nas\music\albums\live", out var root));
            Assert.Equal(@"\\nas\music", root);
            Assert.True(ExplorerPath.IsUncShareRoot(@"\\nas\music"));
            Assert.False(ExplorerPath.IsUncShareRoot(@"\\nas\music\albums"));
            Assert.False(ExplorerPath.TryGetUncShareRoot(@"\\nas", out _));
            Assert.True(ExplorerPath.TryGetUncServerRoot(@"\\nas\music\albums", out var server));
            Assert.Equal(@"\\nas", server);
            Assert.True(ExplorerPath.IsUncServerRoot(@"\\nas"));
            Assert.True(ExplorerPath.IsUncServerRoot(@"//ohanas"));
            Assert.False(ExplorerPath.IsUncServerRoot(@"\\nas\music"));
            if (OperatingSystem.IsWindows())
                Assert.True(ExplorerPath.IsUncServerRoot(@"\\nas\"));
            if (OperatingSystem.IsWindows())
                Assert.True(ExplorerPath.IsUncShareRoot(@"\\nas\music\"));
        }

        /// <summary>
        /// Verifies Go Up from a UNC share opens the server, then Network.
        /// </summary>
        [Fact]
        public void GetParentPath_Unc_Walks_Server_Then_Network()
        {
            if (!OperatingSystem.IsWindows())
                return;

            Assert.Equal(@"\\nas", ExplorerPath.GetParentPath(@"\\nas\music"));
            Assert.Equal(@"\\nas", ExplorerPath.GetParentPath(@"\\nas\music\"));
            Assert.Equal(@"\\nas\music", ExplorerPath.GetParentPath(@"\\nas\music\albums"));
            Assert.Equal(ExplorerPath.NetworkPath, ExplorerPath.GetParentPath(@"\\nas"));
            Assert.Equal(ExplorerPath.ComputerPath, ExplorerPath.GetParentPath(ExplorerPath.NetworkPath));
            Assert.Null(ExplorerPath.GetParentPath(ExplorerPath.ComputerPath));
        }

        /// <summary>
        /// Verifies UNC breadcrumbs start at Network and include the share root.
        /// </summary>
        [Fact]
        public void Breadcrumb_Unc_Starts_At_Network()
        {
            if (!OperatingSystem.IsWindows())
                return;

            var segments = ExplorerPath.BuildBreadcrumbSegments(@"\\nas\music\albums");
            Assert.Equal(
                [
                    ExplorerPath.NetworkDisplayName,
                    "nas",
                    "music",
                    "albums",
                ],
                segments.Select(segment => segment.Label));
            Assert.Equal(ExplorerPath.NetworkDisplayName, segments[0].TargetPath);
            Assert.Equal(@"\\nas", segments[1].TargetPath);
            Assert.Equal(@"\\nas\music", segments[2].TargetPath);
            Assert.False(segments[0].ShowLeadingChevron);
            Assert.True(segments[1].ShowLeadingChevron);
            Assert.True(segments[^1].ShowLeadingChevron);
        }

        /// <summary>
        /// Verifies the Network sentinel is a single breadcrumb.
        /// </summary>
        [Fact]
        public void Breadcrumb_Network_Is_Single_Segment()
        {
            if (!OperatingSystem.IsWindows())
                return;

            var root = Assert.Single(ExplorerPath.BuildBreadcrumbSegments(ExplorerPath.NetworkPath));
            Assert.Equal(ExplorerPath.NetworkDisplayName, root.Label);
            Assert.Equal(ExplorerPath.NetworkDisplayName, root.TargetPath);
            Assert.False(root.ShowLeadingChevron);
        }

        /// <summary>
        /// Verifies a UNC server root is Network plus the computer name.
        /// </summary>
        [Fact]
        public void Breadcrumb_Unc_Server_Shows_Host_Name()
        {
            if (!OperatingSystem.IsWindows())
                return;

            var segments = ExplorerPath.BuildBreadcrumbSegments(@"\\ohanas");
            Assert.Equal(
                [ExplorerPath.NetworkDisplayName, "ohanas"],
                segments.Select(segment => segment.Label));
            Assert.Equal(@"\\ohanas", segments[1].TargetPath);
        }
    }
}
