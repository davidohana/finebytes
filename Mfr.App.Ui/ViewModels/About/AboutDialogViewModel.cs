using CommunityToolkit.Mvvm.Input;
using Mfr.App.Ui.Services;
using Mfr.App.Ui.Services.Shell;

namespace Mfr.App.Ui.ViewModels.About
{
    /// <summary>
    /// View model for the Help → About dialog (product identity and support links).
    /// </summary>
    /// <param name="shellOpener">
    /// Opens URLs. When null, uses <see cref="FileShellOpener.CreateDefault"/>.
    /// </param>
    public sealed partial class AboutDialogViewModel(IFileShellOpener? shellOpener = null) : ViewModelBase
    {
        private readonly IFileShellOpener _shellOpener = shellOpener ?? FileShellOpener.CreateDefault();

        /// <summary>
        /// Gets the product display name.
        /// </summary>
        public string ProductName { get; } = AppProductInfo.GetProductName();

        /// <summary>
        /// Gets the user-facing version string (same source as the main window title).
        /// </summary>
        public string DisplayVersion { get; } = AppProductInfo.GetDisplayVersion();

        /// <summary>
        /// Gets the time-limited beta expiry notice shown below the version.
        /// </summary>
        public string BetaExpiryNotice { get; } = AppProductInfo.GetBetaExpiryNotice();

        /// <summary>
        /// Gets the assembly copyright notice.
        /// </summary>
        public string Copyright { get; } = AppProductInfo.GetCopyright();

        /// <summary>
        /// Opens the product web site in the default browser.
        /// </summary>
        [RelayCommand]
        public void OpenWebSite()
        {
            _shellOpener.OpenWithDefaultApp(AppProductInfo.WebSiteUrl);
        }

        /// <summary>
        /// Opens a mail compose window for support.
        /// </summary>
        [RelayCommand]
        public void OpenSupportEmail()
        {
            _shellOpener.OpenWithDefaultApp(AppProductInfo.SupportEmailUrl);
        }
    }
}
