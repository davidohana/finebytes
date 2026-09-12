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
        public void Save_round_trips_mutated_confirm_replace_and_leaf()
        {
            var configPath = Path.Combine(Path.GetTempPath(), "mfr-test-save-config-" + Guid.NewGuid() + ".json");
            try
            {
                ConfigStoreTestReset.LoadEmpty();
                ConfigStore.Config.Ui.Presets.ConfirmReplaceAppliedFiltersOnLoad = true;
                ConfigStore.Config.Filters.MaxListFileLineLength = 2500;
                ConfigStore.Save(configPath);

                Assert.True(File.Exists(configPath));
                using (var doc = JsonDocument.Parse(File.ReadAllText(configPath)))
                {
                    Assert.Equal(
                        "true",
                        doc.RootElement.GetProperty("ui")
                            .GetProperty("presets")
                            .GetProperty("confirmReplaceAppliedFiltersOnLoad")
                            .GetString()
                    );
                    Assert.Equal(
                        "2500",
                        doc.RootElement.GetProperty("filters").GetProperty("maxListFileLineLength").GetString()
                    );
                }

                ConfigStore.Load(configPath);
                Assert.True(ConfigStore.Config.Ui.Presets.ConfirmReplaceAppliedFiltersOnLoad);
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
                    "presets": {
                      "confirmReplaceAppliedFiltersOnLoad": "false"
                    }
                  }
                }
                """
            );
            try
            {
                ConfigStoreTestReset.LoadEmpty();
                ConfigStore.Config.Ui.Presets.ConfirmReplaceAppliedFiltersOnLoad = true;
                ConfigStore.Save(configPath);

                using var doc = JsonDocument.Parse(File.ReadAllText(configPath));
                Assert.Equal(
                    "true",
                    doc.RootElement.GetProperty("ui")
                        .GetProperty("presets")
                        .GetProperty("confirmReplaceAppliedFiltersOnLoad")
                        .GetString()
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
                ConfigStore.Config.Ui.Presets.ConfirmReplaceAppliedFiltersOnLoad = true;
                ConfigStore.Save(configPath);

                Assert.True(File.Exists(configPath));
                ConfigStore.Load(configPath);
                Assert.True(ConfigStore.Config.Ui.Presets.ConfirmReplaceAppliedFiltersOnLoad);
            }
            finally
            {
                if (Directory.Exists(dir))
                {
                    Directory.Delete(dir, recursive: true);
                }
            }
        }
    }
}
