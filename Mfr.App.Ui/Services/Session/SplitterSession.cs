using Avalonia.Controls;
using Mfr.App.Ui.Views;
using Mfr.Models.Config;

namespace Mfr.App.Ui.Services.Session
{
    /// <summary>
    /// Applies and captures main-window pane splitter ratios for <see cref="SessionStore"/>.
    /// </summary>
    internal static class SplitterSession
    {
        /// <summary>
        /// Restores pane star ratios from <paramref name="saved"/> when each value is in (0, 1).
        /// </summary>
        /// <param name="window">Main window whose pane grids are updated.</param>
        /// <param name="saved">Persisted splitter ratios, or null to skip.</param>
        public static void TryRestore(MainWindow window, SessionSplitterState? saved)
        {
            if (saved is null)
                return;

            _SetColumnRatio(window.TopPanesGrid, saved.FileList);
            _SetColumnRatio(window.FilterListsGrid, saved.AvailableApplied);
            _SetRowRatio(window.FilterPanesGrid, saved.FilterLists);
            _SetRowRatio(window.MainPanesGrid, saved.TopPanes);
        }

        /// <summary>
        /// Builds a <see cref="SessionSplitterState"/> from the main window's current pane sizes.
        /// </summary>
        /// <param name="window">Window being closed.</param>
        /// <returns>Session payload ready to persist.</returns>
        public static SessionSplitterState Capture(MainWindow window)
        {
            return new SessionSplitterState
            {
                FileList = _ColumnRatio(window.TopPanesGrid),
                AvailableApplied = _ColumnRatio(window.FilterListsGrid),
                FilterLists = _RowRatio(window.FilterPanesGrid),
                TopPanes = _RowRatio(window.MainPanesGrid),
            };
        }

        private static void _SetColumnRatio(Grid grid, double? ratio)
        {
            if (ratio is not (> 0 and < 1))
                return;

            grid.ColumnDefinitions[0].Width = new GridLength(ratio.Value, GridUnitType.Star);
            grid.ColumnDefinitions[2].Width = new GridLength(1 - ratio.Value, GridUnitType.Star);
        }

        private static void _SetRowRatio(Grid grid, double? ratio)
        {
            if (ratio is not (> 0 and < 1))
                return;

            grid.RowDefinitions[0].Height = new GridLength(ratio.Value, GridUnitType.Star);
            grid.RowDefinitions[2].Height = new GridLength(1 - ratio.Value, GridUnitType.Star);
        }

        private static double _ColumnRatio(Grid grid)
        {
            var first = grid.ColumnDefinitions[0].ActualWidth;
            var second = grid.ColumnDefinitions[2].ActualWidth;
            return first / (first + second);
        }

        private static double _RowRatio(Grid grid)
        {
            var first = grid.RowDefinitions[0].ActualHeight;
            var second = grid.RowDefinitions[2].ActualHeight;
            return first / (first + second);
        }
    }
}
