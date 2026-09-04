using Mfr.App.Ui.ViewModels.RenameList;
using Mfr.Filters.Formatting;
using Mfr.Models.RenameList.Fields.Basic;
using Mfr.Models.Tags;

namespace Mfr.Tests.Ui.RenameList
{
    /// <summary>
    /// Tests for Rename List Show Preview Error command state.
    /// </summary>
    public sealed class RenameListViewModelPreviewErrorsTests : IDisposable
    {
        private readonly RenameListUiTestContext _context = new();

        /// <inheritdoc />
        public void Dispose()
        {
            _context.Dispose();
        }

        /// <summary>
        /// Verifies Show Preview Error is available for a single selected row with a preview failure.
        /// </summary>
        [Fact]
        public async Task ShowPreviewError_available_for_row_with_preview_error()
        {
            var (renameListViewModel, path, entry) = await _AddTxtWithAudioPreviewErrorAsync();
            Assert.True(entry.HasPreviewError);
            Assert.Equal(1, renameListViewModel.PreviewErrorCount);

            renameListViewModel.SetSelectedEntries([entry]);
            Assert.True(renameListViewModel.CanShowPreviewError);
            Assert.True(renameListViewModel.CanShowRowErrorMenu);

            RenameListPreviewErrorDialogContent? content = null;
            renameListViewModel.PreviewErrorDialogRequested += (_, value) => content = value;
            renameListViewModel.ShowPreviewErrorCommand.Execute(null);

            Assert.NotNull(content);
            Assert.Equal(path, content.FilePath);
            Assert.False(string.IsNullOrWhiteSpace(content.Message));
            Assert.False(string.IsNullOrWhiteSpace(content.TechnicalDetails));
        }

        /// <summary>
        /// Verifies Show Preview Error stays off when nothing is selected.
        /// </summary>
        [Fact]
        public async Task ShowPreviewError_unavailable_when_nothing_selected()
        {
            var (renameListViewModel, _, _) = await _AddTxtWithAudioPreviewErrorAsync();
            Assert.Empty(renameListViewModel.SelectedEntries);
            Assert.False(renameListViewModel.CanShowPreviewError);
            Assert.False(_TryShowPreviewError(renameListViewModel));
        }

        /// <summary>
        /// Verifies Show Preview Error stays off for a multi-row selection.
        /// </summary>
        [Fact]
        public async Task ShowPreviewError_unavailable_when_multiple_rows_selected()
        {
            var dir = _context.CreateTempDir();
            var firstPath = Path.Combine(dir, "a.txt");
            var secondPath = Path.Combine(dir, "b.txt");
            await File.WriteAllTextAsync(firstPath, "x");
            await File.WriteAllTextAsync(secondPath, "x");
            var renameListViewModel = _context.CreateRenameListViewModel(dir);
            await renameListViewModel.AddPathsAsync([firstPath, secondPath]).ConfigureAwait(true);
            _PreviewAudioTitle(renameListViewModel);

            renameListViewModel.SetSelectedEntries([.. renameListViewModel.Entries]);
            Assert.Equal(2, renameListViewModel.SelectedEntries.Count);
            Assert.False(renameListViewModel.CanShowPreviewError);
            Assert.False(_TryShowPreviewError(renameListViewModel));
        }

        /// <summary>
        /// Verifies Show Preview Error stays off when the selected row has no preview failure.
        /// </summary>
        [Fact]
        public async Task ShowPreviewError_unavailable_when_row_has_no_preview_error()
        {
            var dir = _context.CreateTempDir();
            var path = Path.Combine(dir, "notes.txt");
            await File.WriteAllTextAsync(path, "ok");
            var renameListViewModel = _context.CreateRenameListViewModel(dir);
            await renameListViewModel.AddPathsAsync([path]).ConfigureAwait(true);

            var entry = Assert.Single(renameListViewModel.Entries);
            renameListViewModel.SetSelectedEntries([entry]);
            Assert.False(entry.HasPreviewError);
            Assert.False(renameListViewModel.CanShowPreviewError);
            Assert.False(_TryShowPreviewError(renameListViewModel));
        }

        private async Task<(
            RenameListViewModel ViewModel,
            string Path,
            RenameListEntry Entry
        )> _AddTxtWithAudioPreviewErrorAsync()
        {
            var dir = _context.CreateTempDir();
            var path = Path.Combine(dir, "note.txt");
            await File.WriteAllTextAsync(path, "plain text");
            var renameListViewModel = _context.CreateRenameListViewModel(dir);
            renameListViewModel.SetVisibleColumns([
                new RenameListVisibleColumn(
                    RenameListFieldKey.Original(BasicRenameListField.Group, BasicRenameListFields.Key.FullName)
                ),
                new RenameListVisibleColumn(
                    RenameListFieldKey.Preview(BasicRenameListField.Group, BasicRenameListFields.Key.FullName)
                ),
            ]);
            await renameListViewModel.AddPathsAsync([path]).ConfigureAwait(true);
            _PreviewAudioTitle(renameListViewModel);
            return (renameListViewModel, path, Assert.Single(renameListViewModel.Entries));
        }

        private static void _PreviewAudioTitle(RenameListViewModel renameListViewModel)
        {
            renameListViewModel.Preview(
                new FilterChain
                {
                    Steps =
                    [
                        new FilterChainStep(
                            Enabled: true,
                            new FormatterFilter(
                                Target: new SemanticAudioFieldTarget(SemanticAudioField.Title),
                                Options: new FormatterOptions("x")
                            )
                        ),
                    ],
                }
            );
        }

        private static bool _TryShowPreviewError(RenameListViewModel renameListViewModel)
        {
            var shown = false;
            renameListViewModel.PreviewErrorDialogRequested += (_, _) => shown = true;
            renameListViewModel.ShowPreviewErrorCommand.Execute(null);
            return shown;
        }
    }
}
