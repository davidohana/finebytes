using Mfr.App.Cli;
using Serilog;
using Serilog.Events;

namespace Mfr.Tests.Cli
{
    /// <summary>
    /// Tests CLI Serilog bootstrap (console sink on top of the shared file session).
    /// </summary>
    [Collection(SessionLogCollection.Name)]
    public class CliLoggingTests : IDisposable
    {
        private readonly TempDirectoryFixture _tempDirectoryFixture = new();

        /// <summary>
        /// Resets <see cref="ConfigStore"/> via <see cref="ConfigStore.Load()"/> before each test class instance.
        /// </summary>
        public CliLoggingTests()
        {
            ConfigStore.Load();
        }

        /// <summary>
        /// Restores process-level logging and temporary resources.
        /// </summary>
        public void Dispose()
        {
            LogSession.Shutdown();

            ConfigStore.Load();

            _tempDirectoryFixture.Dispose();
        }

        [Fact]
        /// <summary>
        /// Verifies startup creates a session file under <see cref="LogConfig.DirectoryPath"/>.
        /// </summary>
        public void Start_Creates_PerSession_LogFile()
        {
            var logDirectoryPath = _tempDirectoryFixture.CreateTempDir();

            ConfigStore.Log.DirectoryPath = logDirectoryPath;

            CliLogging.Start(LogEventLevel.Information);

            var logFilePath = LogSession.LogFilePath;

            Log.Information("hello from test");

            LogSession.Shutdown();

            Assert.NotNull(logFilePath);

            Assert.True(File.Exists(logFilePath));

            Assert.Equal(logDirectoryPath, Path.GetDirectoryName(logFilePath));

            var content = File.ReadAllText(logFilePath);

            Assert.Contains("hello from test", content, StringComparison.Ordinal);
        }
    }
}
