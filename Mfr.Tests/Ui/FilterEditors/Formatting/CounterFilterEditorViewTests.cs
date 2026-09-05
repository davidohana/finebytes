using Avalonia.Controls;
using Avalonia.Headless.XUnit;
using Avalonia.Threading;
using Avalonia.VisualTree;
using Mfr.App.Ui.ViewModels.FilterEditors.Formatting;
using Mfr.App.Ui.Views.Controls;
using Mfr.App.Ui.Views.FilterEditors.Formatting;
using Mfr.Filters.Formatting;
using Mfr.Tests.Ui.AppliedFilters;

namespace Mfr.Tests.Ui.FilterEditors.Formatting
{
    /// <summary>
    /// Headless tests for <see cref="CounterFilterEditorView"/>.
    /// </summary>
    public sealed class CounterFilterEditorViewTests
    {
        /// <summary>
        /// Verifies Counter option edits persist on the applied step.
        /// </summary>
        [AvaloniaFact]
        public void Counter_controls_update_chain_options()
        {
            var (window, mainViewModel, editorView) = FilterEditorTestUi.ShowFilterEditorPanes();
            mainViewModel.AppliedFiltersViewModel.AppendCommand.Execute(AppliedFiltersTestUi.Entry("Counter"));
            window.UpdateLayout();
            Dispatcher.UIThread.RunJobs();

            Assert.IsType<CounterFilterEditorViewModel>(mainViewModel.FilterEditorViewModel.OptionsEditor);

            var editor = editorView.GetVisualDescendants().OfType<CounterFilterEditorView>().Single();
            var start = editor.FindControl<CompactNumericUpDown>("StartSpinner");
            var increment = editor.FindControl<CompactNumericUpDown>("IncrementSpinner");
            var leadingZerosMode = editor.FindControl<ComboBox>("LeadingZerosModeCombo");
            var customLength = editor.FindControl<CompactNumericUpDown>("CustomLengthSpinner");
            var replaceRadio = editor.FindControl<RadioButton>("ReplaceRadio");
            var separator = editor.FindControl<TextBox>("SeparatorBox");
            var resetPerFolder = editor.FindControl<CompactCheckBox>("ResetPerFolderCheckBox");
            Assert.NotNull(start);
            Assert.NotNull(increment);
            Assert.NotNull(leadingZerosMode);
            Assert.NotNull(customLength);
            Assert.NotNull(replaceRadio);
            Assert.NotNull(separator);
            Assert.NotNull(resetPerFolder);
            Assert.Equal(1, start.Value);
            Assert.Equal(1, increment.Value);
            Assert.Equal(CounterLeadingZerosMode.None, leadingZerosMode.SelectedItem);
            Assert.Equal(2, customLength.Value);
            Assert.Equal(" - ", separator.Text);
            Assert.True(resetPerFolder.IsChecked);

            start.Value = 10;
            increment.Value = 5;
            leadingZerosMode.SelectedItem = CounterLeadingZerosMode.Custom;
            customLength.Value = 3;
            replaceRadio.IsChecked = true;
            separator.Text = "_";
            resetPerFolder.IsChecked = false;
            window.UpdateLayout();
            Dispatcher.UIThread.RunJobs();

            var filter = (CounterFilter)mainViewModel.AppliedFiltersViewModel.ToChain().Steps[0].Filter;
            Assert.Equal(10, filter.Options.Start);
            Assert.Equal(5, filter.Options.Step);
            Assert.Equal(CounterLeadingZerosMode.Custom, filter.Options.LeadingZerosMode);
            Assert.Equal(3, filter.Options.CustomLength);
            Assert.Equal(CounterPosition.Replace, filter.Options.Position);
            Assert.Equal("_", filter.Options.Separator);
            Assert.False(filter.Options.ResetPerFolder);

            window.Close();
        }
    }
}
