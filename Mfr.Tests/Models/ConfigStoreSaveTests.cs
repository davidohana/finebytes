using System.Text.Json;

namespace Mfr.Tests.Models
{
    /// <summary>
    /// Tests for <see cref="ConfigStore.Save"/>.
    /// </summary>
    [Collection(ConfigStoreCollection.Name)]
    public sealed class ConfigStoreSaveTests
    {
        [Fact]
        public void Save_round_trips_mutated_ui_and_file_list_leaves()
        {
            using var temp = ConfigStoreTempFile.CreateReady();
            ConfigStore.Ui.ConfirmationPrompts = ConfirmationPrompts.More;
            ConfigStore.FileList = new FileListPrefs { DoubleClickAddsToRenameList = true };
            ConfigStore.Save(temp.Path);

            Assert.True(File.Exists(temp.Path));
            using (var doc = JsonDocument.Parse(File.ReadAllText(temp.Path)))
            {
                Assert.Equal("more", doc.RootElement.GetProperty("ui").GetProperty("confirmationPrompts").GetString());
                Assert.True(
                    doc.RootElement.GetProperty("fileList").GetProperty("doubleClickAddsToRenameList").GetBoolean()
                );
                Assert.False(doc.RootElement.GetProperty("ui").TryGetProperty("doubleClickAddsToRenameList", out _));
                Assert.False(doc.RootElement.TryGetProperty("filters", out _));
            }

            ConfigStore.Load(temp.Path);
            Assert.Equal(ConfirmationPrompts.More, ConfigStore.Ui.ConfirmationPrompts);
            Assert.True(ConfigStore.FileList?.DoubleClickAddsToRenameList);
        }

        [Fact]
        public void Save_overwrites_existing_file()
        {
            using var temp = ConfigStoreTempFile.CreateWithContent(
                // lang=json,strict
                """
                {
                  "ui": {
                    "confirmationPrompts": "fewer"
                  },
                  "fileList": {
                    "doubleClickAddsToRenameList": false
                  }
                }
                """
            );
            ConfigStoreTestReset.LoadEmpty();
            ConfigStore.Ui.ConfirmationPrompts = ConfirmationPrompts.More;
            ConfigStore.FileList = new FileListPrefs { DoubleClickAddsToRenameList = true };
            ConfigStore.Save(temp.Path);

            using var doc = JsonDocument.Parse(File.ReadAllText(temp.Path));
            Assert.Equal("more", doc.RootElement.GetProperty("ui").GetProperty("confirmationPrompts").GetString());
            Assert.True(
                doc.RootElement.GetProperty("fileList").GetProperty("doubleClickAddsToRenameList").GetBoolean()
            );
        }

        [Fact]
        public void Save_creates_missing_directory()
        {
            using var temp = ConfigStoreTempFile.CreateUnderNewDirectory("nested", "config.json");
            ConfigStoreTestReset.LoadEmpty();
            ConfigStore.Ui.ConfirmationPrompts = ConfirmationPrompts.More;
            ConfigStore.FileList = new FileListPrefs { DoubleClickAddsToRenameList = true };
            ConfigStore.Save(temp.Path);

            Assert.True(File.Exists(temp.Path));
            ConfigStore.Load(temp.Path);
            Assert.Equal(ConfirmationPrompts.More, ConfigStore.Ui.ConfirmationPrompts);
            Assert.True(ConfigStore.FileList?.DoubleClickAddsToRenameList);
        }

        [Fact]
        public void Load_ignores_unknown_old_ui_presets_key()
        {
            using var temp = ConfigStoreTempFile.CreateWithContent(
                // lang=json,strict
                """
                {
                  "ui": {
                    "presets": {
                      "confirmReplaceAppliedFiltersOnLoad": "true"
                    },
                    "confirmationPrompts": "more",
                    "doubleClickAddsToRenameList": "true"
                  }
                }
                """
            );
            ConfigStore.Load(temp.Path);
            Assert.Equal(ConfirmationPrompts.More, ConfigStore.Ui.ConfirmationPrompts);
            Assert.Null(ConfigStore.FileList);
        }
    }
}
