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
                ConfigStore.MainWindow = new SessionStateMainWindow { RememberWindowState = false };
                ConfigStore.FileList = new SessionStateFileList { RememberLastFolder = false };
                ConfigStore.RenameList = new SessionStateRenameList
                {
                    AddMode = RenameListAddMode.Folders,
                    AddFolderContents = false,
                    UseFixedWidthFont = true,
                    PreviewEnabled = false,
                };
                ConfigStore.Save(path);
                ConfigStore.Load(path);

                Assert.False(ConfigStore.MainWindow?.RememberWindowState);
                Assert.False(ConfigStore.FileList?.RememberLastFolder);
                Assert.Equal(RenameListAddMode.Folders, ConfigStore.RenameList?.AddMode);
                Assert.False(ConfigStore.RenameList?.AddFolderContents);
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
                ConfigStore.Load(path);

                Assert.Equal(RenameListAddMode.FilesAndFolders, ConfigStore.RenameList?.AddMode);
                Assert.False(ConfigStore.RenameList?.AddFolderContents);
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
        public void TrySave_writes_session()
        {
            var path = Path.Combine(Path.GetTempPath(), "mfr-test-pref-session-" + Guid.NewGuid() + ".json");
            try
            {
                ConfigStoreTestReset.LoadEmpty();
                var renameList = ConfigStore.EnsureRenameList();
                renameList.AddMode = RenameListAddMode.Folders;
                renameList.UseFixedWidthFont = true;
                ConfigStore.TrySave(path);

                ConfigStore.Load(path);
                Assert.Equal(RenameListAddMode.Folders, ConfigStore.RenameList?.AddMode);
                Assert.True(ConfigStore.RenameList?.UseFixedWidthFont);
            }
            finally
            {
                File.Delete(path);
                ConfigStoreTestReset.LoadEmpty();
            }
        }
    }
}
