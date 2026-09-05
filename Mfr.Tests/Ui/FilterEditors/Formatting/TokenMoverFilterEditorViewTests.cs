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
    /// Headless tests for <see cref="TokenMoverFilterEditorView"/>.
    /// </summary>
    public sealed class TokenMoverFilterEditorViewTests
    {
        /// <summary>
        /// Verifies Token Mover option edits persist on the applied step.
        /// </summary>
        [AvaloniaFact]
        public void Token_mover_controls_update_chain_options()
        {
            var (window, mainViewModel, editorView) = FilterEditorTestUi.ShowFilterEditorPanes();

            mainViewModel.AppliedFiltersViewModel.AppendCommand.Execute(AppliedFiltersTestUi.Entry("TokenMover"));

            window.UpdateLayout();

            Dispatcher.UIThread.RunJobs();

            Assert.IsType<TokenMoverFilterEditorViewModel>(mainViewModel.FilterEditorViewModel.OptionsEditor);

            var editor = editorView.GetVisualDescendants().OfType<TokenMoverFilterEditorView>().Single();

            var delimiter = editor.FindControl<TextBox>("DelimiterBox");

            var tokenNumber = editor.FindControl<CompactNumericUpDown>("TokenNumberSpinner");

            var moveBy = editor.FindControl<CompactNumericUpDown>("MoveBySpinner");

            Assert.NotNull(delimiter);

            Assert.NotNull(tokenNumber);

            Assert.NotNull(moveBy);

            Assert.Equal("-", delimiter.Text);

            Assert.Equal(1, tokenNumber.Value);

            Assert.Equal(1, moveBy.Value);

            delimiter.Text = ",";

            tokenNumber.Value = 2;

            moveBy.Value = -1;

            window.UpdateLayout();

            Dispatcher.UIThread.RunJobs();

            var filter = (TokenMoverFilter)mainViewModel.AppliedFiltersViewModel.ToChain().Steps[0].Filter;

            Assert.Equal(",", filter.Options.Delimiter);

            Assert.Equal(2, filter.Options.TokenNumber);

            Assert.Equal(-1, filter.Options.MoveBy);

            window.Close();
        }
    }
}
