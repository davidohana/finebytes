using Avalonia;
using Avalonia.Controls;
using Avalonia.Headless;
using Avalonia.Input;
using Avalonia.Platform.Storage;
using Avalonia.Threading;
using Avalonia.VisualTree;
using Mfr.App.Ui.Services.FileList;
using Mfr.App.Ui.Services.Shell;
using Mfr.App.Ui.ViewModels.AppliedFilters;
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
        /// Original field key for Parent Directory.
        /// </summary>
        internal static RenameListFieldKey ParentFolderKey =>
            RenameListFieldKey.Original(BasicRenameListField.Group, BasicRenameListFields.Key.Folder);

        /// <summary>
        /// Original field key for Full File Name.
        /// </summary>
        internal static RenameListFieldKey FullFileNameKey =>
            RenameListFieldKey.Original(BasicRenameListField.Group, BasicRenameListFields.Key.FullName);

        /// <summary>
        /// Original field key for Full Path.
        /// </summary>
        internal static RenameListFieldKey FullPathKey =>
            RenameListFieldKey.Original(BasicRenameListField.Group, BasicRenameListFields.Key.FullPath);

        /// <summary>
        /// Builds a one-field sort list for <see cref="RenameListViewModel.ApplySession"/>.
        /// </summary>
        /// <param name="fieldKey">Sort field key.</param>
        /// <param name="descending">When <see langword="true"/>, sort descending.</param>
        /// <returns>Single-element sort key list.</returns>
        internal static List<RenameListSortKey> SortSession(RenameListFieldKey fieldKey, bool descending = false)
        {
            return [new RenameListSortKey(fieldKey, descending)];
        }

        /// <summary>
        /// Scrolls <paramref name="entry"/> / <paramref name="fieldKey"/> into the host viewport.
        /// </summary>
        /// <param name="grid">Rename List grid.</param>
        /// <param name="entry">Row entry.</param>
        /// <param name="fieldKey">Column field key.</param>
        internal static void ScrollFieldIntoView(DataGrid grid, RenameListEntry entry, RenameListFieldKey fieldKey)
        {
            var column = grid.Columns.FirstOrDefault(item => RenameListGridColumns.GetFieldKey(item) == fieldKey);
            Assert.NotNull(column);
            grid.ScrollIntoView(entry, column);
        }

        /// <summary>
        /// Window point over the center of <paramref name="fieldKey"/> on <paramref name="entry"/>.
        /// </summary>
        /// <param name="window">Host window.</param>
        /// <param name="grid">Rename List grid.</param>
        /// <param name="entry">Row entry.</param>
        /// <param name="fieldKey">Column field key.</param>
        /// <returns>Point in window coordinates.</returns>
        internal static Point FieldCellWindowPoint(
            Window window,
            DataGrid grid,
            RenameListEntry entry,
            RenameListFieldKey fieldKey
        )
        {
            var row = grid.GetVisualDescendants()
                .OfType<DataGridRow>()
                .FirstOrDefault(item => ReferenceEquals(item.DataContext, entry));
            Assert.NotNull(row);

            var x = 0.0;
            var found = false;
            foreach (var column in grid.Columns.OrderBy(item => item.DisplayIndex))
            {
                var width = column.Width.IsAbsolute ? column.Width.Value : column.ActualWidth;
                if (RenameListGridColumns.GetFieldKey(column) == fieldKey)
                {
                    x += width / 2;
                    found = true;
                    break;
                }

                x += width;
            }

            Assert.True(found);
            var windowPoint = row.TranslatePoint(new Point(x, Math.Max(1, row.Bounds.Height / 2)), window);
            Assert.True(windowPoint.HasValue);
            return windowPoint.Value;
        }

        /// <summary>
        /// Scrolls a field cell into view and left-clicks it through the host window.
        /// </summary>
        /// <param name="window">Host window.</param>
        /// <param name="grid">Rename List grid.</param>
        /// <param name="entry">Row entry.</param>
        /// <param name="fieldKey">Column field key.</param>
        internal static void ClickFieldCell(
            Window window,
            DataGrid grid,
            RenameListEntry entry,
            RenameListFieldKey fieldKey
        )
        {
            // Wide absolute columns can sit past the host width; scroll before hit-test.
            ScrollFieldIntoView(grid, entry, fieldKey);
            window.UpdateLayout();
            Dispatcher.UIThread.RunJobs();

            var windowPoint = FieldCellWindowPoint(window, grid, entry, fieldKey);
            window.MouseMove(windowPoint, RawInputModifiers.None);
            window.MouseDown(windowPoint, MouseButton.Left, RawInputModifiers.None);
            window.MouseUp(windowPoint, MouseButton.Left, RawInputModifiers.None);
            window.UpdateLayout();
            Dispatcher.UIThread.RunJobs();
        }

        /// <summary>
        /// Scrolls a field cell into view and moves the pointer over it (no click).
        /// </summary>
        /// <param name="window">Host window.</param>
        /// <param name="grid">Rename List grid.</param>
        /// <param name="entry">Row entry.</param>
        /// <param name="fieldKey">Column field key.</param>
        internal static void MoveOverFieldCell(
            Window window,
            DataGrid grid,
            RenameListEntry entry,
            RenameListFieldKey fieldKey
        )
        {
            ScrollFieldIntoView(grid, entry, fieldKey);
            window.UpdateLayout();
            Dispatcher.UIThread.RunJobs();

            var windowPoint = FieldCellWindowPoint(window, grid, entry, fieldKey);
            window.MouseMove(windowPoint, RawInputModifiers.None);
            Dispatcher.UIThread.RunJobs();
        }

        /// <summary>
        /// Builds a dummy internal-reorder <see cref="DataTransfer"/> used by Rename List row drags.
        /// </summary>
        /// <returns>Transfer containing <see cref="RenameListDragFormats.InternalReorder"/>.</returns>
        internal static DataTransfer CreateInternalReorderDataTransfer()
        {
            var dataTransfer = new DataTransfer();
            dataTransfer.Add(DataTransferItem.Create(RenameListDragFormats.InternalReorder, "1"));
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
        /// <param name="shellOpener">
        /// Shell opener for Properties / Show in Explorer, or <see langword="null"/> for the null opener.
        /// </param>
        /// <param name="appliedFilters">
        /// Applied Filters for Edit as Name List tests, or <see langword="null"/> when not needed.
        /// </param>
        /// <returns>Rename List view model.</returns>
        public RenameListViewModel CreateRenameListViewModel(
            string? directoryPath = null,
            IFileShellOpener? shellOpener = null,
            AppliedFiltersViewModel? appliedFilters = null
        )
        {
            return new RenameListViewModel(
                CreateFileListViewModel(directoryPath ?? CreateTempDir()),
                shellOpener ?? NullFileShellOpener.Instance,
                appliedFilters
            );
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
