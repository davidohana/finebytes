namespace Mfr.Tests.Models
{
    /// <summary>
    /// Tests session preferences stored on main-window, File List, and Rename List sections.
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
                ConfigStore.Session = new SessionState
                {
                    MainWindow = new SessionStateMainWindow { RememberWindowState = false },
                    FileList = new SessionStateFileList { RememberLastFolder = false },
                    RenameList = new SessionStateRenameList
                    {
                        AddMode = RenameListAddMode.Folders,
                        AddFolderContents = false,
                        UseFixedWidthFont = true,
                        PreviewEnabled = false,
                    },
                };
                ConfigStore.Save(path);
                ConfigStore.Load(path);

                var loaded = ConfigStore.Session;
                Assert.False(loaded.MainWindow?.RememberWindowState);
                Assert.False(loaded.FileList?.RememberLastFolder);
                Assert.Equal(RenameListAddMode.Folders, loaded.RenameList?.AddMode);
                Assert.False(loaded.RenameList?.AddFolderContents);
                Assert.True(loaded.RenameList?.UseFixedWidthFont);
                Assert.False(loaded.RenameList?.PreviewEnabled);
            }
            finally
            {
                File.Delete(path);
                ConfigStoreTestReset.LoadEmpty();
            }
        }

        [Fact]
        public void Load_json_reads_rename_list_add_mode()
        {
            var path = Path.Combine(Path.GetTempPath(), "mfr-test-pref-session-" + Guid.NewGuid() + ".json");
            File.WriteAllText(
                path,
                // lang=json,strict
                """
                {
                  "session": {
                    "renameList": {
                      "addMode": "filesAndFolders",
                      "addFolderContents": false,
                      "useFixedWidthFont": true,
                      "previewEnabled": false
                    }
                  }
                }
                """
            );
            try
            {
                ConfigStore.Load(path);

                var session = ConfigStore.Session;
                Assert.Equal(RenameListAddMode.FilesAndFolders, session.RenameList?.AddMode);
                Assert.False(session.RenameList?.AddFolderContents);
                Assert.True(session.RenameList?.UseFixedWidthFont);
                Assert.False(session.RenameList?.PreviewEnabled);
            }
            finally
            {
                File.Delete(path);
                ConfigStoreTestReset.LoadEmpty();
            }
        }

        [Fact]
        public void TrySave_writes_session()
        {
            var path = Path.Combine(Path.GetTempPath(), "mfr-test-pref-session-" + Guid.NewGuid() + ".json");
            try
            {
                ConfigStoreTestReset.LoadEmpty();
                ConfigStore.Session = new SessionState();
                ConfigStore.Session.EnsureRenameList().AddMode = RenameListAddMode.Folders;
                ConfigStore.Session.EnsureRenameList().UseFixedWidthFont = true;
                ConfigStore.TrySave(path);

                ConfigStore.Load(path);
                Assert.Equal(RenameListAddMode.Folders, ConfigStore.Session.RenameList?.AddMode);
                Assert.True(ConfigStore.Session.RenameList?.UseFixedWidthFont);
            }
            finally
            {
                File.Delete(path);
                ConfigStoreTestReset.LoadEmpty();
            }
        }
    }
}
