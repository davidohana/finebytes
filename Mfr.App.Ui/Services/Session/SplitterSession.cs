using Avalonia.Controls;
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
        /// <param name="panes">Main window pane grids to update.</param>
        /// <param name="saved">Persisted splitter ratios, or null to skip.</param>
        public static void TryRestore(MainWindowPaneGrids panes, SessionStateSplitters? saved)
        {
            ArgumentNullException.ThrowIfNull(panes);

            if (saved is null)
            {
                return;
            }

            _SetColumnRatio(panes.TopPanes, saved.FileList);
            _SetColumnRatio(panes.FilterLists, saved.AvailableApplied);
            _SetRowRatio(panes.FilterPanes, saved.FilterLists);
            _SetRowRatio(panes.MainPanes, saved.TopPanes);
        }

        /// <summary>
        /// Builds a <see cref="SessionStateSplitters"/> from the current pane sizes.
        /// </summary>
        /// <param name="panes">Pane grids whose sizes are captured.</param>
        /// <returns>Session payload ready to persist.</returns>
        public static SessionStateSplitters Capture(MainWindowPaneGrids panes)
        {
            ArgumentNullException.ThrowIfNull(panes);

            return new SessionStateSplitters
            {
                FileList = _ColumnRatio(panes.TopPanes),
                AvailableApplied = _ColumnRatio(panes.FilterLists),
                FilterLists = _RowRatio(panes.FilterPanes),
                TopPanes = _RowRatio(panes.MainPanes),
            };
        }

        private static void _SetColumnRatio(Grid grid, double? ratio)
        {
            if (ratio is not (> 0 and < 1))
            {
                return;
            }

            grid.ColumnDefinitions[0].Width = new GridLength(ratio.Value, GridUnitType.Star);
            grid.ColumnDefinitions[2].Width = new GridLength(1 - ratio.Value, GridUnitType.Star);
        }

        private static void _SetRowRatio(Grid grid, double? ratio)
        {
            if (ratio is not (> 0 and < 1))
            {
                return;
            }

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
