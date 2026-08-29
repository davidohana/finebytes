namespace Mfr.Tests.Models
{
    /// <summary>
    /// Tests for <see cref="ConfigStore.Save"/>.
    /// </summary>
    public sealed class ConfigStoreSaveTests
    {
        [Fact]
        public void Save_merge_preserves_unrelated_sections_and_round_trips_ui_bool()
        {
            var configPath = Path.Combine(Path.GetTempPath(), "mfr-test-config-save-" + Guid.NewGuid() + ".json");
            File.WriteAllText(
                configPath,
                // lang=json,strict
                """
                {
                  "filters": {
                    "maxListFileLineLength": "2500"
                  },
                  "log": {
                    "maxSessionFiles": "77"
                  },
                  "ui": {
                    "addMode": "folders",
                    "rememberWindowState": "true"
                  }
                }
                """
            );

            ConfigStore.Load(configPath);
            ConfigStore.Config.Ui.RenameListUseFixedWidthFont = true;
            ConfigStore.Save(configPath);

            var savedJson = File.ReadAllText(configPath);
            using var doc = System.Text.Json.JsonDocument.Parse(savedJson);
            var root = doc.RootElement;

            Assert.Equal("2500", root.GetProperty("filters").GetProperty("maxListFileLineLength").GetString());
            Assert.Equal("77", root.GetProperty("log").GetProperty("maxSessionFiles").GetString());
            Assert.Equal("folders", root.GetProperty("ui").GetProperty("addMode").GetString());
            Assert.Equal("true", root.GetProperty("ui").GetProperty("rememberWindowState").GetString());
            Assert.Equal("true", root.GetProperty("ui").GetProperty("renameListUseFixedWidthFont").GetString());

            ConfigStore.Load(configPath);
            Assert.True(ConfigStore.Config.Ui.RenameListUseFixedWidthFont);
            Assert.Equal(RenameListAddMode.Folders, ConfigStore.Config.Ui.AddMode);
            Assert.Equal(2500, ConfigStore.Config.Filters.MaxListFileLineLength);
            Assert.Equal(77, ConfigStore.Config.Log.MaxSessionFiles);
        }

        [Fact]
        public void Save_creates_file_when_missing()
        {
            var configPath = Path.Combine(Path.GetTempPath(), "mfr-test-config-save-" + Guid.NewGuid() + ".json");
            File.WriteAllText(configPath, """{}""");
            ConfigStore.Load(configPath);
            ConfigStore.Config.Ui.RenameListUseFixedWidthFont = true;
            ConfigStore.Save(configPath);

            Assert.True(File.Exists(configPath));
            ConfigStore.Load(configPath);
            Assert.True(ConfigStore.Config.Ui.RenameListUseFixedWidthFont);
        }
    }
}
