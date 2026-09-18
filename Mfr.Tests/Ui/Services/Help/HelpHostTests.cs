using Mfr.App.Ui.Services.Help;
using Mfr.App.Ui.Services.Shell;
using Mfr.Tests.TestSupport;

namespace Mfr.Tests.Ui.Services.Help
{
    /// <summary>
    /// Unit tests for <see cref="HelpHost"/> path resolution and open.
    /// </summary>
    public sealed class HelpHostTests
    {
        /// <summary>
        /// Verifies the host finds a help file under a configured root and opens it.
        /// </summary>
        [Fact]
        public void TryOpen_resolves_and_opens_existing_file()
        {
            TempHelpRoot.Run(
                (helpDir, opener, host) =>
                {
                    var helpFile = Path.Combine(helpDir, "SpaceCharacter.html");
                    Assert.True(host.TryOpen("SpaceCharacter.html", out var fullPath));
                    Assert.Equal(helpFile, fullPath);
                    Assert.Equal([helpFile], opener.OpenedWithDefaultApp);
                },
                "SpaceCharacter.html"
            );
        }

        /// <summary>
        /// Verifies missing help files do not open and return false.
        /// </summary>
        [Fact]
        public void TryOpen_returns_false_when_missing()
        {
            TempHelpRoot.Run(
                (_, opener, host) =>
                {
                    Assert.False(host.TryOpen("SpaceCharacter.html", out var fullPath));
                    Assert.Null(fullPath);
                    Assert.Empty(opener.OpenedWithDefaultApp);
                }
            );
        }

        /// <summary>
        /// Verifies path segments in the help file name are rejected.
        /// </summary>
        [Fact]
        public void TryResolve_rejects_path_segments()
        {
            var host = new HelpHost(NullFileShellOpener.Instance, [@"C:\nowhere"]);
            Assert.False(host.TryResolve(@"..\SpaceCharacter.html", out _));
            Assert.False(host.TryResolve(@"sub\SpaceCharacter.html", out _));
        }

        /// <summary>
        /// Verifies the missing-help message points at the app help folder.
        /// </summary>
        [Fact]
        public void FormatMissingHelpMessage_lists_default_roots()
        {
            var message = HelpHost.FormatMissingHelpMessage("SpaceCharacter.html");
            Assert.Contains("SpaceCharacter.html", message, StringComparison.Ordinal);
            Assert.Contains("application folder", message, StringComparison.OrdinalIgnoreCase);
            foreach (var root in HelpHost.DefaultHelpRoots)
            {
                Assert.Contains(root, message, StringComparison.OrdinalIgnoreCase);
            }
        }

        /// <summary>
        /// Verifies default roots are only the app-local <c>help/</c> folder.
        /// </summary>
        [Fact]
        public void DefaultHelpRoots_is_app_base_help_only()
        {
            Assert.Equal([Path.Combine(AppContext.BaseDirectory, "help")], HelpHost.DefaultHelpRoots);
        }
    }
}
