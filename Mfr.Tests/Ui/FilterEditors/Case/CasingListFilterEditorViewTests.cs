using Avalonia.Controls;
using Avalonia.Headless.XUnit;
using Avalonia.Media;
using Avalonia.Threading;
using Avalonia.VisualTree;
using Mfr.App.Ui.ViewModels.FilterEditors.Case;
using Mfr.App.Ui.Views.Controls;
using Mfr.App.Ui.Views.FilterEditors.Case;
using Mfr.Filters.Case;
using Mfr.Tests.Ui.AppliedFilters;

namespace Mfr.Tests.Ui.FilterEditors.Case
{
    /// <summary>
    /// Headless tests for <see cref="CasingListFilterEditorView"/>.
    /// </summary>
    public sealed class CasingListFilterEditorViewTests
    {
        /// <summary>
        /// Verifies Casing List option edits persist on the applied step.
        /// </summary>
        [AvaloniaFact]
        public void Casing_list_controls_update_chain_options()
        {
            var (window, mainViewModel, editorView) = FilterEditorTestUi.ShowFilterEditorPanes();
            mainViewModel.AppliedFiltersViewModel.AppendCommand.Execute(AppliedFiltersTestUi.Entry("CasingList"));
            window.UpdateLayout();
            Dispatcher.UIThread.RunJobs();

            Assert.IsType<CasingListFilterEditorViewModel>(mainViewModel.FilterEditorViewModel.OptionsEditor);

            var editor = editorView.GetVisualDescendants().OfType<CasingListFilterEditorView>().Single();
            var words = editor.FindControl<TextBox>("WordsBox");
            var uppercase = editor.FindControl<CompactCheckBox>("UppercaseSentenceInitialCheckBox");
            Assert.NotNull(words);
            Assert.NotNull(uppercase);
            Assert.Equal(TextWrapping.Wrap, words.TextWrapping);
            Assert.Equal(string.Empty, words.Text);
            Assert.True(uppercase.IsChecked);

            words.Text = "and or RMX";
            uppercase.IsChecked = false;
            window.UpdateLayout();
            Dispatcher.UIThread.RunJobs();

            var filter = (CasingListFilter)mainViewModel.AppliedFiltersViewModel.ToChain().Steps[0].Filter;
            Assert.Equal(["and", "or", "RMX"], filter.Options.Words);
            Assert.False(filter.Options.UppercaseSentenceInitial);

            window.Close();
        }
    }
}
