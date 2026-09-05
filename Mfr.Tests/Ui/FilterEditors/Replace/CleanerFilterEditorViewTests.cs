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
    /// Headless tests for <see cref="CleanerFilterEditorView"/>.
    /// </summary>
    public sealed class CleanerFilterEditorViewTests
    {
        /// <summary>
        /// Verifies Cleaner option edits persist on the applied step.
        /// </summary>
        [AvaloniaFact]
        public void Cleaner_controls_update_chain_options()
        {
            var (window, mainViewModel, editorView) = FilterEditorTestUi.ShowFilterEditorPanes();
            mainViewModel.AppliedFiltersViewModel.AppendCommand.Execute(AppliedFiltersTestUi.Entry("Cleaner"));
            window.UpdateLayout();
            Dispatcher.UIThread.RunJobs();

            Assert.IsType<CleanerFilterEditorViewModel>(mainViewModel.FilterEditorViewModel.OptionsEditor);

            var editor = editorView.GetVisualDescendants().OfType<CleanerFilterEditorView>().Single();
            var removeIllegal = editor.FindControl<CompactCheckBox>("RemoveIllegalCharsCheckBox");
            var customChars = editor.FindControl<TextBox>("CustomCharsBox");
            var replaceWith = editor.FindControl<CompactCheckBox>("ReplaceWithCheckBox");
            var replacement = editor.FindControl<TextBox>("ReplacementBox");
            Assert.NotNull(removeIllegal);
            Assert.NotNull(customChars);
            Assert.NotNull(replaceWith);
            Assert.NotNull(replacement);
            Assert.True(removeIllegal.IsChecked);
            Assert.Equal(@"!""#$%&'()*+,/:;<=>?@[]\^`{}|~", customChars.Text);
            Assert.False(replaceWith.IsChecked);
            Assert.Equal(string.Empty, replacement.Text);

            removeIllegal.IsChecked = false;
            customChars.Text = "@#";
            replacement.Text = "_";
            replaceWith.IsChecked = true;
            window.UpdateLayout();
            Dispatcher.UIThread.RunJobs();

            var filter = (CleanerFilter)mainViewModel.AppliedFiltersViewModel.ToChain().Steps[0].Filter;
            Assert.False(filter.Options.RemoveIllegalChars);
            Assert.Equal("@#", filter.Options.CustomCharsToRemove);
            Assert.Equal("_", filter.Options.Replacement);

            window.Close();
        }
    }
}
