using Mfr.App.Ui.ViewModels.RenameList;

namespace Mfr.Tests.Ui.RenameList
{
    /// <summary>
    /// Tests for Rename List Show Rename Error command state and content.
    /// </summary>
    public sealed class RenameListViewModelCommitErrorsTests : IDisposable
    {
        private readonly RenameListUiTestContext _context = new();

        /// <inheritdoc />
        public void Dispose()
        {
            _context.Dispose();
        }

        /// <summary>
        /// Verifies Show Rename Error is available for one selected commit-error row and raises shared dialog content.
        /// </summary>
        [Fact]
        public async Task ShowCommitError_available_for_row_with_commit_error()
        {
            var dir = _context.CreateTempDir();
            var path = Path.Combine(dir, "note.txt");
            await File.WriteAllTextAsync(path, "plain text");
            var renameListViewModel = _context.CreateRenameListViewModel(dir);
            await renameListViewModel.AddPathsAsync([path]).ConfigureAwait(true);

            var entry = Assert.Single(renameListViewModel.Entries);
            var cause = new IOException("destination unavailable");
            entry.EngineItem.CommitError = new RenameItemError("The destination could not be written.", cause);
            renameListViewModel.SetSelectedEntries([entry]);

            Assert.True(entry.HasCommitError);
            Assert.True(renameListViewModel.CanShowCommitError);
            Assert.True(renameListViewModel.CanShowRowErrorMenu);

            RenameListRowErrorDialogContent? content = null;
            renameListViewModel.RowErrorDialogRequested += (_, value) => content = value;
            renameListViewModel.ShowCommitErrorCommand.Execute(null);

            Assert.NotNull(content);
            Assert.Equal(RenameListCommitErrorDisplay.DialogTitle, content.Title);
            Assert.Equal(RenameListCommitErrorDisplay.Summary, content.Summary);
            Assert.Equal(path, content.FilePath);
            Assert.Equal("The destination could not be written.", content.UserMessage);
            var technicalDetails = Assert.IsType<string>(content.TechnicalDetails);
            Assert.Contains("System.IO.IOException", technicalDetails, StringComparison.Ordinal);
            Assert.Contains("destination unavailable", technicalDetails, StringComparison.Ordinal);
        }

        /// <summary>
        /// Verifies Show Rename Error remains unavailable without exactly one selected commit-error row.
        /// </summary>
        [Fact]
        public async Task ShowCommitError_unavailable_without_single_commit_error_selection()
        {
            var dir = _context.CreateTempDir();
            var firstPath = Path.Combine(dir, "first.txt");
            var secondPath = Path.Combine(dir, "second.txt");
            await File.WriteAllTextAsync(firstPath, "first");
            await File.WriteAllTextAsync(secondPath, "second");
            var renameListViewModel = _context.CreateRenameListViewModel(dir);
            await renameListViewModel.AddPathsAsync([firstPath, secondPath]).ConfigureAwait(true);

            var first = renameListViewModel.Entries[0];
            first.EngineItem.CommitError = new RenameItemError("commit failed");
            Assert.False(renameListViewModel.CanShowCommitError);

            renameListViewModel.SetSelectedEntries([.. renameListViewModel.Entries]);
            Assert.False(renameListViewModel.CanShowCommitError);

            renameListViewModel.SetSelectedEntries([renameListViewModel.Entries[1]]);
            Assert.False(renameListViewModel.CanShowCommitError);
        }
    }
}
