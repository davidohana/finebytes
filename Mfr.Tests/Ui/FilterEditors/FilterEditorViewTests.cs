using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Presenters;
using Avalonia.Headless.XUnit;
using Avalonia.Media;
using Avalonia.Threading;
using Avalonia.VisualTree;
using Mfr.App.Ui.ViewModels;
using Mfr.App.Ui.ViewModels.FilterEditors.Attributes;
using Mfr.App.Ui.ViewModels.FilterEditors.Case;
using Mfr.App.Ui.ViewModels.FilterEditors.Formatting;
using Mfr.App.Ui.ViewModels.FilterEditors.Misc;
using Mfr.App.Ui.ViewModels.FilterEditors.Replace;
using Mfr.App.Ui.ViewModels.FilterEditors.Space;
using Mfr.App.Ui.ViewModels.FilterEditors.Trimming;
using Mfr.App.Ui.Views.AppliedFilters;
using Mfr.App.Ui.Views.Controls;
using Mfr.App.Ui.Views.FilterEditors;
using Mfr.App.Ui.Views.FilterEditors.Attributes;
using Mfr.App.Ui.Views.FilterEditors.Case;
using Mfr.App.Ui.Views.FilterEditors.Formatting;
using Mfr.App.Ui.Views.FilterEditors.Misc;
using Mfr.App.Ui.Views.FilterEditors.Replace;
using Mfr.App.Ui.Views.FilterEditors.Space;
using Mfr.App.Ui.Views.FilterEditors.Trimming;
using Mfr.Filters;
using Mfr.Filters.Attributes;
using Mfr.Filters.Case;
using Mfr.Filters.Formatting;
using Mfr.Filters.Misc;
using Mfr.Filters.Replace;
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
        /// Verifies Filter Configuration field chrome comes from app theme styles (not host-local).
        /// </summary>
        [AvaloniaFact]
        public void Filter_editor_field_styles_come_from_app_theme()
        {
            var (window, mainViewModel, editorView) = _ShowFilterEditorPanes();
            mainViewModel.AppliedFiltersViewModel.AppendCommand.Execute(
                AppliedFiltersTestUi.Entry("ShrinkDuplicateCharacters")
            );
            window.UpdateLayout();
            Dispatcher.UIThread.RunJobs();

            var editor = editorView.GetVisualDescendants().OfType<ShrinkDuplicateCharactersFilterEditorView>().Single();
            var box = editor.FindControl<TextBox>("CharacterBox");
            Assert.NotNull(box);
            Assert.Equal(22, box.MinHeight);
            Assert.Equal(22, box.Height);

            var titleBar = editorView
                .GetVisualDescendants()
                .OfType<Border>()
                .First(border => border.Classes.Contains("filter-editor-title-bar"));
            Assert.Equal(22, titleBar.MinHeight);

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
        /// Verifies Space After option edits persist on the applied step.
        /// </summary>
        [AvaloniaFact]
        public void Space_after_controls_update_chain_options()
        {
            var (window, mainViewModel, editorView) = _ShowFilterEditorPanes();
            mainViewModel.AppliedFiltersViewModel.AppendCommand.Execute(AppliedFiltersTestUi.Entry("SpaceAfter"));
            window.UpdateLayout();
            Dispatcher.UIThread.RunJobs();

            Assert.IsType<SpaceTriggerFilterEditorViewModel>(mainViewModel.FilterEditorViewModel.OptionsEditor);

            var editor = editorView.GetVisualDescendants().OfType<SpaceTriggerFilterEditorView>().Single();
            var charsBox = editor.FindControl<TextBox>("CharsBox");
            var neighborCheck = editor.FindControl<CompactCheckBox>("NeighborCheckBox");
            Assert.NotNull(charsBox);
            Assert.NotNull(neighborCheck);
            Assert.Equal(",;!", charsBox.Text);
            Assert.True(neighborCheck.IsChecked);

            charsBox.Text = ".,";
            neighborCheck.IsChecked = false;
            window.UpdateLayout();
            Dispatcher.UIThread.RunJobs();

            var filter = (SpaceAfterFilter)mainViewModel.AppliedFiltersViewModel.ToChain().Steps[0].Filter;
            Assert.Equal(".,", filter.Options.AfterChars);
            Assert.False(filter.Options.OnlyWhenNextIsLetterOrDigit);

            window.Close();
        }

        /// <summary>
        /// Verifies Space Around option edits persist on the applied step.
        /// </summary>
        [AvaloniaFact]
        public void Space_around_controls_update_chain_options()
        {
            var (window, mainViewModel, editorView) = _ShowFilterEditorPanes();
            mainViewModel.AppliedFiltersViewModel.AppendCommand.Execute(AppliedFiltersTestUi.Entry("SpaceAround"));
            window.UpdateLayout();
            Dispatcher.UIThread.RunJobs();

            Assert.IsType<SpaceTriggerFilterEditorViewModel>(mainViewModel.FilterEditorViewModel.OptionsEditor);

            var editor = editorView.GetVisualDescendants().OfType<SpaceTriggerFilterEditorView>().Single();
            var charsBox = editor.FindControl<TextBox>("CharsBox");
            var neighborCheck = editor.FindControl<CompactCheckBox>("NeighborCheckBox");
            Assert.NotNull(charsBox);
            Assert.NotNull(neighborCheck);
            Assert.Equal("-", charsBox.Text);
            Assert.True(neighborCheck.IsChecked);

            charsBox.Text = "+=";
            neighborCheck.IsChecked = false;
            window.UpdateLayout();
            Dispatcher.UIThread.RunJobs();

            var filter = (SpaceAroundFilter)mainViewModel.AppliedFiltersViewModel.ToChain().Steps[0].Filter;
            Assert.Equal("+=", filter.Options.AroundChars);
            Assert.False(filter.Options.OnlyWhenNeighboringAreLettersOrDigits);

            window.Close();
        }

        /// <summary>
        /// Verifies character-list option edits persist on the applied step.
        /// </summary>
        [AvaloniaTheory]
        [InlineData("CapitalizeAfter", ",!()[]{};-", "._")]
        [InlineData("SentenceEndCharacters", "-.!", ":;")]
        public void Character_list_box_updates_chain_options(string filterType, string defaultChars, string editedChars)
        {
            var (window, mainViewModel, editorView) = _ShowFilterEditorPanes();
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

        /// <summary>
        /// Verifies Strip Parentheses option edits persist on the applied step.
        /// </summary>
        [AvaloniaFact]
        public void Strip_parentheses_controls_update_chain_options()
        {
            var (window, mainViewModel, editorView) = _ShowFilterEditorPanes();
            mainViewModel.AppliedFiltersViewModel.AppendCommand.Execute(AppliedFiltersTestUi.Entry("StripParentheses"));
            window.UpdateLayout();
            Dispatcher.UIThread.RunJobs();

            Assert.IsType<StripParenthesesFilterEditorViewModel>(mainViewModel.FilterEditorViewModel.OptionsEditor);

            var editor = editorView.GetVisualDescendants().OfType<StripParenthesesFilterEditorView>().Single();
            var squareRadio = editor.FindControl<RadioButton>("SquareRadio");
            var removeContents = editor.FindControl<CompactCheckBox>("RemoveContentsCheckBox");
            Assert.NotNull(squareRadio);
            Assert.NotNull(removeContents);
            Assert.True(removeContents.IsChecked);

            squareRadio.IsChecked = true;
            removeContents.IsChecked = false;
            window.UpdateLayout();
            Dispatcher.UIThread.RunJobs();

            var filter = (StripParenthesesFilter)mainViewModel.AppliedFiltersViewModel.ToChain().Steps[0].Filter;
            Assert.Equal(ParenthesisType.Square, filter.Options.Type);
            Assert.False(filter.Options.RemoveContents);

            window.Close();
        }

        /// <summary>
        /// Verifies Cleaner option edits persist on the applied step.
        /// </summary>
        [AvaloniaFact]
        public void Cleaner_controls_update_chain_options()
        {
            var (window, mainViewModel, editorView) = _ShowFilterEditorPanes();
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

        /// <summary>
        /// Verifies Counter option edits persist on the applied step.
        /// </summary>
        [AvaloniaFact]
        public void Counter_controls_update_chain_options()
        {
            var (window, mainViewModel, editorView) = _ShowFilterEditorPanes();
            mainViewModel.AppliedFiltersViewModel.AppendCommand.Execute(AppliedFiltersTestUi.Entry("Counter"));
            window.UpdateLayout();
            Dispatcher.UIThread.RunJobs();

            Assert.IsType<CounterFilterEditorViewModel>(mainViewModel.FilterEditorViewModel.OptionsEditor);

            var editor = editorView.GetVisualDescendants().OfType<CounterFilterEditorView>().Single();
            var start = editor.FindControl<CompactNumericUpDown>("StartSpinner");
            var increment = editor.FindControl<CompactNumericUpDown>("IncrementSpinner");
            var leadingZerosMode = editor.FindControl<ComboBox>("LeadingZerosModeCombo");
            var customLength = editor.FindControl<CompactNumericUpDown>("CustomLengthSpinner");
            var replaceRadio = editor.FindControl<RadioButton>("ReplaceRadio");
            var separator = editor.FindControl<TextBox>("SeparatorBox");
            var resetPerFolder = editor.FindControl<CompactCheckBox>("ResetPerFolderCheckBox");
            Assert.NotNull(start);
            Assert.NotNull(increment);
            Assert.NotNull(leadingZerosMode);
            Assert.NotNull(customLength);
            Assert.NotNull(replaceRadio);
            Assert.NotNull(separator);
            Assert.NotNull(resetPerFolder);
            Assert.Equal(1, start.Value);
            Assert.Equal(1, increment.Value);
            Assert.Equal(CounterLeadingZerosMode.None, leadingZerosMode.SelectedItem);
            Assert.Equal(2, customLength.Value);
            Assert.Equal(" - ", separator.Text);
            Assert.True(resetPerFolder.IsChecked);

            start.Value = 10;
            increment.Value = 5;
            leadingZerosMode.SelectedItem = CounterLeadingZerosMode.Custom;
            customLength.Value = 3;
            replaceRadio.IsChecked = true;
            separator.Text = "_";
            resetPerFolder.IsChecked = false;
            window.UpdateLayout();
            Dispatcher.UIThread.RunJobs();

            var filter = (CounterFilter)mainViewModel.AppliedFiltersViewModel.ToChain().Steps[0].Filter;
            Assert.Equal(10, filter.Options.Start);
            Assert.Equal(5, filter.Options.Step);
            Assert.Equal(CounterLeadingZerosMode.Custom, filter.Options.LeadingZerosMode);
            Assert.Equal(3, filter.Options.CustomLength);
            Assert.Equal(CounterPosition.Replace, filter.Options.Position);
            Assert.Equal("_", filter.Options.Separator);
            Assert.False(filter.Options.ResetPerFolder);

            window.Close();
        }

        /// <summary>
        /// Verifies Inserter option edits persist on the applied step.
        /// </summary>
        [AvaloniaFact]
        public void Inserter_controls_update_chain_options()
        {
            var (window, mainViewModel, editorView) = _ShowFilterEditorPanes();
            mainViewModel.AppliedFiltersViewModel.AppendCommand.Execute(AppliedFiltersTestUi.Entry("Inserter"));
            window.UpdateLayout();
            Dispatcher.UIThread.RunJobs();

            Assert.IsType<InserterFilterEditorViewModel>(mainViewModel.FilterEditorViewModel.OptionsEditor);

            var editor = editorView.GetVisualDescendants().OfType<InserterFilterEditorView>().Single();
            var insertText = editor.FindControl<TextBox>("InsertTextBox");
            var position = editor.FindControl<CompactNumericUpDown>("PositionSpinner");
            var endRadio = editor.FindControl<RadioButton>("EndRadio");
            var overwrite = editor.FindControl<CompactCheckBox>("OverwriteCheckBox");
            Assert.NotNull(insertText);
            Assert.NotNull(position);
            Assert.NotNull(endRadio);
            Assert.NotNull(overwrite);
            Assert.Equal(string.Empty, insertText.Text);
            Assert.Equal(1, position.Value);
            Assert.False(overwrite.IsChecked);

            insertText.Text = "_-";
            position.Value = 3;
            endRadio.IsChecked = true;
            overwrite.IsChecked = true;
            window.UpdateLayout();
            Dispatcher.UIThread.RunJobs();

            var filter = (InserterFilter)mainViewModel.AppliedFiltersViewModel.ToChain().Steps[0].Filter;
            Assert.Equal("_-", filter.Options.Text);
            Assert.Equal(3, filter.Options.Position);
            Assert.Equal(InserterOrigin.End, filter.Options.StartFrom);
            Assert.True(filter.Options.Overwrite);

            window.Close();
        }

        /// <summary>
        /// Verifies Casing List option edits persist on the applied step.
        /// </summary>
        [AvaloniaFact]
        public void Casing_list_controls_update_chain_options()
        {
            var (window, mainViewModel, editorView) = _ShowFilterEditorPanes();
            mainViewModel.AppliedFiltersViewModel.AppendCommand.Execute(AppliedFiltersTestUi.Entry("CasingList"));
            window.UpdateLayout();
            Dispatcher.UIThread.RunJobs();

            Assert.IsType<CasingListFilterEditorViewModel>(mainViewModel.FilterEditorViewModel.OptionsEditor);

            var editor = editorView.GetVisualDescendants().OfType<CasingListFilterEditorView>().Single();
            var words = editor.FindControl<TextBox>("WordsBox");
            var uppercase = editor.FindControl<CompactCheckBox>("UppercaseSentenceInitialCheckBox");
            Assert.NotNull(words);
            Assert.NotNull(uppercase);
            Assert.Equal(TextWrapping.Wrap, words.TextWrapping);
            Assert.Equal(string.Empty, words.Text);
            Assert.True(uppercase.IsChecked);

            words.Text = "and or RMX";
            uppercase.IsChecked = false;
            window.UpdateLayout();
            Dispatcher.UIThread.RunJobs();

            var filter = (CasingListFilter)mainViewModel.AppliedFiltersViewModel.ToChain().Steps[0].Filter;
            Assert.Equal(["and", "or", "RMX"], filter.Options.Words);
            Assert.False(filter.Options.UppercaseSentenceInitial);

            window.Close();
        }

        /// <summary>
        /// Verifies Replacer option edits persist on the applied step.
        /// </summary>
        [AvaloniaFact]
        public void Replacer_controls_update_chain_options()
        {
            var (window, mainViewModel, editorView) = _ShowFilterEditorPanes();
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

        /// <summary>
        /// Verifies Replace List option edits persist on the applied step.
        /// </summary>
        [AvaloniaFact]
        public void Replace_list_controls_update_chain_options()
        {
            var (window, mainViewModel, editorView) = _ShowFilterEditorPanes();
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

        /// <summary>
        /// Verifies Name List option edits persist on the applied step.
        /// </summary>
        [AvaloniaFact]
        public void Name_list_controls_update_chain_options()
        {
            var (window, mainViewModel, editorView) = _ShowFilterEditorPanes();
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

        /// <summary>
        /// Verifies Token Mover option edits persist on the applied step.
        /// </summary>
        [AvaloniaFact]
        public void Token_mover_controls_update_chain_options()
        {
            var (window, mainViewModel, editorView) = _ShowFilterEditorPanes();
            mainViewModel.AppliedFiltersViewModel.AppendCommand.Execute(AppliedFiltersTestUi.Entry("TokenMover"));
            window.UpdateLayout();
            Dispatcher.UIThread.RunJobs();

            Assert.IsType<TokenMoverFilterEditorViewModel>(mainViewModel.FilterEditorViewModel.OptionsEditor);

            var editor = editorView.GetVisualDescendants().OfType<TokenMoverFilterEditorView>().Single();
            var delimiter = editor.FindControl<TextBox>("DelimiterBox");
            var tokenNumber = editor.FindControl<CompactNumericUpDown>("TokenNumberSpinner");
            var moveBy = editor.FindControl<CompactNumericUpDown>("MoveBySpinner");
            Assert.NotNull(delimiter);
            Assert.NotNull(tokenNumber);
            Assert.NotNull(moveBy);
            Assert.Equal("-", delimiter.Text);
            Assert.Equal(1, tokenNumber.Value);
            Assert.Equal(1, moveBy.Value);

            delimiter.Text = ",";
            tokenNumber.Value = 2;
            moveBy.Value = -1;
            window.UpdateLayout();
            Dispatcher.UIThread.RunJobs();

            var filter = (TokenMoverFilter)mainViewModel.AppliedFiltersViewModel.ToChain().Steps[0].Filter;
            Assert.Equal(",", filter.Options.Delimiter);
            Assert.Equal(2, filter.Options.TokenNumber);
            Assert.Equal(-1, filter.Options.MoveBy);

            window.Close();
        }

        /// <summary>
        /// Verifies Date/Time Setter option edits persist on the applied step.
        /// </summary>
        [AvaloniaFact]
        public void Date_time_setter_controls_update_chain_options()
        {
            var (window, mainViewModel, editorView) = _ShowFilterEditorPanes();
            mainViewModel.AppliedFiltersViewModel.AppendCommand.Execute(AppliedFiltersTestUi.Entry("DateTimeSetter"));
            window.UpdateLayout();
            Dispatcher.UIThread.RunJobs();

            Assert.IsType<DateTimeSetterFilterEditorViewModel>(mainViewModel.FilterEditorViewModel.OptionsEditor);
            var editorVm = (DateTimeSetterFilterEditorViewModel)mainViewModel.FilterEditorViewModel.OptionsEditor!;

            var editor = editorView.GetVisualDescendants().OfType<DateTimeSetterFilterEditorView>().Single();
            var fieldCombo = editor.FindControl<ComboBox>("TimestampFieldCombo");
            var setDateCheck = editor.FindControl<CheckBox>("SetDateCheckBox");
            var setTimeCheck = editor.FindControl<CheckBox>("SetTimeCheckBox");
            var dateBox = editor.FindControl<TextBox>("DateBox");
            var timeBox = editor.FindControl<TextBox>("TimeBox");
            var currentButton = editor.FindControl<Button>("CurrentButton");
            Assert.NotNull(fieldCombo);
            Assert.NotNull(setDateCheck);
            Assert.NotNull(setTimeCheck);
            Assert.NotNull(dateBox);
            Assert.NotNull(timeBox);
            Assert.NotNull(currentButton);
            Assert.True(setDateCheck.IsChecked);
            Assert.True(setTimeCheck.IsChecked);
            Assert.True(dateBox.IsEnabled);
            Assert.True(timeBox.IsEnabled);
            Assert.Equal(
                DateTime.Today.ToString("yyyy-MM-dd", System.Globalization.CultureInfo.InvariantCulture),
                dateBox.Text
            );
            Assert.False(string.IsNullOrWhiteSpace(timeBox.Text));

            fieldCombo.SelectedItem = editorVm.TimestampFields.Single(c => c.Field == TimestampField.Creation);
            dateBox.Text = "2020-12-25";
            timeBox.Text = "09:00:15";
            setTimeCheck.IsChecked = false;
            window.UpdateLayout();
            Dispatcher.UIThread.RunJobs();

            var filter = (DateTimeSetterFilter)mainViewModel.AppliedFiltersViewModel.ToChain().Steps[0].Filter;
            Assert.Equal(TimestampField.Creation, filter.Options.TimestampField);
            Assert.True(filter.Options.SetDate);
            Assert.Equal(new DateOnly(2020, 12, 25), filter.Options.Date);
            Assert.False(filter.Options.SetTime);
            Assert.Equal(new TimeOnly(9, 0, 15), filter.Options.Time);
            Assert.False(timeBox.IsEnabled);

            setTimeCheck.IsChecked = true;
            currentButton.Command!.Execute(null);
            window.UpdateLayout();
            Dispatcher.UIThread.RunJobs();

            filter = (DateTimeSetterFilter)mainViewModel.AppliedFiltersViewModel.ToChain().Steps[0].Filter;
            Assert.Equal(DateOnly.FromDateTime(DateTime.Today), filter.Options.Date);
            Assert.True(filter.Options.SetTime);

            window.Close();
        }

        /// <summary>
        /// Verifies Attributes Setter tri-state checkbox edits persist on the applied step.
        /// </summary>
        [AvaloniaFact]
        public void Attributes_setter_controls_update_chain_options()
        {
            var (window, mainViewModel, editorView) = _ShowFilterEditorPanes();
            mainViewModel.AppliedFiltersViewModel.AppendCommand.Execute(AppliedFiltersTestUi.Entry("AttributesSetter"));
            window.UpdateLayout();
            Dispatcher.UIThread.RunJobs();

            Assert.IsType<AttributesSetterFilterEditorViewModel>(mainViewModel.FilterEditorViewModel.OptionsEditor);

            var editor = editorView.GetVisualDescendants().OfType<AttributesSetterFilterEditorView>().Single();
            var readOnlyBox = editor.FindControl<CheckBox>("ReadOnlyCheckBox");
            var hiddenBox = editor.FindControl<CheckBox>("HiddenCheckBox");
            var archiveBox = editor.FindControl<CheckBox>("ArchiveCheckBox");
            var systemBox = editor.FindControl<CheckBox>("SystemCheckBox");
            Assert.NotNull(readOnlyBox);
            Assert.NotNull(hiddenBox);
            Assert.NotNull(archiveBox);
            Assert.NotNull(systemBox);
            Assert.True(readOnlyBox.IsThreeState);
            Assert.Null(readOnlyBox.IsChecked);
            Assert.Null(hiddenBox.IsChecked);
            Assert.Null(archiveBox.IsChecked);
            Assert.Null(systemBox.IsChecked);

            hiddenBox.IsChecked = true;
            archiveBox.IsChecked = false;
            readOnlyBox.IsChecked = true;
            systemBox.IsChecked = null;
            window.UpdateLayout();
            Dispatcher.UIThread.RunJobs();

            var filter = (AttributesSetterFilter)mainViewModel.AppliedFiltersViewModel.ToChain().Steps[0].Filter;
            Assert.Equal(AttributeTriState.Set, filter.Options.ReadOnly);
            Assert.Equal(AttributeTriState.Set, filter.Options.Hidden);
            Assert.Equal(AttributeTriState.Clear, filter.Options.Archive);
            Assert.Equal(AttributeTriState.Keep, filter.Options.System);

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
