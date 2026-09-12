using Avalonia.Controls;
using Avalonia.Headless.XUnit;
using Avalonia.VisualTree;
using Mfr.App.Ui.ViewModels.MainWindow;
using AppMainWindow = Mfr.App.Ui.Views.MainWindow.MainWindow;

namespace Mfr.Tests.Ui.MainWindow
{
    /// <summary>
    /// Status-bar Selected count for the focused File List / Rename List pane.
    /// </summary>
    public sealed class MainWindowSelectedCountTests : IDisposable
    {
        private readonly TempDirectoryFixture _tempDirectoryFixture = new();

        /// <inheritdoc />
        public void Dispose()
        {
            _tempDirectoryFixture.Dispose();
        }

        /// <summary>
        /// Verifies File List multi-select updates Selected and clearing hides the panel.
        /// </summary>
        [AvaloniaFact]
        public void SelectedCount_tracks_file_list_multi_select_and_hides_when_empty()
        {
            var dir = _tempDirectoryFixture.CreateTempDir();
            File.WriteAllText(Path.Combine(dir, "a.txt"), "a");
            File.WriteAllText(Path.Combine(dir, "b.txt"), "b");
            var viewModel = new MainWindowViewModel(dir);
            var window = new AppMainWindow { DataContext = viewModel };
            window.Show();
            window.UpdateLayout();

            var selectedPanel = window
                .GetVisualDescendants()
                .OfType<TextBlock>()
                .Single(block => block.Classes.Contains("status-bar-selected"));

            Assert.Equal(0, viewModel.SelectedCount);
            Assert.False(viewModel.HasSelected);
            Assert.False(selectedPanel.IsVisible);

            var entries = viewModel.FileListViewModel.Entries.Where(entry => !entry.IsDirectory).Take(2).ToList();
            Assert.Equal(2, entries.Count);
            viewModel.FileListViewModel.SetSelectedEntries(entries);

            Assert.Equal(2, viewModel.SelectedCount);
            Assert.True(viewModel.HasSelected);
            Assert.True(selectedPanel.IsVisible);
            Assert.Equal("Selected: 2", selectedPanel.Text);

            viewModel.FileListViewModel.SetSelectedEntries([]);

            Assert.Equal(0, viewModel.SelectedCount);
            Assert.False(viewModel.HasSelected);
            Assert.False(selectedPanel.IsVisible);
        }

        /// <summary>
        /// Verifies Rename List selection drives Selected when the grid is focused.
        /// </summary>
        [AvaloniaFact]
        public async Task SelectedCount_uses_rename_list_when_grid_focused()
        {
            var dir = _tempDirectoryFixture.CreateTempDir();
            var pathA = Path.Combine(dir, "a.txt");
            var pathB = Path.Combine(dir, "b.txt");
            File.WriteAllText(pathA, "a");
            File.WriteAllText(pathB, "b");
            var viewModel = new MainWindowViewModel(dir);
            var window = new AppMainWindow { DataContext = viewModel };
            window.Show();
            window.UpdateLayout();
            await viewModel.RenameListViewModel.AddPathsAsync([pathA, pathB]).ConfigureAwait(true);

            var selectedPanel = window
                .GetVisualDescendants()
                .OfType<TextBlock>()
                .Single(block => block.Classes.Contains("status-bar-selected"));

            var fileEntries = viewModel.FileListViewModel.Entries.Where(entry => !entry.IsDirectory).Take(1).ToList();
            viewModel.FileListViewModel.SetSelectedEntries(fileEntries);
            Assert.Equal(1, viewModel.SelectedCount);
            Assert.True(selectedPanel.IsVisible);
            Assert.Equal("Selected: 1", selectedPanel.Text);

            viewModel.RenameListViewModel.SetGridFocused(true);
            Assert.Equal(0, viewModel.SelectedCount);
            Assert.False(viewModel.HasSelected);
            Assert.False(selectedPanel.IsVisible);

            viewModel.RenameListViewModel.SetSelectedEntries([.. viewModel.RenameListViewModel.Entries]);
            Assert.Equal(2, viewModel.SelectedCount);
            Assert.True(viewModel.HasSelected);
            Assert.True(selectedPanel.IsVisible);
            Assert.Equal("Selected: 2", selectedPanel.Text);

            viewModel.RenameListViewModel.SetGridFocused(false);
            Assert.Equal(1, viewModel.SelectedCount);
            Assert.True(selectedPanel.IsVisible);
            Assert.Equal("Selected: 1", selectedPanel.Text);
        }
    }
}
