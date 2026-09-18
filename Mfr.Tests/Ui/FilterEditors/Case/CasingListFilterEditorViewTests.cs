using Avalonia.Controls;
using Avalonia.Headless.XUnit;
using Avalonia.Media;
using Avalonia.Threading;
using Avalonia.VisualTree;
using Mfr.App.Ui.ViewModels.FilterEditors;
using Mfr.App.Ui.ViewModels.FilterEditors.Case;
using Mfr.App.Ui.Views.Controls;
using Mfr.App.Ui.Views.FilterEditors.Case;
using Mfr.Filters;
using Mfr.Filters.Case;
using Mfr.Tests.Ui.FilterChainPane;

namespace Mfr.Tests.Ui.FilterEditors.Case
{
    /// <summary>
    /// Headless tests for <see cref="CasingListFilterEditorView"/>.
    /// </summary>
    public sealed class CasingListFilterEditorViewTests
    {
        public CasingListFilterEditorViewTests()
        {
            FilterOptionsEditorViewModel.LiveListTextApplyDebounceMilliseconds = 0;
        }

        /// <summary>
        /// Verifies Casing List option edits persist on the applied step.
        /// </summary>
        [AvaloniaFact]
        public void Casing_list_controls_update_chain_options()
        {
            var (window, mainViewModel, editorView) = FilterEditorTestUi.ShowFilterEditorPanes();
            mainViewModel.FilterChainViewModel.AppendCommand.Execute(FilterChainTestUi.Entry("CasingList"));
            window.UpdateLayout();
            Dispatcher.UIThread.RunJobs();

            Assert.IsType<CasingListFilterEditorViewModel>(mainViewModel.FilterEditorViewModel.OptionsEditor);

            var editor = editorView.GetVisualDescendants().OfType<CasingListFilterEditorView>().Single();
            var words = editor.FindControl<TextBox>("WordsBox");
            var uppercase = editor.FindControl<CompactCheckBox>("UppercaseSentenceInitialCheckBox");
            Assert.NotNull(words);
            Assert.NotNull(uppercase);
            Assert.Equal(TextWrapping.NoWrap, words.TextWrapping);
            Assert.Equal(ListEntryLength.DefaultEditorTextMaxLength, words.MaxLength);
            Assert.Equal(string.Empty, words.Text);
            Assert.True(uppercase.IsChecked);

            words.Text = "and or RMX";
            uppercase.IsChecked = false;
            window.UpdateLayout();
            Dispatcher.UIThread.RunJobs();

            var filter = (CasingListFilter)mainViewModel.FilterChainViewModel.ToChain().Steps[0].Filter;
            Assert.Equal(["and", "or", "RMX"], filter.Options.Words);
            Assert.False(filter.Options.UppercaseSentenceInitial);

            var loadDefaults = editor.FindControl<Button>("LoadDefaultsButton");
            Assert.NotNull(loadDefaults);
            loadDefaults.Command!.Execute(null);
            window.UpdateLayout();
            Dispatcher.UIThread.RunJobs();

            filter = (CasingListFilter)mainViewModel.FilterChainViewModel.ToChain().Steps[0].Filter;
            Assert.Equal(CasingListOptions.DefaultWords, filter.Options.Words);

            window.Close();
        }
    }
}
