using Avalonia.Controls;
using Avalonia.Headless.XUnit;
using Avalonia.Threading;
using Avalonia.VisualTree;
using Mfr.App.Ui.ViewModels.FilterEditors.Trimming;
using Mfr.App.Ui.Views.Controls;
using Mfr.App.Ui.Views.FilterEditors.Trimming;
using Mfr.Filters;
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

        /// <summary>
        /// Verifies Visual Trim Helper pointer selection updates Trim Left count on the chain.
        /// </summary>
        [AvaloniaFact]
        public void Visual_trim_helper_selection_updates_trim_left_count()
        {
            var (window, mainViewModel, editorView) = FilterEditorTestUi.ShowFilterEditorPanes();
            mainViewModel.AppliedFiltersViewModel.AppendCommand.Execute(AppliedFiltersTestUi.Entry("TrimLeft"));
            window.UpdateLayout();
            Dispatcher.UIThread.RunJobs();

            var optionsEditor = Assert.IsType<CountFilterEditorViewModel>(
                mainViewModel.FilterEditorViewModel.OptionsEditor
            );
            optionsEditor.TrimHelper.SetSampleText("abcdef");
            window.UpdateLayout();
            Dispatcher.UIThread.RunJobs();

            var helperView = editorView.GetVisualDescendants().OfType<VisualTrimHelperView>().Single();
            var textBox = helperView.FindControl<TextBox>("TrimHelperText");
            Assert.NotNull(textBox);
            Assert.Equal("abcdef", textBox.Text);

            textBox.SelectionStart = 0;
            textBox.SelectionEnd = 4;
            FilterEditorTestUi.RaisePointerReleased(textBox);
            window.UpdateLayout();
            Dispatcher.UIThread.RunJobs();

            Assert.Equal(4, _CountOf(mainViewModel.AppliedFiltersViewModel.ToChain().Steps[0].Filter));
            Assert.Equal(0, textBox.SelectionStart);
            Assert.Equal(4, textBox.SelectionEnd);

            window.Close();
        }

        /// <summary>
        /// Verifies spinner changes update the helper TextBox selection highlight.
        /// </summary>
        [AvaloniaFact]
        public void Count_spinner_updates_helper_text_selection()
        {
            var (window, mainViewModel, editorView) = FilterEditorTestUi.ShowFilterEditorPanes();
            mainViewModel.AppliedFiltersViewModel.AppendCommand.Execute(AppliedFiltersTestUi.Entry("TrimLeft"));
            window.UpdateLayout();
            Dispatcher.UIThread.RunJobs();

            var optionsEditor = Assert.IsType<CountFilterEditorViewModel>(
                mainViewModel.FilterEditorViewModel.OptionsEditor
            );
            optionsEditor.TrimHelper.SetSampleText("abcdef");
            window.UpdateLayout();
            Dispatcher.UIThread.RunJobs();

            var editor = editorView.GetVisualDescendants().OfType<CountFilterEditorView>().Single();
            var spinner = editor.FindControl<CompactNumericUpDown>("CountSpinner");
            var textBox = editor
                .GetVisualDescendants()
                .OfType<VisualTrimHelperView>()
                .Single()
                .FindControl<TextBox>("TrimHelperText");
            Assert.NotNull(spinner);
            Assert.NotNull(textBox);

            spinner.Value = 2;
            window.UpdateLayout();
            Dispatcher.UIThread.RunJobs();

            Assert.Equal(0, textBox.SelectionStart);
            Assert.Equal(2, textBox.SelectionEnd);

            window.Close();
        }

        private static int _CountOf(BaseFilter filter)
        {
            return Assert.IsAssignableFrom<ICountOptionsFilter>(filter).Options.Count;
        }
    }
}
