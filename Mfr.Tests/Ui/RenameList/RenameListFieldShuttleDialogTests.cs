using Avalonia;
using Avalonia.Controls;
using Avalonia.Headless.XUnit;
using Avalonia.Input;
using Avalonia.Threading;
using Avalonia.VisualTree;
using Mfr.App.Ui.ViewModels.RenameList;
using Mfr.App.Ui.Views.RenameList;
using Mfr.Models.RenameList.Fields.Basic;

namespace Mfr.Tests.Ui.RenameList
{
    /// <summary>
    /// Headless tests for the unified Rename List field shuttle dialog.
    /// </summary>
    public sealed class RenameListFieldShuttleDialogTests
    {
        /// <summary>
        /// Verifies moving a selected column keeps that row selected in the ListBox.
        /// </summary>
        [AvaloniaFact]
        public void Moving_a_column_keeps_the_moved_row_selected()
        {
            var (dialog, dialogVm, list) = _ShowColumnsList();
            var movedKey = dialogVm.SelectedColumnRows[0].Column.Key;

            dialogVm.SetSelectedColumnRows([0], 0);
            Dispatcher.UIThread.RunJobs();
            dialogVm.MoveSelectedColumnDownCommand.Execute(null);
            dialog.UpdateLayout();
            Dispatcher.UIThread.RunJobs();

            Assert.Equal(1, dialogVm.SelectedColumnRowIndex);
            Assert.Equal(movedKey, dialogVm.SelectedColumnRows[1].Column.Key);

            dialogVm.MoveSelectedColumnUpCommand.Execute(null);
            dialog.UpdateLayout();
            Dispatcher.UIThread.RunJobs();

            Assert.Equal(0, dialogVm.SelectedColumnRowIndex);
            Assert.Equal(movedKey, dialogVm.SelectedColumnRows[0].Column.Key);

            dialog.Close();
        }

        /// <summary>
        /// Verifies a contiguous multi-selection stays selected in the ListBox after a block move.
        /// </summary>
        [AvaloniaFact]
        public void Moving_a_column_block_keeps_multi_selection()
        {
            var (dialog, dialogVm, list) = _ShowColumnsList();

            dialogVm.SetSelectedColumnRows([0, 1], 0);
            Dispatcher.UIThread.RunJobs();
            dialogVm.MoveSelectedColumnDownCommand.Execute(null);
            dialog.UpdateLayout();
            Dispatcher.UIThread.RunJobs();

            Assert.Equal([1, 2], dialogVm.SelectedColumnRowIndices);
            Assert.Equal(2, list.Selection.SelectedIndexes.Count);
            Assert.Contains(1, list.Selection.SelectedIndexes);
            Assert.Contains(2, list.Selection.SelectedIndexes);

            dialog.Close();
        }

        /// <summary>
        /// Verifies available-list multi-select is not collapsed back to a single SelectedItem.
        /// </summary>
        [AvaloniaFact]
        public void Available_fields_keep_multi_selection()
        {
            var (dialog, dialogVm, _) = _ShowColumnsList();
            var availableList = dialog.FindControl<ListBox>("AvailableOriginalFieldsList");
            Assert.NotNull(availableList);
            Assert.True(availableList.ItemCount >= 2);

            availableList.Selection.Select(0);
            availableList.Selection.Select(1);
            Dispatcher.UIThread.RunJobs();

            Assert.Equal(2, dialogVm.SelectedAvailableOriginalFields.Count);
            Assert.Equal(2, availableList.Selection.SelectedIndexes.Count);

            dialog.Close();
        }

        /// <summary>
        /// Verifies moving a selected sort key keeps that row selected in the ListBox.
        /// </summary>
        [AvaloniaFact]
        public void Moving_a_sort_key_keeps_the_moved_row_selected()
        {
            var (dialog, dialogVm, list) = _ShowSortList();
            var movedFieldKey = dialogVm.SelectedSortRows[0].Key.FieldKey;

            dialogVm.SetSelectedSortRows([0], 0);
            Dispatcher.UIThread.RunJobs();
            dialogVm.MoveSelectedSortKeyDownCommand.Execute(null);
            dialog.UpdateLayout();
            Dispatcher.UIThread.RunJobs();

            Assert.Equal(1, dialogVm.SelectedSortRowIndex);
            Assert.Equal(movedFieldKey, dialogVm.SelectedSortRows[1].Key.FieldKey);

            dialog.Close();
        }

