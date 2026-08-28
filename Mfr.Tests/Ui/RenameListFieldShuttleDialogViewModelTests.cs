using Mfr.App.Ui.ViewModels.RenameList;
using Mfr.Models.RenameList.Fields.Basic;

namespace Mfr.Tests.Ui
{
    /// <summary>
    /// Tests draft state for <see cref="RenameListFieldShuttleDialogViewModel"/>.
    /// </summary>
    public sealed class RenameListFieldShuttleDialogViewModelTests
    {
        [Fact]
        public void Constructor_snapshots_columns_and_sort_keys()
        {
            var columns = new[]
            {
                new RenameListVisibleColumn(
                    RenameListFieldKey.Original(BasicRenameListField.Group, BasicNameField.Key)
                ),
            };
            var sortKeys = new[] { new RenameListSortKey(RenameListSortColumn.FullPath) };

            var dialogVm = new RenameListFieldShuttleDialogViewModel(columns, sortKeys);

            Assert.Equal(columns, dialogVm.ResultColumns);
            Assert.Equal(sortKeys, dialogVm.ResultSortKeys);
            Assert.Single(dialogVm.SelectedColumnRows);
            Assert.Single(dialogVm.SelectedSortRows);
            Assert.Equal("File Name", dialogVm.SelectedColumnRows[0].DisplayName);
            Assert.Equal("Full Path", dialogVm.SelectedSortRows[0].Label);
        }

        [Fact]
        public void Columns_tab_add_remove_reorder_and_clear()
        {
            var columnsWithoutPreview = RenameListVisibleColumn
                .CreateDefaults()
                .Where(column => !column.Key.IsPreview)
                .ToList();
            var dialogVm = new RenameListFieldShuttleDialogViewModel(
                columnsWithoutPreview,
                RenameListSortKey.DefaultKeys
            );
            var previewKey = RenameListFieldKey.Preview(BasicRenameListField.Group, BasicFullNameField.Key);

            dialogVm.SelectedAvailableOriginalField = RenameListFieldCatalog.GetField(
                BasicRenameListField.Group,
                BasicNameField.Key
            );
            dialogVm.AddSelectedOriginalFieldCommand.Execute(null);
            Assert.Equal(4, dialogVm.SelectedColumnRows.Count);
            Assert.DoesNotContain(dialogVm.AvailableOriginalFields, field => field.PropertyKey == BasicNameField.Key);

            dialogVm.SelectedColumnRowIndex = 0;
            dialogVm.MoveSelectedColumnDownCommand.Execute(null);
            Assert.Equal(BasicFolderField.Key, dialogVm.SelectedColumnRows[0].Column.Key.PropertyKey);

            dialogVm.SelectedAvailablePreviewField = RenameListFieldCatalog.GetField(
                BasicRenameListField.Group,
                BasicFullNameField.Key
            );
            dialogVm.AddSelectedPreviewFieldCommand.Execute(null);
            Assert.Equal(5, dialogVm.SelectedColumnRows.Count);
            Assert.Contains(dialogVm.SelectedColumnRows, row => row.Column.Key == previewKey);

            dialogVm.SelectedColumnRowIndex = dialogVm
                .SelectedColumnRows.ToList()
                .FindIndex(row => row.Column.Key == previewKey);
            dialogVm.RemoveSelectedColumnCommand.Execute(null);
            Assert.DoesNotContain(dialogVm.SelectedColumnRows, row => row.Column.Key == previewKey);

            dialogVm.ClearSelectedColumnsCommand.Execute(null);
            Assert.Empty(dialogVm.SelectedColumnRows);
            Assert.False(dialogVm.CanConfirm);
        }

