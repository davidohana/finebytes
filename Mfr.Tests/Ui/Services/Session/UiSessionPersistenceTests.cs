using Avalonia.Headless.XUnit;
using Mfr.App.Ui.Services.Session;
using Mfr.App.Ui.ViewModels.MainWindow;
using Mfr.Models.RenameList.Fields.Basic;
using AppMainWindow = Mfr.App.Ui.Views.MainWindow.MainWindow;

namespace Mfr.Tests.Ui.Services.Session
{
    /// <summary>
    /// Headless tests for close-save of UI session sections into <see cref="ConfigStore"/>.
    /// </summary>
    [Collection(ConfigStoreCollection.Name)]
    public sealed class UiSessionPersistenceTests
    {
        public UiSessionPersistenceTests()
        {
            ConfigStoreTestReset.LoadEmpty();
        }

        /// <summary>
        /// Verifies close-save writes File List chrome without touching Options-owned prefs.
        /// </summary>
        [AvaloniaFact]
        public void SaveOnClose_Writes_FileList_Chrome_Leaves_Options()
        {
            var configPath = Path.Combine(Path.GetTempPath(), "mfr-test-session-close-" + Guid.NewGuid() + ".json");
            File.WriteAllText(configPath, "{}");
            try
            {
                ConfigStore.Load(configPath);
                ConfigStore.Options.RememberLastFolder = false;
                ConfigStore.Options.DoubleClickAddsToRenameList = true;
                ConfigStore.FileList = new FileListPrefs { FileMask = "*.old" };
                ConfigStore.MainWindow = null;
                ConfigStore.Options.RememberWindowState = false;

                var window = new AppMainWindow
                {
                    DataContext = new MainWindowViewModel(persistSession: false),
                    Width = 1100,
                    Height = 720,
                };
                window.Show();
                window.UpdateLayout();

                var capture = new FileListPrefs { LastOpenedDirectory = Path.GetTempPath(), FileMask = "*.wav" };

                UiSessionPersistence.SaveOnClose(window, window.GetPaneGrids(), capture);

                Assert.True(ConfigStore.Options.DoubleClickAddsToRenameList);
                Assert.False(ConfigStore.Options.RememberLastFolder);
                Assert.Equal("*.wav", ConfigStore.FileList?.FileMask);
                Assert.False(ConfigStore.Options.RememberWindowState);
            }
            finally
            {
                if (File.Exists(configPath))
                {
                    File.Delete(configPath);
                }

                ConfigStoreTestReset.LoadEmpty();
            }
        }

        /// <summary>
        /// Verifies close-save replaces Rename List UI session fields and leaves Options add policy alone.
        /// </summary>
        [AvaloniaFact]
        public void SaveOnClose_Writes_RenameList_Chrome_Leaves_Options_Add_Policy()
        {
            var configPath = Path.Combine(Path.GetTempPath(), "mfr-test-session-rl-close-" + Guid.NewGuid() + ".json");
            File.WriteAllText(configPath, "{}");
            try
            {
                ConfigStore.Load(configPath);
                ConfigStore.Options.AddMode = RenameListAddMode.Folders;
                ConfigStore.Options.AddFolderContents = false;
                ConfigStore.Options.RememberColumnWidths = true;
                ConfigStore.RenameList = new RenameListPrefs
                {
                    UseFixedWidthFont = true,
                    PreviewEnabled = true,
                    AbModeEnabled = false,
                    AbSide = RenameListPrefs.AbSideOriginal,
                    ColumnWidths =
                    [
                        new RenameListVisibleColumnSpec(
                            RenameListFieldKey.Original(BasicRenameListField.Group, BasicRenameListFields.Key.Name),
                            Width: 111
                        ),
                    ],
                };
                ConfigStore.Options.RememberWindowState = false;

                var window = new AppMainWindow
                {
                    DataContext = new MainWindowViewModel(persistSession: false),
                    Width = 1100,
                    Height = 720,
                };
                window.Show();
                window.UpdateLayout();

                var nameKey = RenameListFieldKey.Original(BasicRenameListField.Group, BasicRenameListFields.Key.Name);
                var capture = new RenameListPrefs
                {
                    UseFixedWidthFont = false,
                    PreviewEnabled = false,
                    AbModeEnabled = true,
                    AbSide = RenameListPrefs.AbSidePreview,
                    SortFields = [],
                    ColumnWidths = [new RenameListVisibleColumnSpec(nameKey, Width: 222)],
                };

                UiSessionPersistence.SaveOnClose(window, window.GetPaneGrids(), fileList: null, renameList: capture);

                Assert.Equal(RenameListAddMode.Folders, ConfigStore.Options.AddMode);
                Assert.False(ConfigStore.Options.AddFolderContents);
                Assert.True(ConfigStore.Options.RememberColumnWidths);
                Assert.False(ConfigStore.RenameList?.UseFixedWidthFont);
                Assert.False(ConfigStore.RenameList?.PreviewEnabled);
                Assert.True(ConfigStore.RenameList?.AbModeEnabled);
                Assert.Equal(RenameListPrefs.AbSidePreview, ConfigStore.RenameList?.AbSide);
                Assert.NotNull(ConfigStore.RenameList?.SortFields);
                Assert.Empty(ConfigStore.RenameList.SortFields);
                Assert.NotNull(ConfigStore.RenameList?.ColumnWidths);
                Assert.Equal(nameKey, ConfigStore.RenameList.ColumnWidths[0].Key);
                Assert.Equal(222, ConfigStore.RenameList.ColumnWidths[0].Width);
            }
            finally
            {
                if (File.Exists(configPath))
                {
                    File.Delete(configPath);
                }

                ConfigStoreTestReset.LoadEmpty();
            }
        }

        /// <summary>
        /// Verifies close-save leaves root dialog geometries untouched when rewriting main-window capture.
        /// </summary>
        [AvaloniaFact]
        public void SaveOnClose_Leaves_RootDialogGeometries()
        {
            var configPath = Path.Combine(Path.GetTempPath(), "mfr-test-session-dialogs-" + Guid.NewGuid() + ".json");
            File.WriteAllText(configPath, "{}");
            try
            {
                ConfigStore.Load(configPath);
                ConfigStore.Options.RememberWindowState = true;
                ConfigStore.Dialogs = new Dictionary<string, WindowGeometryPrefs>(StringComparer.Ordinal)
                {
                    [DialogIds.RenameLog] = new WindowGeometryPrefs
                    {
                        X = 11,
                        Y = 22,
                        Width = 700,
                        Height = 500,
                    },
                };

                var window = new AppMainWindow
                {
                    DataContext = new MainWindowViewModel(persistSession: false),
                    Width = 1100,
                    Height = 720,
                };
                window.Show();
                window.UpdateLayout();

                UiSessionPersistence.SaveOnClose(window, window.GetPaneGrids(), fileList: null);

                var saved = Assert.Contains(DialogIds.RenameLog, ConfigStore.Dialogs!);
                Assert.Equal(11, saved.X);
                Assert.Equal(22, saved.Y);
                Assert.Equal(700, saved.Width);
                Assert.Equal(500, saved.Height);
            }
            finally
            {
                if (File.Exists(configPath))
                {
                    File.Delete(configPath);
                }

                ConfigStoreTestReset.LoadEmpty();
            }
        }
    }
}
