using Avalonia;
using Avalonia.Controls;
using Avalonia.Headless;
using Avalonia.Headless.XUnit;
using Avalonia.Input;
using Avalonia.Threading;
using Avalonia.VisualTree;
using Mfr.App.Ui.Services.FileList;
using Mfr.App.Ui.ViewModels.FileList;
using Mfr.App.Ui.ViewModels.RenameList;
using Mfr.App.Ui.Views.RenameList;

namespace Mfr.Tests.Ui
{
    /// <summary>
    /// Headless tests for Rename List status-bar cell hints after delete.
    /// </summary>
    public sealed class RenameListViewHintTests : IDisposable
    {
        private readonly TempDirectoryFixture _tempDirectoryFixture = new();
        private readonly List<FileListViewModel> _fileListViewModels = [];
        private readonly UiConfig _originalUiConfig;

        /// <summary>
        /// Snapshots UI add-policy config for tests that may change it.
        /// </summary>
        public RenameListViewHintTests()
        {
            ConfigStore.Config.Ui.AddMode = RenameListAddMode.Files;
            ConfigStore.Config.Ui.AddFolderContents = true;
            _originalUiConfig = new UiConfig
            {
                AddMode = ConfigStore.Config.Ui.AddMode,
                AddFolderContents = ConfigStore.Config.Ui.AddFolderContents,
                RememberWindowState = ConfigStore.Config.Ui.RememberWindowState,
                RememberLastFolder = ConfigStore.Config.Ui.RememberLastFolder,
            };
        }

        /// <inheritdoc />
        public void Dispose()
        {
            ConfigStore.Config.Ui.AddMode = _originalUiConfig.AddMode;
            ConfigStore.Config.Ui.AddFolderContents = _originalUiConfig.AddFolderContents;

            foreach (var fileListViewModel in _fileListViewModels)
            {
                fileListViewModel.Dispose();
            }

            _tempDirectoryFixture.Dispose();
        }

        /// <summary>
        /// Verifies Del keeps the hint on the row that slides into the deleted index, not a recycled viewport row.
        /// </summary>
        [AvaloniaFact]
        public async Task Delete_Keeps_Hint_On_New_Selection_When_Pointer_Stays()
        {
            var dir = _tempDirectoryFixture.CreateTempDir();
            var paths = new List<string>(30);
            for (var i = 0; i < 30; i++)
            {
                var path = Path.Combine(dir, $"row-{i:00}.txt");
                File.WriteAllText(path, "x");
                paths.Add(path);
            }

            var fileListViewModel = _CreateFileListViewModel(dir);
            var renameListViewModel = new RenameListViewModel(fileListViewModel);
            await renameListViewModel.AddPathsAsync(paths);
            Assert.Equal(30, renameListViewModel.Entries.Count);

            var (view, window) = _Show(renameListViewModel);
            var grid = view.GetVisualDescendants().OfType<DataGrid>().Single();
            var deleteIndex = 12;
            var deletedName = renameListViewModel.Entries[deleteIndex].FullFileName;
            var expectedName = renameListViewModel.Entries[deleteIndex + 1].FullFileName;
            var lastName = renameListViewModel.Entries[^1].FullFileName;

            renameListViewModel.SetSelectedEntries([renameListViewModel.Entries[deleteIndex]]);
            grid.ScrollIntoView(renameListViewModel.Entries[deleteIndex], grid.Columns[2]);
            window.UpdateLayout();
            Dispatcher.UIThread.RunJobs();

            var hoverPoint = _HoverFullFileNameCell(window, grid, renameListViewModel.Entries[deleteIndex]);
            Assert.Contains(
                deletedName,
                renameListViewModel.CellStatusHintDisplay.ToPlainText(),
                StringComparison.Ordinal
            );

            grid.Focus();
            window.KeyPress(Key.Delete, RawInputModifiers.None, PhysicalKey.Delete, "\u007f");
            Dispatcher.UIThread.RunJobs();

            if (renameListViewModel.Entries.Count == 30)
            {
                renameListViewModel.RemoveSelectedCommand.Execute(null);
                Dispatcher.UIThread.RunJobs();
            }

            // Below the 8px freeze threshold; verifies hit-test cannot override the frozen hint.
            window.MouseMove(hoverPoint + new Vector(5, 0), RawInputModifiers.None);
            Dispatcher.UIThread.RunJobs();

            grid.SelectedItem = renameListViewModel.Entries[^1];
            Dispatcher.UIThread.RunJobs();

            Assert.Equal(29, renameListViewModel.Entries.Count);
            Assert.Equal(expectedName, renameListViewModel.SelectedEntries[0].FullFileName);
            var hint = renameListViewModel.CellStatusHintDisplay.ToPlainText();
            Assert.Contains(expectedName, hint, StringComparison.Ordinal);
            Assert.DoesNotContain(lastName, hint, StringComparison.Ordinal);
            Assert.DoesNotContain(deletedName, hint, StringComparison.Ordinal);

            window.Close();
        }

        private FileListViewModel _CreateFileListViewModel(string path)
        {
            var fileListViewModel = new FileListViewModel(NullSystemIconProvider.Instance, path);
            _fileListViewModels.Add(fileListViewModel);
            return fileListViewModel;
        }

        private static (RenameListView View, Window Window) _Show(RenameListViewModel viewModel)
        {
            var view = new RenameListView { DataContext = viewModel };
            var window = new Window
            {
                Width = 800,
                Height = 180,
                Content = view,
            };
            window.Show();
            window.UpdateLayout();
            Dispatcher.UIThread.RunJobs();
            return (view, window);
        }

        private static Point _HoverFullFileNameCell(Window window, DataGrid grid, RenameListEntry entry)
        {
            var row = grid.GetVisualDescendants()
                .OfType<DataGridRow>()
                .FirstOrDefault(item => ReferenceEquals(item.DataContext, entry));
            Assert.NotNull(row);

            var cellText = row.GetVisualDescendants()
                .OfType<TextBlock>()
                .FirstOrDefault(item => item.Text == entry.FullFileName);
            Assert.NotNull(cellText);

            var local = new Point(Math.Max(8, cellText.Bounds.Width / 2), Math.Max(1, cellText.Bounds.Height / 2));
            var windowPoint = cellText.TranslatePoint(local, window);
            Assert.True(windowPoint.HasValue);

            window.MouseMove(windowPoint.Value, RawInputModifiers.None);
            window.MouseDown(windowPoint.Value, MouseButton.Left, RawInputModifiers.None);
            window.MouseUp(windowPoint.Value, MouseButton.Left, RawInputModifiers.None);
            Dispatcher.UIThread.RunJobs();
            return windowPoint.Value;
        }
    }
}
