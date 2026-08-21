using Mfr.Utils;

namespace Mfr.Tests.Engine
{
    /// <summary>
    /// Tests path and on-disk helpers for diagnostic session and crash logs.
    /// </summary>
    public sealed class LogPathsTests : IDisposable
    {
        private readonly TempDirectoryFixture _tempDirectoryFixture = new();

        /// <summary>
        /// Restores temporary directories created by this fixture.
        /// </summary>
        public void Dispose()
        {
            _tempDirectoryFixture.Dispose();
        }

        [Fact]
        /// <summary>
        /// Verifies a blank override resolves to the LocalAppData diagnostic folder.
        /// </summary>
        public void ResolveDirectoryPath_Uses_Default_When_Blank()
        {
            var path = LogPaths.ResolveDirectoryPath(null);
            Assert.Contains(AppDataPaths.VendorDirectoryName, path, StringComparison.OrdinalIgnoreCase);
            Assert.Contains(AppDataPaths.ProductDirectoryName, path, StringComparison.OrdinalIgnoreCase);
            Assert.EndsWith("logs", path, StringComparison.OrdinalIgnoreCase);
            Assert.Equal(LogPaths.DefaultDirectoryPath, path);
        }

        [Fact]
        /// <summary>
        /// Verifies an explicit directory is trimmed and used as-is.
        /// </summary>
        public void ResolveDirectoryPath_Uses_Override()
        {
            var overridePath = _tempDirectoryFixture.CreateTempDir();
            var resolved = LogPaths.ResolveDirectoryPath("  " + overridePath + "  ");
            Assert.Equal(overridePath, resolved);
        }

        [Fact]
        /// <summary>
        /// Verifies session file names use the configured prefix, timestamp, and extension.
        /// </summary>
        public void CreateSessionFilePath_Uses_Prefix_And_Extension()
        {
            var directoryPath = _tempDirectoryFixture.CreateTempDir();
            var settings = new LogSettings { FilePrefix = "session-", FileExtension = ".log" };
            var path = LogPaths.CreateSessionFilePath(directoryPath, settings);
            var fileName = Path.GetFileName(path);
            Assert.StartsWith("session-", fileName, StringComparison.Ordinal);
            Assert.EndsWith(".log", fileName, StringComparison.Ordinal);
            Assert.Equal(directoryPath, Path.GetDirectoryName(path));
        }

        [Fact]
        /// <summary>
        /// Verifies retention pruning keeps only the newest configured session files.
        /// </summary>
        public void PruneSessionFiles_Keeps_Newest_Max()
        {
            var logDirectoryPath = _tempDirectoryFixture.CreateTempDir();
            var baseTime = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);

            for (var i = 0; i < 105; i++)
            {
                var logFilePath = logDirectoryPath.CombinePath($"session-{i:D3}.log");
                File.WriteAllText(logFilePath, $"log-{i:D3}");
                File.SetCreationTimeUtc(logFilePath, baseTime.AddMinutes(i));
            }

            LogPaths.PruneSessionFiles(
                logDirectoryPath: logDirectoryPath,
                maxSessionFiles: 100,
                sessionLogPrefix: "session-",
                sessionLogExtension: ".log");

            var remainingNames = Directory
                .EnumerateFiles(logDirectoryPath, "session-*.log", SearchOption.TopDirectoryOnly)
                .Select(Path.GetFileName)
                .OrderBy(name => name, StringComparer.Ordinal)
                .ToList();

            Assert.Equal(100, remainingNames.Count);
            Assert.DoesNotContain("session-000.log", remainingNames);
            Assert.DoesNotContain("session-004.log", remainingNames);
            Assert.Contains("session-104.log", remainingNames);
        }

        [Fact]
        /// <summary>
        /// Verifies files that do not match the session prefix are left in place.
        /// </summary>
        public void PruneSessionFiles_Does_Not_Delete_Unrelated_Files()
        {
            var logDirectoryPath = _tempDirectoryFixture.CreateTempDir();
            var sessionPath = logDirectoryPath.CombinePath("session-001.log");
            var otherPath = logDirectoryPath.CombinePath("other-001.log");
            File.WriteAllText(sessionPath, "session");
            File.WriteAllText(otherPath, "other");

            LogPaths.PruneSessionFiles(
                logDirectoryPath: logDirectoryPath,
                maxSessionFiles: 1,
                sessionLogPrefix: "session-",
                sessionLogExtension: ".log");

            Assert.True(File.Exists(sessionPath));
            Assert.True(File.Exists(otherPath));
        }

        [Fact]
        /// <summary>
        /// Verifies crash text includes the header, outer exception, and inner exception.
        /// </summary>
        public void FormatCrashText_Includes_Inner_Exception_And_Terminating_Header()
        {
            var exception = new InvalidOperationException(
                "outer boom",
                new ArgumentException("inner boom"));

            var text = LogPaths.FormatCrashText(exception);
            Assert.Contains("An unexpected error occurred.", text, StringComparison.Ordinal);
            Assert.Contains("Application will be terminated.", text, StringComparison.Ordinal);
            Assert.Contains("InvalidOperationException", text, StringComparison.Ordinal);
            Assert.Contains("outer boom", text, StringComparison.Ordinal);
            Assert.Contains("ArgumentException", text, StringComparison.Ordinal);
            Assert.Contains("inner boom", text, StringComparison.Ordinal);
        }

        [Fact]
        /// <summary>
        /// Verifies a best-effort crash file is written under the default log directory.
        /// </summary>
        public void TryWriteCrashFile_Writes_Formatted_Text_To_Default_Directory()
        {
            var exception = new InvalidOperationException("boom", new ArgumentException("inner"));
            string? crashFilePath = null;
            try
            {
                crashFilePath = LogPaths.TryWriteCrashFile(exception);

                Assert.NotNull(crashFilePath);
                Assert.True(File.Exists(crashFilePath));
                Assert.Equal(LogPaths.DefaultDirectoryPath, Path.GetDirectoryName(crashFilePath));
                Assert.StartsWith(
                    LogPaths.CrashFilePrefix,
                    Path.GetFileName(crashFilePath),
                    StringComparison.Ordinal);
                var content = File.ReadAllText(crashFilePath);
                Assert.Contains("boom", content, StringComparison.Ordinal);
                Assert.Contains("inner", content, StringComparison.Ordinal);
                Assert.Contains("terminated", content, StringComparison.OrdinalIgnoreCase);
            }
            finally
            {
                if (crashFilePath is not null && File.Exists(crashFilePath))
                    File.Delete(crashFilePath);
            }
        }
    }
}
