using Avalonia.Headless.XUnit;
using Mfr.App.Ui.Services.FileList;

namespace Mfr.Tests.Ui
{
    /// <summary>
    /// Tests for <see cref="ISystemIconProvider"/> OS integrations.
    /// </summary>
    public sealed class SystemIconProviderTests
    {
        /// <summary>
        /// Verifies large shell icons are supported on Windows (regression for NotImplementedException).
        /// </summary>
        [AvaloniaFact]
        public void CreateDefault_Large_Does_Not_Throw_On_Windows()
        {
            if (!OperatingSystem.IsWindows())
            {
                return;
            }

            var provider = SystemIconProvider.CreateDefault();
            var profile = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);

            var exception = Record.Exception(() => provider.GetIcon(profile, isDirectory: true, ShellIconSize.Large));

            Assert.Null(exception);
        }

        /// <summary>
        /// Verifies jumbo shell icons are supported on Windows for Thumbnails view.
        /// </summary>
        [AvaloniaFact]
        public void CreateDefault_Jumbo_Does_Not_Throw_On_Windows()
        {
            if (!OperatingSystem.IsWindows())
            {
                return;
            }

            var provider = SystemIconProvider.CreateDefault();
            var profile = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);

            var exception = Record.Exception(() => provider.GetIcon(profile, isDirectory: true, ShellIconSize.Jumbo));

            Assert.Null(exception);
        }
    }
}
