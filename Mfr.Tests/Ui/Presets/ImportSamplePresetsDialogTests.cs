using Avalonia.Controls;
using Avalonia.Headless.XUnit;
using Avalonia.Interactivity;
using Avalonia.Threading;
using Avalonia.VisualTree;
using Mfr.App.Ui.ViewModels.AppliedFilters;
using Mfr.App.Ui.ViewModels.Presets;
using Mfr.App.Ui.Views.Controls;
using Mfr.App.Ui.Views.Presets;

namespace Mfr.Tests.Ui.Presets
{
    /// <summary>
    /// View-model and headless interaction tests for sample preset import.
    /// </summary>
    public sealed class ImportSamplePresetsDialogTests
    {
        /// <summary>
        /// Verifies selection state drives the selected names and Import enabled rule.
        /// </summary>
        [Fact]
        public void ViewModel_Selection_Drives_CanImport()
        {
            var viewModel = new ImportSamplePresetsDialogViewModel(SamplePresetCatalog.Presets.Take(2));

            Assert.True(viewModel.CanImport);
            Assert.Equal(2, viewModel.SelectedNames().Count);

            viewModel.SelectNone();
            Assert.False(viewModel.CanImport);
            Assert.Empty(viewModel.SelectedNames());

            viewModel.Items[1].IsSelected = true;
            Assert.True(viewModel.CanImport);
            Assert.Equal([viewModel.Items[1].Name], viewModel.SelectedNames());
        }

        /// <summary>
        /// Verifies the shown checklist follows Select none/all and disables Import when empty.
        /// </summary>
        [AvaloniaFact]
        public void Checklist_Buttons_Update_Control_And_Import_State()
        {
            var viewModel = new ImportSamplePresetsDialogViewModel(SamplePresetCatalog.Presets.Take(2));
            var dialog = new ImportSamplePresetsDialog(viewModel);
            dialog.Show();
            dialog.UpdateLayout();
            Dispatcher.UIThread.RunJobs();

            var import = dialog.FindControl<Button>("ImportButton");
            var selectNone = dialog.FindControl<Button>("SelectNoneButton");
            var selectAll = dialog.FindControl<Button>("SelectAllButton");
            Assert.NotNull(import);
            Assert.NotNull(selectNone);
            Assert.NotNull(selectAll);
            Assert.True(import.IsEnabled);

            selectNone.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
            Dispatcher.UIThread.RunJobs();

            Assert.False(viewModel.CanImport);
            Assert.False(import.IsEnabled);
            Assert.All(
                dialog.GetVisualDescendants().OfType<CompactCheckBox>(),
                checkBox => Assert.False(checkBox.IsChecked)
            );

            selectAll.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
            Dispatcher.UIThread.RunJobs();

            Assert.True(viewModel.CanImport);
            Assert.True(import.IsEnabled);
            Assert.All(
                dialog.GetVisualDescendants().OfType<CompactCheckBox>(),
                checkBox => Assert.True(checkBox.IsChecked)
            );

            var note = dialog
                .GetVisualDescendants()
                .OfType<TextBlock>()
                .Single(text => text.Text?.StartsWith("Presets that already exist", StringComparison.Ordinal) == true);
            Assert.Equal("Presets that already exist under the same name will be skipped.", note.Text);

            dialog.Close();
        }

        /// <summary>
        /// Verifies the Preset Manager Import sample presets button invokes its child-dialog host path.
        /// </summary>
        [AvaloniaFact]
        public void Manager_Import_Samples_Button_Invokes_Dialog_Path()
        {
            var invoked = false;
            var appliedFilters = new AppliedFiltersViewModel();
            var viewModel = new PresetManagerDialogViewModel(appliedFilters);
            var dialog = new PresetManagerDialog(
                viewModel,
                appliedFilters,
                tryLoadAsync: _ => Task.FromResult(false),
                importSamplesAsync: () =>
                {
                    invoked = true;
                    return Task.CompletedTask;
                }
            );
            dialog.Show();
            dialog.UpdateLayout();
            Dispatcher.UIThread.RunJobs();

            var import = dialog.FindControl<Button>("ImportSamplesButton");
            Assert.NotNull(import);
            Assert.True(import.IsEnabled);

            import.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
            Dispatcher.UIThread.RunJobs();

            Assert.True(invoked);
            dialog.Close();
        }
    }
}
