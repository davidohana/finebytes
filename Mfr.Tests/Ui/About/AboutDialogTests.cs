using Avalonia.Controls;
using Avalonia.Headless.XUnit;
using Mfr.App.Ui.ViewModels.About;
using Mfr.App.Ui.Views.About;

namespace Mfr.Tests.Ui.About
{
    /// <summary>
    /// Headless smoke tests for the About dialog shell and splash artwork.
    /// </summary>
    public sealed class AboutDialogTests
    {
        /// <summary>
        /// Verifies About constructs, shows, and loads the MFR splash image resource.
        /// </summary>
        [AvaloniaFact]
        public void AboutDialog_Constructs_With_Splash_Image()
        {
            var dialog = new AboutDialog(new AboutDialogViewModel());
            dialog.Show();
            dialog.UpdateLayout();

            Assert.True(dialog.IsVisible);
            Assert.Equal("About", dialog.Title);

            var splash = dialog.FindControl<Image>("SplashImage");
            Assert.NotNull(splash);
            Assert.NotNull(splash.Source);

            dialog.Close();
        }
    }
}