        [Fact]
        public void Columns_tab_add_all_respects_original_vs_preview_tabs()
        {
            var dialogVm = new RenameListFieldShuttleDialogViewModel(
                [
                    new RenameListVisibleColumn(
                        RenameListFieldKey.Original(BasicRenameListField.Group, BasicItemTypeField.Key)
                    ),
                ],
                []
            );

            dialogVm.AddAllOriginalFieldsCommand.Execute(null);

            Assert.Equal(9, dialogVm.SelectedColumnRows.Count);
            Assert.Empty(dialogVm.AvailableOriginalFields);
            Assert.DoesNotContain(
                dialogVm.SelectedColumnRows,
                row => row.Column.Key.IsPreview && row.Column.Key.PropertyKey == BasicFullNameField.Key
            );

            dialogVm.AddAllPreviewFieldsCommand.Execute(null);

            Assert.Equal(16, dialogVm.SelectedColumnRows.Count);
            Assert.Contains(
                dialogVm.SelectedColumnRows,
                row => row.Column.Key.IsPreview && row.Column.Key.PropertyKey == BasicFullNameField.Key
            );
        }

        [Fact]
        public void Sort_tab_add_remove_reorder_direction_and_clear()
        {
            var dialogVm = _CreateDefaultDialog();

            dialogVm.SelectedAvailableSortField = RenameListFieldCatalog.GetField(
                BasicRenameListField.Group,
                BasicFullPathField.Key
            );
            dialogVm.AddSelectedSortFieldCommand.Execute(null);
            Assert.Equal(4, dialogVm.SelectedSortRows.Count);
            Assert.Equal(RenameListSortColumn.FullPath, dialogVm.SelectedSortRows[^1].Key.Column);

            dialogVm.SelectedSortRowIndex = 0;
            dialogVm.ToggleSelectedSortDirectionCommand.Execute(null);
            Assert.True(dialogVm.SelectedSortRows[0].Key.Descending);

            dialogVm.MoveSelectedSortKeyDownCommand.Execute(null);
            Assert.Equal(RenameListSortColumn.FileFolder, dialogVm.SelectedSortRows[1].Key.Column);

            dialogVm.SelectedSortRowIndex = dialogVm.SelectedSortRows.Count - 1;
            dialogVm.RemoveSelectedSortKeyCommand.Execute(null);
            Assert.Equal(3, dialogVm.SelectedSortRows.Count);

            dialogVm.ClearSelectedSortKeysCommand.Execute(null);
            Assert.Empty(dialogVm.SelectedSortRows);
            Assert.True(dialogVm.CanConfirm);
        }

        [Fact]
        public void Columns_and_sort_drafts_are_independent()
        {
            var dialogVm = _CreateDefaultDialog();

            dialogVm.ClearSelectedSortKeysCommand.Execute(null);
            dialogVm.ClearSelectedColumnsCommand.Execute(null);

            dialogVm.SelectedAvailableOriginalField = RenameListFieldCatalog.GetField(
                BasicRenameListField.Group,
                BasicNameField.Key
            );
            dialogVm.AddSelectedOriginalFieldCommand.Execute(null);

            dialogVm.SelectedAvailableSortField = RenameListFieldCatalog.GetField(
                BasicRenameListField.Group,
                BasicFullPathField.Key
            );
            dialogVm.AddSelectedSortFieldCommand.Execute(null);

            Assert.Single(dialogVm.SelectedColumnRows);
            Assert.Single(dialogVm.SelectedSortRows);
            Assert.Equal(BasicNameField.Key, dialogVm.SelectedColumnRows[0].Column.Key.PropertyKey);
            Assert.Equal(RenameListSortColumn.FullPath, dialogVm.SelectedSortRows[0].Key.Column);
        }

        [Fact]
        public void OpenSortEditor_tab_index_is_sort()
        {
            var dialogVm = new RenameListFieldShuttleDialogViewModel(
                RenameListVisibleColumn.CreateDefaults(),
                RenameListSortKey.DefaultKeys,
                RenameListFieldShuttleTab.Sort
            );

            Assert.Equal(1, dialogVm.SelectedTabIndex);
        }

        private static RenameListFieldShuttleDialogViewModel _CreateDefaultDialog()
        {
            return new RenameListFieldShuttleDialogViewModel(
                RenameListVisibleColumn.CreateDefaults(),
                RenameListSortKey.DefaultKeys
            );
        }
    }
}
