using Avalonia.Controls;
using Avalonia.Headless.XUnit;
using Avalonia.Threading;
using Avalonia.VisualTree;
using Mfr.App.Ui.ViewModels.FilterEditors.Formatting;
using Mfr.App.Ui.Views.FilterEditors.Formatting;
using Mfr.Filters.Formatting;
using Mfr.Tests.Ui.AppliedFilters;

namespace Mfr.Tests.Ui.FilterEditors.Formatting
{
    /// <summary>
    /// Headless tests for <see cref="NameListFilterEditorView"/>.
    /// </summary>
    public sealed class NameListFilterEditorViewTests
    {
        /// <summary>
        /// Verifies Name List option edits persist on the applied step.
        /// </summary>
        [AvaloniaFact]
        public void Name_list_controls_update_chain_options()
        {
            var (window, mainViewModel, editorView) = FilterEditorTestUi.ShowFilterEditorPanes();
            mainViewModel.AppliedFiltersViewModel.AppendCommand.Execute(AppliedFiltersTestUi.Entry("NameList"));
            window.UpdateLayout();
            Dispatcher.UIThread.RunJobs();

            Assert.IsType<NameListFilterEditorViewModel>(mainViewModel.FilterEditorViewModel.OptionsEditor);

            var editor = editorView.GetVisualDescendants().OfType<NameListFilterEditorView>().Single();
            var entries = editor.FindControl<TextBox>("EntriesBox");
            var prefix = editor.FindControl<TextBox>("PrefixBox");
            var suffix = editor.FindControl<TextBox>("SuffixBox");
            Assert.NotNull(entries);
            Assert.NotNull(prefix);
            Assert.NotNull(suffix);
            Assert.True(entries.AcceptsReturn);
            Assert.Equal(string.Empty, entries.Text);
            Assert.Equal(string.Empty, prefix.Text);
            Assert.Equal(string.Empty, suffix.Text);

            entries.Text = "Alpha\nBeta";
            prefix.Text = "pre_";
            suffix.Text = "_suf";
            window.UpdateLayout();
            Dispatcher.UIThread.RunJobs();

            var filter = (NameListFilter)mainViewModel.AppliedFiltersViewModel.ToChain().Steps[0].Filter;
            Assert.Equal(["Alpha", "Beta"], filter.Options.Entries);
            Assert.Equal("pre_", filter.Options.Prefix);
            Assert.Equal("_suf", filter.Options.Suffix);

            window.Close();
        }
    }
}
