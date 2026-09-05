using Avalonia.Controls;
using Avalonia.Headless.XUnit;
using Avalonia.Threading;
using Avalonia.VisualTree;
using Mfr.App.Ui.ViewModels.FilterEditors.Space;
using Mfr.App.Ui.Views.Controls;
using Mfr.App.Ui.Views.FilterEditors.Space;
using Mfr.Filters.Space;
using Mfr.Tests.Ui.AppliedFilters;

namespace Mfr.Tests.Ui.FilterEditors.Space
{
    /// <summary>
    /// Headless tests for <see cref="SpaceTriggerFilterEditorView"/>.
    /// </summary>
    public sealed class SpaceTriggerFilterEditorViewTests
    {
        /// <summary>
        /// Verifies Space After option edits persist on the applied step.
        /// </summary>
        [AvaloniaFact]
        public void Space_after_controls_update_chain_options()
        {
            var (window, mainViewModel, editorView) = FilterEditorTestUi.ShowFilterEditorPanes();
            mainViewModel.AppliedFiltersViewModel.AppendCommand.Execute(AppliedFiltersTestUi.Entry("SpaceAfter"));
            window.UpdateLayout();
            Dispatcher.UIThread.RunJobs();

            Assert.IsType<SpaceTriggerFilterEditorViewModel>(mainViewModel.FilterEditorViewModel.OptionsEditor);

            var editor = editorView.GetVisualDescendants().OfType<SpaceTriggerFilterEditorView>().Single();
            var charsBox = editor.FindControl<TextBox>("CharsBox");
            var neighborCheck = editor.FindControl<CompactCheckBox>("NeighborCheckBox");
            Assert.NotNull(charsBox);
            Assert.NotNull(neighborCheck);
            Assert.Equal(",;!", charsBox.Text);
            Assert.True(neighborCheck.IsChecked);

            charsBox.Text = ".,";
            neighborCheck.IsChecked = false;
            window.UpdateLayout();
            Dispatcher.UIThread.RunJobs();

            var filter = (SpaceAfterFilter)mainViewModel.AppliedFiltersViewModel.ToChain().Steps[0].Filter;
            Assert.Equal(".,", filter.Options.AfterChars);
            Assert.False(filter.Options.OnlyWhenNextIsLetterOrDigit);

            window.Close();
        }

        /// <summary>
        /// Verifies Space Around option edits persist on the applied step.
        /// </summary>
        [AvaloniaFact]
        public void Space_around_controls_update_chain_options()
        {
            var (window, mainViewModel, editorView) = FilterEditorTestUi.ShowFilterEditorPanes();
            mainViewModel.AppliedFiltersViewModel.AppendCommand.Execute(AppliedFiltersTestUi.Entry("SpaceAround"));
            window.UpdateLayout();
            Dispatcher.UIThread.RunJobs();

            Assert.IsType<SpaceTriggerFilterEditorViewModel>(mainViewModel.FilterEditorViewModel.OptionsEditor);

            var editor = editorView.GetVisualDescendants().OfType<SpaceTriggerFilterEditorView>().Single();
            var charsBox = editor.FindControl<TextBox>("CharsBox");
            var neighborCheck = editor.FindControl<CompactCheckBox>("NeighborCheckBox");
            Assert.NotNull(charsBox);
            Assert.NotNull(neighborCheck);
            Assert.Equal("-", charsBox.Text);
            Assert.True(neighborCheck.IsChecked);

            charsBox.Text = "+=";
            neighborCheck.IsChecked = false;
            window.UpdateLayout();
            Dispatcher.UIThread.RunJobs();

            var filter = (SpaceAroundFilter)mainViewModel.AppliedFiltersViewModel.ToChain().Steps[0].Filter;
            Assert.Equal("+=", filter.Options.AroundChars);
            Assert.False(filter.Options.OnlyWhenNeighboringAreLettersOrDigits);

            window.Close();
        }
    }
}
