using Avalonia.Controls;
using Avalonia.Headless.XUnit;
using Avalonia.Input;
using Avalonia.Platform.Storage;
using Avalonia.Threading;
using Mfr.App.Ui.Services.FileList;
using Mfr.App.Ui.ViewModels.FileList;
using Mfr.App.Ui.ViewModels.RenameList;
using Mfr.App.Ui.Views.RenameList;

namespace Mfr.Tests.Ui
{
    /// <summary>
    /// Headless tests for Rename List drag-drop intake.
    /// </summary>
    public sealed class RenameListViewDropTests : IDisposable
    {
        private readonly TempDirectoryFixture _tempDirectoryFixture = new();
        private readonly List<FileListViewModel> _fileListViewModels = [];
        private readonly UiConfig _originalUiConfig;

        /// <summary>
        /// Snapshots UI add-policy config for tests that may change it.
        /// </summary>
        public RenameListViewDropTests()
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
        /// Verifies a File drop on Rename List adds matching paths.
        /// </summary>
        [AvaloniaFact]
        public async Task Drop_Files_Adds_Paths()
        {
            var dir = _tempDirectoryFixture.CreateTempDir();
            var alphaPath = Path.Combine(dir, "alpha.txt");
            var betaPath = Path.Combine(dir, "beta.md");
            File.WriteAllText(alphaPath, "a");
            File.WriteAllText(betaPath, "b");

            var fileListViewModel = _CreateFileListViewModel(dir);
            var renameListViewModel = new RenameListViewModel(fileListViewModel);
            var (view, window) = _Show(renameListViewModel);

            Assert.True(DragDrop.GetAllowDrop(view));

            var dataTransfer = await _CreateFileDataTransferAsync(window, [alphaPath, betaPath]);
            var dropArgs = new DragEventArgs(DragDrop.DropEvent, dataTransfer, view, default, KeyModifiers.None);
            view.RaiseEvent(dropArgs);

            await _WaitUntil(() => renameListViewModel.Entries.Count == 2);
            Assert.Equal(
                ["alpha.txt", "beta.md"],
                renameListViewModel.Entries.Select(entry => entry.FullFileName).OrderBy(n => n, StringComparer.Ordinal)
            );

            window.Close();
        }

        /// <summary>
        /// Verifies DragOver rejects non-file payloads.
        /// </summary>
        [AvaloniaFact]
        public void DragOver_NonFile_Sets_None()
        {
            var dir = _tempDirectoryFixture.CreateTempDir();
            var fileListViewModel = _CreateFileListViewModel(dir);
            var renameListViewModel = new RenameListViewModel(fileListViewModel);
            var (view, window) = _Show(renameListViewModel);

            var dataTransfer = new DataTransfer();
            dataTransfer.Add(DataTransferItem.CreateText("not-a-file"));
            var dragOverArgs = new DragEventArgs(DragDrop.DragOverEvent, dataTransfer, view, default, KeyModifiers.None)
            {
                DragEffects = DragDropEffects.Copy,
            };
            view.RaiseEvent(dragOverArgs);

            Assert.Equal(DragDropEffects.None, dragOverArgs.DragEffects);
            window.Close();
        }

        /// <summary>
        /// Verifies DragOver accepts files when not adding.
        /// </summary>
        [AvaloniaFact]
        public async Task DragOver_Files_Sets_Copy()
        {
            var dir = _tempDirectoryFixture.CreateTempDir();
            var alphaPath = Path.Combine(dir, "alpha.txt");
            File.WriteAllText(alphaPath, "a");
            var fileListViewModel = _CreateFileListViewModel(dir);
            var renameListViewModel = new RenameListViewModel(fileListViewModel);
            var (view, window) = _Show(renameListViewModel);

            var dataTransfer = await _CreateFileDataTransferAsync(window, [alphaPath]);
            var dragOverArgs = new DragEventArgs(DragDrop.DragOverEvent, dataTransfer, view, default, KeyModifiers.None)
            {
                DragEffects = DragDropEffects.None,
            };
            view.RaiseEvent(dragOverArgs);

            Assert.Equal(DragDropEffects.Copy, dragOverArgs.DragEffects);
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
                Width = 600,
                Height = 300,
                Content = view,
            };
            window.Show();
            window.UpdateLayout();
            return (view, window);
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

        private static async Task _WaitUntil(Func<bool> condition)
        {
            for (var i = 0; i < 200; i++)
            {
                if (condition())
                {
                    return;
                }

                await Task.Delay(10).ConfigureAwait(true);
                Dispatcher.UIThread.RunJobs();
            }

            Assert.Fail("Timed out waiting for condition.");
        }
    }
}
