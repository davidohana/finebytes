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
            var configPath = Path.Combine(Path.GetTempPath(), "mfr-test-save-config-" + Guid.NewGuid() + ".json");
            try
            {
                ConfigStoreTestReset.LoadEmpty();
                ConfigStore.Ui.ConfirmationPrompts = ConfirmationPrompts.More;
                ConfigStore.FileList = new FileListPrefs { DoubleClickAddsToRenameList = true };
                ConfigStore.Save(configPath);

                Assert.True(File.Exists(configPath));
                using (var doc = JsonDocument.Parse(File.ReadAllText(configPath)))
                {
                    Assert.Equal(
                        "more",
                        doc.RootElement.GetProperty("ui").GetProperty("confirmationPrompts").GetString()
                    );
                    Assert.True(
                        doc.RootElement.GetProperty("fileList").GetProperty("doubleClickAddsToRenameList").GetBoolean()
                    );
                    Assert.False(
                        doc.RootElement.GetProperty("ui").TryGetProperty("doubleClickAddsToRenameList", out _)
                    );
                    Assert.False(doc.RootElement.TryGetProperty("filters", out _));
                }

                ConfigStore.Load(configPath);
                Assert.Equal(ConfirmationPrompts.More, ConfigStore.Ui.ConfirmationPrompts);
                Assert.True(ConfigStore.FileList?.DoubleClickAddsToRenameList);
            }
            finally
            {
                if (File.Exists(configPath))
                {
                    File.Delete(configPath);
                }
            }
        }

        [Fact]
        public void Save_overwrites_existing_file()
        {
            var configPath = Path.Combine(Path.GetTempPath(), "mfr-test-save-overwrite-" + Guid.NewGuid() + ".json");
            File.WriteAllText(
                configPath,
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
            try
            {
                ConfigStoreTestReset.LoadEmpty();
                ConfigStore.Ui.ConfirmationPrompts = ConfirmationPrompts.More;
                ConfigStore.FileList = new FileListPrefs { DoubleClickAddsToRenameList = true };
                ConfigStore.Save(configPath);

                using var doc = JsonDocument.Parse(File.ReadAllText(configPath));
                Assert.Equal("more", doc.RootElement.GetProperty("ui").GetProperty("confirmationPrompts").GetString());
                Assert.True(
                    doc.RootElement.GetProperty("fileList").GetProperty("doubleClickAddsToRenameList").GetBoolean()
                );
            }
            finally
            {
                if (File.Exists(configPath))
                {
                    File.Delete(configPath);
                }
            }
        }

        [Fact]
        public void Save_creates_missing_directory()
        {
            var dir = Path.Combine(Path.GetTempPath(), "mfr-test-save-dir-" + Guid.NewGuid());
            var configPath = Path.Combine(dir, "nested", "config.json");
            try
            {
                ConfigStoreTestReset.LoadEmpty();
                ConfigStore.Ui.ConfirmationPrompts = ConfirmationPrompts.More;
                ConfigStore.FileList = new FileListPrefs { DoubleClickAddsToRenameList = true };
                ConfigStore.Save(configPath);

                Assert.True(File.Exists(configPath));
                ConfigStore.Load(configPath);
                Assert.Equal(ConfirmationPrompts.More, ConfigStore.Ui.ConfirmationPrompts);
                Assert.True(ConfigStore.FileList?.DoubleClickAddsToRenameList);
            }
            finally
            {
                if (Directory.Exists(dir))
                {
                    Directory.Delete(dir, recursive: true);
                }
            }
        }

        [Fact]
        public void Load_ignores_unknown_old_ui_presets_key()
        {
            var configPath = Path.Combine(
                Path.GetTempPath(),
                "mfr-test-save-legacy-presets-" + Guid.NewGuid() + ".json"
            );
            File.WriteAllText(
                configPath,
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
            try
            {
                ConfigStore.Load(configPath);
                Assert.Equal(ConfirmationPrompts.More, ConfigStore.Ui.ConfirmationPrompts);
                Assert.Null(ConfigStore.FileList);
            }
            finally
            {
                if (File.Exists(configPath))
                {
                    File.Delete(configPath);
                }

                ConfigStoreTestReset.LoadEmpty();
            }
        }
    }
}
