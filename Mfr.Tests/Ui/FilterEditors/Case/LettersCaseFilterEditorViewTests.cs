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
    /// Headless tests for <see cref="LettersCaseFilterEditorView"/>.
    /// </summary>
    public sealed class LettersCaseFilterEditorViewTests
    {
        /// <summary>
        /// Verifies Letters Case mode radio edits persist on the applied step.
        /// </summary>
        [AvaloniaFact]
        public void Letters_case_mode_radio_updates_chain_options()
        {
            var (window, mainViewModel, editorView) = FilterEditorTestUi.ShowFilterEditorPanes();

            mainViewModel.AppliedFiltersViewModel.AppendCommand.Execute(AppliedFiltersTestUi.Entry("LettersCase"));

            window.UpdateLayout();

            Dispatcher.UIThread.RunJobs();

            Assert.IsType<LettersCaseFilterEditorViewModel>(mainViewModel.FilterEditorViewModel.OptionsEditor);

            var editor = editorView.GetVisualDescendants().OfType<LettersCaseFilterEditorView>().Single();

            var radio = editor.FindControl<RadioButton>("UpperCaseRadio");

            Assert.NotNull(radio);

            Assert.False(radio.IsChecked);

            radio.IsChecked = true;

            window.UpdateLayout();

            Dispatcher.UIThread.RunJobs();

            var filter = (LettersCaseFilter)mainViewModel.AppliedFiltersViewModel.ToChain().Steps[0].Filter;

            Assert.Equal(LettersCaseMode.UpperCase, filter.Options.Mode);

            Assert.True(radio.IsChecked);

            window.Close();
        }

        /// <summary>
        /// Verifies Letters Case skip-words edits persist on the applied step.
        /// </summary>
        [AvaloniaFact]
        public void Letters_case_skip_words_box_updates_chain_options()
        {
            var (window, mainViewModel, editorView) = FilterEditorTestUi.ShowFilterEditorPanes();

            mainViewModel.AppliedFiltersViewModel.AppendCommand.Execute(AppliedFiltersTestUi.Entry("LettersCase"));

            window.UpdateLayout();

            Dispatcher.UIThread.RunJobs();

            var editor = editorView.GetVisualDescendants().OfType<LettersCaseFilterEditorView>().Single();

            var skipWords = editor.FindControl<TextBox>("CapitalizeSkipWordsBox");

            Assert.NotNull(skipWords);

            Assert.True(skipWords.IsVisible);

            Assert.Equal(TextWrapping.Wrap, skipWords.TextWrapping);

            skipWords.Text = "a, the";

            window.UpdateLayout();

            Dispatcher.UIThread.RunJobs();

            var filter = (LettersCaseFilter)mainViewModel.AppliedFiltersViewModel.ToChain().Steps[0].Filter;

            Assert.Equal(["a", "the"], filter.Options.CapitalizeSkipWords);

            window.Close();
        }

        /// <summary>
        /// Verifies skip-words and weird-case settings hide when they do not apply to the selected mode.
        /// </summary>
        [AvaloniaFact]
        public void Letters_case_mode_hides_irrelevant_option_groups()
        {
            var (window, mainViewModel, editorView) = FilterEditorTestUi.ShowFilterEditorPanes();

            mainViewModel.AppliedFiltersViewModel.AppendCommand.Execute(AppliedFiltersTestUi.Entry("LettersCase"));

            window.UpdateLayout();

            Dispatcher.UIThread.RunJobs();

            var editor = editorView.GetVisualDescendants().OfType<LettersCaseFilterEditorView>().Single();

            var skipWords = editor.FindControl<FieldsetGroup>("SkipWordsGroup");

            var weirdSettings = editor.FindControl<FieldsetGroup>("WeirdCaseSettingsGroup");

            var upperCase = editor.FindControl<RadioButton>("UpperCaseRadio");

            var weirdCase = editor.FindControl<RadioButton>("WeirdCaseRadio");

            var capitalize = editor.FindControl<RadioButton>("CapitalizeRadio");

            Assert.NotNull(skipWords);

            Assert.NotNull(weirdSettings);

            Assert.NotNull(upperCase);

            Assert.NotNull(weirdCase);

            Assert.NotNull(capitalize);

            Assert.True(skipWords.IsVisible);

            Assert.False(weirdSettings.IsVisible);

            upperCase.IsChecked = true;

            window.UpdateLayout();

            Dispatcher.UIThread.RunJobs();

            Assert.False(skipWords.IsVisible);

            Assert.False(weirdSettings.IsVisible);

            weirdCase.IsChecked = true;

            window.UpdateLayout();

            Dispatcher.UIThread.RunJobs();

            Assert.False(skipWords.IsVisible);

            Assert.True(weirdSettings.IsVisible);

            capitalize.IsChecked = true;

            window.UpdateLayout();

            Dispatcher.UIThread.RunJobs();

            Assert.True(skipWords.IsVisible);

            Assert.False(weirdSettings.IsVisible);

            window.Close();
        }

        /// <summary>
        /// Verifies Letters Case weird-case edits persist on the applied step.
        /// </summary>
        [AvaloniaFact]
        public void Letters_case_weird_settings_update_chain_options()
        {
            var (window, mainViewModel, editorView) = FilterEditorTestUi.ShowFilterEditorPanes();

            mainViewModel.AppliedFiltersViewModel.AppendCommand.Execute(AppliedFiltersTestUi.Entry("LettersCase"));

            window.UpdateLayout();

            Dispatcher.UIThread.RunJobs();

            var editor = editorView.GetVisualDescendants().OfType<LettersCaseFilterEditorView>().Single();

            var weirdCase = editor.FindControl<RadioButton>("WeirdCaseRadio");

            Assert.NotNull(weirdCase);

            weirdCase.IsChecked = true;

            window.UpdateLayout();

            Dispatcher.UIThread.RunJobs();

            var spinner = editor.FindControl<CompactNumericUpDown>("WeirdUppercaseChanceSpinner");

            var fixedPlaces = editor.FindControl<CheckBox>("WeirdFixedPlacesCheckBox");

            Assert.NotNull(spinner);

            Assert.NotNull(fixedPlaces);

            Assert.True(spinner.IsEffectivelyVisible);

            Assert.Equal(50, spinner.Value);

            spinner.Value = 25;

            fixedPlaces.IsChecked = true;

            window.UpdateLayout();

            Dispatcher.UIThread.RunJobs();

            var filter = (LettersCaseFilter)mainViewModel.AppliedFiltersViewModel.ToChain().Steps[0].Filter;

            Assert.Equal(LettersCaseMode.WeirdCase, filter.Options.Mode);

            Assert.Equal(25, filter.Options.WeirdUppercaseChancePercent);

            Assert.True(filter.Options.WeirdFixedPlaces);

            window.Close();
        }

        /// <summary>
        /// Verifies Letters Case radio edits re-run Rename List preview (Phase 10a).
        /// </summary>
        [AvaloniaFact]
        public async Task Letters_case_mode_radio_updates_rename_list_preview()
        {
            var dir = Directory
                .CreateDirectory(
                    Path.Combine(Directory.GetCurrentDirectory(), "mfr_preview_ui_" + Guid.NewGuid().ToString("N"))
                )
                .FullName;

            try
            {
                var path = Path.Combine(dir, "hello.txt");

                File.WriteAllText(path, "x");

                var (window, mainViewModel, editorView) = FilterEditorTestUi.ShowFilterEditorPanes();

                await mainViewModel.RenameListViewModel.AddPathsAsync([path]).ConfigureAwait(true);

                mainViewModel.AppliedFiltersViewModel.AppendCommand.Execute(AppliedFiltersTestUi.Entry("LettersCase"));

                window.UpdateLayout();

                Dispatcher.UIThread.RunJobs();

                var editor = editorView.GetVisualDescendants().OfType<LettersCaseFilterEditorView>().Single();

                var radio = editor.FindControl<RadioButton>("UpperCaseRadio");

                Assert.NotNull(radio);

                radio.IsChecked = true;

                window.UpdateLayout();

                Dispatcher.UIThread.RunJobs();

                await mainViewModel.WaitForPendingPreviewAsync().ConfigureAwait(true);

                Dispatcher.UIThread.RunJobs();

                Assert.Equal("HELLO.txt", mainViewModel.RenameListViewModel.Entries[0].FullFileNamePreview);

                Assert.Equal(1, mainViewModel.ChangeCount);

                window.Close();
            }
            finally
            {
                try
                {
                    Directory.Delete(dir, recursive: true);
                }
                catch (IOException) { }
            }
        }
    }
}
