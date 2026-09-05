using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Presenters;
using Avalonia.Headless.XUnit;
using Avalonia.Threading;
using Avalonia.VisualTree;
using Mfr.App.Ui.Views.AppliedFilters;
using Mfr.App.Ui.Views.Controls;
using Mfr.App.Ui.Views.FilterEditors;
using Mfr.App.Ui.Views.FilterEditors.Space;
using Mfr.App.Ui.Views.FilterEditors.Trimming;
using Mfr.Tests.Ui.AppliedFilters;

namespace Mfr.Tests.Ui.FilterEditors
{
    /// <summary>
    /// Headless tests for the Filter Configuration host pane (selection, optionless, chrome).
    /// </summary>
    public sealed class FilterEditorViewTests
    {
        /// <summary>
        /// Verifies an empty Applied list leaves the configuration title hidden.
        /// </summary>
        [AvaloniaFact]
        public void Empty_applied_list_hides_configuration_title()
        {
            var (window, mainViewModel, editorView) = FilterEditorTestUi.ShowFilterEditorPanes();

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
            var (window, mainViewModel, editorView) = FilterEditorTestUi.ShowFilterEditorPanes();
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
            var (window, mainViewModel, editorView) = FilterEditorTestUi.ShowFilterEditorPanes();
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
            var (window, mainViewModel, editorView) = FilterEditorTestUi.ShowFilterEditorPanes();
            mainViewModel.AppliedFiltersViewModel.AppendCommand.Execute(AppliedFiltersTestUi.Entry("ShrinkSpaces"));
            window.UpdateLayout();
            Dispatcher.UIThread.RunJobs();

            Assert.Null(mainViewModel.FilterEditorViewModel.OptionsEditor);
            Assert.Null(_OptionsEditorSlot(editorView).Content);

            window.Close();
        }

        /// <summary>
        /// Verifies the fieldset header is left-aligned on the top border instead of covering it.
        /// </summary>
        [AvaloniaFact]
        public void Fieldset_header_does_not_cover_full_top_border()
        {
            var (window, mainViewModel, editorView) = FilterEditorTestUi.ShowFilterEditorPanes();
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
        /// Verifies Filter Configuration field chrome comes from app theme styles (not host-local).
        /// </summary>
        [AvaloniaFact]
        public void Filter_editor_field_styles_come_from_app_theme()
        {
            var (window, mainViewModel, editorView) = FilterEditorTestUi.ShowFilterEditorPanes();
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
    }
}
