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
            ConfigStore.Ui.SuppressedConfirmations = [ConfirmationKind.ClearRenameList, ConfirmationKind.DeletePreset];
            ConfigStore.FileList = new FileListPrefs { DoubleClickAddsToRenameList = true };
            ConfigStore.Save(temp.Path);

            Assert.True(File.Exists(temp.Path));
            using (var doc = JsonDocument.Parse(File.ReadAllText(temp.Path)))
            {
                var suppressed = doc
                    .RootElement.GetProperty("ui")
                    .GetProperty("suppressedConfirmations")
                    .EnumerateArray()
                    .Select(e => e.GetString()!)
                    .ToArray();
                Assert.Equal(["clearRenameList", "deletePreset"], suppressed);
                Assert.True(
                    doc.RootElement.GetProperty("fileList").GetProperty("doubleClickAddsToRenameList").GetBoolean()
                );
                Assert.False(doc.RootElement.GetProperty("ui").TryGetProperty("doubleClickAddsToRenameList", out _));
                Assert.False(doc.RootElement.TryGetProperty("filters", out _));
            }

            ConfigStore.Load(temp.Path);
            Assert.Equal(
                [ConfirmationKind.ClearRenameList, ConfirmationKind.DeletePreset],
                ConfigStore.Ui.SuppressedConfirmations
            );
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
                    "suppressedConfirmations": ["goWithPreviewErrors"]
                  },
                  "fileList": {
                    "doubleClickAddsToRenameList": false
                  }
                }
                """
            );
            ConfigStoreTestReset.LoadEmpty();
            ConfigStore.Ui.SuppressedConfirmations = [ConfirmationKind.ClearRenameList];
            ConfigStore.FileList = new FileListPrefs { DoubleClickAddsToRenameList = true };
            ConfigStore.Save(temp.Path);

            using var doc = JsonDocument.Parse(File.ReadAllText(temp.Path));
            var suppressed = doc
                .RootElement.GetProperty("ui")
                .GetProperty("suppressedConfirmations")
                .EnumerateArray()
                .Select(e => e.GetString()!)
                .ToArray();
            Assert.Equal(["clearRenameList"], suppressed);
            Assert.True(
                doc.RootElement.GetProperty("fileList").GetProperty("doubleClickAddsToRenameList").GetBoolean()
            );
        }

        [Fact]
        public void Save_creates_missing_directory()
        {
            using var temp = ConfigStoreTempFile.CreateUnderNewDirectory("nested", "config.json");
            ConfigStoreTestReset.LoadEmpty();
            ConfigStore.Ui.SuppressedConfirmations = [ConfirmationKind.UndoRename];
            ConfigStore.FileList = new FileListPrefs { DoubleClickAddsToRenameList = true };
            ConfigStore.Save(temp.Path);

            Assert.True(File.Exists(temp.Path));
            ConfigStore.Load(temp.Path);
            Assert.Equal([ConfirmationKind.UndoRename], ConfigStore.Ui.SuppressedConfirmations);
            Assert.True(ConfigStore.FileList?.DoubleClickAddsToRenameList);
        }

        [Fact]
        public void Load_ignores_unknown_old_ui_presets_and_confirmationPrompts()
        {
            using var temp = ConfigStoreTempFile.CreateWithContent(
                // lang=json,strict
                """
                {
                  "ui": {
                    "presets": {
                      "confirmReplaceFilterChainOnLoad": "true"
                    },
                    "confirmationPrompts": "more",
                    "doubleClickAddsToRenameList": "true",
                    "suppressedConfirmations": ["overwritePreset"]
                  }
                }
                """
            );
            ConfigStore.Load(temp.Path);
            Assert.Equal([ConfirmationKind.OverwritePreset], ConfigStore.Ui.SuppressedConfirmations);
            Assert.Null(ConfigStore.FileList);
        }
    }
}
