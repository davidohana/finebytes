namespace Mfr.Tests.Models
{
    /// <summary>
    /// Tests Rename List add-policy flags on <see cref="UiConfig"/>.
    /// </summary>
    public sealed class UiConfigTests
    {
        [Fact]
        public void Add_policy_defaults_match_MFR7()
        {
            var ui = new UiConfig();
            Assert.True(ui.AddFiles);
            Assert.False(ui.AddFolders);
            Assert.True(ui.AddFolderContents);
        }

        [Fact]
        public void Load_empty_config_keeps_add_policy_defaults()
        {
            var configPath = Path.Combine(Path.GetTempPath(), "mfr-test-ui-config-" + Guid.NewGuid() + ".json");
            File.WriteAllText(configPath, """{}""");
            ConfigStore.Load(configPath);

            Assert.True(ConfigStore.Config.Ui.AddFiles);
            Assert.False(ConfigStore.Config.Ui.AddFolders);
            Assert.True(ConfigStore.Config.Ui.AddFolderContents);
        }

        [Fact]
        public void Load_json_round_trips_add_policy_flags()
        {
            var configPath = Path.Combine(Path.GetTempPath(), "mfr-test-ui-config-" + Guid.NewGuid() + ".json");
            File.WriteAllText(
                configPath,
                // lang=json,strict
                """
                {
                  "ui": {
                    "addFiles": "false",
                    "addFolders": "true",
                    "addFolderContents": "false"
                  }
                }
                """
            );
            ConfigStore.Load(configPath);

            Assert.False(ConfigStore.Config.Ui.AddFiles);
            Assert.True(ConfigStore.Config.Ui.AddFolders);
            Assert.False(ConfigStore.Config.Ui.AddFolderContents);
        }

        [Fact]
        public void ApplyCliOverrides_sets_add_policy_flags()
        {
            var configPath = Path.Combine(Path.GetTempPath(), "mfr-test-ui-config-" + Guid.NewGuid() + ".json");
            File.WriteAllText(configPath, """{}""");
            ConfigStore.Load(configPath);

            ConfigStore.ApplyCliOverrides(["ui.addFiles=false", "ui.addFolders=true", "ui.addFolderContents=false"]);

            Assert.False(ConfigStore.Config.Ui.AddFiles);
            Assert.True(ConfigStore.Config.Ui.AddFolders);
            Assert.False(ConfigStore.Config.Ui.AddFolderContents);
        }
    }
}
