namespace Mfr.Tests.Models
{
    /// <summary>
    /// Tests session preferences stored on main-window, File List, and Rename List sections.
    /// </summary>
    public sealed class SessionPreferenceTests
    {
        [Fact]
        public void Save_and_Load_round_trips_preferences_on_owning_sections()
        {
            var path = Path.Combine(Path.GetTempPath(), "mfr-test-pref-session-" + Guid.NewGuid() + ".json");
            try
            {
                var original = new SessionState
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

                SessionStore.Save(original, path);
                var loaded = SessionStore.Load(path);

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
                  "renameList": {
                    "addMode": "filesAndFolders",
                    "addFolderContents": false,
                    "useFixedWidthFont": true,
                    "previewEnabled": false
                  }
                }
                """
            );
            try
            {
                var session = SessionStore.Load(path);

                Assert.Equal(RenameListAddMode.FilesAndFolders, session.RenameList?.AddMode);
                Assert.False(session.RenameList?.AddFolderContents);
                Assert.True(session.RenameList?.UseFixedWidthFont);
                Assert.False(session.RenameList?.PreviewEnabled);
            }
            finally
            {
                File.Delete(path);
            }
        }

        [Fact]
        public void TrySave_writes_session()
        {
            var path = Path.Combine(Path.GetTempPath(), "mfr-test-pref-session-" + Guid.NewGuid() + ".json");
            try
            {
                var original = new SessionState();
                original.EnsureRenameList().AddMode = RenameListAddMode.Folders;
                original.EnsureRenameList().UseFixedWidthFont = true;
                SessionStore.TrySave(original, path);

                var loaded = SessionStore.Load(path);
                Assert.Equal(RenameListAddMode.Folders, loaded.RenameList?.AddMode);
                Assert.True(loaded.RenameList?.UseFixedWidthFont);
            }
            finally
            {
                File.Delete(path);
            }
        }
    }
}
