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
            using var temp = ConfigStoreTempFile.CreateReady();
            ConfigStore.EnsureDefaultFile(temp.Path);

            Assert.True(File.Exists(temp.Path));
            using var doc = JsonDocument.Parse(File.ReadAllText(temp.Path));
            Assert.False(doc.RootElement.TryGetProperty("filters", out _));
            Assert.Equal("100", doc.RootElement.GetProperty("log").GetProperty("maxSessionFiles").GetString());
            Assert.Equal(string.Empty, doc.RootElement.GetProperty("log").GetProperty("directoryPath").GetString());
            Assert.Equal("session-", doc.RootElement.GetProperty("log").GetProperty("filePrefix").GetString());
            Assert.Empty(
                doc.RootElement.GetProperty("options").GetProperty("suppressedConfirmations").EnumerateArray().ToArray()
            );
            Assert.Equal("true", doc.RootElement.GetProperty("options").GetProperty("rememberWindowState").GetString());
            Assert.Equal("true", doc.RootElement.GetProperty("options").GetProperty("rememberLastFolder").GetString());
            Assert.Equal(
                "false",
                doc.RootElement.GetProperty("options").GetProperty("doubleClickAddsToRenameList").GetString()
            );
            Assert.Equal("files", doc.RootElement.GetProperty("options").GetProperty("addMode").GetString());
            Assert.Equal("true", doc.RootElement.GetProperty("options").GetProperty("addFolderContents").GetString());
            Assert.Equal("10", doc.RootElement.GetProperty("renameLog").GetProperty("limit").GetString());
            Assert.False(doc.RootElement.GetProperty("options").TryGetProperty("confirmationPrompts", out _));
            Assert.False(doc.RootElement.GetProperty("options").TryGetProperty("presets", out _));
            Assert.False(doc.RootElement.TryGetProperty("session", out _));
            Assert.False(doc.RootElement.TryGetProperty("mainWindow", out _));
            Assert.False(doc.RootElement.TryGetProperty("fileList", out _));
            Assert.False(doc.RootElement.TryGetProperty("filterDefaults", out _));
        }

        [Fact]
        public void EnsureDefaultFile_does_not_overwrite_existing_file()
        {
            using var temp = ConfigStoreTempFile.CreateWithContent(
                // lang=json,strict
                """
                {
                  "log": {
                    "maxSessionFiles": "50"
                  }
                }
                """
            );
            ConfigStore.EnsureDefaultFile(temp.Path);
            using var doc = JsonDocument.Parse(File.ReadAllText(temp.Path));
            Assert.Equal("50", doc.RootElement.GetProperty("log").GetProperty("maxSessionFiles").GetString());
            Assert.False(doc.RootElement.TryGetProperty("options", out _));
        }
    }
}
