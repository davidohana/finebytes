using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Presenters;
using Avalonia.Headless.XUnit;
using Avalonia.Threading;
using Avalonia.VisualTree;
using Mfr.App.Ui.ViewModels.FilterEditors;
using Mfr.App.Ui.ViewModels.FilterEditors.Audio;
using Mfr.App.Ui.ViewModels.FilterEditors.Case;
using Mfr.App.Ui.Views.Controls;
using Mfr.App.Ui.Views.FilterChain;
using Mfr.App.Ui.Views.FilterEditors;
using Mfr.App.Ui.Views.FilterEditors.Case;
using Mfr.App.Ui.Views.FilterEditors.Space;
using Mfr.App.Ui.Views.FilterEditors.Trimming;
using Mfr.Filters.Case;
using Mfr.Tests.Ui.FilterChain;

namespace Mfr.Tests.Ui.FilterEditors
{
    /// <summary>
    /// Headless tests for the Filter Configuration host pane (selection, optionless, chrome).
    /// </summary>
    [Collection(ConfigStoreCollection.Name)]
    public sealed class FilterEditorViewTests
    {
        /// <summary>
        /// Verifies an empty Filter Chain shows the Filter Configuration empty state.
        /// </summary>
        [AvaloniaFact]
        public void Empty_filter_chain_shows_empty_state()
        {
            var (window, mainViewModel, editorView) = FilterEditorTestUi.ShowFilterEditorPanes();

            Assert.False(mainViewModel.FilterEditorViewModel.HasSelectedStep);
            Assert.Equal(FilterEditorViewModel.EmptyTitleText, _TitleText(editorView));

            var emptyHint = editorView.FindControl<TextBlock>("EmptySelectionHint");
            Assert.NotNull(emptyHint);
            Assert.True(emptyHint.IsVisible);
            Assert.Equal(FilterEditorViewModel.EmptySelectionHint, emptyHint.Text);

            var titleBar = editorView
                .GetVisualDescendants()
                .OfType<Border>()
                .First(border => border.Classes.Contains("filter-editor-title-bar"));
            Assert.DoesNotContain("has-selection", titleBar.Classes);

            window.Close();
        }

        /// <summary>
        /// Verifies selecting a Filter Chain row updates the configuration title.
        /// </summary>
        [AvaloniaFact]
        public void Selecting_filter_chain_row_updates_configuration_title()
        {
            var (window, mainViewModel, editorView) = FilterEditorTestUi.ShowFilterEditorPanes();
            var appliedViewModel = mainViewModel.FilterChainViewModel;
            appliedViewModel.AppendCommand.Execute(FilterChainTestUi.Entry("ShrinkSpaces"));
            appliedViewModel.AppendCommand.Execute(FilterChainTestUi.Entry("LettersCase"));
            window.UpdateLayout();
            Dispatcher.UIThread.RunJobs();

            Assert.Equal("Filter: Letters Case", mainViewModel.FilterEditorViewModel.TitleText);
            Assert.Equal("Filter: Letters Case", _TitleText(editorView));
            Assert.False(editorView.FindControl<TextBlock>("EmptySelectionHint")!.IsVisible);

            var list = _FilterChainList(window);
            list.Focus();
            Dispatcher.UIThread.RunJobs();
            FilterChainTestUi.ClickRow(window, list, rowIndex: 0);
            window.UpdateLayout();
            Dispatcher.UIThread.RunJobs();

            Assert.Equal(appliedViewModel.Steps[0], appliedViewModel.SelectedSteps[0]);
            Assert.Equal("Filter: Shrink Spaces", mainViewModel.FilterEditorViewModel.TitleText);
            Assert.Equal("Filter: Shrink Spaces", _TitleText(editorView));

            window.Close();
        }

        /// <summary>
        /// Verifies Audio Tag Remover loads its options editor in the configuration host.
        /// </summary>
        [AvaloniaFact]
        public void Tag_remover_loads_options_editor()
        {
            var (window, mainViewModel, editorView) = FilterEditorTestUi.ShowFilterEditorPanes();
            mainViewModel.FilterChainViewModel.AppendCommand.Execute(FilterChainTestUi.Entry("TagRemover"));
            window.UpdateLayout();
            Dispatcher.UIThread.RunJobs();

            Assert.Equal("Filter: Audio Tag Remover", mainViewModel.FilterEditorViewModel.TitleText);
            Assert.Equal("Filter: Audio Tag Remover", _TitleText(editorView));
            Assert.IsType<TagRemoverFilterEditorViewModel>(mainViewModel.FilterEditorViewModel.OptionsEditor);
            Assert.NotNull(_OptionsEditorSlot(editorView).Content);

            window.Close();
        }

