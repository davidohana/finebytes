namespace Mfr.Tests.Models
{
    /// <summary>
    /// Tests Rename List add-policy settings on <see cref="UiConfig"/>.
    /// </summary>
    public sealed class UiConfigTests
    {
        [Fact]
        public void Add_policy_defaults_match_MFR7()
        {
            var ui = new UiConfig();
            Assert.Equal(RenameListAddMode.Files, ui.AddMode);
            Assert.True(ui.AddFolderContents);
            Assert.Equal(RenameListSortKey.Default, ui.RenameListSortFields);
        }

        [Fact]
        public void Load_empty_config_keeps_add_policy_defaults()
        {
            var configPath = Path.Combine(Path.GetTempPath(), "mfr-test-ui-config-" + Guid.NewGuid() + ".json");
            File.WriteAllText(configPath, """{}""");
            ConfigStore.Load(configPath);

            Assert.Equal(RenameListAddMode.Files, ConfigStore.Config.Ui.AddMode);
            Assert.True(ConfigStore.Config.Ui.AddFolderContents);
            Assert.Equal(RenameListSortKey.Default, ConfigStore.Config.Ui.RenameListSortFields);
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
                    "addFolderContents": "false",
                    "renameListSortFields": "fullFileName:desc"
                  }
                }
                """
            );
            ConfigStore.Load(configPath);

            Assert.Equal(RenameListAddMode.Folders, ConfigStore.Config.Ui.AddMode);
            Assert.False(ConfigStore.Config.Ui.AddFolderContents);
            Assert.Equal("fullFileName:desc", ConfigStore.Config.Ui.RenameListSortFields);
        }

        [Fact]
        public void ApplyCliOverrides_sets_add_policy()
        {
            var configPath = Path.Combine(Path.GetTempPath(), "mfr-test-ui-config-" + Guid.NewGuid() + ".json");
            File.WriteAllText(configPath, """{}""");
            ConfigStore.Load(configPath);

            ConfigStore.ApplyCliOverrides([
                "ui.addMode=filesAndFolders",
                "ui.addFolderContents=false",
                "ui.renameListSortFields=parentFolder",
            ]);

            Assert.Equal(RenameListAddMode.FilesAndFolders, ConfigStore.Config.Ui.AddMode);
            Assert.False(ConfigStore.Config.Ui.AddFolderContents);
            Assert.Equal("parentFolder", ConfigStore.Config.Ui.RenameListSortFields);
        }

        [Fact]
        public void SortKey_parses_default_and_desc()
        {
            var keys = RenameListSortKey.Parse(RenameListSortKey.Default);
            Assert.Equal(
                [
                    new RenameListSortKey(RenameListSortColumn.FileFolder),
                    new RenameListSortKey(RenameListSortColumn.FullPath),
                ],
                keys
            );

            Assert.Equal(
                [new RenameListSortKey(RenameListSortColumn.ParentFolder, Descending: true)],
                RenameListSortKey.Parse("parentFolder:desc")
            );
            Assert.Empty(RenameListSortKey.Parse(string.Empty));
            Assert.Equal("FileFolder,FullPath", RenameListSortKey.Format(keys));
        }
    }
}
