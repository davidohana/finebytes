using Avalonia;

namespace Mfr.App.Ui
{
    /// <summary>
    /// Desktop entry point for the Magic File Renamer GUI.
    /// </summary>
    internal static class Program
    {
        // Initialization code. Do not use Avalonia, third-party APIs, or any
        // SynchronizationContext-reliant code before AppMain is called.
        [STAThread]
        public static void Main(string[] args)
        {
            BuildAvaloniaApp().StartWithClassicDesktopLifetime(args);
        }

        /// <summary>
        /// Configures the Avalonia builder. Also used by the visual designer.
        /// </summary>
        /// <returns>The configured application builder.</returns>
        public static AppBuilder BuildAvaloniaApp()
        {
            return AppBuilder.Configure<App>()
                .UsePlatformDetect()
                .WithInterFont()
                .LogToTrace();
        }
    }
}
