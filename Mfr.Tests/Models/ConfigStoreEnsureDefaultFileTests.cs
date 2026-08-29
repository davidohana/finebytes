namespace Mfr.Tests.Models
{
    /// <summary>
    /// Tests for <see cref="ConfigStore.EnsureDefaultFile"/>.
    /// </summary>
    public sealed class ConfigStoreEnsureDefaultFileTests
    {
        [Fact]
        public void EnsureDefaultFile_creates_missing_file_with_defaults()
        {
            var emptyPath = Path.Combine(Path.GetTempPath(), "mfr-test-empty-config-" + Guid.NewGuid() + ".json");
            var configPath = Path.Combine(Path.GetTempPath(), "mfr-test-ensure-config-" + Guid.NewGuid() + ".json");
            File.WriteAllText(emptyPath, """{}""");
            try
            {
                ConfigStore.Load(emptyPath);
                ConfigStore.EnsureDefaultFile(configPath);

                Assert.True(File.Exists(configPath));
                ConfigStore.Load(configPath);
                Assert.Equal(1000, ConfigStore.Config.Filters.MaxListFileLineLength);
                Assert.Equal(100, ConfigStore.Config.Log.MaxSessionFiles);
                Assert.Equal(string.Empty, ConfigStore.Config.Log.DirectoryPath);
                Assert.Equal("session-", ConfigStore.Config.Log.FilePrefix);
            }
            finally
            {
                File.Delete(emptyPath);
                File.Delete(configPath);
            }
        }

        [Fact]
        public void EnsureDefaultFile_does_not_overwrite_existing_file()
        {
            var configPath = Path.Combine(Path.GetTempPath(), "mfr-test-ensure-config-" + Guid.NewGuid() + ".json");
            File.WriteAllText(
                configPath,
                // lang=json,strict
                """
                {
                  "filters": {
                    "maxListFileLineLength": "2500"
                  }
                }
                """
            );
            try
            {
                ConfigStore.EnsureDefaultFile(configPath);
                ConfigStore.Load(configPath);
                Assert.Equal(2500, ConfigStore.Config.Filters.MaxListFileLineLength);
            }
            finally
            {
                File.Delete(configPath);
            }
        }
    }
}
