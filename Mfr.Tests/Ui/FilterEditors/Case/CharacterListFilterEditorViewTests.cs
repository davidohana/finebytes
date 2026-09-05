using Avalonia.Controls;
using Avalonia.Headless.XUnit;
using Avalonia.Threading;
using Avalonia.VisualTree;
using Mfr.App.Ui.ViewModels.FilterEditors.Case;
using Mfr.App.Ui.Views.FilterEditors.Case;
using Mfr.Filters.Case;
using Mfr.Tests.Ui.AppliedFilters;

namespace Mfr.Tests.Ui.FilterEditors.Case
{
    /// <summary>
    /// Headless tests for <see cref="CharacterListFilterEditorView"/>.
    /// </summary>
    public sealed class CharacterListFilterEditorViewTests
    {
        /// <summary>
        /// Verifies character-list option edits persist on the applied step.
        /// </summary>
        [AvaloniaTheory]
        [InlineData("CapitalizeAfter", ",!()[]{};-", "._")]
        [InlineData("SentenceEndCharacters", "-.!", ":;")]
        public void Character_list_box_updates_chain_options(string filterType, string defaultChars, string editedChars)
        {
            var (window, mainViewModel, editorView) = FilterEditorTestUi.ShowFilterEditorPanes();
            mainViewModel.AppliedFiltersViewModel.AppendCommand.Execute(AppliedFiltersTestUi.Entry(filterType));
            window.UpdateLayout();
            Dispatcher.UIThread.RunJobs();

            Assert.IsType<CharacterListFilterEditorViewModel>(mainViewModel.FilterEditorViewModel.OptionsEditor);

            var editor = editorView.GetVisualDescendants().OfType<CharacterListFilterEditorView>().Single();
            var charsBox = editor.FindControl<TextBox>("CharsBox");
            Assert.NotNull(charsBox);
            Assert.Equal(defaultChars, charsBox.Text);

            charsBox.Text = editedChars;
            window.UpdateLayout();
            Dispatcher.UIThread.RunJobs();

            var filter = mainViewModel.AppliedFiltersViewModel.ToChain().Steps[0].Filter;
            var actualChars = filter switch
            {
                CapitalizeAfterFilter capitalizeAfter => capitalizeAfter.Options.CapitalizeAfterChars,
                SentenceEndCharactersFilter sentenceEnd => sentenceEnd.Options.Characters,
                _ => throw new InvalidOperationException($"Unexpected filter type {filter.GetType().Name}."),
            };
            Assert.Equal(editedChars, actualChars);

            window.Close();
        }
    }
}
