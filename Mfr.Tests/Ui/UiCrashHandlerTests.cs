using Mfr.App.Ui.Diagnostics;
using Serilog.Events;

namespace Mfr.Tests.Ui
{
    /// <summary>
    /// Tests crash persistence and process-handler registration.
    /// </summary>
    [Collection(SessionLogCollection.Name)]
    public sealed class UiCrashHandlerTests : IDisposable
    {
        private readonly TempDirectoryFixture _tempDirectoryFixture = new();

        /// <summary>
        /// Restores temporary directories created by this fixture.
        /// </summary>
        public void Dispose()
        {
            LogSession.Shutdown();
            ConfigLoader.Load();
            _tempDirectoryFixture.Dispose();
        }

        [Fact]
        /// <summary>
        /// Verifies process handlers can be registered more than once.
        /// </summary>
        public void RegisterProcessHandlers_Is_Idempotent()
        {
            UiCrashHandler.RegisterProcessHandlers();
            UiCrashHandler.RegisterProcessHandlers();
        }

        [Fact]
        /// <summary>
        /// Verifies a crash file is written when Serilog is not running.
        /// </summary>
        public void Persist_WritesCrashFile_WhenLoggingNotStarted()
        {
            LogSession.Shutdown();
            var logDirectoryPath = _tempDirectoryFixture.CreateTempDir();
            ConfigLoader.Load();
            ConfigLoader.Settings.Log.DirectoryPath = logDirectoryPath;
            var exception = new InvalidOperationException("boom", new ArgumentException("inner"));

            var report = UiCrashHandler.Persist(exception, isTerminating: true);

            Assert.NotNull(report.LogFilePath);
            Assert.True(File.Exists(report.LogFilePath));
            Assert.Equal(logDirectoryPath, report.LogDirectoryPath);
            Assert.Contains("boom", report.Details, StringComparison.Ordinal);
            Assert.Contains("inner", report.Details, StringComparison.Ordinal);
            var content = File.ReadAllText(report.LogFilePath);
            Assert.Contains("terminated", content, StringComparison.OrdinalIgnoreCase);
        }

        [Fact]
        /// <summary>
        /// Verifies an active session log is used instead of a separate crash file.
        /// </summary>
        public void Persist_UsesSessionFile_WhenLoggingStarted()
        {
            var logDirectoryPath = _tempDirectoryFixture.CreateTempDir();
            LogSession.Start(
                logLevel: LogEventLevel.Information,
                logSettings: new LogSettings { DirectoryPath = logDirectoryPath });
            var sessionLogFilePath = LogSession.LogFilePath;
            var sessionLogDirectoryPath = LogSession.LogDirectoryPath;

            var report = UiCrashHandler.Persist(
                new InvalidOperationException("boom"),
                isTerminating: false);

            Assert.Equal(sessionLogFilePath, report.LogFilePath);
            Assert.Equal(sessionLogDirectoryPath, report.LogDirectoryPath);
            LogSession.Shutdown();
            Assert.NotNull(sessionLogFilePath);
            var content = File.ReadAllText(sessionLogFilePath);
            Assert.Contains("boom", content, StringComparison.Ordinal);
            var crashFiles = Directory.EnumerateFiles(
                logDirectoryPath,
                "crash-*.log",
                SearchOption.TopDirectoryOnly);
            Assert.Empty(crashFiles);
        }
    }
}
