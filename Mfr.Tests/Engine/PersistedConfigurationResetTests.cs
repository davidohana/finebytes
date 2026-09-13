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
        public void DeleteAppDataFiles_removes_config_not_presets()
        {
            var dir = _tempDirectoryFixture.CreateTempDir();
            var configPath = dir.CombinePath("config.json");
            var presetsPath = dir.CombinePath("presets.json");

            ConfigStoreTestReset.LoadEmpty();
            ConfigStore.Session = new SessionState { FileList = new SessionStateFileList { FileMask = "*.mp3" } };
            ConfigStore.FilterDefaultsJson = new System.Text.Json.Nodes.JsonObject
            {
                ["LettersCase"] = new System.Text.Json.Nodes.JsonObject(),
            };
            ConfigStore.Save(configPath);
            File.WriteAllText(
                presetsPath, /*lang=json,strict*/
                """{"presets":[]}"""
            );

            PersistedConfigurationReset.DeleteAppDataFiles(configPath);

            Assert.False(File.Exists(configPath));
            Assert.True(File.Exists(presetsPath));
            Assert.Null(ConfigStore.Session.FileList);
            Assert.Empty(ConfigStore.FilterDefaultsJson);
        }

        /// <summary>
        /// Verifies missing files do not throw.
        /// </summary>
        [Fact]
        public void DeleteAppDataFiles_missing_files_is_noop()
        {
            var dir = _tempDirectoryFixture.CreateTempDir();
            PersistedConfigurationReset.DeleteAppDataFiles(dir.CombinePath("config.json"));
        }
    }
}
