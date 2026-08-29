using Avalonia.Controls;
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
