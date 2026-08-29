using Avalonia;
using Avalonia.Controls;
using Avalonia.Headless;
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

            _ClickListIndex(dialog, list, 0, RawInputModifiers.None);
            Assert.Equal(0, dialogVm.SelectedColumnRowIndex);
            Assert.Equal(0, list.Selection.SelectedIndex);

            dialogVm.MoveSelectedColumnDownCommand.Execute(null);
            dialog.UpdateLayout();
            Dispatcher.UIThread.RunJobs();

            Assert.Equal(1, dialogVm.SelectedColumnRowIndex);
            Assert.Equal(movedKey, dialogVm.SelectedColumnRows[1].Column.Key);
            Assert.Equal(1, list.Selection.SelectedIndex);

            dialogVm.MoveSelectedColumnUpCommand.Execute(null);
            dialog.UpdateLayout();
            Dispatcher.UIThread.RunJobs();

            Assert.Equal(0, dialogVm.SelectedColumnRowIndex);
            Assert.Equal(movedKey, dialogVm.SelectedColumnRows[0].Column.Key);
            Assert.Equal(0, list.Selection.SelectedIndex);

            dialog.Close();
        }

        /// <summary>
        /// Verifies a contiguous multi-selection stays selected in the ListBox after a block move.
        /// </summary>
        [AvaloniaFact]
        public void Moving_a_column_block_keeps_multi_selection()
        {
            var (dialog, dialogVm, list) = _ShowColumnsList();

            _ClickListIndex(dialog, list, 0, RawInputModifiers.None);
            _ClickListIndex(dialog, list, 1, RawInputModifiers.Control);
            Assert.Equal([0, 1], dialogVm.SelectedColumnRowIndices);
            Assert.Equal(2, list.Selection.SelectedIndexes.Count);

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
        /// Verifies Ctrl-clicking the last selected column clears selection in the ListBox and VM.
        /// </summary>
        [AvaloniaFact]
        public void Ctrl_click_last_selected_column_clears_selection()
        {
            var (dialog, dialogVm, list) = _ShowColumnsList();

            _ClickListIndex(dialog, list, 0, RawInputModifiers.None);
            Assert.Equal(0, dialogVm.SelectedColumnRowIndex);

            _ClickListIndex(dialog, list, 0, RawInputModifiers.Control);
            Dispatcher.UIThread.RunJobs();

            Assert.Empty(dialogVm.SelectedColumnRowIndices);
            Assert.Equal(-1, dialogVm.SelectedColumnRowIndex);
            Assert.Empty(list.Selection.SelectedIndexes);

            dialog.Close();
        }

        /// <summary>
        /// Verifies pressing a row in a multi-selection keeps the full selection before drag starts.
        /// </summary>
        [AvaloniaFact]
        public void Press_on_multi_selected_column_keeps_selection()
        {
            var (dialog, dialogVm, list) = _ShowColumnsList();

            _ClickListIndex(dialog, list, 0, RawInputModifiers.None);
            _ClickListIndex(dialog, list, 1, RawInputModifiers.Control);
            Assert.Equal(2, dialogVm.SelectedColumnRowIndices.Count);
            Assert.Equal(2, list.Selection.SelectedIndexes.Count);

            list.Focus();
            dialog.UpdateLayout();
            Dispatcher.UIThread.RunJobs();

            _PressListIndex(list, 1);

            Assert.Equal(2, dialogVm.SelectedColumnRowIndices.Count);
            Assert.Equal(2, list.Selection.SelectedIndexes.Count);

            var releasePoint = _ListIndexClickPoint(dialog, list, 1);
            dialog.MouseUp(releasePoint, MouseButton.Left, RawInputModifiers.None);
            Dispatcher.UIThread.RunJobs();

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

            _ClickListIndex(dialog, availableList, 0, RawInputModifiers.None);
            _ClickListIndex(dialog, availableList, 1, RawInputModifiers.Control);

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

            _ClickListIndex(dialog, list, 0, RawInputModifiers.None);
            Assert.Equal(0, dialogVm.SelectedSortRowIndex);
            Assert.Equal(0, list.Selection.SelectedIndex);

            dialogVm.MoveSelectedSortKeyDownCommand.Execute(null);
            dialog.UpdateLayout();
            Dispatcher.UIThread.RunJobs();

            Assert.Equal(1, dialogVm.SelectedSortRowIndex);
            Assert.Equal(movedFieldKey, dialogVm.SelectedSortRows[1].Key.FieldKey);
            Assert.Equal(1, list.Selection.SelectedIndex);

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

        private static void _ClickListIndex(
            RenameListFieldShuttleDialog dialog,
            ListBox list,
            int index,
            RawInputModifiers modifiers
        )
        {
            var windowPoint = _ListIndexClickPoint(dialog, list, index);
            dialog.MouseMove(windowPoint, modifiers);
            dialog.MouseDown(windowPoint, MouseButton.Left, modifiers);
            dialog.MouseUp(windowPoint, MouseButton.Left, modifiers);
            Dispatcher.UIThread.RunJobs();
        }

        private static void _PressListIndex(ListBox list, int index)
        {
            var item = list.ContainerFromIndex(index) as ListBoxItem;
            Assert.NotNull(item);

            var point = new Point(8, 4);
            var props = new PointerPointProperties(
                RawInputModifiers.LeftMouseButton,
                PointerUpdateKind.LeftButtonPressed
            );
            var pointer = new Pointer(1, PointerType.Mouse, true);
            var args = new PointerPressedEventArgs(
                item,
                pointer,
                list,
                point,
                0,
                props,
                KeyModifiers.None,
                clickCount: 1
            )
            {
                RoutedEvent = InputElement.PointerPressedEvent,
            };
            list.RaiseEvent(args);
            Dispatcher.UIThread.RunJobs();
        }

        private static Point _ListIndexClickPoint(RenameListFieldShuttleDialog dialog, ListBox list, int index)
        {
            var container = list.ContainerFromIndex(index) as Visual;
            Assert.NotNull(container);

            var labelText = container
                .GetVisualDescendants()
                .OfType<TextBlock>()
                .FirstOrDefault(text => !string.IsNullOrEmpty(text.Text));
            var target = (Visual?)labelText ?? container;
            var local = new Point(Math.Max(8, target.Bounds.Width / 2), Math.Max(1, target.Bounds.Height / 2));
            var windowPoint = target.TranslatePoint(local, dialog);
            Assert.True(windowPoint.HasValue);
            return windowPoint.Value;
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
            var dialog = new RenameListFieldShuttleDialog(dialogVm) { Width = 900, Height = 700 };
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
            var dialog = new RenameListFieldShuttleDialog(dialogVm) { Width = 900, Height = 700 };
            dialog.Show();
            dialog.UpdateLayout();
            Dispatcher.UIThread.RunJobs();

            var list = dialog.FindControl<ListBox>("SelectedSortList");
            Assert.NotNull(list);
            return (dialog, dialogVm, list);
        }
    }
}
