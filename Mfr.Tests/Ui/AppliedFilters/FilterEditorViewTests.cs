using Avalonia.Controls;
using Avalonia.Headless.XUnit;
using Avalonia.Threading;
using Mfr.App.Ui.ViewModels;
using Mfr.App.Ui.Views;
using Mfr.App.Ui.Views.AppliedFilters;

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
            Assert.False(_TitleBlock(editorView)!.IsVisible);

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
            Assert.True(_TitleBlock(editorView)!.IsVisible);

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
    }
}
