namespace Mfr.Tests.Models
{
    /// <summary>
    /// Tests Rename List UI preferences persisted on <see cref="SessionStateUi"/>.
    /// </summary>
    public sealed class SessionStateUiTests
    {
        [Fact]
        public void Load_empty_session_keeps_ui_defaults()
        {
            var path = Path.Combine(Path.GetTempPath(), "mfr-test-ui-session-" + Guid.NewGuid() + ".json");
            File.WriteAllText(path, """{}""");
            try
            {
                var session = SessionStore.Load(path);

                Assert.Equal(RenameListAddMode.Files, session.Ui.AddMode);
                Assert.True(session.Ui.AddFolderContents);
                Assert.True(session.Ui.RememberWindowState);
                Assert.True(session.Ui.RememberLastFolder);
                Assert.False(session.Ui.RenameListUseFixedWidthFont);
            }
            finally
            {
                File.Delete(path);
            }
        }

        [Fact]
        public void Save_and_Load_round_trips_ui_preferences()
        {
            var path = Path.Combine(Path.GetTempPath(), "mfr-test-ui-session-" + Guid.NewGuid() + ".json");
            try
            {
                var original = new SessionState
                {
                    Ui = new SessionStateUi
                    {
                        AddMode = RenameListAddMode.Folders,
                        AddFolderContents = false,
                        RememberWindowState = false,
                        RememberLastFolder = false,
                        RenameListUseFixedWidthFont = true,
                    },
                };

                SessionStore.Save(original, path);
                var loaded = SessionStore.Load(path);

                Assert.Equal(RenameListAddMode.Folders, loaded.Ui.AddMode);
                Assert.False(loaded.Ui.AddFolderContents);
                Assert.False(loaded.Ui.RememberWindowState);
                Assert.False(loaded.Ui.RememberLastFolder);
                Assert.True(loaded.Ui.RenameListUseFixedWidthFont);
            }
            finally
            {
                File.Delete(path);
            }
        }

        [Fact]
        public void Load_json_reads_camel_case_add_mode()
        {
            var path = Path.Combine(Path.GetTempPath(), "mfr-test-ui-session-" + Guid.NewGuid() + ".json");
            File.WriteAllText(
                path,
                // lang=json,strict
                """
                {
                  "ui": {
                    "addMode": "filesAndFolders",
                    "addFolderContents": false,
                    "renameListUseFixedWidthFont": true
                  }
                }
                """
            );
            try
            {
                var session = SessionStore.Load(path);

                Assert.Equal(RenameListAddMode.FilesAndFolders, session.Ui.AddMode);
                Assert.False(session.Ui.AddFolderContents);
                Assert.True(session.Ui.RenameListUseFixedWidthFont);
            }
            finally
            {
                File.Delete(path);
            }
        }

        [Fact]
        public void SaveCurrentUi_preserves_other_session_sections()
        {
            var path = Path.Combine(Path.GetTempPath(), "mfr-test-ui-session-" + Guid.NewGuid() + ".json");
            var originalUi = SessionStore.Current.Ui.Clone();
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
                        Ui = new SessionStateUi { AddMode = RenameListAddMode.Files },
                    },
                    path
                );

                SessionStore.Current.Ui = new SessionStateUi
                {
                    AddMode = RenameListAddMode.Folders,
                    RenameListUseFixedWidthFont = true,
                };
                SessionStore.SaveCurrentUi(path);

                var loaded = SessionStore.Load(path);
                Assert.NotNull(loaded.MainWindow);
                Assert.Equal(40, loaded.MainWindow.X);
                Assert.Equal(RenameListAddMode.Folders, loaded.Ui.AddMode);
                Assert.True(loaded.Ui.RenameListUseFixedWidthFont);
            }
            finally
            {
                SessionStore.Current.Ui = originalUi;
                File.Delete(path);
            }
        }
    }
}
