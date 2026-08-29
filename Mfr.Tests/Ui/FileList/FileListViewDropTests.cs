using Avalonia.Controls;
using Avalonia.Headless.XUnit;
using Avalonia.Input;
using Avalonia.Platform.Storage;
using Avalonia.Threading;
using Mfr.App.Ui.Services.FileList;
using Mfr.App.Ui.ViewModels.FileList;
using Mfr.App.Ui.ViewModels.RenameList;
using Mfr.App.Ui.Views.FileList;
using Mfr.App.Ui.Views.RenameList;

namespace Mfr.Tests.Ui.FileList
{
    /// <summary>
    /// Headless tests for dropping Rename List rows onto the File List pane.
    /// </summary>
    public sealed class FileListViewDropTests : IDisposable
    {
        private readonly TempDirectoryFixture _tempDirectoryFixture = new();
        private readonly List<FileListViewModel> _viewModels = [];

        /// <inheritdoc />
        public void Dispose()
        {
            foreach (var viewModel in _viewModels)
            {
                viewModel.Dispose();
            }

            _tempDirectoryFixture.Dispose();
        }

        /// <summary>
        /// Verifies DragOver accepts Rename List internal reorder payloads as Move.
        /// </summary>
        [AvaloniaFact]
        public async Task DragOver_RenameList_Payload_Sets_Move()
        {
            var (_, renameListViewModel, view, window) = await _ShowWithRenameListAsync();
            renameListViewModel.SetSelectedEntries([renameListViewModel.Entries[0]]);

            var dataTransfer = _CreateInternalReorderDataTransfer();
            var dragOverArgs = new DragEventArgs(DragDrop.DragOverEvent, dataTransfer, view, default, KeyModifiers.None)
            {
                DragEffects = DragDropEffects.None,
            };
            view.RaiseEvent(dragOverArgs);

            Assert.Equal(DragDropEffects.Move, dragOverArgs.DragEffects);
            window.Close();
        }

        /// <summary>
        /// Verifies dropping a Rename List row removes it without selecting anything in File List.
        /// </summary>
        [AvaloniaFact]
        public async Task Drop_RenameList_Removes_Row_Without_File_List_Selection()
        {
            var (fileListViewModel, renameListViewModel, view, window) = await _ShowWithRenameListAsync();
            var alphaEntry = renameListViewModel.Entries.First(entry => entry.FullFileName == "alpha.txt");
            renameListViewModel.SetSelectedEntries([alphaEntry]);

            view.RaiseEvent(
                new DragEventArgs(
                    DragDrop.DropEvent,
                    _CreateInternalReorderDataTransfer(),
                    view,
                    default,
                    KeyModifiers.None
                )
            );
            _PumpDeferredDrop();

            Assert.Single(renameListViewModel.Entries);
            Assert.Equal("beta.md", renameListViewModel.Entries[0].FullFileName);
            Assert.Empty(fileListViewModel.SelectedEntries);
            Assert.Null(fileListViewModel.SelectedEntry);

            var list = view.FindControl<ListBox>("ListViewList");
            Assert.NotNull(list);
            Assert.Empty(list.SelectedItems!);

            window.Close();
        }

        /// <summary>
        /// Verifies drop clears a transient listing selection under the pointer.
        /// </summary>
        [AvaloniaFact]
        public async Task Drop_Clears_Transient_Listing_Selection()
        {
            var (fileListViewModel, renameListViewModel, view, window) = await _ShowWithRenameListAsync();
            var beta = fileListViewModel.Entries.First(entry => entry.Name == "beta.md");
            var alphaEntry = renameListViewModel.Entries.First(entry => entry.FullFileName == "alpha.txt");
            renameListViewModel.SetSelectedEntries([alphaEntry]);

            var list = view.FindControl<ListBox>("ListViewList");
            Assert.NotNull(list);
            list.SelectedItems!.Clear();
            list.SelectedItems.Add(beta);
            list.SelectedItem = beta;
            fileListViewModel.SetSelectedEntries([beta], beta);

            view.RaiseEvent(
                new DragEventArgs(
                    DragDrop.DropEvent,
                    _CreateInternalReorderDataTransfer(),
                    view,
                    default,
                    KeyModifiers.None
                )
            );
            _PumpDeferredDrop();

            Assert.Single(renameListViewModel.Entries);
            Assert.Empty(fileListViewModel.SelectedEntries);
            Assert.Empty(list.SelectedItems!);

            window.Close();
        }

