using Avalonia.Controls;
using Avalonia.Headless.XUnit;
using Avalonia.Threading;
using Avalonia.VisualTree;
using Mfr.App.Ui.ViewModels.FilterEditors.Replace;
using Mfr.App.Ui.Views.Controls;
using Mfr.App.Ui.Views.FilterEditors.Replace;
using Mfr.Filters.Replace;
using Mfr.Tests.Ui.AppliedFilters;

namespace Mfr.Tests.Ui.FilterEditors.Replace
{
    /// <summary>
    /// Headless tests for <see cref="ReplacerFilterEditorView"/>.
    /// </summary>
    public sealed class ReplacerFilterEditorViewTests
    {
        /// <summary>
        /// Verifies Replacer option edits persist on the applied step.
        /// </summary>
        [AvaloniaFact]
        public void Replacer_controls_update_chain_options()
        {
            var (window, mainViewModel, editorView) = FilterEditorTestUi.ShowFilterEditorPanes();
            mainViewModel.AppliedFiltersViewModel.AppendCommand.Execute(AppliedFiltersTestUi.Entry("Replacer"));
            window.UpdateLayout();
            Dispatcher.UIThread.RunJobs();

            Assert.IsType<ReplacerFilterEditorViewModel>(mainViewModel.FilterEditorViewModel.OptionsEditor);

            var editor = editorView.GetVisualDescendants().OfType<ReplacerFilterEditorView>().Single();
            var mode = editor.FindControl<ReplacerModeFieldset>("ModeFieldset");
            var matchOptions = editor.FindControl<ReplacerMatchOptionsFieldset>("MatchOptionsFieldset");
            Assert.NotNull(mode);
            Assert.NotNull(matchOptions);
            var find = editor.FindControl<TextBox>("FindBox");
            var replacement = editor.FindControl<TextBox>("ReplacementBox");
            var literal = mode.FindControl<CompactRadioButton>("LiteralRadio");
            var wildcard = mode.FindControl<CompactRadioButton>("WildcardRadio");
            var regex = mode.FindControl<CompactRadioButton>("RegexRadio");
            var caseSensitive = matchOptions.FindControl<CompactCheckBox>("CaseSensitiveCheckBox");
            var replaceAll = matchOptions.FindControl<CompactCheckBox>("ReplaceAllCheckBox");
            var wholeWord = matchOptions.FindControl<CompactCheckBox>("WholeWordCheckBox");
            Assert.NotNull(find);
            Assert.NotNull(replacement);
            Assert.NotNull(literal);
            Assert.NotNull(wildcard);
            Assert.NotNull(regex);
            Assert.NotNull(caseSensitive);
            Assert.NotNull(replaceAll);
            Assert.NotNull(wholeWord);
            Assert.Equal(string.Empty, find.Text);
            Assert.Equal(string.Empty, replacement.Text);
            Assert.Equal("feat.", find.Watermark);
            Assert.Equal("feature.", replacement.Watermark);
            Assert.True(literal.IsChecked);
            Assert.False(caseSensitive.IsChecked);
            Assert.True(replaceAll.IsChecked);
            Assert.False(wholeWord.IsChecked);

            find.Text = "dog";
            replacement.Text = "cat";
            wildcard.IsChecked = true;
            caseSensitive.IsChecked = true;
            replaceAll.IsChecked = false;
            wholeWord.IsChecked = true;
            window.UpdateLayout();
            Dispatcher.UIThread.RunJobs();

            Assert.Equal("DSC*.JPG", find.Watermark);
            Assert.Equal("photo.jpg", replacement.Watermark);

            var filter = (ReplacerFilter)mainViewModel.AppliedFiltersViewModel.ToChain().Steps[0].Filter;
            Assert.Equal("dog", filter.Options.Find);
            Assert.Equal("cat", filter.Options.Replacement);
            Assert.Equal(ReplacerMode.Wildcard, filter.Options.Match.Mode);
            Assert.True(filter.Options.Match.CaseSensitive);
            Assert.False(filter.Options.Match.ReplaceAll);
            Assert.True(filter.Options.Match.WholeWord);

            regex.IsChecked = true;
            window.UpdateLayout();
            Dispatcher.UIThread.RunJobs();
            filter = (ReplacerFilter)mainViewModel.AppliedFiltersViewModel.ToChain().Steps[0].Filter;
            Assert.Equal(ReplacerMode.Regex, filter.Options.Match.Mode);
            Assert.Equal(@"\((.+)\)", find.Watermark);
            Assert.Equal("$1", replacement.Watermark);

            window.Close();
        }
    }
}
