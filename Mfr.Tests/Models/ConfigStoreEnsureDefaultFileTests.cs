using System.Text.Json;

namespace Mfr.Tests.Models
{
    /// <summary>
    /// Tests for <see cref="ConfigStore.EnsureDefaultFile"/>.
    /// </summary>
    [Collection(ConfigStoreCollection.Name)]
    public sealed class ConfigStoreEnsureDefaultFileTests
    {
        [Fact]
        public void EnsureDefaultFile_creates_missing_file_with_defaults()
        {
            var emptyPath = Path.Combine(Path.GetTempPath(), "mfr-test-empty-config-" + Guid.NewGuid() + ".json");
            var configPath = Path.Combine(Path.GetTempPath(), "mfr-test-ensure-config-" + Guid.NewGuid() + ".json");
            File.WriteAllText(emptyPath, """{}""");
            try
            {
                ConfigStore.Load(emptyPath);
                ConfigStore.EnsureDefaultFile(configPath);

                Assert.True(File.Exists(configPath));
                using var doc = JsonDocument.Parse(File.ReadAllText(configPath));
                Assert.Equal(
                    "1000",
                    doc.RootElement.GetProperty("filters").GetProperty("maxListFileLineLength").GetString()
                );
                Assert.Equal("100", doc.RootElement.GetProperty("log").GetProperty("maxSessionFiles").GetString());
                Assert.Equal(string.Empty, doc.RootElement.GetProperty("log").GetProperty("directoryPath").GetString());
                Assert.Equal("session-", doc.RootElement.GetProperty("log").GetProperty("filePrefix").GetString());
                Assert.Equal(
                    "false",
                    doc.RootElement.GetProperty("ui")
                        .GetProperty("presets")
                        .GetProperty("confirmReplaceAppliedFiltersOnLoad")
                        .GetString()
                );
            }
            finally
            {
                File.Delete(emptyPath);
                File.Delete(configPath);
            }
        }

        [Fact]
        public void EnsureDefaultFile_does_not_overwrite_existing_file()
        {
            var configPath = Path.Combine(Path.GetTempPath(), "mfr-test-ensure-config-" + Guid.NewGuid() + ".json");
            File.WriteAllText(
                configPath,
                // lang=json,strict
                """
                {
                  "filters": {
                    "maxListFileLineLength": "2500"
                  }
                }
                """
            );
            try
            {
                ConfigStore.EnsureDefaultFile(configPath);
                using var doc = JsonDocument.Parse(File.ReadAllText(configPath));
                Assert.Equal(
                    "2500",
                    doc.RootElement.GetProperty("filters").GetProperty("maxListFileLineLength").GetString()
                );
                Assert.False(doc.RootElement.TryGetProperty("log", out _));
            }
            finally
            {
                File.Delete(configPath);
            }
        }
    }
}
