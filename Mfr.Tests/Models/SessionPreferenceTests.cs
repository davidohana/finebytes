namespace Mfr.Tests.Models
{
    /// <summary>
    /// Tests session preferences stored on main-window, File List, and Rename List sections.
    /// </summary>
    public sealed class SessionPreferenceTests
    {
        [Fact]
        public void Load_empty_session_uses_preference_defaults()
        {
            var path = Path.Combine(Path.GetTempPath(), "mfr-test-pref-session-" + Guid.NewGuid() + ".json");
            File.WriteAllText(path, """{}""");
            try
            {
                var session = SessionStore.Load(path);

                Assert.Null(session.MainWindow);
                Assert.Null(session.FileList);
                Assert.Null(session.RenameList);
                Assert.True(session.MainWindow?.RememberWindowState ?? true);
                Assert.True(session.FileList?.RememberLastFolder ?? true);
                Assert.Equal(RenameListAddMode.Files, session.RenameList?.AddMode ?? RenameListAddMode.Files);
                Assert.True(session.RenameList?.AddFolderContents ?? true);
                Assert.False(session.RenameList?.UseFixedWidthFont ?? false);
            }
            finally
            {
                File.Delete(path);
            }
        }

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
        public void SaveCurrentPreferences_preserves_layout_fields()
        {
            var path = Path.Combine(Path.GetTempPath(), "mfr-test-pref-session-" + Guid.NewGuid() + ".json");
            var originalRenameList = SessionStore.Current.RenameList;
            try
            {
                SessionStore.Save(
                    new SessionState
                    {
                        MainWindow = new SessionStateMainWindow
                        {
                            X = 40,
                            Width = 800,
                            Height = 600,
                        },
                    },
                    path
                );

                SessionStore.Current.EnsureRenameList().AddMode = RenameListAddMode.Folders;
                SessionStore.Current.EnsureRenameList().UseFixedWidthFont = true;
                SessionStore.SaveCurrentPreferences(path);

                var loaded = SessionStore.Load(path);
                Assert.NotNull(loaded.MainWindow);
                Assert.Equal(40, loaded.MainWindow.X);
                Assert.Equal(RenameListAddMode.Folders, loaded.RenameList?.AddMode);
                Assert.True(loaded.RenameList?.UseFixedWidthFont);
            }
            finally
            {
                SessionStore.Current.RenameList = originalRenameList;
                File.Delete(path);
            }
        }
    }
}
