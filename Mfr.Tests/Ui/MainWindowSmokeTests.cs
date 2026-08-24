using Avalonia.Headless.XUnit;
using Mfr.App.Ui.Input;
using Mfr.App.Ui.ViewModels;
using Mfr.App.Ui.ViewModels.FileList;
using Mfr.App.Ui.Views;

namespace Mfr.Tests.Ui
{
    /// <summary>
    /// Headless smoke tests for the main window shell.
    /// </summary>
    public sealed class MainWindowSmokeTests : IDisposable
    {
        private readonly TempDirectoryFixture _tempDirectoryFixture = new();

        /// <inheritdoc />
        public void Dispose()
        {
            _tempDirectoryFixture.Dispose();
        }

        /// <summary>
        /// Verifies the main window constructs with a File List pane.
        /// </summary>
        [AvaloniaFact]
        public void MainWindow_Constructs()
        {
            var window = new MainWindow { DataContext = new MainWindowViewModel() };

            window.Show();

            Assert.True(window.IsVisible);
            Assert.IsType<MainWindowViewModel>(window.DataContext);
        }

        /// <summary>
        /// Verifies the window title includes the product version.
        /// </summary>
        [AvaloniaFact]
        public void MainWindow_TitleIncludesVersion()
        {
            var viewModel = new MainWindowViewModel();
            var window = new MainWindow { DataContext = viewModel };

            window.Show();

            Assert.Equal(viewModel.WindowTitle, window.Title);
            Assert.Matches(@"^Magic File Renamer \S+", viewModel.WindowTitle);
        }

        /// <summary>
        /// Verifies documented global shortcuts are window key bindings, not Backspace or Alt+F4.
        /// </summary>
        [AvaloniaFact]
        public void MainWindow_RegistersGlobalKeyBindings()
        {
            var window = new MainWindow { DataContext = new MainWindowViewModel() };

            window.Show();

            var gestures = window.KeyBindings.Select(binding => binding.Gesture).ToList();
            Assert.Contains(AppShortcuts.Go, gestures);
            Assert.Contains(AppShortcuts.UndoLast, gestures);
            Assert.Contains(AppShortcuts.ShowLog, gestures);
            Assert.Contains(AppShortcuts.ShowOptions, gestures);
            Assert.Contains(AppShortcuts.Refresh, gestures);
            Assert.Contains(AppShortcuts.GoToAddress, gestures);
            Assert.Contains(AppShortcuts.GoToAddressAlt, gestures);
            Assert.Contains(AppShortcuts.ViewLargeIcons, gestures);
            Assert.Contains(AppShortcuts.ViewSmallIcons, gestures);
            Assert.Contains(AppShortcuts.ViewReport, gestures);
            Assert.Contains(AppShortcuts.ViewList, gestures);
            Assert.Contains(AppShortcuts.ViewTiles, gestures);
            Assert.Contains(AppShortcuts.ViewThumbnails, gestures);
            Assert.Contains(AppShortcuts.AddSelected, gestures);
            Assert.Contains(AppShortcuts.AddAll, gestures);
            Assert.Contains(AppShortcuts.RemoveSelected, gestures);
            Assert.Contains(AppShortcuts.ClearRenameList, gestures);
            Assert.DoesNotContain(AppShortcuts.RemoveSelectedDelete, gestures);
            Assert.DoesNotContain(AppShortcuts.LocateInFileList, gestures);
            Assert.DoesNotContain(AppShortcuts.GoUp, gestures);
            Assert.DoesNotContain(AppShortcuts.Exit, gestures);
            Assert.DoesNotContain(AppShortcuts.ZoomIn, gestures);
        }

        /// <summary>
        /// Verifies status-bar ItemCount tracks Rename List add and clear.
        /// </summary>
        [AvaloniaFact]
        public async Task MainWindow_ItemCount_Tracks_RenameList()
        {
            var dir = _tempDirectoryFixture.CreateTempDir();
            File.WriteAllText(Path.Combine(dir, "alpha.txt"), "a");

            var viewModel = new MainWindowViewModel(dir);
            var fileListViewModel = viewModel.FileListViewModel;
            var renameListViewModel = viewModel.RenameListViewModel;

            Assert.Equal(0, viewModel.ItemCount);

            fileListViewModel.SetSelectedEntries([
                new FileListEntry
                {
                    Name = "alpha.txt",
                    FullPath = Path.Combine(dir, "alpha.txt"),
                    IsDirectory = false,
                },
            ]);
            await renameListViewModel.AddSelectedCommand.ExecuteAsync(null);
            Assert.Equal(1, viewModel.ItemCount);

            renameListViewModel.ClearCommand.Execute(null);
            Assert.Equal(0, viewModel.ItemCount);
        }
    }
}
