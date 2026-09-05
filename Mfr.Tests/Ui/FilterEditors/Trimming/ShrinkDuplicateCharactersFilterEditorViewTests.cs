using Avalonia.Controls;
using Avalonia.Headless.XUnit;
using Avalonia.Threading;
using Avalonia.VisualTree;
using Mfr.App.Ui.ViewModels.FilterEditors.Trimming;
using Mfr.App.Ui.Views.FilterEditors.Trimming;
using Mfr.Filters.Trimming;
using Mfr.Tests.Ui.AppliedFilters;

namespace Mfr.Tests.Ui.FilterEditors.Trimming
{
    /// <summary>
    /// Headless tests for <see cref="ShrinkDuplicateCharactersFilterEditorView"/>.
    /// </summary>
    public sealed class ShrinkDuplicateCharactersFilterEditorViewTests
    {
        /// <summary>
        /// Verifies Shrink Duplicate Characters edits persist on the applied step.
        /// </summary>
        [AvaloniaFact]
        public void Shrink_duplicate_character_box_updates_chain_options()
        {
            var (window, mainViewModel, editorView) = FilterEditorTestUi.ShowFilterEditorPanes();
            mainViewModel.AppliedFiltersViewModel.AppendCommand.Execute(
                AppliedFiltersTestUi.Entry("ShrinkDuplicateCharacters")
            );
            window.UpdateLayout();
            Dispatcher.UIThread.RunJobs();

            Assert.IsType<ShrinkDuplicateCharactersFilterEditorViewModel>(
                mainViewModel.FilterEditorViewModel.OptionsEditor
            );

            var editor = editorView.GetVisualDescendants().OfType<ShrinkDuplicateCharactersFilterEditorView>().Single();
            var box = editor.FindControl<TextBox>("CharacterBox");
            Assert.NotNull(box);
            Assert.Equal("-", box.Text);

            box.Text = ">";
            window.UpdateLayout();
            Dispatcher.UIThread.RunJobs();

            var filter = (ShrinkDuplicateCharactersFilter)
                mainViewModel.AppliedFiltersViewModel.ToChain().Steps[0].Filter;
            Assert.Equal('>', filter.Options.Character);

            box.Text = string.Empty;
            window.UpdateLayout();
            Dispatcher.UIThread.RunJobs();
            filter = (ShrinkDuplicateCharactersFilter)mainViewModel.AppliedFiltersViewModel.ToChain().Steps[0].Filter;
            Assert.Equal('\0', filter.Options.Character);

            window.Close();
        }
    }
}
