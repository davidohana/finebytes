using Serilog;
using Serilog.Events;

namespace Mfr.Tests.Engine
{
    /// <summary>
    /// Tests Serilog file-session bootstrap shared by CLI and UI.
    /// </summary>
    [Collection(SessionLogCollection.Name)]
    public sealed class LogSessionTests : IDisposable
    {
        private readonly TempDirectoryFixture _tempDirectoryFixture = new();

        /// <summary>
        /// Restores temporary directories created by this fixture.
        /// </summary>
        public void Dispose()
        {
            LogSession.Shutdown();
            _tempDirectoryFixture.Dispose();
        }

        [Fact]
        /// <summary>
        /// Verifies Start creates a session file and assigns <see cref="Log.Logger"/>.
        /// </summary>
        public void Start_Creates_PerSession_LogFile()
        {
            var logDirectoryPath = _tempDirectoryFixture.CreateTempDir();

            LogSession.Start(
                logLevel: LogEventLevel.Information,
                logDirectoryPath: logDirectoryPath,
                logSettings: new LogSettings());
            var logFilePath = LogSession.LogFilePath;
            Log.Information("hello from session log");
            LogSession.Shutdown();

            Assert.NotNull(logFilePath);
            Assert.True(File.Exists(logFilePath));
            var content = File.ReadAllText(logFilePath);
            Assert.Contains("hello from session log", content, StringComparison.Ordinal);
        }

        [Fact]
        /// <summary>
        /// Verifies Start prunes old session files after creating the new one.
        /// </summary>
        public void Start_Prunes_Old_Session_Files()
        {
            var logDirectoryPath = _tempDirectoryFixture.CreateTempDir();
            var settings = new LogSettings { MaxSessionFiles = 3 };
            var baseTime = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);
            for (var i = 0; i < 5; i++)
            {
                var existingPath = Path.Combine(logDirectoryPath, $"session-{i:D3}.log");
                File.WriteAllText(existingPath, $"log-{i:D3}");
                File.SetCreationTimeUtc(existingPath, baseTime.AddMinutes(i));
            }

            LogSession.Start(
                logLevel: LogEventLevel.Information,
                logDirectoryPath: logDirectoryPath,
                logSettings: settings);
            Assert.True(File.Exists(LogSession.LogFilePath));
            LogSession.Shutdown();

            var remaining = Directory
                .EnumerateFiles(logDirectoryPath, "session-*.log", SearchOption.TopDirectoryOnly)
                .Select(Path.GetFileName)
                .OrderBy(name => name, StringComparer.Ordinal)
                .ToList();
            Assert.Equal(3, remaining.Count);
            Assert.DoesNotContain("session-000.log", remaining);
            Assert.DoesNotContain("session-001.log", remaining);
        }
    }
}
