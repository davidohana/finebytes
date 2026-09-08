using Avalonia.Controls;
using Avalonia.Threading;
using Mfr.App.Ui.ViewModels;
using Mfr.App.Ui.Views.AppliedFilters;
using Mfr.App.Ui.Views.FilterEditors;
using Mfr.Models.Config;

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
        /// <param name="session">
        /// Session restored onto the main view model, or <see langword="null"/> for first-launch defaults.
        /// </param>
        /// <returns>Host window, main view model, and filter editor view.</returns>
        public static (
            Window Window,
            MainWindowViewModel MainViewModel,
            FilterEditorView EditorView
        ) ShowFilterEditorPanes(SessionState? session = null)
        {
            var mainViewModel = new MainWindowViewModel(session: session);
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
                Width = 960,
                Height = 480,
                Content = grid,
                DataContext = mainViewModel,
            };
            window.Show();
            window.UpdateLayout();
            Dispatcher.UIThread.RunJobs();

            return (window, mainViewModel, editorView);
        }
    }
}
