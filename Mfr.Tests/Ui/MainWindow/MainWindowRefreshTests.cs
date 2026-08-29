using Avalonia.Headless.XUnit;
using Mfr.App.Ui.ViewModels;

namespace Mfr.Tests.Ui.MainWindow
{
    /// <summary>
    /// F5 routing for File List vs Rename List via <see cref="MainWindowViewModel.RefreshFocusedPaneAsync"/>.
    /// </summary>
    public sealed class MainWindowRefreshTests : IDisposable
    {
        private readonly TempDirectoryFixture _tempDirectoryFixture = new();

        /// <inheritdoc />
        public void Dispose()
        {
            _tempDirectoryFixture.Dispose();
        }

        /// <summary>
        /// Verifies F5 reloads the File List when the Rename List grid is not focused.
        /// </summary>
        [AvaloniaFact]
        public async Task RefreshFocusedPane_reloads_file_list_when_rename_grid_unfocused()
        {
            var dir = _tempDirectoryFixture.CreateTempDir();
            File.WriteAllText(Path.Combine(dir, "a.txt"), "a");
            var viewModel = new MainWindowViewModel(dir);
            File.WriteAllText(Path.Combine(dir, "b.txt"), "b");

            Assert.DoesNotContain(viewModel.FileListViewModel.Entries, entry => entry.Name == "b.txt");

            await viewModel.RefreshFocusedPaneCommand.ExecuteAsync(null).ConfigureAwait(true);

            Assert.Contains(viewModel.FileListViewModel.Entries, entry => entry.Name == "b.txt");
        }

        /// <summary>
        /// Verifies F5 does not reload the File List when the Rename List grid has focus, even if that list is empty.
        /// </summary>
        [AvaloniaFact]
        public async Task RefreshFocusedPane_skips_file_list_when_rename_grid_focused()
        {
            var dir = _tempDirectoryFixture.CreateTempDir();
            File.WriteAllText(Path.Combine(dir, "a.txt"), "a");
            var viewModel = new MainWindowViewModel(dir);
            viewModel.RenameListViewModel.SetGridFocused(true);
            File.WriteAllText(Path.Combine(dir, "b.txt"), "b");

            await viewModel.RefreshFocusedPaneCommand.ExecuteAsync(null).ConfigureAwait(true);

            Assert.DoesNotContain(viewModel.FileListViewModel.Entries, entry => entry.Name == "b.txt");
        }

        /// <summary>
        /// Verifies F5 re-reads Rename List originals when that grid has focus and the list is non-empty.
        /// </summary>
        [AvaloniaFact]
        public async Task RefreshFocusedPane_refreshes_rename_originals_when_grid_focused()
        {
            var dir = _tempDirectoryFixture.CreateTempDir();
            var path = Path.Combine(dir, "size.txt");
            File.WriteAllText(path, "a");
            var viewModel = new MainWindowViewModel(dir);
            await viewModel.RenameListViewModel.AddPathsAsync([path]).ConfigureAwait(true);
            var entry = Assert.Single(viewModel.RenameListViewModel.Entries);
            var beforeSize = entry.EngineItem.Original.FileSize;
            File.WriteAllText(path, new string('x', 4096));
            viewModel.RenameListViewModel.SetGridFocused(true);

            await viewModel.RefreshFocusedPaneCommand.ExecuteAsync(null).ConfigureAwait(true);

            Assert.NotEqual(beforeSize, entry.EngineItem.Original.FileSize);
        }
    }
}
