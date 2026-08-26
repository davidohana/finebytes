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
    /// Headless tests for Rename List status-bar cell hints.
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
        /// Verifies clicking a cell publishes that cell's value to the status-bar hint.
        /// </summary>
        [AvaloniaFact]
        public async Task Click_Sets_Hint_From_Cell()
        {
            var (renameListViewModel, window, grid) = await _ShowWithRowsAsync(rowCount: 8);
            var target = renameListViewModel.Entries[3];

            _ClickFullFileNameCell(window, grid, target);

            Assert.Contains(
                target.FullFileName,
                renameListViewModel.CellStatusHintDisplay.ToPlainText(),
                StringComparison.Ordinal
            );

            window.Close();
        }

        /// <summary>
        /// Verifies Del updates the hint to the row that slides into the deleted index.
        /// </summary>
        [AvaloniaFact]
        public async Task Delete_Updates_Hint_To_New_Selection()
        {
            var (renameListViewModel, window, grid) = await _ShowWithRowsAsync(rowCount: 30);
            var deleteIndex = 12;
            var deletedName = renameListViewModel.Entries[deleteIndex].FullFileName;
            var expectedName = renameListViewModel.Entries[deleteIndex + 1].FullFileName;

            renameListViewModel.SetSelectedEntries([renameListViewModel.Entries[deleteIndex]]);
            grid.ScrollIntoView(renameListViewModel.Entries[deleteIndex], grid.Columns[2]);
            window.UpdateLayout();
            Dispatcher.UIThread.RunJobs();

            _ClickFullFileNameCell(window, grid, renameListViewModel.Entries[deleteIndex]);
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

            Assert.Equal(29, renameListViewModel.Entries.Count);
            Assert.Equal(expectedName, renameListViewModel.SelectedEntries[0].FullFileName);
            var hint = renameListViewModel.CellStatusHintDisplay.ToPlainText();
            Assert.Contains(expectedName, hint, StringComparison.Ordinal);
            Assert.DoesNotContain(deletedName, hint, StringComparison.Ordinal);

            window.Close();
        }

        /// <summary>
        /// Verifies moving the pointer over another row does not steal the status-bar hint.
        /// </summary>
        [AvaloniaFact]
        public async Task PointerMove_Does_Not_Change_Hint()
        {
            var (renameListViewModel, window, grid) = await _ShowWithRowsAsync(rowCount: 8);
            var selected = renameListViewModel.Entries[1];
            var other = renameListViewModel.Entries[4];

            _ClickFullFileNameCell(window, grid, selected);
            Assert.Contains(
                selected.FullFileName,
                renameListViewModel.CellStatusHintDisplay.ToPlainText(),
                StringComparison.Ordinal
            );

            _MoveOverFullFileNameCell(window, grid, other);
            Dispatcher.UIThread.RunJobs();

            var hint = renameListViewModel.CellStatusHintDisplay.ToPlainText();
            Assert.Contains(selected.FullFileName, hint, StringComparison.Ordinal);
            Assert.DoesNotContain(other.FullFileName, hint, StringComparison.Ordinal);

            window.Close();
        }

        private async Task<(RenameListViewModel ViewModel, Window Window, DataGrid Grid)> _ShowWithRowsAsync(
            int rowCount
        )
        {
            var dir = _tempDirectoryFixture.CreateTempDir();
            var paths = new List<string>(rowCount);
            for (var i = 0; i < rowCount; i++)
            {
                var path = Path.Combine(dir, $"row-{i:00}.txt");
                File.WriteAllText(path, "x");
                paths.Add(path);
            }

            var fileListViewModel = _CreateFileListViewModel(dir);
            var renameListViewModel = new RenameListViewModel(fileListViewModel);
            await renameListViewModel.AddPathsAsync(paths);
            Assert.Equal(rowCount, renameListViewModel.Entries.Count);

            var view = new RenameListView { DataContext = renameListViewModel };
            var window = new Window
            {
                Width = 800,
                Height = 180,
                Content = view,
            };
            window.Show();
            window.UpdateLayout();
            Dispatcher.UIThread.RunJobs();

            var grid = view.GetVisualDescendants().OfType<DataGrid>().Single();
            return (renameListViewModel, window, grid);
        }

        private FileListViewModel _CreateFileListViewModel(string path)
        {
            var fileListViewModel = new FileListViewModel(NullSystemIconProvider.Instance, path, NullFileShellOpener.Instance);
            _fileListViewModels.Add(fileListViewModel);
            return fileListViewModel;
        }

        private static void _ClickFullFileNameCell(Window window, DataGrid grid, RenameListEntry entry)
        {
            var windowPoint = _FullFileNameCellPoint(window, grid, entry);
            window.MouseMove(windowPoint, RawInputModifiers.None);
            window.MouseDown(windowPoint, MouseButton.Left, RawInputModifiers.None);
            window.MouseUp(windowPoint, MouseButton.Left, RawInputModifiers.None);
            Dispatcher.UIThread.RunJobs();
        }

        private static void _MoveOverFullFileNameCell(Window window, DataGrid grid, RenameListEntry entry)
        {
            var windowPoint = _FullFileNameCellPoint(window, grid, entry);
            window.MouseMove(windowPoint, RawInputModifiers.None);
            Dispatcher.UIThread.RunJobs();
        }

        private static Point _FullFileNameCellPoint(Window window, DataGrid grid, RenameListEntry entry)
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
            return windowPoint.Value;
        }
    }
}
