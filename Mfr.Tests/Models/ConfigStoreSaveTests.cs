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
        public void Save_round_trips_mutated_ui_leaves_and_filter_leaf()
        {
            var configPath = Path.Combine(Path.GetTempPath(), "mfr-test-save-config-" + Guid.NewGuid() + ".json");
            try
            {
                ConfigStoreTestReset.LoadEmpty();
                ConfigStore.Config.Ui.ConfirmationPrompts = ConfirmationPrompts.More;
                ConfigStore.Config.Ui.DoubleClickAddsToRenameList = true;
                ConfigStore.Config.Filters.MaxListFileLineLength = 2500;
                ConfigStore.Save(configPath);

                Assert.True(File.Exists(configPath));
                using (var doc = JsonDocument.Parse(File.ReadAllText(configPath)))
                {
                    Assert.Equal(
                        "more",
                        doc.RootElement.GetProperty("ui").GetProperty("confirmationPrompts").GetString()
                    );
                    Assert.Equal(
                        "true",
                        doc.RootElement.GetProperty("ui").GetProperty("doubleClickAddsToRenameList").GetString()
                    );
                    Assert.Equal(
                        "2500",
                        doc.RootElement.GetProperty("filters").GetProperty("maxListFileLineLength").GetString()
                    );
                }

                ConfigStore.Load(configPath);
                Assert.Equal(ConfirmationPrompts.More, ConfigStore.Config.Ui.ConfirmationPrompts);
                Assert.True(ConfigStore.Config.Ui.DoubleClickAddsToRenameList);
                Assert.Equal(2500, ConfigStore.Config.Filters.MaxListFileLineLength);
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
                    "confirmationPrompts": "fewer",
                    "doubleClickAddsToRenameList": "false"
                  }
                }
                """
            );
            try
            {
                ConfigStoreTestReset.LoadEmpty();
                ConfigStore.Config.Ui.ConfirmationPrompts = ConfirmationPrompts.More;
                ConfigStore.Config.Ui.DoubleClickAddsToRenameList = true;
                ConfigStore.Save(configPath);

                using var doc = JsonDocument.Parse(File.ReadAllText(configPath));
                Assert.Equal("more", doc.RootElement.GetProperty("ui").GetProperty("confirmationPrompts").GetString());
                Assert.Equal(
                    "true",
                    doc.RootElement.GetProperty("ui").GetProperty("doubleClickAddsToRenameList").GetString()
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
                ConfigStore.Config.Ui.ConfirmationPrompts = ConfirmationPrompts.More;
                ConfigStore.Config.Ui.DoubleClickAddsToRenameList = true;
                ConfigStore.Save(configPath);

                Assert.True(File.Exists(configPath));
                ConfigStore.Load(configPath);
                Assert.Equal(ConfirmationPrompts.More, ConfigStore.Config.Ui.ConfirmationPrompts);
                Assert.True(ConfigStore.Config.Ui.DoubleClickAddsToRenameList);
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
                    "confirmationPrompts": "more"
                  }
                }
                """
            );
            try
            {
                ConfigStore.Load(configPath);
                Assert.Equal(ConfirmationPrompts.More, ConfigStore.Config.Ui.ConfirmationPrompts);
                Assert.False(ConfigStore.Config.Ui.DoubleClickAddsToRenameList);
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