        /// <summary>
        /// Verifies optionless filters do not load an options editor template.
        /// </summary>
        [AvaloniaFact]
        public void Optionless_filter_has_no_options_editor()
        {
            var (window, mainViewModel, editorView) = FilterEditorTestUi.ShowFilterEditorPanes();
            mainViewModel.FilterChainViewModel.AppendCommand.Execute(FilterChainTestUi.Entry("ShrinkSpaces"));
            window.UpdateLayout();
            Dispatcher.UIThread.RunJobs();

            Assert.Null(mainViewModel.FilterEditorViewModel.OptionsEditor);
            Assert.Null(_OptionsEditorSlot(editorView).Content);

            window.Close();
        }

        /// <summary>
        /// Verifies the title-bar help button is present and wired for a single selection.
        /// </summary>
        [AvaloniaFact]
        public void Help_button_present_and_enabled_for_single_selection()
        {
            var (window, mainViewModel, editorView) = FilterEditorTestUi.ShowFilterEditorPanes();
            mainViewModel.FilterChainViewModel.AppendCommand.Execute(FilterChainTestUi.Entry("SpaceCharacter"));
            window.UpdateLayout();
            Dispatcher.UIThread.RunJobs();

            var helpButton = editorView.FindControl<Button>("ShowFilterHelpButton");
            Assert.NotNull(helpButton);
            Assert.True(helpButton.IsVisible);
            Assert.Equal("?", helpButton.Content);
            Assert.True(helpButton.Command!.CanExecute(null));

            window.Close();
        }

        /// <summary>
        /// Verifies the title-bar reset button restores catalog defaults and refreshes the options editor.
        /// </summary>
        [AvaloniaFact]
        public void Reset_button_restores_defaults_and_refreshes_editor()
        {
            var (window, mainViewModel, editorView) = FilterEditorTestUi.ShowFilterEditorPanes();
            var applied = mainViewModel.FilterChainViewModel;
            applied.AppendCommand.Execute(FilterChainTestUi.Entry("LettersCase"));
            var step = applied.Steps[0];
            step.SetDisplayName("Custom Letters");
            window.UpdateLayout();
            Dispatcher.UIThread.RunJobs();

            var editor = editorView.GetVisualDescendants().OfType<LettersCaseFilterEditorView>().Single();
            var upperCaseRadio = editor.FindControl<RadioButton>("UpperCaseRadio");
            Assert.NotNull(upperCaseRadio);
            upperCaseRadio.IsChecked = true;
            window.UpdateLayout();
            Dispatcher.UIThread.RunJobs();

            Assert.Equal(LettersCaseMode.UpperCase, ((LettersCaseFilter)step.Filter).Options.Mode);

            var resetButton = editorView.FindControl<Button>("ResetFilterDefaultsButton");
            Assert.NotNull(resetButton);
            Assert.True(resetButton.IsVisible);
            Assert.True(resetButton.Command!.CanExecute(null));
            resetButton.Command.Execute(null);
            window.UpdateLayout();
            Dispatcher.UIThread.RunJobs();

            Assert.Equal("Custom Letters", step.DisplayName);
            Assert.Equal(new LettersCaseFilter(), step.Filter);
            Assert.Equal("Filter: Custom Letters", mainViewModel.FilterEditorViewModel.TitleText);
            var refreshed = Assert.IsType<LettersCaseFilterEditorViewModel>(
                mainViewModel.FilterEditorViewModel.OptionsEditor
            );
            Assert.Equal(LettersCaseMode.Capitalize, refreshed.Mode);

            window.Close();
        }

