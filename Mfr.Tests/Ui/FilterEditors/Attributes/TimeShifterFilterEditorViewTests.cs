using Avalonia.Controls;
using Avalonia.Headless.XUnit;
using Avalonia.Threading;
using Avalonia.VisualTree;
using Mfr.App.Ui.ViewModels.FilterEditors.Attributes;
using Mfr.App.Ui.Views.Controls;
using Mfr.App.Ui.Views.FilterEditors.Attributes;
using Mfr.Filters.Attributes;
using Mfr.Tests.Ui.AppliedFilters;

namespace Mfr.Tests.Ui.FilterEditors.Attributes
{
    /// <summary>
    /// Headless tests for <see cref="TimeShifterFilterEditorView"/>.
    /// </summary>
    public sealed class TimeShifterFilterEditorViewTests
    {
        /// <summary>
        /// Verifies Time Shifter option edits persist on the applied step.
        /// </summary>
        [AvaloniaFact]
        public void Time_shifter_controls_update_chain_options()
        {
            var (window, mainViewModel, editorView) = FilterEditorTestUi.ShowFilterEditorPanes();
            mainViewModel.AppliedFiltersViewModel.AppendCommand.Execute(AppliedFiltersTestUi.Entry("TimeShifter"));
            window.UpdateLayout();
            Dispatcher.UIThread.RunJobs();

            Assert.IsType<TimeShifterFilterEditorViewModel>(mainViewModel.FilterEditorViewModel.OptionsEditor);
            var editorVm = (TimeShifterFilterEditorViewModel)mainViewModel.FilterEditorViewModel.OptionsEditor;

            var editor = editorView.GetVisualDescendants().OfType<TimeShifterFilterEditorView>().Single();
            var fieldCombo = editor.FindControl<ComboBox>("TimestampFieldCombo");
            var amountSpinner = editor.FindControl<CompactNumericUpDown>("AmountSpinner");
            var unitCombo = editor.FindControl<ComboBox>("UnitCombo");
            Assert.NotNull(fieldCombo);
            Assert.NotNull(amountSpinner);
            Assert.NotNull(unitCombo);
            Assert.Equal(1m, amountSpinner.Value);
            Assert.Equal(TimeShiftUnit.Days, unitCombo.SelectedItem);

            fieldCombo.SelectedItem = editorVm.TimestampFields.Single(c => c.Field == TimestampField.Creation);
            amountSpinner.Value = -2;
            unitCombo.SelectedItem = TimeShiftUnit.Hours;
            window.UpdateLayout();
            Dispatcher.UIThread.RunJobs();

            var filter = (TimeShifterFilter)mainViewModel.AppliedFiltersViewModel.ToChain().Steps[0].Filter;
            Assert.Equal(TimestampField.Creation, filter.Options.TimestampField);
            Assert.Equal(-2, filter.Options.Amount);
            Assert.Equal(TimeShiftUnit.Hours, filter.Options.Unit);

            window.Close();
        }
    }
}
