using Mfr.App.Ui.Services.RenameLog;
using Mfr.App.Ui.ViewModels.LogDialog;
using Mfr.Utils;

namespace Mfr.Tests.Ui.LogDialog
{
    /// <summary>
    /// View-model tests for the Rename Log dialog list, details, Erase, and Undo selection.
    /// </summary>
    [Collection(ConfigStoreCollection.Name)]
    public sealed class RenameLogDialogViewModelTests : IDisposable
    {
        private readonly TempDirectoryFixture _tempDirectoryFixture = new();

        public RenameLogDialogViewModelTests()
        {
            ConfigStoreTestReset.LoadEmpty();
            RenameLogStore.ClearLastOperation();
        }

        /// <inheritdoc />
        public void Dispose()
        {
            RenameLogStore.ClearLastOperation();
            ConfigStoreTestReset.LoadEmpty();
            _tempDirectoryFixture.Dispose();
        }

        /// <summary>
        /// Verifies last operation appears first with a timestamp title, followed by disk logs newest-first.
        /// </summary>
        [Fact]
        public void Lists_LastOperation_Then_Disk_Newest_First()
        {
            var logDir = _tempDirectoryFixture.CreateTempDir();
            var olderPath = _WriteDiskLog(
                logDir,
                stamp: "20260101000000",
                destinationPath: TestPaths.Absolute("older.txt")
            );
            var newerPath = _WriteDiskLog(
                logDir,
                stamp: "20260301120000",
                destinationPath: TestPaths.Absolute("newer.txt")
            );

            RenameLogStore.CaptureFromCommit(
                [
                    new RenameResultItem(
                        OriginalPath: TestPaths.Absolute("mem-old.txt"),
                        Status: RenameStatus.CommitOk,
                        Error: null,
                        Changes: [new RenamePropertyChange("Prefix", "mem-old", "mem-new")],
                        DestinationPath: TestPaths.Absolute("mem-new.txt"),
                        IsFolder: false
                    ),
                ],
                directoryPath: logDir,
                limit: 0
            );
            Assert.NotNull(RenameLogStore.LastOperation);

            var viewModel = new RenameLogDialogViewModel(logDir);

            Assert.Equal(3, viewModel.Items.Count);
            Assert.Equal(
                RenameLogDisplay.FormatListTitle(RenameLogStore.LastOperation.CommittedAt),
                viewModel.Items[0].Title
            );
            Assert.Equal(
                RenameLogDisplay.FormatListSummary(RenameLogStore.LastOperation),
                viewModel.Items[0].Summary
            );
            Assert.True(viewModel.Items[0].IsLastOperation);
            Assert.Equal(RenameLogDisplay.FormatDiskListTitle(newerPath), viewModel.Items[1].Title);
            Assert.Equal("GO · 1 item", viewModel.Items[1].Summary);
            Assert.Equal(RenameLogDisplay.FormatDiskListTitle(olderPath), viewModel.Items[2].Title);
            Assert.Equal("GO · 1 item", viewModel.Items[2].Summary);
            Assert.Same(viewModel.Items[0], viewModel.SelectedItem);
            Assert.Contains("mem-new.txt", viewModel.DetailsText);
            Assert.True(viewModel.UndoCommand.CanExecute(null));
            Assert.True(viewModel.EraseCommand.CanExecute(null));
        }

        /// <summary>
        /// Verifies an in-memory last op already written to disk appears once (disk row only).
        /// </summary>
        [Fact]
        public void Dedups_LastOperation_When_Also_On_Disk()
        {
            var logDir = _tempDirectoryFixture.CreateTempDir();
            var written = RenameLogStore.CaptureFromCommit(
                [
                    new RenameResultItem(
                        OriginalPath: TestPaths.Absolute("a.txt"),
                        Status: RenameStatus.CommitOk,
                        Error: null,
                        Changes: [new RenamePropertyChange("Prefix", "a", "b")],
                        DestinationPath: TestPaths.Absolute("b.txt"),
                        IsFolder: false
                    ),
                ],
                directoryPath: logDir,
                limit: 100
            );
            Assert.NotNull(written);
            Assert.NotNull(RenameLogStore.LastOperation);

            var viewModel = new RenameLogDialogViewModel(logDir);

            var item = Assert.Single(viewModel.Items);
            Assert.False(item.IsLastOperation);
            Assert.Equal(written, item.FilePath);
            Assert.Equal(written, RenameLogStore.LastWrittenFilePath);
            Assert.Equal(RenameLogDisplay.FormatDiskListTitle(written), item.Title);
            Assert.Equal(RenameLogDisplay.FormatListSummary(RenameLogStore.LastOperation), item.Summary);
            Assert.Same(RenameLogStore.LastOperation, item.TryGetLog(out var error));
            Assert.Null(error);
            Assert.Contains("b.txt", viewModel.DetailsText);
        }

