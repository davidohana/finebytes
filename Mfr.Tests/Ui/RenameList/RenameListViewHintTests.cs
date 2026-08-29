using Avalonia;
using Avalonia.Controls;
using Avalonia.Headless;
using Avalonia.Headless.XUnit;
using Avalonia.Input;
using Avalonia.Threading;
using Avalonia.VisualTree;
using Mfr.App.Ui.ViewModels.RenameList;
using Mfr.App.Ui.Views.RenameList;
using Mfr.Models.RenameList.Fields.Basic;

namespace Mfr.Tests.Ui.RenameList
{
    /// <summary>
    /// Headless tests for Rename List status-bar cell hints.
    /// </summary>
    public sealed class RenameListViewHintTests : IDisposable
    {
        private readonly RenameListUiTestContext _context = new();

        /// <inheritdoc />
        public void Dispose()
        {
            _context.Dispose();
        }

        /// <summary>
        /// Verifies clicking a cell publishes that cell's value to the status-bar hint.
        /// </summary>
        [AvaloniaFact]
        public async Task Click_Sets_Hint_From_Cell()
        {
            var (renameListViewModel, window, grid) = await _context.ShowWithRowsAsync(rowCount: 8);
            var target = renameListViewModel.Entries[3];

            _ClickFullFileNameCell(window, grid, target);

            Assert.Contains(
                target.FullFileName,
                renameListViewModel.CellStatusHintDisplay.ToPlainText(),
                StringComparison.Ordinal
            );

            window.Close();
        }

        /// <summary>
        /// Verifies Del updates the hint to the row that slides into the deleted index.
        /// </summary>
        [AvaloniaFact]
        public async Task Delete_Updates_Hint_To_New_Selection()
        {
            var (renameListViewModel, window, grid) = await _context.ShowWithRowsAsync(rowCount: 30);
            var deleteIndex = 12;
            var deletedName = renameListViewModel.Entries[deleteIndex].FullFileName;
            var expectedName = renameListViewModel.Entries[deleteIndex + 1].FullFileName;
            var fullNameColumn = grid.Columns.First(column =>
                RenameListGridColumns.GetFieldKey(column)
                == RenameListFieldKey.Original(BasicRenameListField.Group, BasicRenameListFields.Key.FullName)
            );

            renameListViewModel.SetSelectedEntries([renameListViewModel.Entries[deleteIndex]]);
            grid.CurrentColumn = fullNameColumn;
            grid.ScrollIntoView(renameListViewModel.Entries[deleteIndex], fullNameColumn);
            window.UpdateLayout();
            Dispatcher.UIThread.RunJobs();

            _ClickFullFileNameCell(window, grid, renameListViewModel.Entries[deleteIndex]);
            Assert.Contains(
                deletedName,
                renameListViewModel.CellStatusHintDisplay.ToPlainText(),
                StringComparison.Ordinal
            );

            grid.Focus();
            window.KeyPress(Key.Delete, RawInputModifiers.None, PhysicalKey.Delete, "\u007f");
            Dispatcher.UIThread.RunJobs();

            if (renameListViewModel.Entries.Count == 30)
            {
                renameListViewModel.RemoveSelectedCommand.Execute(null);
                Dispatcher.UIThread.RunJobs();
            }

            Assert.Equal(29, renameListViewModel.Entries.Count);
            Assert.Equal(expectedName, renameListViewModel.SelectedEntries[0].FullFileName);
            var hint = renameListViewModel.CellStatusHintDisplay.ToPlainText();
            Assert.Contains(expectedName, hint, StringComparison.Ordinal);
            Assert.DoesNotContain(deletedName, hint, StringComparison.Ordinal);

            window.Close();
        }

        /// <summary>
        /// Verifies moving the pointer over another row does not steal the status-bar hint.
        /// </summary>
        [AvaloniaFact]
        public async Task PointerMove_Does_Not_Change_Hint()
        {
            var (renameListViewModel, window, grid) = await _context.ShowWithRowsAsync(rowCount: 8);
            var selected = renameListViewModel.Entries[1];
            var other = renameListViewModel.Entries[4];

            _ClickFullFileNameCell(window, grid, selected);
            Assert.Contains(
                selected.FullFileName,
                renameListViewModel.CellStatusHintDisplay.ToPlainText(),
                StringComparison.Ordinal
            );

            _MoveOverFullFileNameCell(window, grid, other);
            Dispatcher.UIThread.RunJobs();

            var hint = renameListViewModel.CellStatusHintDisplay.ToPlainText();
            Assert.Contains(selected.FullFileName, hint, StringComparison.Ordinal);
            Assert.DoesNotContain(other.FullFileName, hint, StringComparison.Ordinal);

            window.Close();
        }

        private static void _ClickFullFileNameCell(Window window, DataGrid grid, RenameListEntry entry)
        {
            var windowPoint = _FullFileNameCellPoint(window, grid, entry);
            window.MouseMove(windowPoint, RawInputModifiers.None);
            window.MouseDown(windowPoint, MouseButton.Left, RawInputModifiers.None);
            window.MouseUp(windowPoint, MouseButton.Left, RawInputModifiers.None);
            Dispatcher.UIThread.RunJobs();
        }

        private static void _MoveOverFullFileNameCell(Window window, DataGrid grid, RenameListEntry entry)
        {
            var windowPoint = _FullFileNameCellPoint(window, grid, entry);
            window.MouseMove(windowPoint, RawInputModifiers.None);
            Dispatcher.UIThread.RunJobs();
        }

        private static Point _FullFileNameCellPoint(Window window, DataGrid grid, RenameListEntry entry)
        {
            var row = grid.GetVisualDescendants()
                .OfType<DataGridRow>()
                .FirstOrDefault(item => ReferenceEquals(item.DataContext, entry));
            Assert.NotNull(row);

            var fullNameKey = RenameListFieldKey.Original(
                BasicRenameListField.Group,
                BasicRenameListFields.Key.FullName
            );
            var x = 0.0;
            var found = false;
            foreach (var column in grid.Columns.OrderBy(column => column.DisplayIndex))
            {
                var width = column.Width.IsAbsolute ? column.Width.Value : column.ActualWidth;
                if (RenameListGridColumns.GetFieldKey(column) == fullNameKey)
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
