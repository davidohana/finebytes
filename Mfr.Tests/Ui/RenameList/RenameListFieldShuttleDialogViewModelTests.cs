using Mfr.App.Ui.ViewModels.RenameList;
using Mfr.Models.RenameList.Fields.AudioTag;
using Mfr.Models.RenameList.Fields.Basic;
using Mfr.Models.RenameList.Fields.Extended;
using Mfr.Models.RenameList.Fields.Image;
using Mfr.Models.RenameList.Fields.Jpeg;

namespace Mfr.Tests.Ui.RenameList
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
                    RenameListFieldKey.Original(BasicRenameListField.Group, BasicRenameListFields.Key.Name)
                ),
            };
            var sortKeys = new[] { new RenameListSortKey(RenameListSortColumn.FullPath) };

            var dialogVm = new RenameListFieldShuttleDialogViewModel(columns, sortKeys);

            Assert.Equal(columns, dialogVm.ResultColumns);
            Assert.Equal(sortKeys, dialogVm.ResultSortKeys);
            Assert.Single(dialogVm.SelectedColumnRows);
            Assert.Single(dialogVm.SelectedSortRows);
            Assert.Equal("File Name", dialogVm.SelectedColumnRows[0].DisplayName);
            Assert.Equal("Full File Path", dialogVm.SelectedSortRows[0].Label);
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
            var previewKey = RenameListFieldKey.Preview(BasicRenameListField.Group, BasicRenameListFields.Key.FullName);

            dialogVm.SelectedAvailableOriginalField = RenameListFieldCatalog.GetField(
                BasicRenameListField.Group,
                BasicRenameListFields.Key.Name
            );
            dialogVm.AddSelectedOriginalFieldCommand.Execute(null);
            Assert.Equal(4, dialogVm.SelectedColumnRows.Count);
            Assert.DoesNotContain(
                dialogVm.AvailableOriginalFields,
                field => field.PropertyKey == BasicRenameListFields.Key.Name
            );

            dialogVm.SelectedColumnRowIndex = 0;
            dialogVm.MoveSelectedColumnDownCommand.Execute(null);
            Assert.Equal(BasicRenameListFields.Key.Folder, dialogVm.SelectedColumnRows[0].Column.Key.PropertyKey);
            Assert.Equal(1, dialogVm.SelectedColumnRowIndex);

            dialogVm.SelectedAvailablePreviewField = RenameListFieldCatalog.GetField(
                BasicRenameListField.Group,
                BasicRenameListFields.Key.FullName
            );
            dialogVm.AddSelectedPreviewFieldCommand.Execute(null);
            Assert.Equal(5, dialogVm.SelectedColumnRows.Count);
            Assert.Contains(dialogVm.SelectedColumnRows, row => row.Column.Key == previewKey);

            dialogVm.SelectedColumnRowIndex = dialogVm
                .SelectedColumnRows.ToList()
                .FindIndex(row => row.Column.Key == previewKey);
            dialogVm.RemoveSelectedColumnCommand.Execute(null);
            Assert.DoesNotContain(dialogVm.SelectedColumnRows, row => row.Column.Key == previewKey);

            Assert.True(dialogVm.ClearSelectedColumnsCommand.CanExecute(null));
            dialogVm.ClearSelectedColumnsCommand.Execute(null);
            Assert.Empty(dialogVm.SelectedColumnRows);
            Assert.False(dialogVm.CanConfirm);
            Assert.False(dialogVm.ClearSelectedColumnsCommand.CanExecute(null));
        }

        [Fact]
        public void Columns_tab_add_all_respects_original_vs_preview_tabs()
        {
            var dialogVm = new RenameListFieldShuttleDialogViewModel(
                [
                    new RenameListVisibleColumn(
                        RenameListFieldKey.Original(BasicRenameListField.Group, BasicRenameListFields.Key.ItemType)
                    ),
                ],
                []
            );

            dialogVm.AddAllOriginalFieldsCommand.Execute(null);

            Assert.Equal(9, dialogVm.SelectedColumnRows.Count);
            Assert.Empty(dialogVm.AvailableOriginalFields);
            Assert.DoesNotContain(
                dialogVm.SelectedColumnRows,
                row => row.Column.Key.IsPreview && row.Column.Key.PropertyKey == BasicRenameListFields.Key.FullName
            );

            dialogVm.AddAllPreviewFieldsCommand.Execute(null);

            Assert.Equal(16, dialogVm.SelectedColumnRows.Count);
            Assert.Contains(
                dialogVm.SelectedColumnRows,
                row => row.Column.Key.IsPreview && row.Column.Key.PropertyKey == BasicRenameListFields.Key.FullName
            );
        }

        [Fact]
        public void Sort_tab_add_remove_reorder_direction_and_clear()
        {
            var dialogVm = _CreateDefaultDialog();

            dialogVm.SelectedAvailableSortField = RenameListFieldCatalog.GetField(
                BasicRenameListField.Group,
                BasicRenameListFields.Key.FullPath
            );
            dialogVm.AddSelectedSortFieldCommand.Execute(null);
            Assert.Equal(4, dialogVm.SelectedSortRows.Count);
            Assert.Equal(RenameListSortColumn.FullPath, dialogVm.SelectedSortRows[^1].Key.Column);

            dialogVm.SelectedSortRowIndex = 0;
            dialogVm.ToggleSelectedSortDirectionCommand.Execute(null);
            Assert.True(dialogVm.SelectedSortRows[0].Key.Descending);

            dialogVm.MoveSelectedSortKeyDownCommand.Execute(null);
            Assert.Equal(RenameListSortColumn.FileFolder, dialogVm.SelectedSortRows[1].Key.Column);
            Assert.Equal(1, dialogVm.SelectedSortRowIndex);

            dialogVm.SelectedSortRowIndex = dialogVm.SelectedSortRows.Count - 1;
            dialogVm.RemoveSelectedSortKeyCommand.Execute(null);
            Assert.Equal(3, dialogVm.SelectedSortRows.Count);

            Assert.True(dialogVm.ClearSelectedSortKeysCommand.CanExecute(null));
            dialogVm.ClearSelectedSortKeysCommand.Execute(null);
            Assert.Empty(dialogVm.SelectedSortRows);
            Assert.True(dialogVm.CanConfirm);
            Assert.False(dialogVm.ClearSelectedSortKeysCommand.CanExecute(null));
        }

        [Fact]
        public void Columns_and_sort_drafts_are_independent()
        {
            var dialogVm = _CreateDefaultDialog();

            dialogVm.ClearSelectedSortKeysCommand.Execute(null);
            dialogVm.ClearSelectedColumnsCommand.Execute(null);

            dialogVm.SelectedAvailableOriginalField = RenameListFieldCatalog.GetField(
                BasicRenameListField.Group,
                BasicRenameListFields.Key.Name
            );
            dialogVm.AddSelectedOriginalFieldCommand.Execute(null);

            dialogVm.SelectedAvailableSortField = RenameListFieldCatalog.GetField(
                BasicRenameListField.Group,
                BasicRenameListFields.Key.FullPath
            );
            dialogVm.AddSelectedSortFieldCommand.Execute(null);

            Assert.Single(dialogVm.SelectedColumnRows);
            Assert.Single(dialogVm.SelectedSortRows);
            Assert.Equal(BasicRenameListFields.Key.Name, dialogVm.SelectedColumnRows[0].Column.Key.PropertyKey);
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

        [Fact]
        public void Adding_a_column_twice_is_a_noop()
        {
            var dialogVm = new RenameListFieldShuttleDialogViewModel(
                [
                    new RenameListVisibleColumn(
                        RenameListFieldKey.Original(BasicRenameListField.Group, BasicRenameListFields.Key.ItemType)
                    ),
                ],
                []
            )
            {
                SelectedAvailableOriginalField = RenameListFieldCatalog.GetField(
                    BasicRenameListField.Group,
                    BasicRenameListFields.Key.Name
                ),
            };
            dialogVm.AddSelectedOriginalFieldCommand.Execute(null);
            var count = dialogVm.SelectedColumnRows.Count;
            dialogVm.AddSelectedOriginalFieldCommand.Execute(null);

            Assert.Equal(count, dialogVm.SelectedColumnRows.Count);
        }

        [Fact]
        public void Column_move_commands_follow_selected_index()
        {
            var dialogVm = _CreateDefaultDialog();

            dialogVm.SelectedColumnRowIndex = 0;
            Assert.False(dialogVm.MoveSelectedColumnUpCommand.CanExecute(null));
            Assert.True(dialogVm.MoveSelectedColumnDownCommand.CanExecute(null));

            dialogVm.SelectedColumnRowIndex = dialogVm.SelectedColumnRows.Count - 1;
            Assert.True(dialogVm.MoveSelectedColumnUpCommand.CanExecute(null));
            Assert.False(dialogVm.MoveSelectedColumnDownCommand.CanExecute(null));
        }

        [Fact]
        public void Preview_tab_flag_toggles_original_tab()
        {
            var dialogVm = _CreateDefaultDialog();

            Assert.True(dialogVm.IsOriginalColumnsTab);
            dialogVm.IsPreviewColumnsTab = true;
            Assert.True(dialogVm.IsPreviewColumnsTab);
            Assert.False(dialogVm.IsOriginalColumnsTab);

            dialogVm.IsOriginalColumnsTab = true;
            Assert.False(dialogVm.IsPreviewColumnsTab);
            Assert.True(dialogVm.IsOriginalColumnsTab);
        }

        [Fact]
        public void Constructor_exposes_phase7a_original_groups()
        {
            var dialogVm = _CreateDefaultDialog();

            Assert.Equal(
                [
                    BasicRenameListField.GroupLabel,
                    ExtendedRenameListFields.GroupLabel,
                    AudioTagRenameListFields.GroupLabel,
                    ImageRenameListFields.GroupLabel,
                    JpegRenameListFields.GroupLabel,
                ],
                dialogVm.Groups.Select(group => group.DisplayName)
            );
        }

        [Fact]
        public void Original_only_group_has_no_preview_fields_and_add_all_adds_originals()
        {
            var dialogVm = new RenameListFieldShuttleDialogViewModel(
                [
                    new RenameListVisibleColumn(
                        RenameListFieldKey.Original(BasicRenameListField.Group, BasicRenameListFields.Key.ItemType)
                    ),
                ],
                []
            );
            dialogVm.SelectedGroup = dialogVm.Groups.Single(group => group.GroupId == ExtendedRenameListFields.Group);

            Assert.Equal(6, dialogVm.AvailableOriginalFields.Count);
            Assert.Empty(dialogVm.AvailablePreviewFields);
            Assert.Empty(dialogVm.AvailableSortFields);

            dialogVm.AddAllOriginalFieldsCommand.Execute(null);

            Assert.Equal(7, dialogVm.SelectedColumnRows.Count);
            Assert.Empty(dialogVm.AvailableOriginalFields);
            Assert.DoesNotContain(dialogVm.SelectedColumnRows, row => row.Column.Key.IsPreview);
        }

        [Fact]
        public void Jpeg_tag_group_has_no_preview_fields()
        {
            var dialogVm = _CreateDefaultDialog();
            dialogVm.SelectedGroup = dialogVm.Groups.Single(group => group.GroupId == JpegRenameListFields.Group);

            Assert.Equal(12, dialogVm.AvailableOriginalFields.Count);
            Assert.Empty(dialogVm.AvailablePreviewFields);
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
