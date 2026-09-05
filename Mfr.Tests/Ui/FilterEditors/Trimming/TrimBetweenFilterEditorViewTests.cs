using Avalonia.Controls;
using Avalonia.Headless.XUnit;
using Avalonia.Threading;
using Avalonia.VisualTree;
using Mfr.App.Ui.ViewModels.FilterEditors.Trimming;
using Mfr.App.Ui.Views.Controls;
using Mfr.App.Ui.Views.FilterEditors.Trimming;
using Mfr.Filters.Trimming;
using Mfr.Tests.Ui.AppliedFilters;

namespace Mfr.Tests.Ui.FilterEditors.Trimming
{
    /// <summary>
    /// Headless tests for <see cref="TrimBetweenFilterEditorView"/>.
    /// </summary>
    public sealed class TrimBetweenFilterEditorViewTests
    {
        /// <summary>
        /// Verifies Trim Between position/anchor edits persist on the applied step.
        /// </summary>
        [AvaloniaFact]
        public void Trim_between_controls_update_chain_options()
        {
            var (window, mainViewModel, editorView) = FilterEditorTestUi.ShowFilterEditorPanes();
            mainViewModel.AppliedFiltersViewModel.AppendCommand.Execute(AppliedFiltersTestUi.Entry("TrimBetween"));
            window.UpdateLayout();
            Dispatcher.UIThread.RunJobs();

            Assert.IsType<TrimBetweenFilterEditorViewModel>(mainViewModel.FilterEditorViewModel.OptionsEditor);

            var editor = editorView.GetVisualDescendants().OfType<TrimBetweenFilterEditorView>().Single();
            var startSpinner = editor.FindControl<CompactNumericUpDown>("StartValueSpinner");
            var endSpinner = editor.FindControl<CompactNumericUpDown>("EndValueSpinner");
            var startAnchor = editor.FindControl<ComboBox>("StartAnchorCombo");
            var endAnchor = editor.FindControl<ComboBox>("EndAnchorCombo");
            Assert.NotNull(startSpinner);
            Assert.NotNull(endSpinner);
            Assert.NotNull(startAnchor);
            Assert.NotNull(endAnchor);
            Assert.Equal(2, startSpinner.Value);
            Assert.Equal(4, endSpinner.Value);
            Assert.Equal(Side.Left, startAnchor.SelectedItem);
            Assert.Equal(Side.Left, endAnchor.SelectedItem);

            startSpinner.Value = 13;
            endSpinner.Value = 5;
            endAnchor.SelectedItem = Side.Right;
            window.UpdateLayout();
            Dispatcher.UIThread.RunJobs();

            var filter = (TrimBetweenFilter)mainViewModel.AppliedFiltersViewModel.ToChain().Steps[0].Filter;
            Assert.Equal(new Position(13, Side.Left), filter.Options.Start);
            Assert.Equal(new Position(5, Side.Right), filter.Options.End);

            window.Close();
        }
    }
}
