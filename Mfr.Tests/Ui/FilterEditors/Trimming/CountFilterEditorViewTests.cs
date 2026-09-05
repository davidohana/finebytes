using Avalonia.Controls;
using Avalonia.Headless.XUnit;
using Avalonia.Threading;
using Avalonia.VisualTree;
using Mfr.App.Ui.ViewModels.FilterEditors.Trimming;
using Mfr.App.Ui.Views.Controls;
using Mfr.App.Ui.Views.FilterEditors.Trimming;
using Mfr.Filters;
using Mfr.Filters.Trimming;
using Mfr.Tests.Ui.AppliedFilters;

namespace Mfr.Tests.Ui.FilterEditors.Trimming
{
    /// <summary>
    /// Headless tests for <see cref="CountFilterEditorView"/>.
    /// </summary>
    public sealed class CountFilterEditorViewTests
    {
        /// <summary>
        /// Verifies Count filter numeric edits persist on the applied step for all four count filter types.
        /// </summary>
        [AvaloniaTheory]
        [InlineData("TrimLeft")]
        [InlineData("TrimRight")]
        [InlineData("ExtractLeft")]
        [InlineData("ExtractRight")]
        public void Count_filter_numeric_box_updates_chain_options(string filterType)
        {
            var (window, mainViewModel, editorView) = FilterEditorTestUi.ShowFilterEditorPanes();
            mainViewModel.AppliedFiltersViewModel.AppendCommand.Execute(AppliedFiltersTestUi.Entry(filterType));
            window.UpdateLayout();
            Dispatcher.UIThread.RunJobs();

            Assert.IsType<CountFilterEditorViewModel>(mainViewModel.FilterEditorViewModel.OptionsEditor);

            var editor = editorView.GetVisualDescendants().OfType<CountFilterEditorView>().Single();
            var spinner = editor.FindControl<CompactNumericUpDown>("CountSpinner");
            Assert.NotNull(spinner);
            Assert.Equal(1, spinner.Value);

            spinner.Value = 5;
            window.UpdateLayout();
            Dispatcher.UIThread.RunJobs();
            Assert.Equal(5, _CountOf(mainViewModel.AppliedFiltersViewModel.ToChain().Steps[0].Filter));

            spinner.Value = 0;
            window.UpdateLayout();
            Dispatcher.UIThread.RunJobs();
            Assert.Equal(0, _CountOf(mainViewModel.AppliedFiltersViewModel.ToChain().Steps[0].Filter));

            window.Close();
        }

        private static int _CountOf(BaseFilter filter)
        {
            return Assert.IsAssignableFrom<ICountOptionsFilter>(filter).Options.Count;
        }
    }
}
