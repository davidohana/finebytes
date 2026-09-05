using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Platform.Storage;
using Avalonia.Threading;
using Avalonia.VisualTree;
using Mfr.App.Ui.Services.FileList;
using Mfr.App.Ui.ViewModels.FileList;
using Mfr.App.Ui.ViewModels.RenameList;
using Mfr.App.Ui.Views.RenameList;
using Mfr.Models.RenameList.Fields.Basic;

namespace Mfr.Tests.Ui.RenameList
{
    /// <summary>
    /// Shared Rename List test helpers.
    /// </summary>
    internal static class RenameListTestHelpers
    {
        /// <summary>
        /// Original field key for File/Folder.
        /// </summary>
        internal static RenameListFieldKey FileFolderKey =>
            RenameListFieldKey.Original(BasicRenameListField.Group, BasicRenameListFields.Key.ItemType);

        /// <summary>
        /// Original field key for Parent Folder.
        /// </summary>
        internal static RenameListFieldKey ParentFolderKey =>
            RenameListFieldKey.Original(BasicRenameListField.Group, BasicRenameListFields.Key.Folder);

        /// <summary>
        /// Original field key for Full File Name.
        /// </summary>
        internal static RenameListFieldKey FullFileNameKey =>
            RenameListFieldKey.Original(BasicRenameListField.Group, BasicRenameListFields.Key.FullName);

        /// <summary>
        /// Original field key for Full File Path.
        /// </summary>
        internal static RenameListFieldKey FullPathKey =>
            RenameListFieldKey.Original(BasicRenameListField.Group, BasicRenameListFields.Key.FullPath);

        /// <summary>
        /// Builds a one-field session sort list for <see cref="RenameListViewModel.ApplySession"/>.
        /// </summary>
        /// <param name="fieldKey">Sort field key.</param>
        /// <param name="descending">When <see langword="true"/>, sort descending.</param>
        /// <returns>Single-element session field list.</returns>
        internal static List<SessionStateRenameListSortField> SortSession(
            RenameListFieldKey fieldKey,
            bool descending = false
        )
        {
            return [new SessionStateRenameListSortField(fieldKey, descending)];
        }

        /// <summary>
        /// Builds a dummy internal-reorder <see cref="DataTransfer"/> used by Rename List row drags.
        /// </summary>
        /// <returns>Transfer containing <see cref="RenameListView.InternalReorderFormat"/>.</returns>
        internal static DataTransfer CreateInternalReorderDataTransfer()
        {
            var dataTransfer = new DataTransfer();
            dataTransfer.Add(DataTransferItem.Create(RenameListView.InternalReorderFormat, "1"));
            return dataTransfer;
        }

        /// <summary>
        /// Builds an OS file <see cref="DataTransfer"/> from disk paths.
        /// </summary>
        /// <param name="window">Window whose storage provider resolves the paths.</param>
        /// <param name="paths">Absolute file paths.</param>
        /// <returns>Transfer containing those files.</returns>
        internal static async Task<DataTransfer> CreateFileDataTransferAsync(Window window, IReadOnlyList<string> paths)
        {
            var storage = window.StorageProvider;
            var dataTransfer = new DataTransfer();
            foreach (var path in paths)
            {
                IStorageItem? item = Directory.Exists(path)
                    ? await storage.TryGetFolderFromPathAsync(path).ConfigureAwait(true)
                    : await storage.TryGetFileFromPathAsync(path).ConfigureAwait(true);
                Assert.NotNull(item);
                dataTransfer.Add(DataTransferItem.CreateFile(item));
            }

            return dataTransfer;
        }
    }

    /// <summary>
    /// Temp folders and File List hosts for Rename List tests.
    /// </summary>
    internal sealed class RenameListUiTestContext : IDisposable
    {
        private readonly TempDirectoryFixture _tempDirectoryFixture = new();
        private readonly List<FileListViewModel> _fileListViewModels = [];

        /// <inheritdoc />
        public void Dispose()
        {
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
        /// Shows a Rename List view hosted in a window.
        /// </summary>
        /// <param name="viewModel">Rename List view model.</param>
        /// <param name="width">Host window width.</param>
        /// <param name="height">Host window height.</param>
        /// <returns>View and host window.</returns>
        public (RenameListView View, Window Window) Show(
            RenameListViewModel viewModel,
            double width = 600,
            double height = 300
        )
        {
            var view = new RenameListView { DataContext = viewModel };
            var window = new Window
            {
                Width = width,
                Height = height,
                Content = view,
            };
            window.Show();
            window.UpdateLayout();
            return (view, window);
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

            var (view, window) = Show(renameListViewModel, width: 800, height: 180);
            Dispatcher.UIThread.RunJobs();

            var grid = view.GetVisualDescendants().OfType<DataGrid>().Single();
            return (renameListViewModel, window, grid);
        }
    }
}
