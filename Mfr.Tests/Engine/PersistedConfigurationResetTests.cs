using Mfr.Engine.Config;
using Mfr.Utils;

namespace Mfr.Tests.Engine
{
    /// <summary>
    /// Tests for <see cref="PersistedConfigurationReset"/>.
    /// </summary>
    [Collection(ConfigStoreCollection.Name)]
    public sealed class PersistedConfigurationResetTests : IDisposable
    {
        private readonly TempDirectoryFixture _tempDirectoryFixture = new();

        /// <inheritdoc />
        public void Dispose()
        {
            ConfigStoreTestReset.LoadEmpty();
            _tempDirectoryFixture.Dispose();
        }

        /// <summary>
        /// Verifies only <c>config.json</c> is deleted and presets are left alone.
        /// </summary>
        [Fact]
        public void Reset_removes_config_not_presets()
        {
            var dir = _tempDirectoryFixture.CreateTempDir();
            var configPath = dir.CombinePath("config.json");
            var presetsPath = dir.CombinePath("presets.json");

            ConfigStoreTestReset.LoadEmpty();
            ConfigStore.FileList = new FileListPrefs { FileMask = "*.mp3" };
            ConfigStore.Save(configPath);
            File.WriteAllText(
                presetsPath, /*lang=json,strict*/
                """{"presets":[]}"""
            );

            PersistedConfigurationReset.Reset(configPath);

            Assert.False(File.Exists(configPath));
            Assert.True(File.Exists(presetsPath));
            // In-memory prefs are intentionally left alone; UI exits after delete.
            Assert.NotNull(ConfigStore.FileList);
        }

        /// <summary>
        /// Verifies a missing prefs file does not throw.
        /// </summary>
        [Fact]
        public void Reset_missing_file_is_noop()
        {
            var dir = _tempDirectoryFixture.CreateTempDir();

            PersistedConfigurationReset.Reset(dir.CombinePath("config.json"));
        }
    }
}
