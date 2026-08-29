using Mfr.Tests.Ui.RenameList;

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
                    },
                };

                SessionStore.Save(original, path);
                var loaded = SessionStore.Load(path);

                Assert.False(loaded.MainWindow?.RememberWindowState);
                Assert.False(loaded.FileList?.RememberLastFolder);
                Assert.Equal(RenameListAddMode.Folders, loaded.RenameList?.AddMode);
                Assert.False(loaded.RenameList?.AddFolderContents);
                Assert.True(loaded.RenameList?.UseFixedWidthFont);
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
                    "useFixedWidthFont": true
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
            }
            finally
            {
                File.Delete(path);
            }
        }

        [Fact]
        public void TrySaveCurrent_writes_current_session()
        {
            var path = Path.Combine(Path.GetTempPath(), "mfr-test-pref-session-" + Guid.NewGuid() + ".json");
            var originalPrefs = RenameListTestHelpers.SnapshotSessionPrefs();
            try
            {
                SessionStore.Current.EnsureRenameList().AddMode = RenameListAddMode.Folders;
                SessionStore.Current.EnsureRenameList().UseFixedWidthFont = true;
                SessionStore.TrySaveCurrent(path);

                var loaded = SessionStore.Load(path);
                Assert.Equal(RenameListAddMode.Folders, loaded.RenameList?.AddMode);
                Assert.True(loaded.RenameList?.UseFixedWidthFont);
            }
            finally
            {
                RenameListTestHelpers.RestoreSessionPrefs(originalPrefs);
                File.Delete(path);
            }
        }
    }
}
