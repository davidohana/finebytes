using System.Text.Json;

namespace Mfr.Tests.Models
{
    /// <summary>
    /// Tests Options vs UI session ownership on <see cref="ConfigStore"/> sections.
    /// </summary>
    [Collection(ConfigStoreCollection.Name)]
    public sealed class SessionPreferenceTests
    {
        [Fact]
        public void Save_and_Load_round_trips_preferences_on_owning_sections()
        {
            var path = Path.Combine(Path.GetTempPath(), "mfr-test-pref-session-" + Guid.NewGuid() + ".json");
            try
            {
                ConfigStoreTestReset.LoadEmpty();
                ConfigStore.Options.RememberWindowState = false;
                ConfigStore.Options.RememberLastFolder = false;
                ConfigStore.Options.AddMode = RenameListAddMode.Folders;
                ConfigStore.Options.AddFolderContents = false;
                ConfigStore.RenameList = new RenameListPrefs { UseFixedWidthFont = true, PreviewEnabled = false };
                ConfigStore.Save(path);
                ConfigStore.Load(path);

                Assert.False(ConfigStore.Options.RememberWindowState);
                Assert.False(ConfigStore.Options.RememberLastFolder);
                Assert.Equal(RenameListAddMode.Folders, ConfigStore.Options.AddMode);
                Assert.False(ConfigStore.Options.AddFolderContents);
                Assert.True(ConfigStore.RenameList?.UseFixedWidthFont);
                Assert.False(ConfigStore.RenameList?.PreviewEnabled);
            }
            finally
            {
                File.Delete(path);
                ConfigStoreTestReset.LoadEmpty();
            }
        }

        [Fact]
        public void Load_json_reads_options_add_mode()
        {
            var path = Path.Combine(Path.GetTempPath(), "mfr-test-pref-session-" + Guid.NewGuid() + ".json");
            File.WriteAllText(
                path,
                // lang=json,strict
                """
                {
                  "options": {
                    "addMode": "filesAndFolders",
                    "addFolderContents": "false"
                  },
                  "renameList": {
                    "useFixedWidthFont": true,
                    "previewEnabled": false
                  }
                }
                """
            );
            try
            {
                ConfigStore.Load(path);

                Assert.Equal(RenameListAddMode.FilesAndFolders, ConfigStore.Options.AddMode);
                Assert.False(ConfigStore.Options.AddFolderContents);
                Assert.True(ConfigStore.RenameList?.UseFixedWidthFont);
                Assert.False(ConfigStore.RenameList?.PreviewEnabled);
            }
            finally
            {
                File.Delete(path);
                ConfigStoreTestReset.LoadEmpty();
            }
        }

        [Fact]
        public void Load_ignores_obsolete_options_keys_under_session_sections()
        {
            var path = Path.Combine(Path.GetTempPath(), "mfr-test-pref-obsolete-" + Guid.NewGuid() + ".json");
            File.WriteAllText(
                path,
                // lang=json,strict
                """
                {
                  "fileList": {
                    "rememberLastFolder": false,
                    "doubleClickAddsToRenameList": true,
                    "fileMask": "*.flac"
                  },
                  "renameList": {
                    "addMode": "folders",
                    "addFolderContents": false,
                    "useFixedWidthFont": false
                  }
                }
                """
            );
            try
            {
                ConfigStore.Load(path);

                Assert.True(ConfigStore.Options.RememberLastFolder);
                Assert.False(ConfigStore.Options.DoubleClickAddsToRenameList);
                Assert.Equal(RenameListAddMode.Files, ConfigStore.Options.AddMode);
                Assert.True(ConfigStore.Options.AddFolderContents);
                Assert.Equal("*.flac", ConfigStore.FileList?.FileMask);
                Assert.False(ConfigStore.RenameList?.UseFixedWidthFont);
            }
            finally
            {
                File.Delete(path);
                ConfigStoreTestReset.LoadEmpty();
            }
        }

        [Fact]
        public void TrySave_writes_options_and_session()
        {
            var path = Path.Combine(Path.GetTempPath(), "mfr-test-pref-session-" + Guid.NewGuid() + ".json");
            try
            {
                ConfigStoreTestReset.LoadEmpty();
                ConfigStore.Options.AddMode = RenameListAddMode.Folders;
                var renameList = ConfigStore.EnsureRenameList();
                renameList.UseFixedWidthFont = true;
                ConfigStore.TrySave(path);

                ConfigStore.Load(path);
                Assert.Equal(RenameListAddMode.Folders, ConfigStore.Options.AddMode);
                Assert.True(ConfigStore.RenameList?.UseFixedWidthFont);

                using var doc = JsonDocument.Parse(File.ReadAllText(path));
                Assert.Equal("folders", doc.RootElement.GetProperty("options").GetProperty("addMode").GetString());
            }
            finally
            {
                File.Delete(path);
                ConfigStoreTestReset.LoadEmpty();
            }
        }
    }
}
