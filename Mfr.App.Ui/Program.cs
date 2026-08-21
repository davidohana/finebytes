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
                ConfigStore.Load();
                LogSession.Start(
                    logLevel: LogEventLevel.Information,
                    logConfig: ConfigStore.Config.Log);
                return BuildAvaloniaApp().StartWithClassicDesktopLifetime(args);
            }
            catch (Exception ex)
            {
                UiCrashHandler.Report(ex);
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
                .LogToTrace()
                .AfterSetup(_ => UiCrashHandler.RegisterDispatcherHandler());
        }
    }
}
