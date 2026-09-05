using Mfr.App.Ui.ViewModels.RenameList;
using Mfr.Models.RenameList.Fields.AudioTag;
using Mfr.Models.RenameList.Fields.Basic;
using Mfr.Models.RenameList.Fields.Extended;
using Mfr.Models.RenameList.Fields.Image;
using Mfr.Models.RenameList.Fields.Jpeg;
using Mfr.Models.RenameList.Fields.Media;
using Mfr.Models.RenameList.Fields.Mpeg;

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
            var sortKeys = new[] { new RenameListSortKey(RenameListTestHelpers.FullPathKey) };

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

            dialogVm.SelectedSortRowIndex = 0;
            dialogVm.SelectedAvailableSortField = RenameListFieldCatalog.GetField(
                BasicRenameListField.Group,
                BasicRenameListFields.Key.FullPath
            );
            dialogVm.AddSelectedSortFieldCommand.Execute(null);
            Assert.Equal(4, dialogVm.SelectedSortRows.Count);
            Assert.Equal(RenameListTestHelpers.FullPathKey, dialogVm.SelectedSortRows[1].Key.FieldKey);

            dialogVm.SelectedSortRowIndex = 0;
            dialogVm.ToggleSelectedSortDirectionCommand.Execute(null);
            Assert.True(dialogVm.SelectedSortRows[0].Key.Descending);

            dialogVm.MoveSelectedSortKeyDownCommand.Execute(null);
            Assert.Equal(RenameListTestHelpers.FileFolderKey, dialogVm.SelectedSortRows[1].Key.FieldKey);
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
        public void Add_inserts_below_selected_row_or_appends_when_unselected()
        {
            var dialogVm = _CreateDefaultDialog();
            var initialFirstKey = dialogVm.SelectedColumnRows[0].Column.Key;

            dialogVm.SelectedColumnRowIndex = 0;
            dialogVm.SelectedAvailableOriginalField = RenameListFieldCatalog.GetField(
                BasicRenameListField.Group,
                BasicRenameListFields.Key.Name
            );
            dialogVm.AddSelectedOriginalFieldCommand.Execute(null);

            Assert.Equal(initialFirstKey, dialogVm.SelectedColumnRows[0].Column.Key);
            Assert.Equal(BasicRenameListFields.Key.Name, dialogVm.SelectedColumnRows[1].Column.Key.PropertyKey);

            dialogVm.ClearSelectedColumnsCommand.Execute(null);
            dialogVm.SelectedColumnRowIndex = -1;
            dialogVm.SelectedAvailableOriginalField = RenameListFieldCatalog.GetField(
                BasicRenameListField.Group,
                BasicRenameListFields.Key.Name
            );
            dialogVm.AddSelectedOriginalFieldCommand.Execute(null);

            Assert.Single(dialogVm.SelectedColumnRows);
            Assert.Equal(BasicRenameListFields.Key.Name, dialogVm.SelectedColumnRows[0].Column.Key.PropertyKey);
        }

        [Fact]
        public void Add_all_inserts_block_below_selected_row()
        {
            var dialogVm = new RenameListFieldShuttleDialogViewModel(
                [
                    new RenameListVisibleColumn(
                        RenameListFieldKey.Original(BasicRenameListField.Group, BasicRenameListFields.Key.ItemType)
                    ),
                    new RenameListVisibleColumn(
                        RenameListFieldKey.Original(BasicRenameListField.Group, BasicRenameListFields.Key.Folder)
                    ),
                ],
                []
            )
            {
                SelectedColumnRowIndex = 0,
            };
            var firstAvailable = dialogVm.AvailableOriginalFields[0];
            dialogVm.AddAllOriginalFieldsCommand.Execute(null);

            Assert.Equal(BasicRenameListFields.Key.ItemType, dialogVm.SelectedColumnRows[0].Column.Key.PropertyKey);
            Assert.Equal(firstAvailable.PropertyKey, dialogVm.SelectedColumnRows[1].Column.Key.PropertyKey);
            Assert.DoesNotContain(
                dialogVm.SelectedColumnRows.Take(1),
                row => row.Column.Key.PropertyKey == firstAvailable.PropertyKey
            );
        }

        [Fact]
        public void Multi_add_remove_and_block_move_columns()
        {
            var dialogVm = _CreateDefaultDialog();

            dialogVm.SetSelectedAvailableOriginalFields(
                [
                    RenameListFieldCatalog.GetField(BasicRenameListField.Group, BasicRenameListFields.Key.Name),
                    RenameListFieldCatalog.GetField(BasicRenameListField.Group, BasicRenameListFields.Key.Extension),
                ],
                RenameListFieldCatalog.GetField(BasicRenameListField.Group, BasicRenameListFields.Key.Name)
            );
            dialogVm.SelectedColumnRowIndex = 0;
            dialogVm.AddSelectedOriginalFieldCommand.Execute(null);

            Assert.Equal(BasicRenameListFields.Key.Name, dialogVm.SelectedColumnRows[1].Column.Key.PropertyKey);
            Assert.Equal(BasicRenameListFields.Key.Extension, dialogVm.SelectedColumnRows[2].Column.Key.PropertyKey);
            Assert.DoesNotContain(
                dialogVm.SelectedAvailableOriginalFields,
                field => field.PropertyKey is BasicRenameListFields.Key.Name or BasicRenameListFields.Key.Extension
            );

            dialogVm.SetSelectedColumnRows([1, 2], 1);
            dialogVm.RemoveSelectedColumnCommand.Execute(null);
            Assert.Equal(4, dialogVm.SelectedColumnRows.Count);
            Assert.Single(dialogVm.SelectedColumnRowIndices);

            var firstKey = dialogVm.SelectedColumnRows[0].Column.Key;
            var secondKey = dialogVm.SelectedColumnRows[1].Column.Key;
            var thirdKey = dialogVm.SelectedColumnRows[2].Column.Key;
            dialogVm.SetSelectedColumnRows([0, 1], 0);
            dialogVm.MoveSelectedColumnDownCommand.Execute(null);

            Assert.Equal(thirdKey, dialogVm.SelectedColumnRows[0].Column.Key);
            Assert.Equal(firstKey, dialogVm.SelectedColumnRows[1].Column.Key);
            Assert.Equal(secondKey, dialogVm.SelectedColumnRows[2].Column.Key);
            Assert.Equal([1, 2], dialogVm.SelectedColumnRowIndices);
            Assert.True(dialogVm.MoveSelectedColumnUpCommand.CanExecute(null));
        }

        [Fact]
        public void Empty_right_selection_appends_added_columns()
        {
            var dialogVm = _CreateDefaultDialog();
            var lastKey = dialogVm.SelectedColumnRows[^1].Column.Key;

            dialogVm.SetSelectedColumnRows([], -1);
            dialogVm.SelectedAvailableOriginalField = RenameListFieldCatalog.GetField(
                BasicRenameListField.Group,
                BasicRenameListFields.Key.Name
            );
            dialogVm.AddSelectedOriginalFieldCommand.Execute(null);

            Assert.Equal(lastKey, dialogVm.SelectedColumnRows[^2].Column.Key);
            Assert.Equal(BasicRenameListFields.Key.Name, dialogVm.SelectedColumnRows[^1].Column.Key.PropertyKey);
        }

        [Fact]
        public void Add_inserts_below_last_index_of_multi_selection()
        {
            var dialogVm = _CreateDefaultDialog();

            dialogVm.SetSelectedColumnRows([0, 1], 0);
            dialogVm.SelectedAvailableOriginalField = RenameListFieldCatalog.GetField(
                BasicRenameListField.Group,
                BasicRenameListFields.Key.Name
            );
            dialogVm.AddSelectedOriginalFieldCommand.Execute(null);

            Assert.Equal(BasicRenameListFields.Key.Name, dialogVm.SelectedColumnRows[2].Column.Key.PropertyKey);
            Assert.Equal([2], dialogVm.SelectedColumnRowIndices);
        }

        [Fact]
        public void Clear_columns_clears_row_selection()
        {
            var dialogVm = _CreateDefaultDialog();
            dialogVm.SetSelectedColumnRows([0, 1], 0);

            dialogVm.ClearSelectedColumnsCommand.Execute(null);

            Assert.Empty(dialogVm.SelectedColumnRowIndices);
            Assert.Equal(-1, dialogVm.SelectedColumnRowIndex);
            Assert.False(dialogVm.RemoveSelectedColumnCommand.CanExecute(null));
        }

        [Fact]
        public void SetSelectedColumnRows_ignores_out_of_range_indices()
        {
            var dialogVm = _CreateDefaultDialog();

            dialogVm.SetSelectedColumnRows([0, 99], 99);

            Assert.Equal([0], dialogVm.SelectedColumnRowIndices);
            Assert.Equal(0, dialogVm.SelectedColumnRowIndex);
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
            Assert.Equal(RenameListTestHelpers.FullPathKey, dialogVm.SelectedSortRows[0].Key.FieldKey);
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
        public void Non_contiguous_column_selection_can_move_up()
        {
            var dialogVm = _CreateDefaultDialog();
            var firstKey = dialogVm.SelectedColumnRows[0].Column.Key;
            var secondKey = dialogVm.SelectedColumnRows[1].Column.Key;
            var thirdKey = dialogVm.SelectedColumnRows[2].Column.Key;

            dialogVm.SetSelectedColumnRows([0, 2], 2);

            Assert.True(dialogVm.MoveSelectedColumnUpCommand.CanExecute(null));

            dialogVm.MoveSelectedColumnUpCommand.Execute(null);

            Assert.Equal(firstKey, dialogVm.SelectedColumnRows[0].Column.Key);
            Assert.Equal(thirdKey, dialogVm.SelectedColumnRows[1].Column.Key);
            Assert.Equal(secondKey, dialogVm.SelectedColumnRows[2].Column.Key);
            Assert.Equal([0, 1], dialogVm.SelectedColumnRowIndices);
            Assert.Equal(1, dialogVm.SelectedColumnRowIndex);
        }

        [Fact]
        public void Contiguous_column_block_at_top_cannot_move_up()
        {
            var dialogVm = _CreateDefaultDialog();

            dialogVm.SetSelectedColumnRows([0, 1], 0);

            Assert.False(dialogVm.MoveSelectedColumnUpCommand.CanExecute(null));
            Assert.True(dialogVm.MoveSelectedColumnDownCommand.CanExecute(null));
        }

        [Fact]
        public void Toggle_sort_direction_keeps_multi_selection()
        {
            var dialogVm = _CreateDefaultDialog();
            dialogVm.SetSelectedSortRows([0, 1], 0);
            var firstDescending = dialogVm.SelectedSortRows[0].Key.Descending;

            dialogVm.ToggleSortDirectionAt(1);

            Assert.Equal([0, 1], dialogVm.SelectedSortRowIndices);
            Assert.Equal(0, dialogVm.SelectedSortRowIndex);
            Assert.Equal(firstDescending, dialogVm.SelectedSortRows[0].Key.Descending);
            Assert.True(dialogVm.SelectedSortRows[1].Key.Descending);
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
                    MediaRenameListFields.GroupLabel,
                    MpegRenameListFields.GroupLabel,
                    ImageRenameListFields.GroupLabel,
                    JpegRenameListFields.GroupLabel,
                ],
                dialogVm.Groups.Select(group => group.DisplayName)
            );
        }

        [Fact]
        public void Extended_group_offers_date_and_attrs_preview_fields()
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
            Assert.Equal(
                [
                    ExtendedCreationDateField.CreationDateKey,
                    ExtendedLastWriteDateField.LastWriteDateKey,
                    ExtendedLastAccessDateField.LastAccessDateKey,
                    ExtendedAttributesField.AttributesKey,
                ],
                dialogVm.AvailablePreviewFields.Select(field => field.PropertyKey)
            );
            Assert.DoesNotContain(
                dialogVm.AvailablePreviewFields,
                field => field.PropertyKey == ExtendedSizeField.SizeKey
            );
            Assert.DoesNotContain(
                dialogVm.AvailablePreviewFields,
                field => field.PropertyKey == ExtendedFileCountField.FileCountKey
            );
            Assert.Equal(6, dialogVm.AvailableSortFields.Count);

            dialogVm.AddAllOriginalFieldsCommand.Execute(null);

            Assert.Equal(7, dialogVm.SelectedColumnRows.Count);
            Assert.Empty(dialogVm.AvailableOriginalFields);
            Assert.DoesNotContain(dialogVm.SelectedColumnRows, row => row.Column.Key.IsPreview);
        }

        [Fact]
        public void Sort_tab_can_add_image_width_field()
        {
            var dialogVm = _CreateDefaultDialog();
            dialogVm.SelectedGroup = dialogVm.Groups.Single(group => group.GroupId == ImageRenameListFields.Group);

            Assert.Contains(dialogVm.AvailableSortFields, field => field.PropertyKey == "Width");

            dialogVm.SelectedAvailableSortField = RenameListFieldCatalog.GetField(ImageRenameListFields.Group, "Width");
            dialogVm.AddSelectedSortFieldCommand.Execute(null);

            Assert.Equal("Width", dialogVm.SelectedSortRows[^1].Key.FieldKey.PropertyKey);
            Assert.Equal("Width", dialogVm.SelectedSortRows[^1].Label);
        }

        [Fact]
        public void AudioTag_group_offers_writable_preview_fields()
        {
            var dialogVm = new RenameListFieldShuttleDialogViewModel(
                [
                    new RenameListVisibleColumn(
                        RenameListFieldKey.Original(BasicRenameListField.Group, BasicRenameListFields.Key.ItemType)
                    ),
                ],
                []
            );
            dialogVm.SelectedGroup = dialogVm.Groups.Single(group => group.GroupId == AudioTagRenameListFields.Group);

            Assert.Equal(32, dialogVm.AvailableOriginalFields.Count);
            Assert.Equal(27, dialogVm.AvailablePreviewFields.Count);
            Assert.Contains(dialogVm.AvailablePreviewFields, field => field.PropertyKey == "Title");
            Assert.DoesNotContain(dialogVm.AvailablePreviewFields, field => field.PropertyKey == "TagTypes");
            Assert.DoesNotContain(dialogVm.AvailablePreviewFields, field => field.PropertyKey == "FirstPerformer");
            Assert.Equal(32, dialogVm.AvailableSortFields.Count);
        }

        [Fact]
        public void Jpeg_tag_group_has_no_preview_fields()
        {
            var dialogVm = _CreateDefaultDialog();
            dialogVm.SelectedGroup = dialogVm.Groups.Single(group => group.GroupId == JpegRenameListFields.Group);

            Assert.Equal(17, dialogVm.AvailableOriginalFields.Count);
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
