using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Headless.XUnit;
using Avalonia.Input;
using Mfr.App.Ui.Services.Session;
using Mfr.App.Ui.ViewModels;
using Mfr.App.Ui.Views;

namespace Mfr.Tests.Ui
{
    /// <summary>
    /// Headless tests for dragging the main window pane splitters.
    /// </summary>
    public sealed class MainWindowSplitterTests
    {
        /// <summary>
        /// Verifies dragging the File List splitter changes the left column width.
        /// </summary>
        [AvaloniaFact]
        public void FileListSplitter_Resizes_Columns()
        {
            var window = _ShowMainWindow();
            var grid = window.FindControl<Grid>("TopPanesGrid")!;
            var splitter = window.FindControl<GridSplitter>("FileListSplitter")!;
            var before = grid.ColumnDefinitions[0].ActualWidth;

            _Drag(splitter, deltaX: 80, deltaY: 0);
            window.UpdateLayout();

            Assert.True(
                grid.ColumnDefinitions[0].ActualWidth > before + 40,
                $"Expected left column to grow from {before}, got {grid.ColumnDefinitions[0].ActualWidth}."
            );
        }

        /// <summary>
        /// Verifies dragging the Available/Applied splitter changes those column widths.
        /// </summary>
        [AvaloniaFact]
        public void AvailableAppliedSplitter_Resizes_Columns()
        {
            var window = _ShowMainWindow();
            var grid = window.FindControl<Grid>("FilterListsGrid")!;
            var splitter = window.FindControl<GridSplitter>("AvailableAppliedSplitter")!;
            var before = grid.ColumnDefinitions[0].ActualWidth;

            _Drag(splitter, deltaX: 60, deltaY: 0);
            window.UpdateLayout();

            Assert.True(
                grid.ColumnDefinitions[0].ActualWidth > before + 20,
                $"Expected available-filters column to grow from {before}, got {grid.ColumnDefinitions[0].ActualWidth}."
            );
        }

        /// <summary>
        /// Verifies dragging the filter-editor splitter changes the filter-list row.
        /// </summary>
        [AvaloniaFact]
        public void FilterEditorSplitter_Resizes_Rows()
        {
            var window = _ShowMainWindow();
            var grid = window.FindControl<Grid>("FilterPanesGrid")!;
            var splitter = window.FindControl<GridSplitter>("FilterEditorSplitter")!;
            var before = grid.RowDefinitions[0].ActualHeight;

            _Drag(splitter, deltaX: 0, deltaY: 50);
            window.UpdateLayout();

            Assert.True(
                grid.RowDefinitions[0].ActualHeight > before + 20,
                $"Expected filter-list row to grow from {before}, got {grid.RowDefinitions[0].ActualHeight}."
            );
        }

        /// <summary>
        /// Verifies dragging the Rename List splitter changes the top-panes row.
        /// </summary>
        [AvaloniaFact]
        public void RenameListSplitter_Resizes_Rows()
        {
            var window = _ShowMainWindow();
            var grid = window.FindControl<Grid>("MainPanesGrid")!;
            var splitter = window.FindControl<GridSplitter>("RenameListSplitter")!;
            var before = grid.RowDefinitions[0].ActualHeight;

            _Drag(splitter, deltaX: 0, deltaY: 60);
            window.UpdateLayout();

            Assert.True(
                grid.RowDefinitions[0].ActualHeight > before + 20,
                $"Expected top-panes row to grow from {before}, got {grid.RowDefinitions[0].ActualHeight}."
            );
        }

        /// <summary>
        /// Verifies capture/restore round-trips File List column share after a drag.
        /// </summary>
        [AvaloniaFact]
        public void SplitterSession_Capture_And_Restore_FileList_Ratio()
        {
            var window = _ShowMainWindow();
            var splitter = window.FindControl<GridSplitter>("FileListSplitter")!;

            _Drag(splitter, deltaX: 80, deltaY: 0);
            window.UpdateLayout();

            var captured = SplitterSession.Capture(window);
            Assert.NotNull(captured.FileList);

            var other = _ShowMainWindow();
            SplitterSession.TryRestore(other, new SessionSplitterState { FileList = captured.FileList });
            other.UpdateLayout();

            var restored = other.TopPanesGrid;
            var expectedRatio = captured.FileList.Value;
            var actualRatio =
                restored.ColumnDefinitions[0].ActualWidth
                / (restored.ColumnDefinitions[0].ActualWidth + restored.ColumnDefinitions[2].ActualWidth);

            Assert.InRange(actualRatio, expectedRatio - 0.03, expectedRatio + 0.03);
        }

        private static MainWindow _ShowMainWindow()
        {
            var window = new MainWindow
            {
                DataContext = new MainWindowViewModel(),
                Width = 1100,
                Height = 720,
            };
            window.Show();
            window.UpdateLayout();
            return window;
        }

        private static void _Drag(GridSplitter splitter, double deltaX, double deltaY)
        {
            splitter.RaiseEvent(new VectorEventArgs { RoutedEvent = Thumb.DragStartedEvent, Vector = default });
            splitter.RaiseEvent(
                new VectorEventArgs { RoutedEvent = Thumb.DragDeltaEvent, Vector = new Vector(deltaX, deltaY) }
            );
            splitter.RaiseEvent(
                new VectorEventArgs { RoutedEvent = Thumb.DragCompletedEvent, Vector = new Vector(deltaX, deltaY) }
            );
        }
    }
}
