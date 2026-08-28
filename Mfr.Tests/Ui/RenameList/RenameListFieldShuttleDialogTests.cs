using Avalonia.Controls;
using Avalonia.Headless.XUnit;
using Avalonia.Threading;
using Mfr.App.Ui.ViewModels.RenameList;
using Mfr.App.Ui.Views.RenameList;

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

            list.SelectedIndex = 0;
            Dispatcher.UIThread.RunJobs();
            dialogVm.MoveSelectedColumnDownCommand.Execute(null);
            dialog.UpdateLayout();
            Dispatcher.UIThread.RunJobs();

            Assert.Equal(1, list.SelectedIndex);
            Assert.Equal(1, dialogVm.SelectedColumnRowIndex);
            Assert.Equal(movedKey, dialogVm.SelectedColumnRows[1].Column.Key);
            Assert.True(dialogVm.MoveSelectedColumnUpCommand.CanExecute(null));
            Assert.True(dialogVm.MoveSelectedColumnDownCommand.CanExecute(null));

            dialogVm.MoveSelectedColumnUpCommand.Execute(null);
            dialog.UpdateLayout();
            Dispatcher.UIThread.RunJobs();

            Assert.Equal(0, list.SelectedIndex);
            Assert.Equal(0, dialogVm.SelectedColumnRowIndex);
            Assert.Equal(movedKey, dialogVm.SelectedColumnRows[0].Column.Key);

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

            list.SelectedIndex = 0;
            Dispatcher.UIThread.RunJobs();
            dialogVm.MoveSelectedSortKeyDownCommand.Execute(null);
            dialog.UpdateLayout();
            Dispatcher.UIThread.RunJobs();

            Assert.Equal(1, list.SelectedIndex);
            Assert.Equal(1, dialogVm.SelectedSortRowIndex);
            Assert.Equal(movedFieldKey, dialogVm.SelectedSortRows[1].Key.FieldKey);
            Assert.True(dialogVm.MoveSelectedSortKeyUpCommand.CanExecute(null));
            Assert.True(dialogVm.MoveSelectedSortKeyDownCommand.CanExecute(null));

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
