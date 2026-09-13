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
                ConfigStore.FileList = new SessionStateFileList
                {
                    RememberLastFolder = false,
                    DoubleClickAddsToRenameList = true,
                    FileMask = "*.old",
                };
                ConfigStore.MainWindow = new SessionStateMainWindow { RememberWindowState = false };

                var window = new AppMainWindow
                {
                    DataContext = new MainWindowViewModel(persistSession: false),
                    Width = 1100,
                    Height = 720,
                };
                window.Show();
                window.UpdateLayout();

                var capture = new SessionStateFileList
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
    }
}