        /// <summary>
        /// Verifies DragOver only advertises Move for Rename List internal payloads.
        /// </summary>
        [AvaloniaFact]
        public async Task DragOver_NonRenameList_Payload_Does_Not_Advertise_Move()
        {
            var (_, renameListViewModel, view, window) = await _ShowWithRenameListAsync();
            renameListViewModel.SetSelectedEntries([renameListViewModel.Entries[0]]);

            var textTransfer = new DataTransfer();
            textTransfer.Add(DataTransferItem.CreateText("not-a-rename-list-row"));
            var textArgs = new DragEventArgs(DragDrop.DragOverEvent, textTransfer, view, default, KeyModifiers.None)
            {
                DragEffects = DragDropEffects.Copy,
            };
            view.RaiseEvent(textArgs);
            Assert.NotEqual(DragDropEffects.Move, textArgs.DragEffects);

            var alphaPath = Path.Combine(((FileListViewModel)view.DataContext!).CurrentPath, "alpha.txt");
            var fileTransfer = await _CreateFileDataTransferAsync(window, [alphaPath]).ConfigureAwait(true);
            var fileArgs = new DragEventArgs(DragDrop.DragOverEvent, fileTransfer, view, default, KeyModifiers.None)
            {
                DragEffects = DragDropEffects.Copy,
            };
            view.RaiseEvent(fileArgs);
            Assert.NotEqual(DragDropEffects.Move, fileArgs.DragEffects);

            window.Close();
        }

        /// <summary>
        /// Verifies dropping a foreign payload does not remove Rename List rows.
        /// </summary>
        [AvaloniaFact]
        public async Task Drop_NonRenameList_Payload_Does_Not_Remove()
        {
            var (_, renameListViewModel, view, window) = await _ShowWithRenameListAsync();
            renameListViewModel.SetSelectedEntries([renameListViewModel.Entries[0]]);

            var dataTransfer = new DataTransfer();
            dataTransfer.Add(DataTransferItem.CreateText("not-a-rename-list-row"));
            view.RaiseEvent(new DragEventArgs(DragDrop.DropEvent, dataTransfer, view, default, KeyModifiers.None));
            Dispatcher.UIThread.RunJobs();

            Assert.Equal(2, renameListViewModel.Entries.Count);
            window.Close();
        }

        private async Task<(
            FileListViewModel FileListViewModel,
            RenameListViewModel RenameListViewModel,
            FileListView View,
            Window Window
        )> _ShowWithRenameListAsync()
        {
            var dir = _CreateSampleDir();
            var fileListViewModel = new FileListViewModel(
                NullSystemIconProvider.Instance,
                dir,
                NullFileShellOpener.Instance
            );
            _viewModels.Add(fileListViewModel);
            fileListViewModel.SetViewMode(FileListViewMode.List);

            var renameListViewModel = new RenameListViewModel(fileListViewModel);
            var alpha = fileListViewModel.Entries.First(entry => entry.Name == "alpha.txt");
            var beta = fileListViewModel.Entries.First(entry => entry.Name == "beta.md");
            fileListViewModel.SetSelectedEntries([alpha, beta]);
            await renameListViewModel.AddSelectedCommand.ExecuteAsync(null).ConfigureAwait(true);

            var view = new FileListView
            {
                DataContext = fileListViewModel,
                RemoveSelectedFromRenameListCommand = renameListViewModel.RemoveSelectedCommand,
            };
            var window = new Window
            {
                Width = 420,
                Height = 360,
                Content = view,
            };
            window.Show();
            window.UpdateLayout();
            Dispatcher.UIThread.RunJobs();
            return (fileListViewModel, renameListViewModel, view, window);
        }

        private string _CreateSampleDir()
        {
            var dir = _tempDirectoryFixture.CreateTempDir();
            File.WriteAllText(Path.Combine(dir, "alpha.txt"), "a");
            File.WriteAllText(Path.Combine(dir, "beta.md"), "b");
            return dir;
        }

        private static void _PumpDeferredDrop()
        {
            Dispatcher.UIThread.RunJobs();
            Dispatcher.UIThread.RunJobs();
        }

        private static DataTransfer _CreateInternalReorderDataTransfer()
        {
            var dataTransfer = new DataTransfer();
            dataTransfer.Add(DataTransferItem.Create(RenameListView.InternalReorderFormat, "1"));
            return dataTransfer;
        }

        private static async Task<DataTransfer> _CreateFileDataTransferAsync(Window window, IReadOnlyList<string> paths)
        {
            var storage = window.StorageProvider;
            var dataTransfer = new DataTransfer();
            foreach (var path in paths)
            {
                var item = await storage.TryGetFileFromPathAsync(path).ConfigureAwait(true);
                Assert.NotNull(item);
                dataTransfer.Add(DataTransferItem.CreateFile(item));
            }

            return dataTransfer;
        }
    }
}
