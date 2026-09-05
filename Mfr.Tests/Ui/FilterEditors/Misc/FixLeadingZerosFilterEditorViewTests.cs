using Avalonia.Controls;
using Avalonia.Headless.XUnit;
using Avalonia.Threading;
using Avalonia.VisualTree;
using Mfr.App.Ui.ViewModels.FilterEditors.Misc;
using Mfr.App.Ui.Views.Controls;
using Mfr.App.Ui.Views.FilterEditors.Misc;
using Mfr.Filters.Misc;
using Mfr.Tests.Ui.AppliedFilters;

namespace Mfr.Tests.Ui.FilterEditors.Misc
{
    /// <summary>
    /// Headless tests for <see cref="FixLeadingZerosFilterEditorView"/>.
    /// </summary>
    public sealed class FixLeadingZerosFilterEditorViewTests
    {
        /// <summary>
        /// Verifies Fix Leading 0's option edits persist on the applied step.
        /// </summary>
        [AvaloniaFact]
        public void Fix_leading_zeros_controls_update_chain_options()
        {
            var (window, mainViewModel, editorView) = FilterEditorTestUi.ShowFilterEditorPanes();
            mainViewModel.AppliedFiltersViewModel.AppendCommand.Execute(AppliedFiltersTestUi.Entry("FixLeadingZeros"));
            window.UpdateLayout();
            Dispatcher.UIThread.RunJobs();

            Assert.IsType<FixLeadingZerosFilterEditorViewModel>(mainViewModel.FilterEditorViewModel.OptionsEditor);

            var editor = editorView.GetVisualDescendants().OfType<FixLeadingZerosFilterEditorView>().Single();
            var widthSpinner = editor.FindControl<CompactNumericUpDown>("WidthSpinner");
            var maxCountSpinner = editor.FindControl<CompactNumericUpDown>("MaxCountSpinner");
            var removeExtraZeros = editor.FindControl<CompactCheckBox>("RemoveExtraZerosCheckBox");
            var wholeWordOnly = editor.FindControl<CompactCheckBox>("WholeWordOnlyCheckBox");
            Assert.NotNull(widthSpinner);
            Assert.NotNull(maxCountSpinner);
            Assert.NotNull(removeExtraZeros);
            Assert.NotNull(wholeWordOnly);
            Assert.Equal(2, widthSpinner.Value);
            Assert.Equal(1, maxCountSpinner.Value);
            Assert.False(removeExtraZeros.IsChecked);
            Assert.True(wholeWordOnly.IsChecked);

            widthSpinner.Value = 4;
            maxCountSpinner.Value = 0;
            removeExtraZeros.IsChecked = true;
            wholeWordOnly.IsChecked = false;
            window.UpdateLayout();
            Dispatcher.UIThread.RunJobs();

            var filter = (FixLeadingZerosFilter)mainViewModel.AppliedFiltersViewModel.ToChain().Steps[0].Filter;
            Assert.Equal(4, filter.Options.Width);
            Assert.Equal(0, filter.Options.MaxCount);
            Assert.True(filter.Options.RemoveExtraZeros);
            Assert.False(filter.Options.WholeWordOnly);

            window.Close();
        }
    }
}
