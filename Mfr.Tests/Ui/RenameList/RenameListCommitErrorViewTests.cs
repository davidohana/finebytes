using Avalonia.Controls;
using Avalonia.Headless.XUnit;
using Avalonia.Threading;
using Avalonia.VisualTree;

namespace Mfr.Tests.Ui.RenameList
{
    /// <summary>
    /// Headless tests for Rename List commit-error row chrome.
    /// </summary>
    public sealed class RenameListCommitErrorViewTests : IDisposable
    {
        private readonly RenameListUiTestContext _context = new();

        /// <inheritdoc />
        public void Dispose()
        {
            _context.Dispose();
        }

        /// <summary>
        /// Verifies commit-error plum takes precedence over preview-error lavender.
        /// </summary>
        [AvaloniaFact]
        public async Task Commit_error_row_uses_plum_class_instead_of_preview_error_class()
        {
            var dir = _context.CreateTempDir();
            var path = Path.Combine(dir, "note.txt");
            await File.WriteAllTextAsync(path, "plain text");
            var renameListViewModel = _context.CreateRenameListViewModel(dir);
            await renameListViewModel.AddPathsAsync([path]).ConfigureAwait(true);

            var entry = Assert.Single(renameListViewModel.Entries);
            entry.EngineItem.PreviewError = new RenameItemError("preview failed");
            entry.EngineItem.CommitError = new RenameItemError("commit failed");

            var (view, window) = _context.Show(renameListViewModel, width: 800, height: 180);
            Dispatcher.UIThread.RunJobs();
            var grid = view.GetVisualDescendants().OfType<DataGrid>().Single();
            var row = Assert.Single(grid.GetVisualDescendants().OfType<DataGridRow>());

            Assert.Contains("rename-list-commit-error", row.Classes);
            Assert.DoesNotContain("rename-list-preview-error", row.Classes);

            window.Close();
        }
    }
}