        /// <summary>
        /// Verifies dropping an available field onto the selected-columns list inserts at the drop index.
        /// </summary>
        [AvaloniaFact]
        public void Drag_from_available_to_selected_inserts_column_at_drop_index()
        {
            var (dialog, dialogVm, selectedList) = _ShowColumnsList();
            var availableList = dialog.FindControl<ListBox>("AvailableOriginalFieldsList");
            Assert.NotNull(availableList);

            var nameField = RenameListFieldCatalog.GetField(BasicRenameListField.Group, BasicRenameListFields.Key.Name);
            availableList.SelectedItem = nameField;
            Dispatcher.UIThread.RunJobs();

            var payload = new ShuttleDragPayload(
                ShuttleDragKind.AvailableField,
                [ShuttleFieldKeyCodec.Encode(nameField.OriginalKey)]
            );
            var dataTransfer = new DataTransfer();
            dataTransfer.Add(DataTransferItem.Create(ShuttleDragPayload.Format, payload.Serialize()));

            selectedList.RaiseEvent(
                new DragEventArgs(DragDrop.DropEvent, dataTransfer, selectedList, default, KeyModifiers.None)
            );
            dialog.UpdateLayout();
            Dispatcher.UIThread.RunJobs();

            Assert.Contains(
                dialogVm.SelectedColumnRows,
                row => row.Column.Key.PropertyKey == BasicRenameListFields.Key.Name
            );

            dialog.Close();
        }

        /// <summary>
        /// Verifies drag-over on the selected list paints the salmon insert marker on the target row.
        /// </summary>
        [AvaloniaFact]
        public void DragOver_selected_list_marks_insert_row()
        {
            var (dialog, _, selectedList) = _ShowColumnsList();
            var nameField = RenameListFieldCatalog.GetField(BasicRenameListField.Group, BasicRenameListFields.Key.Name);
            var payload = new ShuttleDragPayload(
                ShuttleDragKind.AvailableField,
                [ShuttleFieldKeyCodec.Encode(nameField.OriginalKey)]
            );
            var dataTransfer = new DataTransfer();
            dataTransfer.Add(DataTransferItem.Create(ShuttleDragPayload.Format, payload.Serialize()));

            var firstItem = selectedList.ContainerFromIndex(0) as ListBoxItem;
            Assert.NotNull(firstItem);
            var position = firstItem.TranslatePoint(new Point(8, 4), selectedList) ?? new Point(8, 4);

            selectedList.RaiseEvent(
                new DragEventArgs(DragDrop.DragOverEvent, dataTransfer, selectedList, position, KeyModifiers.None)
            );
            dialog.UpdateLayout();
            Dispatcher.UIThread.RunJobs();

            Assert.Contains("drop-mark", firstItem.Classes);

            selectedList.RaiseEvent(
                new DragEventArgs(
                    DragDrop.DragLeaveEvent,
                    dataTransfer,
                    selectedList,
                    new Point(-8, -8),
                    KeyModifiers.None
                )
            );
            Dispatcher.UIThread.RunJobs();

            Assert.DoesNotContain("drop-mark", firstItem.Classes);

            dialog.Close();
        }

        private static (
            RenameListFieldShuttleDialog Dialog,
            RenameListFieldShuttleDialogViewModel ViewModel,
            ListBox List
        ) _ShowColumnsList()
        {
            var dialogVm = new RenameListFieldShuttleDialogViewModel(
                RenameListVisibleColumn.CreateDefaults(),
                RenameListSortKey.DefaultKeys
            );
            var dialog = new RenameListFieldShuttleDialog(dialogVm);
            dialog.Show();
            dialog.UpdateLayout();
            Dispatcher.UIThread.RunJobs();

            var list = dialog.FindControl<ListBox>("SelectedColumnsList");
            Assert.NotNull(list);
            return (dialog, dialogVm, list);
        }

        private static (
            RenameListFieldShuttleDialog Dialog,
            RenameListFieldShuttleDialogViewModel ViewModel,
            ListBox List
        ) _ShowSortList()
        {
            var dialogVm = new RenameListFieldShuttleDialogViewModel(
                RenameListVisibleColumn.CreateDefaults(),
                RenameListSortKey.DefaultKeys,
                RenameListFieldShuttleTab.Sort
            );
            var dialog = new RenameListFieldShuttleDialog(dialogVm);
            dialog.Show();
            dialog.UpdateLayout();
            Dispatcher.UIThread.RunJobs();

            var list = dialog.FindControl<ListBox>("SelectedSortList");
            Assert.NotNull(list);
            return (dialog, dialogVm, list);
        }
    }
}
