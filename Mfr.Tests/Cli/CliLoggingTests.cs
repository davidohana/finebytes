using Mfr.Cli;
using Mfr.Tests.TestSupport;
using Serilog;
using Serilog.Events;

namespace Mfr.Tests.Cli
{
    /// <summary>
    /// Tests Serilog bootstrap and session log retention behavior.
    /// </summary>
    public class CliLoggingTests : IDisposable
    {
        private readonly TempDirectoryFixture _tempDirectoryFixture = new();

        /// <summary>
        /// Restores process-level environment variables and temporary resources.
        /// </summary>
        public void Dispose()
        {
            _tempDirectoryFixture.Dispose();
        }

        [Fact]
        /// <summary>
        /// Verifies that startup creates one session file in the configured log directory.
        /// </summary>
        public void Start_Creates_PerSession_LogFile()
        {
            var logDirectoryPath = _tempDirectoryFixture.CreateTempDir();

            string logFilePath;
            using (var loggerSession = CliLogging.Start(LogEventLevel.Information, logDirectoryPath))
            {
                logFilePath = loggerSession.LogFilePath;
                Log.Information("hello from test");
            }

            Assert.True(File.Exists(logFilePath));
            var content = File.ReadAllText(logFilePath);
            Assert.Contains("hello from test", content, StringComparison.Ordinal);
        }

        [Fact]
        /// <summary>
        /// Verifies retention pruning keeps only the newest configured session files.
        /// </summary>
        public void PruneSessionLogFiles_Keeps_Newest_Max()
        {
            var logDirectoryPath = _tempDirectoryFixture.CreateTempDir();
            var baseTime = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);

            for (var i = 0; i < 105; i++)
            {
                var logFilePath = Path.Combine(logDirectoryPath, $"session-{i:D3}.log");
                File.WriteAllText(logFilePath, $"log-{i:D3}");
                File.SetCreationTimeUtc(logFilePath, baseTime.AddMinutes(i));
            }

            CliLogging.PruneSessionLogFiles(logDirectoryPath, CliLogging.MaxSessionLogFiles);

            var remainingNames = Directory
                .EnumerateFiles(logDirectoryPath, "session-*.log", SearchOption.TopDirectoryOnly)
                .Select(Path.GetFileName)
                .OrderBy(name => name, StringComparer.Ordinal)
                .ToList();

            Assert.Equal(CliLogging.MaxSessionLogFiles, remainingNames.Count);
            Assert.DoesNotContain("session-000.log", remainingNames);
            Assert.DoesNotContain("session-001.log", remainingNames);
            Assert.DoesNotContain("session-002.log", remainingNames);
            Assert.DoesNotContain("session-003.log", remainingNames);
            Assert.DoesNotContain("session-004.log", remainingNames);
            Assert.Contains("session-104.log", remainingNames);
        }
    }
}
