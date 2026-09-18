namespace Mfr.Tests.Engine
{
    /// <summary>
    /// Tests for <see cref="ConfigStore.DeleteDefaultFile"/>.
    /// </summary>
    [Collection(ConfigStoreCollection.Name)]
    public sealed class ConfigStoreDeleteDefaultFileTests
    {
        /// <summary>
        /// Verifies a missing file is a no-op.
        /// </summary>
        [Fact]
        public void DeleteDefaultFile_missing_is_noop()
        {
            using var temp = ConfigStoreTempFile.Create();
            ConfigStore.DeleteDefaultFile(temp.Path);
            Assert.False(File.Exists(temp.Path));
        }

        /// <summary>
        /// Verifies an existing config file is removed.
        /// </summary>
        [Fact]
        public void DeleteDefaultFile_removes_existing_file()
        {
            using var temp = ConfigStoreTempFile.CreateWithContent("{}");
            Assert.True(File.Exists(temp.Path));
            ConfigStore.DeleteDefaultFile(temp.Path);
            Assert.False(File.Exists(temp.Path));
        }

        /// <summary>
        /// Verifies only <c>config.json</c> is deleted and presets are left alone.
        /// </summary>
        [Fact]
        public void DeleteDefaultFile_removes_config_not_presets()
        {
            using var temp = ConfigStoreTempFile.CreateUnderNewDirectory("config.json");
            ConfigStoreTestReset.LoadEmpty();
            var dir = Path.GetDirectoryName(temp.Path)!;
            var presetsPath = Path.Combine(dir, "presets.json");

            ConfigStore.FileList = new FileListPrefs { FileMask = "*.mp3" };
            ConfigStore.Save(temp.Path);
            File.WriteAllText(
                presetsPath, /*lang=json,strict*/
                """{"presets":[]}"""
            );

            ConfigStore.DeleteDefaultFile(temp.Path);

            Assert.False(File.Exists(temp.Path));
            Assert.True(File.Exists(presetsPath));
            // In-memory prefs are intentionally left alone; UI exits after delete.
            Assert.NotNull(ConfigStore.FileList);
        }
    }
}
