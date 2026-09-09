using Mfr.Engine.Config;
using Mfr.Filters.Case;
using Mfr.Utils;

namespace Mfr.Tests.Engine
{
    /// <summary>
    /// Tests for <see cref="PersistedConfigurationReset"/>.
    /// </summary>
    public sealed class PersistedConfigurationResetTests : IDisposable
    {
        private readonly TempDirectoryFixture _tempDirectoryFixture = new();

        /// <inheritdoc />
        public void Dispose()
        {
            _tempDirectoryFixture.Dispose();
        }

        /// <summary>
        /// Verifies the three AppData-shaped files are deleted and presets are left alone.
        /// </summary>
        [Fact]
        public void DeleteAppDataFiles_removes_config_session_and_defaults_not_presets()
        {
            var dir = _tempDirectoryFixture.CreateTempDir();
            var configPath = dir.CombinePath("config.json");
            var sessionPath = dir.CombinePath("session.json");
            var defaultsPath = dir.CombinePath("filter-defaults.json");
            var presetsPath = dir.CombinePath("presets.json");

            File.WriteAllText(configPath, "{}");
            SessionStore.Save(new SessionState(), sessionPath);
            var defaults = new FilterDefaultsStore(defaultsPath);
            defaults.SetDefault(new LettersCaseFilter());
            File.WriteAllText(
                presetsPath, /*lang=json,strict*/
                """{"presets":[]}"""
            );

            PersistedConfigurationReset.DeleteAppDataFiles(configPath, sessionPath, defaultsPath);

            Assert.False(File.Exists(configPath));
            Assert.False(File.Exists(sessionPath));
            Assert.False(File.Exists(defaultsPath));
            Assert.True(File.Exists(presetsPath));
        }

        /// <summary>
        /// Verifies missing files do not throw.
        /// </summary>
        [Fact]
        public void DeleteAppDataFiles_missing_files_is_noop()
        {
            var dir = _tempDirectoryFixture.CreateTempDir();
            PersistedConfigurationReset.DeleteAppDataFiles(
                dir.CombinePath("config.json"),
                dir.CombinePath("session.json"),
                dir.CombinePath("filter-defaults.json")
            );
        }
    }
}
