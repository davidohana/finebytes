using Avalonia;
using Avalonia.Headless;

[assembly: AvaloniaTestApplication(typeof(Mfr.Tests.Ui.TestAppBuilder))]

namespace Mfr.Tests.Ui
{
    /// <summary>
    /// Headless Avalonia application builder for UI smoke tests.
    /// </summary>
    public sealed class TestAppBuilder
    {
        /// <summary>
        /// Configures the headless Avalonia app used by <c>AvaloniaFact</c> tests.
        /// </summary>
        /// <returns>The configured application builder.</returns>
        public static AppBuilder BuildAvaloniaApp()
        {
            return AppBuilder.Configure<App.Ui.App>().UseHeadless(new AvaloniaHeadlessPlatformOptions());
        }
    }
}
