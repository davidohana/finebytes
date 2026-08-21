using Mfr.App.Ui.Services.FileExplorer;
using Mfr.Utils;

namespace Mfr.Tests.Ui
{
    /// <summary>
    /// Tests WSL UNC host detection and path rewriting.
    /// </summary>
    public sealed class WindowsWslUncTests
    {
        /// <summary>
        /// Verifies WSL hosts and the short <c>\\wsl</c> alias are recognized.
        /// </summary>
        [Fact]
        public void Detects_Wsl_Hosts_And_Ignores_Smb()
        {
            Assert.True(WindowsWslUnc.IsWslUncPath(@"\\wsl"));
            Assert.True(WindowsWslUnc.IsWslUncPath(@"\\wsl$"));
            Assert.True(WindowsWslUnc.IsWslUncPath(@"\\WSL$\Ubuntu"));
            Assert.True(WindowsWslUnc.IsWslUncPath(@"//wsl.localhost/Ubuntu/home"));
            Assert.True(WindowsWslUnc.IsWslUncPath(@"\\?\UNC\wsl$\Ubuntu"));
            Assert.True(WindowsWslUnc.IsWslServerRoot(@"\\wsl$"));
            Assert.True(WindowsWslUnc.IsWslServerRoot(@"\\wsl.localhost\"));
            Assert.False(WindowsWslUnc.IsWslServerRoot(@"\\wsl$\Ubuntu"));
            Assert.False(WindowsWslUnc.IsWslUncPath(@"\\nas\music"));
            Assert.False(WindowsWslUnc.IsWslUncPath(@"\\"));
            Assert.False(WindowsWslUnc.IsWslUncPath("C:\\Users"));
            Assert.False(WindowsWslUnc.IsWslUncPath("Network"));
        }

        /// <summary>
        /// Verifies <c>\\wsl</c> is rewritten onto the live root and explicit hosts are kept.
        /// </summary>
        [Fact]
        public void MapPath_Rewrites_Short_Alias_Onto_Live_Root()
        {
            Assert.True(WindowsWslUnc.TryMapPath(@"\\wsl", @"\\wsl$", out var mapped));
            Assert.Equal(@"\\wsl$", mapped);
            Assert.True(WindowsWslUnc.TryMapPath(@"\\wsl\Ubuntu\home", @"\\wsl.localhost", out mapped));
            Assert.Equal(@"\\wsl.localhost\Ubuntu\home", mapped);
            Assert.True(WindowsWslUnc.TryMapPath(@"//wsl$/Ubuntu", @"\\wsl.localhost", out mapped));
            Assert.Equal(@"\\wsl$\Ubuntu", mapped);
            Assert.True(WindowsWslUnc.TryMapPath(@"\\wsl.localhost", @"\\wsl$", out mapped));
            Assert.Equal(@"\\wsl.localhost", mapped);
            Assert.False(WindowsWslUnc.TryMapPath(@"\\nas", @"\\wsl$", out _));
        }

        /// <summary>
        /// Verifies a live WSL root is used when the redirector is present.
        /// </summary>
        [Fact]
        public void Resolve_Maps_Short_Alias_When_Wsl_Is_Present()
        {
            if (!OperatingSystem.IsWindows() || !WindowsWslUnc.TryGetLiveRoot(out var liveRoot))
                return;

            Assert.True(WindowsWslUnc.TryResolve(@"\\wsl", out var resolved));
            Assert.True(PathRelations.IsSamePath(liveRoot, resolved));
            Assert.True(WindowsWslUnc.TryResolve(liveRoot, out resolved));
            Assert.True(PathRelations.IsSamePath(liveRoot, resolved));
            Assert.True(WindowsWslUnc.TryListDistroPaths(liveRoot, out var distroPaths));
            Assert.NotEmpty(distroPaths);
        }

        /// <summary>
        /// Verifies WSL resolve stays off when the redirector is missing.
        /// </summary>
        [Fact]
        public void Resolve_Returns_False_Without_Live_Root()
        {
            if (OperatingSystem.IsWindows() && WindowsWslUnc.TryGetLiveRoot(out _))
                return;

            Assert.False(WindowsWslUnc.TryResolve(@"\\wsl", out _));
            Assert.False(WindowsWslUnc.TryResolve(@"\\wsl$", out _));
            Assert.False(WindowsWslUnc.TryGetLiveRoot(out _));
        }
    }
}
