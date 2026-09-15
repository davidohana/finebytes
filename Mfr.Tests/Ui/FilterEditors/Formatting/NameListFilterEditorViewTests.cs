using Avalonia.Controls;
using Avalonia.Headless.XUnit;
using Avalonia.Threading;
using Avalonia.VisualTree;
using AvaloniaEdit;
using Mfr.App.Ui.ViewModels.FilterEditors;
using Mfr.App.Ui.ViewModels.FilterEditors.Formatting;
using Mfr.App.Ui.Views.FilterEditors.Formatting;
using Mfr.Filters;
using Mfr.Filters.Formatting;
using Mfr.Tests.Ui.FilterChain;
using FormatEditorControl = Mfr.App.Ui.Views.FormatEditor.FormatEditor;

namespace Mfr.Tests.Ui.FilterEditors.Formatting
{
    /// <summary>
    /// Headless tests for <see cref="NameListFilterEditorView"/>.
    /// </summary>
    public sealed class NameListFilterEditorViewTests
    {
        public NameListFilterEditorViewTests()
        {
            FilterOptionsEditorViewModel.LiveListTextApplyDebounceMilliseconds = 0;
        }

        /// <summary>
        /// Verifies Name List option edits persist on the applied step.
        /// </summary>
        [AvaloniaFact]
        public void Name_list_controls_update_chain_options()
        {
            var (window, mainViewModel, editorView) = FilterEditorTestUi.ShowFilterEditorPanes();
            mainViewModel.FilterChainViewModel.AppendCommand.Execute(FilterChainTestUi.Entry("NameList"));
            window.UpdateLayout();
            Dispatcher.UIThread.RunJobs();

            Assert.IsType<NameListFilterEditorViewModel>(mainViewModel.FilterEditorViewModel.OptionsEditor);

            var editor = editorView.GetVisualDescendants().OfType<NameListFilterEditorView>().Single();
            var entries = editor.FindControl<TextEditor>("EntriesBox");
            var prefix = editor.FindControl<FormatEditorControl>("PrefixEditor");
            var suffix = editor.FindControl<FormatEditorControl>("SuffixEditor");
            Assert.NotNull(entries);
            Assert.NotNull(prefix);
            Assert.NotNull(suffix);
            Assert.False(entries.WordWrap);
            Assert.Equal(NameListFilterEditorView.EntriesMaxHeight, entries.MaxHeight);
            Assert.False(prefix.AcceptsReturn);
            Assert.False(suffix.AcceptsReturn);
            Assert.Equal(string.Empty, entries.Text);
            Assert.Equal(string.Empty, prefix.Text);
            Assert.Equal(string.Empty, suffix.Text);

            entries.Text = "Alpha\nBeta";
            prefix.Text = "pre_";
            suffix.Text = "_suf";
            window.UpdateLayout();
            Dispatcher.UIThread.RunJobs();

            var filter = (NameListFilter)mainViewModel.FilterChainViewModel.ToChain().Steps[0].Filter;
            Assert.Equal(["Alpha", "Beta"], filter.Options.Entries);
            Assert.Equal("pre_", filter.Options.Prefix);
            Assert.Equal("_suf", filter.Options.Suffix);

            window.Close();
        }

        /// <summary>
        /// Verifies a large Name List document stays inside the editor ceiling instead of expanding
        /// Filter Configuration's unconstrained <see cref="ScrollViewer"/>.
        /// </summary>
        [AvaloniaFact]
        public void Large_entries_stay_within_max_height()
        {
            var (window, mainViewModel, editorView) = FilterEditorTestUi.ShowFilterEditorPanes();
            mainViewModel.FilterChainViewModel.AppendCommand.Execute(FilterChainTestUi.Entry("NameList"));
            window.UpdateLayout();
            Dispatcher.UIThread.RunJobs();

            var editor = editorView.GetVisualDescendants().OfType<NameListFilterEditorView>().Single();
            var entries = editor.FindControl<TextEditor>("EntriesBox");
            Assert.NotNull(entries);

            entries.Text = string.Join('\n', Enumerable.Range(0, 80).Select(i => $"name-{i}.mp3"));
            window.UpdateLayout();
            Dispatcher.UIThread.RunJobs();

            Assert.True(entries.Bounds.Height <= NameListFilterEditorView.EntriesMaxHeight + 1);

            var scroll = editorView.GetVisualDescendants().OfType<ScrollViewer>().First();
            Assert.True(scroll.Extent.Height < 2000);

            window.Close();
        }

        /// <summary>
        /// Verifies the entries document is truncated to the paste budget without throwing.
        /// </summary>
        [AvaloniaFact]
        public void Entries_paste_budget_truncates_document()
        {
            var (window, mainViewModel, editorView) = FilterEditorTestUi.ShowFilterEditorPanes();
            mainViewModel.FilterChainViewModel.AppendCommand.Execute(FilterChainTestUi.Entry("NameList"));
            window.UpdateLayout();
            Dispatcher.UIThread.RunJobs();

            var editor = editorView.GetVisualDescendants().OfType<NameListFilterEditorView>().Single();
            var entries = editor.FindControl<TextEditor>("EntriesBox");
            Assert.NotNull(entries);

            entries.Text = new string('a', ListEntryLength.DefaultEditorTextMaxLength + 50);
            window.UpdateLayout();
            Dispatcher.UIThread.RunJobs();

            Assert.Equal(ListEntryLength.DefaultEditorTextMaxLength, entries.Text.Length);

            window.Close();
        }
    }
}
