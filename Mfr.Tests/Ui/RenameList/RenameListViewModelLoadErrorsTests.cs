using Mfr.App.Ui.ViewModels.RenameList;
using Mfr.Models.RenameList.Fields.AudioTag;

namespace Mfr.Tests.Ui.RenameList
{
    /// <summary>
    /// Tests for Rename List Show Load Errors command state.
    /// </summary>
    public sealed class RenameListViewModelLoadErrorsTests : IDisposable
    {
        private readonly RenameListUiTestContext _context = new();

        /// <inheritdoc />
        public void Dispose()
        {
            _context.Dispose();
        }

        /// <summary>
        /// Verifies Show Load Errors is available for a single selected row with any load error.
        /// </summary>
        [Fact]
        public async Task ShowLoadErrors_available_for_row_with_load_error()
        {
            var (renameListViewModel, path, entry) = await _AddHtmWithTitleAsync();
            Assert.Equal("info.htm", entry.GetFieldText(RenameListTestHelpers.FullFileNameKey));
            Assert.Equal(RenameListFieldCatalog.LoadErrorText, entry.GetFieldText(_TitleKey()));
            Assert.True(entry.IsLoadError(_TitleKey()));

            renameListViewModel.SetSelectedEntries([entry]);
            Assert.True(renameListViewModel.CanShowLoadErrors);

            RenameListLoadErrorsDialogContent? content = null;
            renameListViewModel.LoadErrorsDialogRequested += (_, value) => content = value;
            renameListViewModel.ShowLoadErrorsCommand.Execute(null);

            Assert.NotNull(content);
            Assert.Equal(path, content.FilePath);
            var error = Assert.Single(content.Errors);
            Assert.Equal("This file could not be read as audio or media metadata.", error.UserExplanation);
            Assert.False(string.IsNullOrWhiteSpace(error.TechnicalDetails));
        }

        /// <summary>
        /// Verifies Show Load Errors stays off when nothing is selected.
        /// </summary>
        [Fact]
        public async Task ShowLoadErrors_unavailable_when_nothing_selected()
        {
            var (renameListViewModel, _, _) = await _AddHtmWithTitleAsync();
            Assert.Empty(renameListViewModel.SelectedEntries);
            Assert.False(renameListViewModel.CanShowLoadErrors);
            Assert.False(_TryShowLoadErrors(renameListViewModel));
        }

        /// <summary>
        /// Verifies Show Load Errors stays off for a multi-row selection.
        /// </summary>
        [Fact]
        public async Task ShowLoadErrors_unavailable_when_multiple_rows_selected()
        {
            var dir = _context.CreateTempDir();
            var firstPath = Path.Combine(dir, "info.htm");
            var secondPath = Path.Combine(dir, "notes.htm");
            await File.WriteAllTextAsync(firstPath, "<html></html>");
            await File.WriteAllTextAsync(secondPath, "<html></html>");
            var renameListViewModel = _CreateViewModelWithTitleColumn(dir);
            await renameListViewModel.AddPathsAsync([firstPath, secondPath]).ConfigureAwait(true);

            renameListViewModel.SetSelectedEntries([.. renameListViewModel.Entries]);
            Assert.Equal(2, renameListViewModel.SelectedEntries.Count);
            Assert.False(renameListViewModel.CanShowLoadErrors);
            Assert.False(_TryShowLoadErrors(renameListViewModel));
        }

        /// <summary>
        /// Verifies Show Load Errors stays off when the selected row has no stored load failure.
        /// </summary>
        [Fact]
        public async Task ShowLoadErrors_unavailable_when_row_has_no_load_error()
        {
            var dir = _context.CreateTempDir();
            var path = Path.Combine(dir, "notes.txt");
            await File.WriteAllTextAsync(path, "ok");
            var renameListViewModel = _context.CreateRenameListViewModel(dir);
            await renameListViewModel.AddPathsAsync([path]).ConfigureAwait(true);

            var entry = Assert.Single(renameListViewModel.Entries);
            renameListViewModel.SetSelectedEntries([entry]);
            Assert.False(entry.IsLoadError(RenameListTestHelpers.FullFileNameKey));
            Assert.False(renameListViewModel.CanShowLoadErrors);
            Assert.False(_TryShowLoadErrors(renameListViewModel));
        }

        private async Task<(RenameListViewModel ViewModel, string Path, RenameListEntry Entry)> _AddHtmWithTitleAsync()
        {
            var dir = _context.CreateTempDir();
            var path = Path.Combine(dir, "info.htm");
            await File.WriteAllTextAsync(path, "<html></html>");
            var renameListViewModel = _CreateViewModelWithTitleColumn(dir);
            await renameListViewModel.AddPathsAsync([path]).ConfigureAwait(true);
            return (renameListViewModel, path, Assert.Single(renameListViewModel.Entries));
        }

        private RenameListViewModel _CreateViewModelWithTitleColumn(string dir)
        {
            var renameListViewModel = _context.CreateRenameListViewModel(dir);
            renameListViewModel.SetVisibleColumns([
                new RenameListVisibleColumn(RenameListTestHelpers.FullFileNameKey),
                new RenameListVisibleColumn(_TitleKey()),
            ]);
            return renameListViewModel;
        }

        private static RenameListFieldKey _TitleKey()
        {
            return RenameListFieldKey.Original(AudioTagRenameListFields.Group, "Title");
        }

        private static bool _TryShowLoadErrors(RenameListViewModel renameListViewModel)
        {
            var shown = false;
            renameListViewModel.LoadErrorsDialogRequested += (_, _) => shown = true;
            renameListViewModel.ShowLoadErrorsCommand.Execute(null);
            return shown;
        }
    }
}
