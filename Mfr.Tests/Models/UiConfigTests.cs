namespace Mfr.Tests.Models
{
    /// <summary>
    /// Tests Rename List add-policy settings on <see cref="UiConfig"/>.
    /// </summary>
    public sealed class UiConfigTests
    {
        [Fact]
        public void Load_empty_config_keeps_add_policy_defaults()
        {
            var configPath = Path.Combine(Path.GetTempPath(), "mfr-test-ui-config-" + Guid.NewGuid() + ".json");
            File.WriteAllText(configPath, """{}""");
            ConfigStore.Load(configPath);

            Assert.Equal(RenameListAddMode.Files, ConfigStore.Config.Ui.AddMode);
            Assert.True(ConfigStore.Config.Ui.AddFolderContents);
            Assert.False(ConfigStore.Config.Ui.RenameListUseFixedWidthFont);
        }

        [Fact]
        public void Load_json_round_trips_add_policy()
        {
            var configPath = Path.Combine(Path.GetTempPath(), "mfr-test-ui-config-" + Guid.NewGuid() + ".json");
            File.WriteAllText(
                configPath,
                // lang=json,strict
                """
                {
                  "ui": {
                    "addMode": "folders",
                    "addFolderContents": "false"
                  }
                }
                """
            );
            ConfigStore.Load(configPath);

            Assert.Equal(RenameListAddMode.Folders, ConfigStore.Config.Ui.AddMode);
            Assert.False(ConfigStore.Config.Ui.AddFolderContents);
        }

        [Fact]
        public void Load_json_round_trips_rename_list_fixed_width_font()
        {
            var configPath = Path.Combine(Path.GetTempPath(), "mfr-test-ui-config-" + Guid.NewGuid() + ".json");
            File.WriteAllText(
                configPath,
                // lang=json,strict
                """
                {
                  "ui": {
                    "renameListUseFixedWidthFont": "true"
                  }
                }
                """
            );
            ConfigStore.Load(configPath);

            Assert.True(ConfigStore.Config.Ui.RenameListUseFixedWidthFont);
        }

        [Fact]
        public void ApplyCliOverrides_sets_add_policy()
        {
            var configPath = Path.Combine(Path.GetTempPath(), "mfr-test-ui-config-" + Guid.NewGuid() + ".json");
            File.WriteAllText(configPath, """{}""");
            ConfigStore.Load(configPath);

            ConfigStore.ApplyCliOverrides(["ui.addMode=filesAndFolders", "ui.addFolderContents=false"]);

            Assert.Equal(RenameListAddMode.FilesAndFolders, ConfigStore.Config.Ui.AddMode);
            Assert.False(ConfigStore.Config.Ui.AddFolderContents);
        }

        [Fact]
        public void ApplyCliOverrides_sets_rename_list_fixed_width_font()
        {
            var configPath = Path.Combine(Path.GetTempPath(), "mfr-test-ui-config-" + Guid.NewGuid() + ".json");
            File.WriteAllText(configPath, """{}""");
            ConfigStore.Load(configPath);

            ConfigStore.ApplyCliOverrides(["ui.renameListUseFixedWidthFont=true"]);

            Assert.True(ConfigStore.Config.Ui.RenameListUseFixedWidthFont);
        }
    }
}
