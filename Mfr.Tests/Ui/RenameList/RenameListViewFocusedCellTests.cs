using Avalonia;
using Avalonia.Controls;
using Avalonia.Headless.XUnit;
using Avalonia.Media;
using Avalonia.VisualTree;
using Mfr.App.Ui.ViewModels.RenameList;

namespace Mfr.Tests.Ui.RenameList
{
    /// <summary>
    /// Headless tests for Rename List focused-cell (amber) chrome.
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

            RenameListTestHelpers.ClickFieldCell(window, grid, entry, RenameListTestHelpers.FullFileNameKey);
            Assert.Equal(RenameListTestHelpers.FullFileNameKey, renameListViewModel.FocusedFieldKey);
            var fullNameCell = _AssertSingleCurrentCell(grid, entry, expectedBrush);

            RenameListTestHelpers.ClickFieldCell(window, grid, entry, RenameListTestHelpers.ParentFolderKey);
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
    }
}
