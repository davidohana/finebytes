using Avalonia;
using Avalonia.Controls;
using Avalonia.Headless;
using Avalonia.Headless.XUnit;
using Avalonia.Input;
using Avalonia.Threading;
using Avalonia.VisualTree;
using Mfr.App.Ui.ViewModels.AppliedFilters;
using Mfr.App.Ui.ViewModels.Presets;
using Mfr.App.Ui.Views.Presets;

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
            var (dialog, viewModel, list) = _ShowWithPresets("A", "B", "C");
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

            _ClickListIndex(dialog, list, 0, RawInputModifiers.None);
            _ClickListIndex(dialog, list, 1, RawInputModifiers.Control);

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
            var (dialog, viewModel, list) = _ShowWithPresets("A", "B", "C");

            _ClickListIndex(dialog, list, 0, RawInputModifiers.None);
            _ClickListIndex(dialog, list, 1, RawInputModifiers.Control);
            Assert.True(viewModel.MoveSelectedDownCommand.CanExecute(null));

            viewModel.MoveSelectedDownCommand.Execute(null);
            dialog.UpdateLayout();
            Dispatcher.UIThread.RunJobs();

            Assert.Equal(["C", "A", "B"], viewModel.Presets.Select(preset => preset.Name));
            Assert.Equal(["A", "B"], viewModel.SelectedPresets.Select(preset => preset.Name));
            Assert.Equal([1, 2], list.Selection.SelectedIndexes.OrderBy(index => index).ToList());

            dialog.Close();
        }

        private static (
            PresetManagerDialog Dialog,
            PresetManagerDialogViewModel ViewModel,
            ListBox List
        ) _ShowWithPresets(params string[] names)
        {
            var manager = PresetManager.CreateEmpty();
            foreach (var name in names)
            {
                manager.Upsert(
                    new FilterPreset
                    {
                        Id = Guid.NewGuid(),
                        Name = name,
                        Description = $"desc-{name}",
                        Chain = new FilterChain { Steps = [] },
                    }
                );
            }

            var appliedFilters = new AppliedFiltersViewModel(presetManager: manager);
            var viewModel = new PresetManagerDialogViewModel(appliedFilters);
            var dialog = new PresetManagerDialog(viewModel, appliedFilters, tryLoadAsync: _ => Task.FromResult(false));
            dialog.Show();
            dialog.UpdateLayout();
            Dispatcher.UIThread.RunJobs();

            var list = dialog.FindControl<ListBox>("PresetsList");
            Assert.NotNull(list);
            return (dialog, viewModel, list);
        }

        private static void _ClickListIndex(
            PresetManagerDialog dialog,
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

        private static Point _ListIndexClickPoint(PresetManagerDialog dialog, ListBox list, int index)
        {
            list.ScrollIntoView(index);
            dialog.UpdateLayout();
            Dispatcher.UIThread.RunJobs();

            var container = list.ContainerFromIndex(index);
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
    }
}
