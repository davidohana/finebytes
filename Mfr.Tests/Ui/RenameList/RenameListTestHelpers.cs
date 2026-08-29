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

        /// <summary>
        /// Snapshots add-policy, remember, and display flags from <see cref="SessionStore.Current"/>.
        /// </summary>
        /// <returns>Preference values to restore after a test.</returns>
        internal static SessionPrefSnapshot SnapshotSessionPrefs()
        {
            var session = SessionStore.Current;
            return new SessionPrefSnapshot(
                AddMode: session.RenameList?.AddMode ?? RenameListAddMode.Files,
                AddFolderContents: session.RenameList?.AddFolderContents ?? true,
                UseFixedWidthFont: session.RenameList?.UseFixedWidthFont ?? false,
                RememberWindowState: session.MainWindow?.RememberWindowState ?? true,
                RememberLastFolder: session.FileList?.RememberLastFolder ?? true
            );
        }

        /// <summary>
        /// Restores add-policy, remember, and display flags onto <see cref="SessionStore.Current"/>.
        /// </summary>
        /// <param name="snapshot">Previously captured preferences.</param>
        internal static void RestoreSessionPrefs(SessionPrefSnapshot snapshot)
        {
            ArgumentNullException.ThrowIfNull(snapshot);

            var session = SessionStore.Current;
            var renameList = session.EnsureRenameList();
            renameList.AddMode = snapshot.AddMode;
            renameList.AddFolderContents = snapshot.AddFolderContents;
            renameList.UseFixedWidthFont = snapshot.UseFixedWidthFont;
            session.EnsureMainWindow().RememberWindowState = snapshot.RememberWindowState;
            session.EnsureFileList().RememberLastFolder = snapshot.RememberLastFolder;
        }
    }

    /// <summary>
    /// Add-policy, remember, and display flags copied from <see cref="SessionStore.Current"/>.
    /// </summary>
    /// <param name="AddMode">Rename List add mode.</param>
    /// <param name="AddFolderContents">Whether folder sources recurse.</param>
    /// <param name="UseFixedWidthFont">Rename List fixed-width font flag.</param>
    /// <param name="RememberWindowState">Whether window geometry is remembered.</param>
    /// <param name="RememberLastFolder">Whether the last File List folder is remembered.</param>
    internal sealed record SessionPrefSnapshot(
        RenameListAddMode AddMode,
        bool AddFolderContents,
        bool UseFixedWidthFont,
        bool RememberWindowState,
        bool RememberLastFolder
    );

    /// <summary>
    /// Temp folders, File List hosts, and optional UI add-policy pinning for Rename List tests.
    /// </summary>
    internal sealed class RenameListUiTestContext : IDisposable
    {
        private readonly TempDirectoryFixture _tempDirectoryFixture = new();
        private readonly List<FileListViewModel> _fileListViewModels = [];
        private readonly SessionPrefSnapshot? _originalPrefs;

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

            _originalPrefs = RenameListTestHelpers.SnapshotSessionPrefs();
            var renameList = SessionStore.Current.EnsureRenameList();
            renameList.AddMode = RenameListAddMode.Files;
            renameList.AddFolderContents = true;
        }

        /// <inheritdoc />
        public void Dispose()
        {
            if (_originalPrefs is not null)
            {
                RenameListTestHelpers.RestoreSessionPrefs(_originalPrefs);
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
