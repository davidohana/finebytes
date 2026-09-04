using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Presenters;
using Avalonia.Headless.XUnit;
using Avalonia.Threading;
using Avalonia.VisualTree;
using Mfr.App.Ui.ViewModels;
using Mfr.App.Ui.ViewModels.FilterEditors.Case;
using Mfr.App.Ui.ViewModels.FilterEditors.Misc;
using Mfr.App.Ui.ViewModels.FilterEditors.Space;
using Mfr.App.Ui.ViewModels.FilterEditors.Trimming;
using Mfr.App.Ui.Views.AppliedFilters;
using Mfr.App.Ui.Views.Controls;
using Mfr.App.Ui.Views.FilterEditors;
using Mfr.App.Ui.Views.FilterEditors.Case;
using Mfr.App.Ui.Views.FilterEditors.Misc;
using Mfr.App.Ui.Views.FilterEditors.Space;
using Mfr.App.Ui.Views.FilterEditors.Trimming;
using Mfr.Filters;
using Mfr.Filters.Case;
using Mfr.Filters.Misc;
using Mfr.Filters.Space;
using Mfr.Filters.Trimming;
using Mfr.Tests.Ui.AppliedFilters;

namespace Mfr.Tests.Ui.FilterEditors
{
    /// <summary>
    /// Headless tests for the Filter Configuration pane.
    /// </summary>
    public sealed class FilterEditorViewTests
    {
        /// <summary>
        /// Verifies an empty Applied list leaves the configuration title hidden.
        /// </summary>
        [AvaloniaFact]
        public void Empty_applied_list_hides_configuration_title()
        {
            var (window, mainViewModel, editorView) = _ShowFilterEditorPanes();

            Assert.False(mainViewModel.FilterEditorViewModel.HasSelectedStep);
            Assert.Equal(string.Empty, _TitleText(editorView));

            window.Close();
        }

        /// <summary>
        /// Verifies selecting an Applied row updates the configuration title.
        /// </summary>
        [AvaloniaFact]
        public void Selecting_applied_row_updates_configuration_title()
        {
            var (window, mainViewModel, editorView) = _ShowFilterEditorPanes();
            var appliedViewModel = mainViewModel.AppliedFiltersViewModel;
            appliedViewModel.AppendCommand.Execute(AppliedFiltersTestUi.Entry("ShrinkSpaces"));
            appliedViewModel.AppendCommand.Execute(AppliedFiltersTestUi.Entry("LettersCase"));
            window.UpdateLayout();
            Dispatcher.UIThread.RunJobs();

            Assert.Equal("Applied Filter: Letters Case", mainViewModel.FilterEditorViewModel.TitleText);
            Assert.Equal("Applied Filter: Letters Case", _TitleText(editorView));

            var list = _AppliedList(window);
            list.Focus();
            Dispatcher.UIThread.RunJobs();
            AppliedFiltersTestUi.ClickRow(window, list, rowIndex: 0);
            window.UpdateLayout();
            Dispatcher.UIThread.RunJobs();

            Assert.Equal(appliedViewModel.Steps[0], appliedViewModel.SelectedSteps[0]);
            Assert.Equal("Applied Filter: Shrink Spaces", mainViewModel.FilterEditorViewModel.TitleText);
            Assert.Equal("Applied Filter: Shrink Spaces", _TitleText(editorView));

            window.Close();
        }

        /// <summary>
        /// Verifies non-string filters show the title only.
        /// </summary>
        [AvaloniaFact]
        public void Non_string_filter_shows_title_only()
        {
            var (window, mainViewModel, editorView) = _ShowFilterEditorPanes();
            mainViewModel.AppliedFiltersViewModel.AppendCommand.Execute(AppliedFiltersTestUi.Entry("TagRemover"));
            window.UpdateLayout();
            Dispatcher.UIThread.RunJobs();

            Assert.Equal("Applied Filter: Audio Tag Remover", mainViewModel.FilterEditorViewModel.TitleText);
            Assert.Equal("Applied Filter: Audio Tag Remover", _TitleText(editorView));

            window.Close();
        }

