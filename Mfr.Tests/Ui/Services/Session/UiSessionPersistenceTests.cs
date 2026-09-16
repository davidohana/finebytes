using Avalonia.Headless.XUnit;
using Mfr.App.Ui.Services.Session;
using Mfr.App.Ui.ViewModels.MainWindow;
using AppMainWindow = Mfr.App.Ui.Views.MainWindow.MainWindow;

namespace Mfr.Tests.Ui.Services.Session
{
    /// <summary>
    /// Headless tests for close-save merge into <see cref="ConfigStore"/> sections.
    /// </summary>
    [Collection(ConfigStoreCollection.Name)]
    public sealed class UiSessionPersistenceTests
    {
        public UiSessionPersistenceTests()
        {
            ConfigStoreTestReset.LoadEmpty();
        }

        /// <summary>
        /// Verifies close-save keeps Options-owned File List prefs when merging a pane capture.
        /// </summary>
        [AvaloniaFact]
        public void SaveOnClose_Preserves_DoubleClick_And_Remember_Flags()
        {
            var configPath = Path.Combine(Path.GetTempPath(), "mfr-test-session-close-" + Guid.NewGuid() + ".json");
            File.WriteAllText(configPath, "{}");
            try
            {
                ConfigStore.Load(configPath);
                ConfigStore.FileList = new FileListPrefs
                {
                    RememberLastFolder = false,
                    DoubleClickAddsToRenameList = true,
                    FileMask = "*.old",
                };
                ConfigStore.MainWindow = new MainWindowPrefs { RememberWindowState = false };

                var window = new AppMainWindow
                {
                    DataContext = new MainWindowViewModel(persistSession: false),
                    Width = 1100,
                    Height = 720,
                };
                window.Show();
                window.UpdateLayout();

                var capture = new FileListPrefs
                {
                    LastOpenedDirectory = Path.GetTempPath(),
                    FileMask = "*.wav",
                    DoubleClickAddsToRenameList = false,
                    RememberLastFolder = true,
                };

                UiSessionPersistence.SaveOnClose(window, window.GetPaneGrids(), capture);

                Assert.True(ConfigStore.FileList?.DoubleClickAddsToRenameList);
                Assert.False(ConfigStore.FileList?.RememberLastFolder);
                Assert.Equal("*.wav", ConfigStore.FileList?.FileMask);
                Assert.False(ConfigStore.MainWindow?.RememberWindowState);
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
        /// Verifies close-save keeps Options-owned Rename List add policy when merging a pane capture,
        /// and writes pane-owned Before/After Mode fields from the capture.
        /// </summary>
        [AvaloniaFact]
        public void SaveOnClose_Preserves_RenameList_Add_Policy()
        {
            var configPath = Path.Combine(Path.GetTempPath(), "mfr-test-session-rl-close-" + Guid.NewGuid() + ".json");
            File.WriteAllText(configPath, "{}");
            try
            {
                ConfigStore.Load(configPath);
                ConfigStore.RenameList = new RenameListPrefs
                {
                    AddMode = RenameListAddMode.Folders,
                    AddFolderContents = false,
                    UseFixedWidthFont = true,
                    PreviewEnabled = true,
                    AbModeEnabled = false,
                    AbSide = RenameListPrefs.AbSideOriginal,
                };
                ConfigStore.MainWindow = new MainWindowPrefs { RememberWindowState = false };

                var window = new AppMainWindow
                {
                    DataContext = new MainWindowViewModel(persistSession: false),
                    Width = 1100,
                    Height = 720,
                };
                window.Show();
                window.UpdateLayout();

                var capture = new RenameListPrefs
                {
                    AddMode = RenameListAddMode.Files,
                    AddFolderContents = true,
                    UseFixedWidthFont = false,
                    PreviewEnabled = false,
                    AbModeEnabled = true,
                    AbSide = RenameListPrefs.AbSidePreview,
                    SortFields = [],
                };

                UiSessionPersistence.SaveOnClose(window, window.GetPaneGrids(), fileList: null, renameList: capture);

                Assert.Equal(RenameListAddMode.Folders, ConfigStore.RenameList?.AddMode);
                Assert.False(ConfigStore.RenameList?.AddFolderContents);
                Assert.False(ConfigStore.RenameList?.UseFixedWidthFont);
                Assert.False(ConfigStore.RenameList?.PreviewEnabled);
                Assert.True(ConfigStore.RenameList?.AbModeEnabled);
                Assert.Equal(RenameListPrefs.AbSidePreview, ConfigStore.RenameList?.AbSide);
                Assert.NotNull(ConfigStore.RenameList?.SortFields);
                Assert.Empty(ConfigStore.RenameList.SortFields);
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
        /// Verifies close-save keeps dialog geometries when rewriting main-window capture.
        /// </summary>
        [AvaloniaFact]
        public void SaveOnClose_Preserves_DialogGeometries()
        {
            var configPath = Path.Combine(Path.GetTempPath(), "mfr-test-session-dialogs-" + Guid.NewGuid() + ".json");
            File.WriteAllText(configPath, "{}");
            try
            {
                ConfigStore.Load(configPath);
                ConfigStore.MainWindow = new MainWindowPrefs
                {
                    RememberWindowState = true,
                    Dialogs = new Dictionary<string, WindowGeometryPrefs>(StringComparer.Ordinal)
                    {
                        ["renameLog"] = new WindowGeometryPrefs
                        {
                            X = 11,
                            Y = 22,
                            Width = 700,
                            Height = 500,
                        },
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

                var saved = Assert.Contains("renameLog", ConfigStore.MainWindow!.Dialogs!);
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
