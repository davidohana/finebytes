using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
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
        /// <param name="session">
        /// Session restored onto the main view model, or <see langword="null"/> for first-launch defaults.
        /// </param>
        /// <param name="filterDefaults">
        /// Optional per-type add defaults store (isolated temp store when null).
        /// </param>
        /// <returns>Host window, main view model, and filter editor view.</returns>
        public static (
            Window Window,
            MainWindowViewModel MainViewModel,
            FilterEditorView EditorView
        ) ShowFilterEditorPanes(SessionState? session = null, FilterDefaultsStore? filterDefaults = null)
        {
            var mainViewModel = new MainWindowViewModel(session: session, filterDefaults: filterDefaults);
            var appliedView = new AppliedFiltersView
            {
                DataContext = mainViewModel.AppliedFiltersViewModel,
                AddFromPaletteCommand = mainViewModel.AddSelectedFilterFromPaletteCommand,
            };
            var editorView = new FilterEditorView
            {
                DataContext = mainViewModel.FilterEditorViewModel,
                ResetSelectedToDefaultsCommand = mainViewModel.AppliedFiltersViewModel.ResetSelectedToDefaultsCommand,
                SaveSelectedAsDefaultCommand = mainViewModel.AppliedFiltersViewModel.SaveSelectedAsDefaultCommand,
            };

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

        /// <summary>
        /// Raises a left-button pointer release on <paramref name="control"/> (e.g. Visual Trim Helper selection).
        /// </summary>
        /// <param name="control">Control that handles <see cref="InputElement.PointerReleasedEvent"/>.</param>
        public static void RaisePointerReleased(Control control)
        {
            ArgumentNullException.ThrowIfNull(control);

            var pointer = new Pointer(1, PointerType.Mouse, true);
            var props = new PointerPointProperties(RawInputModifiers.None, PointerUpdateKind.LeftButtonReleased);
            control.RaiseEvent(
                new PointerReleasedEventArgs(
                    control,
                    pointer,
                    control,
                    new Point(1, 1),
                    0,
                    props,
                    KeyModifiers.None,
                    MouseButton.Left
                )
                {
                    RoutedEvent = InputElement.PointerReleasedEvent,
                }
            );
        }
    }
}