        /// <summary>
        /// Verifies optionless filters do not load an options editor template.
        /// </summary>
        [AvaloniaFact]
        public void Optionless_filter_has_no_options_editor()
        {
            var (window, mainViewModel, editorView) = _ShowFilterEditorPanes();
            mainViewModel.AppliedFiltersViewModel.AppendCommand.Execute(AppliedFiltersTestUi.Entry("ShrinkSpaces"));
            window.UpdateLayout();
            Dispatcher.UIThread.RunJobs();

            Assert.Null(mainViewModel.FilterEditorViewModel.OptionsEditor);
            Assert.Null(_OptionsEditorSlot(editorView).Content);

            window.Close();
        }

        /// <summary>
        /// Verifies Space Character checkbox edits persist on the applied step.
        /// </summary>
        [AvaloniaFact]
        public void Space_character_checkbox_updates_chain_options()
        {
            var (window, mainViewModel, editorView) = _ShowFilterEditorPanes();
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
        /// Verifies Letters Case mode radio edits persist on the applied step.
        /// </summary>
        [AvaloniaFact]
        public void Letters_case_mode_radio_updates_chain_options()
        {
            var (window, mainViewModel, editorView) = _ShowFilterEditorPanes();
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
        /// Verifies Space Character definition radios persist on the applied step.
        /// </summary>
        [AvaloniaFact]
        public void Space_character_definition_radio_updates_chain_options()
        {
            var (window, mainViewModel, editorView) = _ShowFilterEditorPanes();
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

        /// <summary>
        /// Verifies Letters Case skip-words edits persist on the applied step.
        /// </summary>
        [AvaloniaFact]
        public void Letters_case_skip_words_box_updates_chain_options()
        {
            var (window, mainViewModel, editorView) = _ShowFilterEditorPanes();
            mainViewModel.AppliedFiltersViewModel.AppendCommand.Execute(AppliedFiltersTestUi.Entry("LettersCase"));
            window.UpdateLayout();
            Dispatcher.UIThread.RunJobs();

            var editor = editorView.GetVisualDescendants().OfType<LettersCaseFilterEditorView>().Single();
            var skipWords = editor.FindControl<TextBox>("CapitalizeSkipWordsBox");
            Assert.NotNull(skipWords);
            Assert.True(skipWords.IsVisible);
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
            var (window, mainViewModel, editorView) = _ShowFilterEditorPanes();
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
            var (window, mainViewModel, editorView) = _ShowFilterEditorPanes();
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
        /// Verifies the fieldset header is left-aligned on the top border instead of covering it.
        /// </summary>
        [AvaloniaFact]
        public void Fieldset_header_does_not_cover_full_top_border()
        {
            var (window, mainViewModel, editorView) = _ShowFilterEditorPanes();
            mainViewModel.AppliedFiltersViewModel.AppendCommand.Execute(AppliedFiltersTestUi.Entry("SpaceCharacter"));
            window.UpdateLayout();
            Dispatcher.UIThread.RunJobs();

            var editor = editorView.GetVisualDescendants().OfType<SpaceCharacterFilterEditorView>().Single();
            var group = editor.GetVisualDescendants().OfType<FieldsetGroup>().First();
            var headerPresenter = group
                .GetVisualDescendants()
                .OfType<ContentPresenter>()
                .Single(item => item.Name == "PART_HeaderPresenter");
            var border = group.GetVisualDescendants().OfType<Border>().Single(item => item.Name == "PART_Border");
            Assert.Equal(new Thickness(1, 0, 1, 1), border.BorderThickness);
            Assert.True(headerPresenter.Bounds.Width > 0);
            Assert.True(headerPresenter.Bounds.Width < group.Bounds.Width / 2);

            window.Close();
        }

        /// <summary>
        /// Verifies Count filter numeric edits persist on the applied step for all four count filter types.
        /// </summary>
        [AvaloniaTheory]
        [InlineData("TrimLeft")]
        [InlineData("TrimRight")]
        [InlineData("ExtractLeft")]
        [InlineData("ExtractRight")]
        public void Count_filter_numeric_box_updates_chain_options(string filterType)
        {
            var (window, mainViewModel, editorView) = _ShowFilterEditorPanes();
            mainViewModel.AppliedFiltersViewModel.AppendCommand.Execute(AppliedFiltersTestUi.Entry(filterType));
            window.UpdateLayout();
            Dispatcher.UIThread.RunJobs();

            Assert.IsType<CountFilterEditorViewModel>(mainViewModel.FilterEditorViewModel.OptionsEditor);

            var editor = editorView.GetVisualDescendants().OfType<CountFilterEditorView>().Single();
            var spinner = editor.FindControl<CompactNumericUpDown>("CountSpinner");
            Assert.NotNull(spinner);
            Assert.Equal(1, spinner.Value);

            spinner.Value = 5;
            window.UpdateLayout();
            Dispatcher.UIThread.RunJobs();
            Assert.Equal(5, _CountOf(mainViewModel.AppliedFiltersViewModel.ToChain().Steps[0].Filter));

            spinner.Value = 0;
            window.UpdateLayout();
            Dispatcher.UIThread.RunJobs();
            Assert.Equal(0, _CountOf(mainViewModel.AppliedFiltersViewModel.ToChain().Steps[0].Filter));

            window.Close();
        }

        /// <summary>
        /// Verifies Shrink Duplicate Characters edits persist on the applied step.
        /// </summary>
        [AvaloniaFact]
        public void Shrink_duplicate_character_box_updates_chain_options()
        {
            var (window, mainViewModel, editorView) = _ShowFilterEditorPanes();
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

        /// <summary>
        /// Verifies Trim Between position/anchor edits persist on the applied step.
        /// </summary>
        [AvaloniaFact]
        public void Trim_between_controls_update_chain_options()
        {
            var (window, mainViewModel, editorView) = _ShowFilterEditorPanes();
            mainViewModel.AppliedFiltersViewModel.AppendCommand.Execute(AppliedFiltersTestUi.Entry("TrimBetween"));
            window.UpdateLayout();
            Dispatcher.UIThread.RunJobs();

            Assert.IsType<TrimBetweenFilterEditorViewModel>(mainViewModel.FilterEditorViewModel.OptionsEditor);

            var editor = editorView.GetVisualDescendants().OfType<TrimBetweenFilterEditorView>().Single();
            var startSpinner = editor.FindControl<CompactNumericUpDown>("StartValueSpinner");
            var endSpinner = editor.FindControl<CompactNumericUpDown>("EndValueSpinner");
            var startAnchor = editor.FindControl<ComboBox>("StartAnchorCombo");
            var endAnchor = editor.FindControl<ComboBox>("EndAnchorCombo");
            Assert.NotNull(startSpinner);
            Assert.NotNull(endSpinner);
            Assert.NotNull(startAnchor);
            Assert.NotNull(endAnchor);
            Assert.Equal(2, startSpinner.Value);
            Assert.Equal(4, endSpinner.Value);
            Assert.Equal(Side.Left, startAnchor.SelectedItem);
            Assert.Equal(Side.Left, endAnchor.SelectedItem);

            startSpinner.Value = 13;
            endSpinner.Value = 5;
            endAnchor.SelectedItem = Side.Right;
            window.UpdateLayout();
            Dispatcher.UIThread.RunJobs();

            var filter = (TrimBetweenFilter)mainViewModel.AppliedFiltersViewModel.ToChain().Steps[0].Filter;
            Assert.Equal(new Position(13, Side.Left), filter.Options.Start);
            Assert.Equal(new Position(5, Side.Right), filter.Options.End);

            window.Close();
        }

        /// <summary>
        /// Verifies Fix Leading 0's option edits persist on the applied step.
        /// </summary>
        [AvaloniaFact]
        public void Fix_leading_zeros_controls_update_chain_options()
        {
            var (window, mainViewModel, editorView) = _ShowFilterEditorPanes();
            mainViewModel.AppliedFiltersViewModel.AppendCommand.Execute(AppliedFiltersTestUi.Entry("FixLeadingZeros"));
            window.UpdateLayout();
            Dispatcher.UIThread.RunJobs();

            Assert.IsType<FixLeadingZerosFilterEditorViewModel>(mainViewModel.FilterEditorViewModel.OptionsEditor);

            var editor = editorView.GetVisualDescendants().OfType<FixLeadingZerosFilterEditorView>().Single();
            var widthSpinner = editor.FindControl<CompactNumericUpDown>("WidthSpinner");
            var maxCountSpinner = editor.FindControl<CompactNumericUpDown>("MaxCountSpinner");
            var removeExtraZeros = editor.FindControl<CompactCheckBox>("RemoveExtraZerosCheckBox");
            var wholeWordOnly = editor.FindControl<CompactCheckBox>("WholeWordOnlyCheckBox");
            Assert.NotNull(widthSpinner);
            Assert.NotNull(maxCountSpinner);
            Assert.NotNull(removeExtraZeros);
            Assert.NotNull(wholeWordOnly);
            Assert.Equal(2, widthSpinner.Value);
            Assert.Equal(1, maxCountSpinner.Value);
            Assert.False(removeExtraZeros.IsChecked);
            Assert.True(wholeWordOnly.IsChecked);

            widthSpinner.Value = 4;
            maxCountSpinner.Value = 0;
            removeExtraZeros.IsChecked = true;
            wholeWordOnly.IsChecked = false;
            window.UpdateLayout();
            Dispatcher.UIThread.RunJobs();

            var filter = (FixLeadingZerosFilter)mainViewModel.AppliedFiltersViewModel.ToChain().Steps[0].Filter;
            Assert.Equal(4, filter.Options.Width);
            Assert.Equal(0, filter.Options.MaxCount);
            Assert.True(filter.Options.RemoveExtraZeros);
            Assert.False(filter.Options.WholeWordOnly);

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

                var (window, mainViewModel, editorView) = _ShowFilterEditorPanes();
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

        private static (
            Window Window,
            MainWindowViewModel MainViewModel,
            FilterEditorView EditorView
        ) _ShowFilterEditorPanes()
        {
            var mainViewModel = new MainWindowViewModel();
            var appliedView = new AppliedFiltersView
            {
                DataContext = mainViewModel.AppliedFiltersViewModel,
                AddFromPaletteCommand = mainViewModel.AddSelectedFilterFromPaletteCommand,
            };
            var editorView = new FilterEditorView { DataContext = mainViewModel.FilterEditorViewModel };

            var grid = new Grid { RowDefinitions = new RowDefinitions("*,*"), Children = { appliedView, editorView } };
            Grid.SetRow(editorView, 1);

            var window = new Window
            {
                Width = 320,
                Height = 280,
                Content = grid,
            };
            window.Show();
            window.UpdateLayout();
            Dispatcher.UIThread.RunJobs();

            return (window, mainViewModel, editorView);
        }

        private static ListBox _AppliedList(Window window)
        {
            var appliedView = window.Content is Grid grid
                ? grid.Children.OfType<AppliedFiltersView>().FirstOrDefault()
                : null;
            Assert.NotNull(appliedView);

            var list = appliedView.FindControl<ListBox>("AppliedFiltersList");
            Assert.NotNull(list);
            return list;
        }

        private static string _TitleText(FilterEditorView editorView)
        {
            return _TitleBlock(editorView)?.Text ?? string.Empty;
        }

        private static TextBlock? _TitleBlock(FilterEditorView editorView)
        {
            return editorView.FindControl<TextBlock>("AppliedFilterTitle");
        }

        private static ContentControl _OptionsEditorSlot(FilterEditorView editorView)
        {
            var slot = editorView.FindControl<ContentControl>("OptionsEditorSlot");
            Assert.NotNull(slot);
            return slot;
        }

        private static int _CountOf(BaseFilter filter)
        {
            return Assert.IsAssignableFrom<ICountOptionsFilter>(filter).Options.Count;
        }
    }
}
