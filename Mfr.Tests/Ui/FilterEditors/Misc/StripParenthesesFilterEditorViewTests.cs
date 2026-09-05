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
    /// Headless tests for <see cref="StripParenthesesFilterEditorView"/>.
    /// </summary>
    public sealed class StripParenthesesFilterEditorViewTests
    {
        /// <summary>
        /// Verifies Strip Parentheses option edits persist on the applied step.
        /// </summary>
        [AvaloniaFact]
        public void Strip_parentheses_controls_update_chain_options()
        {
            var (window, mainViewModel, editorView) = FilterEditorTestUi.ShowFilterEditorPanes();

            mainViewModel.AppliedFiltersViewModel.AppendCommand.Execute(AppliedFiltersTestUi.Entry("StripParentheses"));

            window.UpdateLayout();

            Dispatcher.UIThread.RunJobs();

            Assert.IsType<StripParenthesesFilterEditorViewModel>(mainViewModel.FilterEditorViewModel.OptionsEditor);

            var editor = editorView.GetVisualDescendants().OfType<StripParenthesesFilterEditorView>().Single();

            var squareRadio = editor.FindControl<RadioButton>("SquareRadio");

            var removeContents = editor.FindControl<CompactCheckBox>("RemoveContentsCheckBox");

            Assert.NotNull(squareRadio);

            Assert.NotNull(removeContents);

            Assert.True(removeContents.IsChecked);

            squareRadio.IsChecked = true;

            removeContents.IsChecked = false;

            window.UpdateLayout();

            Dispatcher.UIThread.RunJobs();

            var filter = (StripParenthesesFilter)mainViewModel.AppliedFiltersViewModel.ToChain().Steps[0].Filter;

            Assert.Equal(ParenthesisType.Square, filter.Options.Type);

            Assert.False(filter.Options.RemoveContents);

            window.Close();
        }
    }
}
