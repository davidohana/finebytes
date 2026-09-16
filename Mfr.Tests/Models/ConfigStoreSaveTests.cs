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
        public void Save_round_trips_mutated_options_leaves()
        {
            using var temp = ConfigStoreTempFile.CreateReady();
            ConfigStore.Options.SuppressedConfirmations =
            [
                ConfirmationKind.ClearRenameList,
                ConfirmationKind.DeletePreset,
            ];
            ConfigStore.Options.DoubleClickAddsToRenameList = true;
            ConfigStore.Save(temp.Path);

            Assert.True(File.Exists(temp.Path));
            using (var doc = JsonDocument.Parse(File.ReadAllText(temp.Path)))
            {
                var options = doc.RootElement.GetProperty("options");
                var suppressed = options
                    .GetProperty("suppressedConfirmations")
                    .EnumerateArray()
                    .Select(e => e.GetString()!)
                    .ToArray();
                Assert.Equal(["clearRenameList", "deletePreset"], suppressed);
                Assert.Equal("true", options.GetProperty("doubleClickAddsToRenameList").GetString());
                Assert.False(doc.RootElement.TryGetProperty("fileList", out _));
                Assert.False(doc.RootElement.TryGetProperty("filters", out _));
            }

            ConfigStore.Load(temp.Path);
            Assert.Equal(
                [ConfirmationKind.ClearRenameList, ConfirmationKind.DeletePreset],
                ConfigStore.Options.SuppressedConfirmations
            );
            Assert.True(ConfigStore.Options.DoubleClickAddsToRenameList);
            Assert.Null(ConfigStore.FileList);
        }

        [Fact]
        public void Save_overwrites_existing_file()
        {
            using var temp = ConfigStoreTempFile.CreateWithContent(
                // lang=json,strict
                """
                {
                  "options": {
                    "suppressedConfirmations": ["goWithPreviewErrors"],
                    "doubleClickAddsToRenameList": "false"
                  }
                }
                """
            );
            ConfigStoreTestReset.LoadEmpty();
            ConfigStore.Options.SuppressedConfirmations = [ConfirmationKind.ClearRenameList];
            ConfigStore.Options.DoubleClickAddsToRenameList = true;
            ConfigStore.Save(temp.Path);

            using var doc = JsonDocument.Parse(File.ReadAllText(temp.Path));
            var options = doc.RootElement.GetProperty("options");
            var suppressed = options
                .GetProperty("suppressedConfirmations")
                .EnumerateArray()
                .Select(e => e.GetString()!)
                .ToArray();
            Assert.Equal(["clearRenameList"], suppressed);
            Assert.Equal("true", options.GetProperty("doubleClickAddsToRenameList").GetString());
        }

        [Fact]
        public void Save_creates_missing_directory()
        {
            using var temp = ConfigStoreTempFile.CreateUnderNewDirectory("nested", "config.json");
            ConfigStoreTestReset.LoadEmpty();
            ConfigStore.Options.SuppressedConfirmations = [ConfirmationKind.UndoRename];
            ConfigStore.Options.DoubleClickAddsToRenameList = true;
            ConfigStore.Save(temp.Path);

            Assert.True(File.Exists(temp.Path));
            ConfigStore.Load(temp.Path);
            Assert.Equal([ConfirmationKind.UndoRename], ConfigStore.Options.SuppressedConfirmations);
            Assert.True(ConfigStore.Options.DoubleClickAddsToRenameList);
        }

        [Fact]
        public void Load_ignores_unknown_old_ui_presets_and_confirmationPrompts()
        {
            using var temp = ConfigStoreTempFile.CreateWithContent(
                // lang=json,strict
                """
                {
                  "options": {
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
            Assert.Equal([ConfirmationKind.OverwritePreset], ConfigStore.Options.SuppressedConfirmations);
            Assert.True(ConfigStore.Options.DoubleClickAddsToRenameList);
            Assert.Null(ConfigStore.FileList);
        }
    }
}
