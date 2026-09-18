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
            var builder = AppBuilder.Configure<App.Ui.App>();

            // Real Skia pixels for help screenshot capture; default headless drawing for tests.
            if (Environment.GetEnvironmentVariable("MFR_CAPTURE_HELP_SCREENSHOTS") == "1")
            {
                return builder
                    .UseSkia()
                    .UseHeadless(new AvaloniaHeadlessPlatformOptions { UseHeadlessDrawing = false });
            }

            return builder.UseHeadless(new AvaloniaHeadlessPlatformOptions());
        }
    }
}
