using Avalonia.Controls;
using Avalonia.Threading;
using Mfr.App.Ui.ViewModels;
using Mfr.App.Ui.Views.AppliedFilters;
using Mfr.App.Ui.Views.FilterEditors;

namespace Mfr.Tests.Ui.FilterEditors
{
    /// <summary>
    /// Shared headless host for Filter Configuration editor tests.
    /// </summary>
    internal static class FilterEditorTestUi
    {
        /// <summary>
        /// Shows Applied Filters above Filter Configuration for headless editor tests.
        /// </summary>
        /// <returns>Host window, main view model, and filter editor view.</returns>
        public static (
            Window Window,
            MainWindowViewModel MainViewModel,
            FilterEditorView EditorView
        ) ShowFilterEditorPanes()
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
    }
}
