using Avalonia.Controls;
using Avalonia.Headless;
using Avalonia.Headless.XUnit;
using Avalonia.Threading;
using Avalonia.VisualTree;
using Mfr.App.Ui.ViewModels.FilterEditors.Space;
using Mfr.App.Ui.Views.FilterEditors.Space;
using Mfr.Filters.Space;
using Mfr.Tests.Ui.AppliedFilters;

namespace Mfr.Tests.Ui.FilterEditors.Space
{
    /// <summary>
    /// Headless tests for <see cref="SpaceCharacterFilterEditorView"/>.
    /// </summary>
    public sealed class SpaceCharacterFilterEditorViewTests
    {
        /// <summary>
        /// Verifies keyboard input into the Other character box updates the filter.
        /// </summary>
        [AvaloniaFact]
        public void Space_character_other_box_accepts_typed_character()
        {
            var (window, mainViewModel, editorView) = FilterEditorTestUi.ShowFilterEditorPanes();
            mainViewModel.AppliedFiltersViewModel.AppendCommand.Execute(AppliedFiltersTestUi.Entry("SpaceCharacter"));
            window.UpdateLayout();
            Dispatcher.UIThread.RunJobs();

            var editor = editorView.GetVisualDescendants().OfType<SpaceCharacterFilterEditorView>().Single();
            var otherRadio = editor.FindControl<RadioButton>("OtherDefinitionRadio");
            var otherBox = editor.FindControl<TextBox>("OtherCharacterBox");
            Assert.NotNull(otherRadio);
            Assert.NotNull(otherBox);

            otherRadio.IsChecked = true;
            window.UpdateLayout();
            Dispatcher.UIThread.RunJobs();

            Assert.True(otherBox.IsFocused);

            window.KeyTextInput("-");
            window.UpdateLayout();
            Dispatcher.UIThread.RunJobs();

            Assert.Equal("-", otherBox.Text);
            var filter = (SpaceCharacterFilter)mainViewModel.AppliedFiltersViewModel.ToChain().Steps[0].Filter;
            Assert.Equal('-', filter.Options.SpaceCharacter);

            window.KeyTextInput(".");
            window.UpdateLayout();
            Dispatcher.UIThread.RunJobs();

            Assert.Equal(".", otherBox.Text);
            filter = (SpaceCharacterFilter)mainViewModel.AppliedFiltersViewModel.ToChain().Steps[0].Filter;
            Assert.Equal('.', filter.Options.SpaceCharacter);

            window.Close();
        }

        /// <summary>
        /// Verifies Space Character checkbox edits persist on the applied step.
        /// </summary>
        [AvaloniaFact]
        public void Space_character_checkbox_updates_chain_options()
        {
            var (window, mainViewModel, editorView) = FilterEditorTestUi.ShowFilterEditorPanes();

            mainViewModel.AppliedFiltersViewModel.AppendCommand.Execute(AppliedFiltersTestUi.Entry("SpaceCharacter"));

            window.UpdateLayout();

            Dispatcher.UIThread.RunJobs();

            Assert.IsType<SpaceCharacterFilterEditorViewModel>(mainViewModel.FilterEditorViewModel.OptionsEditor);

            var editor = editorView.GetVisualDescendants().OfType<SpaceCharacterFilterEditorView>().Single();

            var checkBox = editor.FindControl<CheckBox>("ReplaceUnderscoresCheckBox");

            Assert.NotNull(checkBox);

            Assert.True(checkBox.IsChecked);

            checkBox.IsChecked = false;

            window.UpdateLayout();

            Dispatcher.UIThread.RunJobs();

            var filter = (SpaceCharacterFilter)mainViewModel.AppliedFiltersViewModel.ToChain().Steps[0].Filter;

            Assert.DoesNotContain("_", filter.Options.Replacements);

            window.Close();
        }

        /// <summary>
        /// Verifies Space Character definition radios persist on the applied step.
        /// </summary>
        [AvaloniaFact]
        public void Space_character_definition_radio_updates_chain_options()
        {
            var (window, mainViewModel, editorView) = FilterEditorTestUi.ShowFilterEditorPanes();

            mainViewModel.AppliedFiltersViewModel.AppendCommand.Execute(AppliedFiltersTestUi.Entry("SpaceCharacter"));

            window.UpdateLayout();

            Dispatcher.UIThread.RunJobs();

            var editor = editorView.GetVisualDescendants().OfType<SpaceCharacterFilterEditorView>().Single();

            var radio = editor.FindControl<RadioButton>("UnderscoreDefinitionRadio");

            Assert.NotNull(radio);

            Assert.False(radio.IsChecked);

            radio.IsChecked = true;

            window.UpdateLayout();

            Dispatcher.UIThread.RunJobs();

            var filter = (SpaceCharacterFilter)mainViewModel.AppliedFiltersViewModel.ToChain().Steps[0].Filter;

            Assert.Equal('_', filter.Options.SpaceCharacter);

            Assert.True(radio.IsChecked);

            window.Close();
        }
    }
}
