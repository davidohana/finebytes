using Mfr.App.Ui.ViewModels.RenameList;
using Mfr.Models.RenameList.Fields.AudioTag;
using Mfr.Models.RenameList.Fields.Basic;

namespace Mfr.Tests.Ui.RenameList
{
    /// <summary>
    /// Tests for Rename List Show Field Error command state.
    /// </summary>
    public sealed class RenameListViewModelFieldErrorTests : IDisposable
    {
        private readonly RenameListUiTestContext _context = new(pinAddPolicy: true);

        /// <inheritdoc />
        public void Dispose()
        {
            _context.Dispose();
        }

        /// <summary>
        /// Verifies Show Field Error is available only for a single selected row with a metadata error cell.
        /// </summary>
        [Fact]
        public async Task ShowFieldError_available_for_metadata_error_cell_only()
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
            Assert.Equal(RenameListFieldLoadError.DisplayText, entry.GetFieldText(titleKey));

            renameListViewModel.SetSelectedEntries([entry]);
            renameListViewModel.SetFocusedFieldKey(fullNameKey);
            Assert.False(renameListViewModel.CanShowFieldError);

            renameListViewModel.SetFocusedFieldKey(titleKey);
            Assert.True(renameListViewModel.CanShowFieldError);

            RenameListFieldErrorDialogContent? content = null;
            renameListViewModel.FieldErrorDialogRequested += (_, value) => content = value;
            renameListViewModel.ShowFieldErrorCommand.Execute(null);

            Assert.NotNull(content);
            Assert.Equal("Title", content.FieldDisplayName);
            Assert.Equal("This file could not be read as audio or media metadata.", content.UserExplanation);
            Assert.False(string.IsNullOrWhiteSpace(content.TechnicalDetails));
        }

        /// <summary>
        /// Verifies preview columns never offer Show Field Error.
        /// </summary>
        [Fact]
        public async Task ShowFieldError_not_available_for_preview_columns()
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

            Assert.False(renameListViewModel.CanShowFieldError);
        }
    }
}