        /// <summary>
        /// Verifies the title-bar save-as-default button persists options for the next palette add.
        /// </summary>
        [AvaloniaFact]
        public void Save_as_default_button_persists_options_for_next_add()
        {
            var configPath = Path.Combine(Path.GetTempPath(), $"mfr-filter-defaults-ui-{Guid.NewGuid():N}.json");
            File.WriteAllText(configPath, """{}""");
            try
            {
                ConfigStore.Load(configPath);
                var store = FilterDefaultsStore.CreateEmpty();
                var (window, mainViewModel, editorView) = FilterEditorTestUi.ShowFilterEditorPanes(
                    filterDefaults: store
                );
                var applied = mainViewModel.FilterChainViewModel;
                applied.AppendCommand.Execute(FilterChainTestUi.Entry("LettersCase"));
                window.UpdateLayout();
                Dispatcher.UIThread.RunJobs();

                var editor = editorView.GetVisualDescendants().OfType<LettersCaseFilterEditorView>().Single();
                var upperCaseRadio = editor.FindControl<RadioButton>("UpperCaseRadio");
                Assert.NotNull(upperCaseRadio);
                upperCaseRadio.IsChecked = true;
                window.UpdateLayout();
                Dispatcher.UIThread.RunJobs();

                var saveButton = editorView.FindControl<Button>("SaveFilterAsDefaultButton");
                Assert.NotNull(saveButton);
                Assert.True(saveButton.IsVisible);
                Assert.Equal("📌", saveButton.Content);
                Assert.True(saveButton.Command!.CanExecute(null));
                saveButton.Command.Execute(null);

                // Silent replace — attached FilterChainView wires ConfirmClearAsync (headless dialog hang).
                applied.ReplaceFromChain(new FilterChainModel { Steps = [] });
                applied.AppendCommand.Execute(FilterChainTestUi.Entry("LettersCase"));
                Assert.Equal(LettersCaseMode.UpperCase, ((LettersCaseFilter)applied.Steps[0].Filter).Options.Mode);

                window.Close();
            }
            finally
            {
                if (File.Exists(configPath))
                {
                    File.Delete(configPath);
                }

                ConfigStoreTestReset.LoadEmpty();
            }
        }

        /// <summary>
        /// Verifies the fieldset header is left-aligned on the top border instead of covering it.
        /// </summary>
        [AvaloniaFact]
        public void Fieldset_header_does_not_cover_full_top_border()
        {
            var (window, mainViewModel, editorView) = FilterEditorTestUi.ShowFilterEditorPanes();
            mainViewModel.FilterChainViewModel.AppendCommand.Execute(FilterChainTestUi.Entry("SpaceCharacter"));
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
        /// Verifies Filter Configuration field chrome comes from app theme styles (not host-local).
        /// </summary>
        [AvaloniaFact]
        public void Filter_editor_field_styles_come_from_app_theme()
        {
            var (window, mainViewModel, editorView) = FilterEditorTestUi.ShowFilterEditorPanes();
            mainViewModel.FilterChainViewModel.AppendCommand.Execute(
                FilterChainTestUi.Entry("ShrinkDuplicateCharacters")
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

            Assert.True(box.Focus());
            window.UpdateLayout();
            Dispatcher.UIThread.RunJobs();

            var app = Assert.IsAssignableFrom<Application>(Application.Current);
            Assert.True(app.TryGetResource("AppChromeBorderBrush", app.ActualThemeVariant, out var chromeBorder));
            var border = Assert.Single(
                box.GetVisualDescendants().OfType<Border>(),
                part => part.Name == "PART_BorderElement"
            );
            Assert.Same(chromeBorder, border.BorderBrush);

            window.Close();
        }

        private static ListBox _FilterChainList(Window window)
        {
            var appliedView = window.Content is Grid grid
                ? grid.Children.OfType<FilterChainView>().FirstOrDefault()
                : null;
            Assert.NotNull(appliedView);

            var list = appliedView.FindControl<ListBox>("FilterChainList");
            Assert.NotNull(list);
            return list;
        }

        private static string _TitleText(FilterEditorView editorView)
        {
            return _TitleBlock(editorView)?.Text ?? string.Empty;
        }

        private static TextBlock? _TitleBlock(FilterEditorView editorView)
        {
            return editorView.FindControl<TextBlock>("FilterEditorTitle");
        }

        private static ContentControl _OptionsEditorSlot(FilterEditorView editorView)
        {
            var slot = editorView.FindControl<ContentControl>("OptionsEditorSlot");
            Assert.NotNull(slot);
            return slot;
        }
    }
}
