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
    /// Headless tests for <see cref="ReplaceListFilterEditorView"/>.
    /// </summary>
    public sealed class ReplaceListFilterEditorViewTests
    {
        /// <summary>
        /// Verifies Replace List option edits persist on the applied step.
        /// </summary>
        [AvaloniaFact]
        public void Replace_list_controls_update_chain_options()
        {
            var (window, mainViewModel, editorView) = FilterEditorTestUi.ShowFilterEditorPanes();
            mainViewModel.AppliedFiltersViewModel.AppendCommand.Execute(AppliedFiltersTestUi.Entry("ReplaceList"));
            window.UpdateLayout();
            Dispatcher.UIThread.RunJobs();

            Assert.IsType<ReplaceListFilterEditorViewModel>(mainViewModel.FilterEditorViewModel.OptionsEditor);

            var editor = editorView.GetVisualDescendants().OfType<ReplaceListFilterEditorView>().Single();
            var mode = editor.FindControl<ReplacerModeFieldset>("ModeFieldset");
            var matchOptions = editor.FindControl<ReplacerMatchOptionsFieldset>("MatchOptionsFieldset");
            Assert.NotNull(mode);
            Assert.NotNull(matchOptions);
            var entries = editor.FindControl<TextBox>("EntriesBox");
            var literal = mode.FindControl<CompactRadioButton>("LiteralRadio");
            var wildcard = mode.FindControl<CompactRadioButton>("WildcardRadio");
            var caseSensitive = matchOptions.FindControl<CompactCheckBox>("CaseSensitiveCheckBox");
            var replaceAll = matchOptions.FindControl<CompactCheckBox>("ReplaceAllCheckBox");
            var wholeWord = matchOptions.FindControl<CompactCheckBox>("WholeWordCheckBox");
            Assert.NotNull(entries);
            Assert.NotNull(literal);
            Assert.NotNull(wildcard);
            Assert.NotNull(caseSensitive);
            Assert.NotNull(replaceAll);
            Assert.NotNull(wholeWord);
            Assert.True(entries.AcceptsReturn);
            Assert.Equal(string.Empty, entries.Text);
            Assert.True(literal.IsChecked);
            Assert.Equal(". => _\nfeat. => feature.\nLive", entries.Watermark);
            Assert.False(caseSensitive.IsChecked);
            Assert.True(replaceAll.IsChecked);
            Assert.True(wholeWord.IsChecked);

            wildcard.IsChecked = true;
            window.UpdateLayout();
            Dispatcher.UIThread.RunJobs();
            Assert.Equal("DSC*.JPG => photo.jpg\ntrack?.mp3 => track0.mp3\n*.tmp", entries.Watermark);

            entries.Text = "a => b\n. => _";
            caseSensitive.IsChecked = true;
            replaceAll.IsChecked = false;
            wholeWord.IsChecked = false;
            window.UpdateLayout();
            Dispatcher.UIThread.RunJobs();

            var filter = (ReplaceListFilter)mainViewModel.AppliedFiltersViewModel.ToChain().Steps[0].Filter;
            Assert.Equal(2, filter.Options.Entries.Count);
            Assert.Equal("a", filter.Options.Entries[0].Search);
            Assert.Equal("b", filter.Options.Entries[0].Replacement);
            Assert.Equal(".", filter.Options.Entries[1].Search);
            Assert.Equal("_", filter.Options.Entries[1].Replacement);
            Assert.Equal(ReplacerMode.Wildcard, filter.Options.Match.Mode);
            Assert.True(filter.Options.Match.CaseSensitive);
            Assert.False(filter.Options.Match.ReplaceAll);
            Assert.False(filter.Options.Match.WholeWord);

            window.Close();
        }
    }
}