        /// <summary>
        /// Verifies Erase on last operation removes the list row but keeps <see cref="RenameLogStore.LastOperation"/>.
        /// </summary>
        [Fact]
        public async Task Erase_LastOperation_Removes_Row_Only()
        {
            RenameLogStore.CaptureFromCommit(
                [
                    new RenameResultItem(
                        OriginalPath: TestPaths.Absolute("a.txt"),
                        Status: RenameStatus.CommitOk,
                        Error: null,
                        Changes: [new RenamePropertyChange("Prefix", "a", "b")],
                        DestinationPath: TestPaths.Absolute("b.txt"),
                        IsFolder: false
                    ),
                ],
                limit: 0
            );
            Assert.NotNull(RenameLogStore.LastOperation);

            var viewModel = new RenameLogDialogViewModel(directoryPath: _tempDirectoryFixture.CreateTempDir());
            Assert.Single(viewModel.Items);

            await viewModel.EraseCommand.ExecuteAsync(null);

            Assert.Empty(viewModel.Items);
            Assert.Null(viewModel.SelectedItem);
            Assert.NotNull(RenameLogStore.LastOperation);
        }

        /// <summary>
        /// Verifies Erase deletes the disk file and drops it from the list.
        /// </summary>
        [Fact]
        public async Task Erase_DiskLog_Deletes_File()
        {
            var logDir = _tempDirectoryFixture.CreateTempDir();
            var path = _WriteDiskLog(logDir, stamp: "20260401101010", destinationPath: TestPaths.Absolute("disk.txt"));
            var viewModel = new RenameLogDialogViewModel(logDir);
            Assert.Single(viewModel.Items);
            Assert.True(File.Exists(path));

            await viewModel.EraseCommand.ExecuteAsync(null);

            Assert.Empty(viewModel.Items);
            Assert.False(File.Exists(path));
        }

        /// <summary>
        /// Verifies Undo sets <see cref="RenameLogDialogViewModel.LogToUndo"/> and requests close with accept.
        /// </summary>
        [Fact]
        public void Undo_Sets_LogToUndo_And_Requests_Close()
        {
            var logDir = _tempDirectoryFixture.CreateTempDir();
            _WriteDiskLog(logDir, stamp: "20260505151515", destinationPath: TestPaths.Absolute("disk.txt"));
            var viewModel = new RenameLogDialogViewModel(logDir);
            bool? accepted = null;
            viewModel.CloseRequested = value => accepted = value;

            viewModel.UndoCommand.Execute(null);

            Assert.True(accepted);
            Assert.NotNull(viewModel.LogToUndo);
            Assert.Equal(TestPaths.Absolute("disk.txt"), Assert.Single(viewModel.LogToUndo.Entries).DestinationPath);
        }

        /// <summary>
        /// Verifies selecting a disk log populates details from the file.
        /// </summary>
        [Fact]
        public void Selecting_DiskLog_Shows_Details()
        {
            var logDir = _tempDirectoryFixture.CreateTempDir();
            _WriteDiskLog(logDir, stamp: "20260606161616", destinationPath: TestPaths.Absolute("shown.txt"));
            var viewModel = new RenameLogDialogViewModel(logDir);

            Assert.Contains("shown.txt", viewModel.DetailsText);
            Assert.Contains("Changed 'Prefix'", viewModel.DetailsText);
        }

        private static string _WriteDiskLog(string logDir, string stamp, string destinationPath)
        {
            var written = RenameLogStore.CaptureFromCommit(
                [
                    new RenameResultItem(
                        OriginalPath: TestPaths.Absolute("orig.txt"),
                        Status: RenameStatus.CommitOk,
                        Error: null,
                        Changes: [new RenamePropertyChange("Prefix", "orig", "shown")],
                        DestinationPath: destinationPath,
                        IsFolder: false
                    ),
                ],
                directoryPath: logDir,
                limit: 100
            );
            Assert.NotNull(written);

            var path = logDir.CombinePath(stamp + RenameLogStore.FileExtension);
            if (!string.Equals(written, path, StringComparison.OrdinalIgnoreCase))
            {
                File.Move(written, path, overwrite: true);
            }

            // CaptureFromCommit also sets LastOperation; disk-only tests clear it explicitly.
            RenameLogStore.ClearLastOperation();
            return path;
        }
    }
}
