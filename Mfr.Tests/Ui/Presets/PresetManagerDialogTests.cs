using Avalonia.Controls;
using Avalonia.Headless.XUnit;
using Avalonia.Input;
using Avalonia.Threading;

namespace Mfr.Tests.Ui.Presets
{
    /// <summary>
    /// Headless tests for Preset Manager multi-select and Up/Down list sync.
    /// </summary>
    public sealed class PresetManagerDialogTests
    {
        /// <summary>
        /// Verifies Ctrl-click multi-select syncs the ListBox and VM, and gates Load/Rename.
        /// </summary>
        [AvaloniaFact]
        public void Multi_select_syncs_list_and_gates_load_rename()
        {
            var (dialog, viewModel, list) = PresetManagerDialogTestUi.ShowWithPresets("A", "B", "C");
            var load = dialog.FindControl<Button>("LoadButton");
            var rename = dialog.FindControl<Button>("RenameButton");
            var delete = dialog.FindControl<Button>("DeleteButton");
            Assert.NotNull(load);
            Assert.NotNull(rename);
            Assert.NotNull(delete);

            Assert.Equal(["A"], viewModel.SelectedPresets.Select(preset => preset.Name));
            Assert.Equal([0], list.Selection.SelectedIndexes.ToList());
            Assert.True(load.IsEnabled);
            Assert.True(rename.IsEnabled);
            Assert.True(delete.IsEnabled);

            PresetManagerDialogTestUi.ClickListIndex(dialog, list, 0, RawInputModifiers.None);
            PresetManagerDialogTestUi.ClickListIndex(dialog, list, 1, RawInputModifiers.Control);

            Assert.Equal(["A", "B"], viewModel.SelectedPresets.Select(preset => preset.Name));
            Assert.Equal([0, 1], list.Selection.SelectedIndexes.OrderBy(index => index).ToList());
            Assert.False(load.IsEnabled);
            Assert.False(rename.IsEnabled);
            Assert.True(delete.IsEnabled);
            Assert.Equal(string.Empty, viewModel.SelectedDescription);

            dialog.Close();
        }

        /// <summary>
        /// Verifies Up/Down reorders the list control and keeps the moved multi-selection.
        /// </summary>
        [AvaloniaFact]
        public void Move_down_keeps_multi_selection_in_list()
        {
            var (dialog, viewModel, list) = PresetManagerDialogTestUi.ShowWithPresets("A", "B", "C");

            PresetManagerDialogTestUi.ClickListIndex(dialog, list, 0, RawInputModifiers.None);
            PresetManagerDialogTestUi.ClickListIndex(dialog, list, 1, RawInputModifiers.Control);
            Assert.True(viewModel.MoveSelectedDownCommand.CanExecute(null));

            viewModel.MoveSelectedDownCommand.Execute(null);
            dialog.UpdateLayout();
            Dispatcher.UIThread.RunJobs();

            Assert.Equal(["C", "A", "B"], viewModel.Presets.Select(preset => preset.Name));
            Assert.Equal(["A", "B"], viewModel.SelectedPresets.Select(preset => preset.Name));
            Assert.Equal([1, 2], list.Selection.SelectedIndexes.OrderBy(index => index).ToList());

            dialog.Close();
        }
    }
}
