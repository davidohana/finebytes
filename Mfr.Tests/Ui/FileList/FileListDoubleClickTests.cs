using System.Windows.Input;
using Avalonia.Controls;
using Avalonia.Headless.XUnit;
using Avalonia.Input;
using Avalonia.Threading;
using Mfr.App.Ui.Services.FileList;
using Mfr.App.Ui.Services.Shell;
using Mfr.App.Ui.ViewModels.FileList;
using Mfr.App.Ui.Views.FileList;
using Mfr.Models.Config;

namespace Mfr.Tests.Ui.FileList
{
    /// <summary>
    /// Headless tests for File List double-tap open vs add-to-Rename-List.
    /// </summary>
    [Collection(ConfigStoreCollection.Name)]
    public sealed class FileListDoubleClickTests : IDisposable
    {
        private readonly TempDirectoryFixture _tempDirectoryFixture = new();
        private readonly List<FileListViewModel> _viewModels = [];

        /// <summary>
        /// Resets config defaults for isolated flag tests.
        /// </summary>
        public FileListDoubleClickTests()
        {
            ConfigStoreTestReset.LoadEmpty();
        }

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
        /// Verifies double-tap opens the selected file when the Options flag is off.
        /// </summary>
        [AvaloniaFact]
        public void DoubleTap_When_Flag_Off_Opens_Selected_File()
        {
            ConfigStore.Config.Ui.DoubleClickAddsToRenameList = false;
            var shell = new RecordingFileShellOpener();
            var addCommand = new CountingCommand();
            var (window, list, file) = _ShowListWithSelectedFile(shell, addCommand);

            list.RaiseEvent(new TappedEventArgs(InputElement.DoubleTappedEvent, null!));
            Dispatcher.UIThread.RunJobs();

            Assert.Equal([file.FullPath], shell.OpenedWithDefaultApp);
            Assert.Equal(0, addCommand.ExecuteCount);

            window.Close();
        }

        /// <summary>
        /// Verifies double-tap runs Add Selected when the Options flag is on.
        /// </summary>
        [AvaloniaFact]
        public void DoubleTap_When_Flag_On_Runs_AddSelected()
        {
            ConfigStore.Config.Ui.DoubleClickAddsToRenameList = true;
            var shell = new RecordingFileShellOpener();
            var addCommand = new CountingCommand();
            var (window, list, _) = _ShowListWithSelectedFile(shell, addCommand);

            list.RaiseEvent(new TappedEventArgs(InputElement.DoubleTappedEvent, null!));
            Dispatcher.UIThread.RunJobs();

            Assert.Equal(1, addCommand.ExecuteCount);
            Assert.Empty(shell.OpenedWithDefaultApp);

            window.Close();
        }

        /// <summary>
        /// Verifies the open-vs-add decision table for the double-click helper.
        /// </summary>
        /// <param name="addsToRenameList">Config flag value.</param>
        /// <param name="addCanExecute">Whether Add Selected reports CanExecute.</param>
        /// <param name="openCanExecute">Whether Open Selected reports CanExecute.</param>
        /// <param name="expectedAddCount">Expected Add Selected execute count.</param>
        /// <param name="expectedOpenCount">Expected Open Selected execute count.</param>
        [Theory]
        [InlineData(false, true, true, 0, 1)]
        [InlineData(true, true, true, 1, 0)]
        [InlineData(true, false, true, 0, 1)]
        [InlineData(true, true, false, 1, 0)]
        [InlineData(true, false, false, 0, 0)]
        public void Execute_Matches_Flag_And_CanExecute_Table(
            bool addsToRenameList,
            bool addCanExecute,
            bool openCanExecute,
            int expectedAddCount,
            int expectedOpenCount
        )
        {
            var addCommand = new CountingCommand(addCanExecute);
            var openCommand = new CountingCommand(openCanExecute);

            FileListDoubleClickAction.Execute(
                addsToRenameList: addsToRenameList,
                addSelectedCommand: addCommand,
                openSelectedCommand: openCommand
            );

            Assert.Equal(expectedAddCount, addCommand.ExecuteCount);
            Assert.Equal(expectedOpenCount, openCommand.ExecuteCount);
        }

        private (Window Window, ListBox List, FileListEntry File) _ShowListWithSelectedFile(
            IFileShellOpener shell,
            ICommand addSelectedCommand
        )
        {
            var dir = _tempDirectoryFixture.CreateTempDir();
            File.WriteAllText(Path.Combine(dir, "alpha.txt"), "a");

            var viewModel = new FileListViewModel(NullSystemIconProvider.Instance, dir, shell);
            _viewModels.Add(viewModel);
            viewModel.SetViewMode(FileListViewMode.List);

            var view = new FileListView { DataContext = viewModel, AddSelectedCommand = addSelectedCommand };
            var window = new Window
            {
                Width = 420,
                Height = 360,
                Content = view,
            };
            window.Show();
            window.UpdateLayout();
            Dispatcher.UIThread.RunJobs();

            var list = view.FindControl<ListBox>("ListViewList");
            Assert.NotNull(list);
            Assert.True(list.IsVisible);

            var file = viewModel.Entries.First(entry => entry.Name == "alpha.txt");
            viewModel.SetSelectedEntries([file], file);
            Dispatcher.UIThread.RunJobs();

            return (window, list, file);
        }

        private sealed class CountingCommand(bool canExecute = true) : ICommand
        {
            public int ExecuteCount { get; private set; }

            public bool CanExecute(object? parameter)
            {
                return canExecute;
            }

            public void Execute(object? parameter)
            {
                ExecuteCount++;
            }

#pragma warning disable CS0067 // Required by ICommand; unused in headless counting.
            public event EventHandler? CanExecuteChanged;
#pragma warning restore CS0067
        }
    }
}
