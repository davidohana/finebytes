using Avalonia;
using Avalonia.Controls;
using Avalonia.Headless.XUnit;
using Avalonia.Threading;
using Avalonia.VisualTree;
using Mfr.App.Ui.ViewModels;
using Mfr.App.Ui.ViewModels.AppliedFilters;
using Mfr.App.Ui.ViewModels.FilterEditors;
using Mfr.App.Ui.ViewModels.FilterEditors;
using Mfr.App.Ui.Views;
using Mfr.App.Ui.Views.AppliedFilters;
using Mfr.App.Ui.Views.FilterEditors;
using Mfr.Filters.Case;
using Mfr.Filters.Space;

namespace Mfr.Tests.Ui.AppliedFilters
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
        /// Verifies Filter Options updates the Applied list Apply-To subtitle.
        /// </summary>
        [AvaloniaFact]
        public void Filter_options_updates_applied_list_subtitle()
        {
            var (window, mainViewModel, _) = _ShowFilterEditorPanes();
            mainViewModel.AppliedFiltersViewModel.AppendCommand.Execute(AppliedFiltersTestUi.Entry("ShrinkSpaces"));
            window.UpdateLayout();
            Dispatcher.UIThread.RunJobs();

            var list = _AppliedList(window);
            Assert.Equal("File Prefix", _RowApplyToLabel(list, 0));

            var dialogVm = new FilterOptionsDialogViewModel(mainViewModel.AppliedFiltersViewModel.Steps[0])
            {
                SelectedApplyTo = FilterTargetOption.All[2],
            };
            var dialog = new FilterOptionsDialog(dialogVm);
            dialog.Show();
            window.UpdateLayout();
            Dispatcher.UIThread.RunJobs();

            dialog.Close(true);
            mainViewModel.AppliedFiltersViewModel.ApplyFilterOptions(dialogVm);
            window.UpdateLayout();
            Dispatcher.UIThread.RunJobs();

            Assert.Equal("Full File Name", mainViewModel.AppliedFiltersViewModel.Steps[0].ApplyToLabel);
            Assert.Equal("Full File Name", _RowApplyToLabel(list, 0));

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
            Assert.False(filter.Options.ReplaceUnderscores);

            window.Close();
        }

        /// <summary>
        /// Verifies Letters Case mode combo edits persist on the applied step.
        /// </summary>
        [AvaloniaFact]
        public void Letters_case_mode_combo_updates_chain_options()
        {
            var (window, mainViewModel, editorView) = _ShowFilterEditorPanes();
            mainViewModel.AppliedFiltersViewModel.AppendCommand.Execute(AppliedFiltersTestUi.Entry("LettersCase"));
            window.UpdateLayout();
            Dispatcher.UIThread.RunJobs();

            Assert.IsType<LettersCaseFilterEditorViewModel>(mainViewModel.FilterEditorViewModel.OptionsEditor);

            var editor = editorView.GetVisualDescendants().OfType<LettersCaseFilterEditorView>().Single();
            var combo = editor.FindControl<ComboBox>("ModeCombo");
            Assert.NotNull(combo);
            combo.SelectedItem = LettersCaseModeOption.FromMode(LettersCaseMode.UpperCase);
            window.UpdateLayout();
            Dispatcher.UIThread.RunJobs();

            var filter = (LettersCaseFilter)mainViewModel.AppliedFiltersViewModel.ToChain().Steps[0].Filter;
            Assert.Equal(LettersCaseMode.UpperCase, filter.Options.Mode);

            window.Close();
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

        private static string _RowApplyToLabel(ListBox list, int rowIndex)
        {
            var container = list.ContainerFromIndex(rowIndex) as Visual;
            Assert.NotNull(container);

            var textBlocks = container.GetVisualDescendants().OfType<TextBlock>().ToList();
            Assert.True(textBlocks.Count > 1);
            return textBlocks[1].Text ?? string.Empty;
        }
    }
}
