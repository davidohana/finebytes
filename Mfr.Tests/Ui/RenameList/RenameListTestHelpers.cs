using Avalonia.Controls;
using Avalonia.Threading;
using Avalonia.VisualTree;
using Mfr.App.Ui.Services.FileList;
using Mfr.App.Ui.ViewModels.FileList;
using Mfr.App.Ui.ViewModels.RenameList;
using Mfr.App.Ui.Views.RenameList;

namespace Mfr.Tests.Ui.RenameList
{
    /// <summary>
    /// Shared Rename List test helpers.
    /// </summary>
    internal static class RenameListTestHelpers
    {
        /// <summary>
        /// Builds a one-field session sort list for <see cref="App.Ui.ViewModels.RenameList.RenameListViewModel.ApplySession"/>.
        /// </summary>
        /// <param name="column">Sort column.</param>
        /// <param name="descending">When <see langword="true"/>, sort descending.</param>
        /// <returns>Single-element session field list.</returns>
        internal static List<SessionStateRenameListSortField> SortSession(
            RenameListSortColumn column,
            bool descending = false
        )
        {
            return [new SessionStateRenameListSortField(column, descending)];
        }
    }

    /// <summary>
    /// Temp folders, File List hosts, and optional UI add-policy pinning for Rename List tests.
    /// </summary>
    internal sealed class RenameListUiTestContext : IDisposable
    {
        private readonly TempDirectoryFixture _tempDirectoryFixture = new();
        private readonly List<FileListViewModel> _fileListViewModels = [];
        private readonly UiConfig? _originalUiConfig;

        /// <summary>
        /// Creates a test context and optionally pins File List add-policy flags.
        /// </summary>
        /// <param name="pinAddPolicy">
        /// When <see langword="true"/>, snapshots and sets Files/add-folder-contents for headless add tests.
        /// </param>
        public RenameListUiTestContext(bool pinAddPolicy = false)
        {
            if (!pinAddPolicy)
            {
                return;
            }

            _originalUiConfig = new UiConfig
            {
                AddMode = ConfigStore.Config.Ui.AddMode,
                AddFolderContents = ConfigStore.Config.Ui.AddFolderContents,
                RememberWindowState = ConfigStore.Config.Ui.RememberWindowState,
                RememberLastFolder = ConfigStore.Config.Ui.RememberLastFolder,
            };
            ConfigStore.Config.Ui.AddMode = RenameListAddMode.Files;
            ConfigStore.Config.Ui.AddFolderContents = true;
        }

        /// <inheritdoc />
        public void Dispose()
        {
            if (_originalUiConfig is not null)
            {
                ConfigStore.Config.Ui.AddMode = _originalUiConfig.AddMode;
                ConfigStore.Config.Ui.AddFolderContents = _originalUiConfig.AddFolderContents;
                ConfigStore.Config.Ui.RememberWindowState = _originalUiConfig.RememberWindowState;
                ConfigStore.Config.Ui.RememberLastFolder = _originalUiConfig.RememberLastFolder;
            }

            foreach (var fileListViewModel in _fileListViewModels)
            {
                fileListViewModel.Dispose();
            }

            _tempDirectoryFixture.Dispose();
        }

        /// <summary>
        /// Creates an empty temporary directory.
        /// </summary>
        /// <returns>Absolute directory path.</returns>
        public string CreateTempDir()
        {
            return _tempDirectoryFixture.CreateTempDir();
        }

        /// <summary>
        /// Creates a File List view model rooted at <paramref name="path"/>.
        /// </summary>
        /// <param name="path">Directory path.</param>
        /// <returns>File List view model owned by this context.</returns>
        public FileListViewModel CreateFileListViewModel(string path)
        {
            var fileListViewModel = new FileListViewModel(
                NullSystemIconProvider.Instance,
                path,
                NullFileShellOpener.Instance
            );
            _fileListViewModels.Add(fileListViewModel);
            return fileListViewModel;
        }

        /// <summary>
        /// Creates a Rename List view model over a File List at <paramref name="directoryPath"/>.
        /// </summary>
        /// <param name="directoryPath">Directory path, or a new temp dir when omitted.</param>
        /// <returns>Rename List view model.</returns>
        public RenameListViewModel CreateRenameListViewModel(string? directoryPath = null)
        {
            return new RenameListViewModel(CreateFileListViewModel(directoryPath ?? CreateTempDir()));
        }

        /// <summary>
        /// Shows a Rename List window with <paramref name="rowCount"/> sample files added.
        /// </summary>
        /// <param name="rowCount">Number of files to create and add.</param>
        /// <returns>View model, host window, and grid.</returns>
        public async Task<(RenameListViewModel ViewModel, Window Window, DataGrid Grid)> ShowWithRowsAsync(int rowCount)
        {
            var dir = CreateTempDir();
            var paths = new List<string>(rowCount);
            for (var i = 0; i < rowCount; i++)
            {
                var path = Path.Combine(dir, $"row-{i:00}.txt");
                File.WriteAllText(path, "x");
                paths.Add(path);
            }

            var renameListViewModel = CreateRenameListViewModel(dir);
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
    }
}
