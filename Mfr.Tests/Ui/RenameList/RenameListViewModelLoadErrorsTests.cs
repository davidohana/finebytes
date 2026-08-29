using Mfr.App.Ui.ViewModels.RenameList;
using Mfr.Models.RenameList.Fields.AudioTag;
using Mfr.Models.RenameList.Fields.Basic;

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
            var dir = _context.CreateTempDir();
            var path = Path.Combine(dir, "info.htm");
            await File.WriteAllTextAsync(path, "<html></html>");
            var titleKey = RenameListFieldKey.Original(AudioTagRenameListFields.Group, "Title");
            var fullNameKey = RenameListFieldKey.Original(
                BasicRenameListField.Group,
                BasicRenameListFields.Key.FullName
            );

            var renameListViewModel = _context.CreateRenameListViewModel(dir);
            renameListViewModel.SetVisibleColumns([
                new RenameListVisibleColumn(fullNameKey),
                new RenameListVisibleColumn(titleKey),
            ]);
            await renameListViewModel.AddPathsAsync([path]).ConfigureAwait(true);

            var entry = Assert.Single(renameListViewModel.Entries);
            Assert.Equal("info.htm", entry.GetFieldText(fullNameKey));
            Assert.Equal(RenameListFieldCatalog.FieldLoadErrorText, entry.GetFieldText(titleKey));

            renameListViewModel.SetSelectedEntries([entry]);
            renameListViewModel.SetFocusedFieldKey(fullNameKey);
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
        /// Verifies Show Load Errors remains available when the focused column is a preview field.
        /// </summary>
        [Fact]
        public async Task ShowLoadErrors_available_when_preview_column_is_focused()
        {
            var dir = _context.CreateTempDir();
            var path = Path.Combine(dir, "info.htm");
            await File.WriteAllTextAsync(path, "<html></html>");
            var titleKey = RenameListFieldKey.Original(AudioTagRenameListFields.Group, "Title");
            var previewFullNameKey = RenameListFieldKey.Preview(
                BasicRenameListField.Group,
                BasicRenameListFields.Key.FullName
            );

            var renameListViewModel = _context.CreateRenameListViewModel(dir);
            renameListViewModel.SetVisibleColumns([
                new RenameListVisibleColumn(titleKey),
                new RenameListVisibleColumn(previewFullNameKey),
            ]);
            await renameListViewModel.AddPathsAsync([path]).ConfigureAwait(true);

            var entry = Assert.Single(renameListViewModel.Entries);
            renameListViewModel.SetSelectedEntries([entry]);
            renameListViewModel.SetFocusedFieldKey(previewFullNameKey);

            Assert.True(renameListViewModel.CanShowLoadErrors);
        }
    }
}
