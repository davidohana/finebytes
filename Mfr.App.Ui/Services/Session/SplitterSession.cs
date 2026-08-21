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
        /// Restores pane star ratios from <paramref name="saved"/> when each value is valid.
        /// </summary>
        /// <param name="window">Main window whose named pane grids are updated.</param>
        /// <param name="saved">Persisted splitter ratios, or null to skip.</param>
        public static void TryRestore(Window window, SessionSplitterState? saved)
        {
            if (saved is null)
                return;

            _TryApplyColumnRatio(window, "TopPanesGrid", saved.FileList);
            _TryApplyColumnRatio(window, "FilterListsGrid", saved.AvailableApplied);
            _TryApplyRowRatio(window, "FilterPanesGrid", saved.FilterLists);
            _TryApplyRowRatio(window, "MainPanesGrid", saved.TopPanes);
        }

        /// <summary>
        /// Builds a <see cref="SessionSplitterState"/> from the main window's current pane sizes.
        /// </summary>
        /// <param name="window">Window being closed.</param>
        /// <returns>Session payload ready to persist, or null when grids are not ready.</returns>
        public static SessionSplitterState? Capture(Window window)
        {
            var fileList = _TryReadColumnRatio(window, "TopPanesGrid");
            var availableApplied = _TryReadColumnRatio(window, "FilterListsGrid");
            var filterLists = _TryReadRowRatio(window, "FilterPanesGrid");
            var topPanes = _TryReadRowRatio(window, "MainPanesGrid");

            var hasAny =
                fileList is not null || availableApplied is not null || filterLists is not null || topPanes is not null;
            if (!hasAny)
                return null;

            return new SessionSplitterState
            {
                FileList = fileList,
                AvailableApplied = availableApplied,
                FilterLists = filterLists,
                TopPanes = topPanes,
            };
        }

        private static void _TryApplyColumnRatio(Window window, string gridName, double? ratio)
        {
            if (!_IsValidRatio(ratio))
                return;

            var grid = window.FindControl<Grid>(gridName);
            if (grid is null || grid.ColumnDefinitions.Count < 3)
                return;

            grid.ColumnDefinitions[0].Width = new GridLength(ratio!.Value, GridUnitType.Star);
            grid.ColumnDefinitions[2].Width = new GridLength(1.0 - ratio.Value, GridUnitType.Star);
        }

        private static void _TryApplyRowRatio(Window window, string gridName, double? ratio)
        {
            if (!_IsValidRatio(ratio))
                return;

            var grid = window.FindControl<Grid>(gridName);
            if (grid is null || grid.RowDefinitions.Count < 3)
                return;

            grid.RowDefinitions[0].Height = new GridLength(ratio!.Value, GridUnitType.Star);
            grid.RowDefinitions[2].Height = new GridLength(1.0 - ratio.Value, GridUnitType.Star);
        }

        private static double? _TryReadColumnRatio(Window window, string gridName)
        {
            var grid = window.FindControl<Grid>(gridName);
            if (grid is null || grid.ColumnDefinitions.Count < 3)
                return null;

            return _RatioFromSizes(grid.ColumnDefinitions[0].ActualWidth, grid.ColumnDefinitions[2].ActualWidth);
        }

        private static double? _TryReadRowRatio(Window window, string gridName)
        {
            var grid = window.FindControl<Grid>(gridName);
            if (grid is null || grid.RowDefinitions.Count < 3)
                return null;

            return _RatioFromSizes(grid.RowDefinitions[0].ActualHeight, grid.RowDefinitions[2].ActualHeight);
        }

        private static double? _RatioFromSizes(double first, double second)
        {
            if (!_IsPositiveFinite(first) || !_IsPositiveFinite(second))
                return null;

            var total = first + second;
            if (!_IsPositiveFinite(total))
                return null;

            var ratio = first / total;
            if (!_IsValidRatio(ratio))
                return null;

            return ratio;
        }

        private static bool _IsValidRatio(double? ratio)
        {
            // Keep both panes usable; extreme values leave XAML defaults.
            return ratio is > 0.05 and < 0.95;
        }

        private static bool _IsPositiveFinite(double value)
        {
            return value is > 0 and not double.NaN and not double.PositiveInfinity and not double.NegativeInfinity;
        }
    }
}
