using Avalonia;
using Avalonia.Controls;
using Avalonia.Headless;
using Avalonia.Headless.XUnit;
using Avalonia.Input;
using Avalonia.Media;
using Avalonia.Threading;
using Avalonia.VisualTree;
using Mfr.App.Ui.ViewModels.RenameList;
using Mfr.App.Ui.Views.RenameList;

namespace Mfr.Tests.Ui.RenameList
{
    /// <summary>
    /// Headless tests for Rename List focused-cell (light blue) chrome.
    /// </summary>
    public sealed class RenameListViewFocusedCellTests : IDisposable
    {
        private readonly RenameListUiTestContext _context = new();

        /// <inheritdoc />
        public void Dispose()
        {
            _context.Dispose();
        }

        /// <summary>
        /// Verifies the current cell uses the focused-cell brush and moves with column clicks.
        /// </summary>
        [AvaloniaFact]
        public async Task Current_Cell_Uses_Focused_Brush_And_Moves_With_Column()
        {
            var (renameListViewModel, window, grid) = await _context.ShowWithRowsAsync(rowCount: 4);
            var entry = renameListViewModel.Entries[1];
            var expectedBrush = _FocusedCellBrush(window);

            _ClickFieldCell(window, grid, entry, RenameListTestHelpers.FullFileNameKey);
            Assert.Equal(RenameListTestHelpers.FullFileNameKey, renameListViewModel.FocusedFieldKey);
            var fullNameCell = _AssertSingleCurrentCell(grid, entry, expectedBrush);

            _ClickFieldCell(window, grid, entry, RenameListTestHelpers.ParentFolderKey);
            Assert.Equal(RenameListTestHelpers.ParentFolderKey, renameListViewModel.FocusedFieldKey);
            var parentFolderCell = _AssertSingleCurrentCell(grid, entry, expectedBrush);
            Assert.False(ReferenceEquals(fullNameCell, parentFolderCell));
            Assert.DoesNotContain(":current", fullNameCell.Classes);

            window.Close();
        }

        private static ISolidColorBrush _FocusedCellBrush(Window window)
        {
            var app = Assert.IsAssignableFrom<Application>(Application.Current);
            Assert.True(app.TryGetResource("RenameListFocusedCellBrush", window.ActualThemeVariant, out var resource));
            return Assert.IsAssignableFrom<ISolidColorBrush>(resource);
        }

        private static DataGridCell _AssertSingleCurrentCell(
            DataGrid grid,
            RenameListEntry entry,
            ISolidColorBrush expectedBrush
        )
        {
            var row = grid.GetVisualDescendants()
                .OfType<DataGridRow>()
                .First(item => ReferenceEquals(item.DataContext, entry));
            var currentCells = row.GetVisualDescendants()
                .OfType<DataGridCell>()
                .Where(cell => cell.Classes.Contains(":current"))
                .ToList();
            var current = Assert.Single(currentCells);
            var actualBrush = Assert.IsAssignableFrom<ISolidColorBrush>(current.Background);
            Assert.Equal(expectedBrush.Color, actualBrush.Color);
            return current;
        }

        private static void _ClickFieldCell(
            Window window,
            DataGrid grid,
            RenameListEntry entry,
            RenameListFieldKey fieldKey
        )
        {
            var windowPoint = _FieldCellPoint(window, grid, entry, fieldKey);
            window.MouseMove(windowPoint, RawInputModifiers.None);
            window.MouseDown(windowPoint, MouseButton.Left, RawInputModifiers.None);
            window.MouseUp(windowPoint, MouseButton.Left, RawInputModifiers.None);
            window.UpdateLayout();
            Dispatcher.UIThread.RunJobs();
        }

        private static Point _FieldCellPoint(
            Window window,
            DataGrid grid,
            RenameListEntry entry,
            RenameListFieldKey fieldKey
        )
        {
            var row = grid.GetVisualDescendants()
                .OfType<DataGridRow>()
                .FirstOrDefault(item => ReferenceEquals(item.DataContext, entry));
            Assert.NotNull(row);

            var x = 0.0;
            var found = false;
            foreach (var column in grid.Columns.OrderBy(column => column.DisplayIndex))
            {
                var width = column.Width.IsAbsolute ? column.Width.Value : column.ActualWidth;
                if (RenameListGridColumns.GetFieldKey(column) == fieldKey)
                {
                    x += width / 2;
                    found = true;
                    break;
                }

                x += width;
            }

            Assert.True(found);
            var windowPoint = row.TranslatePoint(new Point(x, Math.Max(1, row.Bounds.Height / 2)), window);
            Assert.True(windowPoint.HasValue);
            return windowPoint.Value;
        }
    }
}
