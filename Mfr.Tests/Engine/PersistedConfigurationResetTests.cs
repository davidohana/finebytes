using Mfr.Engine.Config;
using Mfr.Utils;

namespace Mfr.Tests.Engine
{
    /// <summary>
    /// Tests for <see cref="PersistedConfigurationReset"/>.
    /// </summary>
    [Collection(ConfigStoreCollection.Name)]
    public sealed class PersistedConfigurationResetTests
    {
        /// <summary>
        /// Verifies only <c>config.json</c> is deleted and presets are left alone.
        /// </summary>
        [Fact]
        public void Reset_removes_config_not_presets()
        {
            using var temp = ConfigStoreTempFile.CreateUnderNewDirectory("config.json");
            ConfigStoreTestReset.LoadEmpty();
            var dir = Path.GetDirectoryName(temp.Path)!;
            var presetsPath = dir.CombinePath("presets.json");

            ConfigStore.FileList = new FileListPrefs { FileMask = "*.mp3" };
            ConfigStore.Save(temp.Path);
            File.WriteAllText(
                presetsPath, /*lang=json,strict*/
                """{"presets":[]}"""
            );

            PersistedConfigurationReset.Reset(temp.Path);

            Assert.False(File.Exists(temp.Path));
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
            using var temp = ConfigStoreTempFile.Create();
            PersistedConfigurationReset.Reset(temp.Path);
        }
    }
}
