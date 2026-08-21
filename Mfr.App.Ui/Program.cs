using Avalonia;
using Mfr.App.Ui.Diagnostics;
using Mfr.Engine.Logging;
using Mfr.Models.Config;
using Serilog.Events;

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
        public static int Main(string[] args)
        {
            UiCrashHandler.RegisterProcessHandlers();
            try
            {
                ConfigLoader.Load();
                LogSession.Start(
                    logLevel: LogEventLevel.Information,
                    logDirectoryPath: null,
                    logSettings: ConfigLoader.Settings.Log);
                BuildAvaloniaApp().StartWithClassicDesktopLifetime(args);
                return 0;
            }
            catch (Exception ex)
            {
                UiCrashHandler.Report(ex, isTerminating: true);
                return 1;
            }
            finally
            {
                LogSession.Shutdown();
            }
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
