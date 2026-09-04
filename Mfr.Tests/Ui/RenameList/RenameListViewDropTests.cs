using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Headless.XUnit;
using Avalonia.Input;
using Avalonia.Threading;
using Avalonia.VisualTree;
using Mfr.App.Ui.ViewModels.RenameList;
using Mfr.App.Ui.Views.RenameList;

namespace Mfr.Tests.Ui.RenameList
{
    /// <summary>
    /// Headless tests for Rename List drag-drop intake.
    /// </summary>
    public sealed class RenameListViewDropTests : IDisposable
    {
        private readonly RenameListUiTestContext _context = new();

        /// <inheritdoc />
        public void Dispose()
        {
            _context.Dispose();
        }

        /// <summary>
        /// Verifies DragOver rejects non-file payloads.
        /// </summary>
        [AvaloniaFact]
        public void DragOver_NonFile_Sets_None()
        {
            var dir = _context.CreateTempDir();
            var renameListViewModel = _context.CreateRenameListViewModel(dir);
            var (view, window) = _context.Show(renameListViewModel);

            var dataTransfer = new DataTransfer();
            dataTransfer.Add(DataTransferItem.CreateText("not-a-file"));
            var dragOverArgs = new DragEventArgs(DragDrop.DragOverEvent, dataTransfer, view, default, KeyModifiers.None)
            {
                DragEffects = DragDropEffects.Copy,
            };
            view.RaiseEvent(dragOverArgs);

            Assert.Equal(DragDropEffects.None, dragOverArgs.DragEffects);
            window.Close();
        }

        /// <summary>
        /// Verifies DragOver accepts files when not adding.
        /// </summary>
        [AvaloniaFact]
        public async Task DragOver_Files_Sets_Copy()
        {
            var dir = _context.CreateTempDir();
            var alphaPath = Path.Combine(dir, "alpha.txt");
            File.WriteAllText(alphaPath, "a");
            var renameListViewModel = _context.CreateRenameListViewModel(dir);
            var (view, window) = _context.Show(renameListViewModel);

            var dataTransfer = await RenameListTestHelpers.CreateFileDataTransferAsync(window, [alphaPath]);
            var dragOverArgs = new DragEventArgs(DragDrop.DragOverEvent, dataTransfer, view, default, KeyModifiers.None)
            {
                DragEffects = DragDropEffects.None,
            };
            view.RaiseEvent(dragOverArgs);

            Assert.Equal(DragDropEffects.Copy, dragOverArgs.DragEffects);
            window.Close();
        }

        /// <summary>
        /// Verifies Alt+DragOver clears existing Rename List rows (MFR7 tip).
        /// </summary>
        [AvaloniaFact]
        public async Task DragOver_Alt_Clears_Existing_Entries()
        {
            var dir = _context.CreateTempDir();
            var existingPath = Path.Combine(dir, "existing.txt");
            var dragPath = Path.Combine(dir, "drag.txt");
            File.WriteAllText(existingPath, "e");
            File.WriteAllText(dragPath, "d");

            var renameListViewModel = _context.CreateRenameListViewModel(dir);
            await renameListViewModel.AddPathsAsync([existingPath]);
            Assert.Single(renameListViewModel.Entries);

            var (view, window) = _context.Show(renameListViewModel);
            var dataTransfer = await RenameListTestHelpers.CreateFileDataTransferAsync(window, [dragPath]);
            var dragOverArgs = new DragEventArgs(DragDrop.DragOverEvent, dataTransfer, view, default, KeyModifiers.Alt)
            {
                DragEffects = DragDropEffects.None,
            };
            view.RaiseEvent(dragOverArgs);

            Assert.Empty(renameListViewModel.Entries);
            Assert.Equal(DragDropEffects.Copy, dragOverArgs.DragEffects);
            window.Close();
        }

        /// <summary>
        /// Verifies Alt+DragOver then Drop replaces the list with only the dropped files.
        /// </summary>
        [AvaloniaFact]
        public async Task DragOver_Alt_Then_Drop_Replaces_List()
        {
            var dir = _context.CreateTempDir();
            var existingPath = Path.Combine(dir, "existing.txt");
            var dragPath = Path.Combine(dir, "drag.txt");
            File.WriteAllText(existingPath, "e");
            File.WriteAllText(dragPath, "d");

            var renameListViewModel = _context.CreateRenameListViewModel(dir);
            await renameListViewModel.AddPathsAsync([existingPath]);

            var (view, window) = _context.Show(renameListViewModel);
            var dataTransfer = await RenameListTestHelpers.CreateFileDataTransferAsync(window, [dragPath]);
            view.RaiseEvent(new DragEventArgs(DragDrop.DragOverEvent, dataTransfer, view, default, KeyModifiers.Alt));
            Assert.Empty(renameListViewModel.Entries);

            view.RaiseEvent(new DragEventArgs(DragDrop.DropEvent, dataTransfer, view, default, KeyModifiers.None));
            await _WaitUntil(() => renameListViewModel.Entries.Count == 1);

            Assert.Equal("drag.txt", renameListViewModel.Entries[0].FullFileName);
            window.Close();
        }

        /// <summary>
        /// Verifies DragLeave clears the drop mark when the pointer left the pane.
        /// </summary>
        [AvaloniaFact]
        public async Task DragLeave_Clears_DropMark()
        {
            var dir = _context.CreateTempDir();
            var alphaPath = Path.Combine(dir, "alpha.txt");
            var dragPath = Path.Combine(dir, "drag.txt");
            File.WriteAllText(alphaPath, "a");
            File.WriteAllText(dragPath, "d");

            var renameListViewModel = _context.CreateRenameListViewModel(dir);
            await renameListViewModel.AddPathsAsync([alphaPath]);

            var (view, window) = _context.Show(renameListViewModel);
            var grid = view.FindControl<DataGrid>("RenameGrid");
            Assert.NotNull(grid);
            Dispatcher.UIThread.RunJobs();

            var pointOnView = _PointOverEntry(view, grid, renameListViewModel.Entries[0]);
            var dataTransfer = await RenameListTestHelpers.CreateFileDataTransferAsync(window, [dragPath]);
            view.RaiseEvent(
                new DragEventArgs(DragDrop.DragOverEvent, dataTransfer, view, pointOnView, KeyModifiers.None)
            );
            Assert.Equal(0, renameListViewModel.DropMarkIndex);

            view.RaiseEvent(
                new DragEventArgs(DragDrop.DragLeaveEvent, dataTransfer, view, new Point(-1, -1), KeyModifiers.None)
            );
            Assert.Null(renameListViewModel.DropMarkIndex);

            window.Close();
        }

        /// <summary>
        /// Verifies DragLeave while still inside the pane keeps the drop mark (no nested-child flicker).
        /// </summary>
        [AvaloniaFact]
        public async Task DragLeave_Inside_Pane_Keeps_DropMark()
        {
            var dir = _context.CreateTempDir();
            var alphaPath = Path.Combine(dir, "alpha.txt");
            var dragPath = Path.Combine(dir, "drag.txt");
            File.WriteAllText(alphaPath, "a");
            File.WriteAllText(dragPath, "d");

            var renameListViewModel = _context.CreateRenameListViewModel(dir);
            await renameListViewModel.AddPathsAsync([alphaPath]);

            var (view, window) = _context.Show(renameListViewModel);
            var grid = view.FindControl<DataGrid>("RenameGrid");
            Assert.NotNull(grid);
            Dispatcher.UIThread.RunJobs();

            var pointOnView = _PointOverEntry(view, grid, renameListViewModel.Entries[0]);
            var dataTransfer = await RenameListTestHelpers.CreateFileDataTransferAsync(window, [dragPath]);
            view.RaiseEvent(
                new DragEventArgs(DragDrop.DragOverEvent, dataTransfer, view, pointOnView, KeyModifiers.None)
            );
            Assert.Equal(0, renameListViewModel.DropMarkIndex);

            view.RaiseEvent(
                new DragEventArgs(DragDrop.DragLeaveEvent, dataTransfer, view, pointOnView, KeyModifiers.None)
            );
            Assert.Equal(0, renameListViewModel.DropMarkIndex);

            window.Close();
        }

        /// <summary>
        /// Verifies DragOver of an internal reorder payload sets Move and the drop mark.
        /// </summary>
        [AvaloniaFact]
        public async Task DragOver_Internal_Reorder_Sets_Move_And_DropMark()
        {
            var dir = _context.CreateTempDir();
            var alphaPath = Path.Combine(dir, "alpha.txt");
            var betaPath = Path.Combine(dir, "beta.md");
            File.WriteAllText(alphaPath, "a");
            File.WriteAllText(betaPath, "b");

            var renameListViewModel = _context.CreateRenameListViewModel(dir);
            await renameListViewModel.AddPathsAsync([alphaPath, betaPath]);

            var (view, window) = _context.Show(renameListViewModel);
            var grid = view.FindControl<DataGrid>("RenameGrid");
            Assert.NotNull(grid);
            Dispatcher.UIThread.RunJobs();
            renameListViewModel.SetSelectedEntries([renameListViewModel.Entries[0]]);

            var pointOnView = _PointOverEntry(view, grid, renameListViewModel.Entries[1]);
            var dataTransfer = RenameListTestHelpers.CreateInternalReorderDataTransfer();
            var dragOverArgs = new DragEventArgs(
                DragDrop.DragOverEvent,
                dataTransfer,
                view,
                pointOnView,
                KeyModifiers.None
            )
            {
                DragEffects = DragDropEffects.None,
            };
            view.RaiseEvent(dragOverArgs);

            Assert.Equal(DragDropEffects.Move, dragOverArgs.DragEffects);
            Assert.Equal(1, renameListViewModel.DropMarkIndex);

            window.Close();
        }

        /// <summary>
        /// Verifies internal reorder DragOver cancels Auto-Sort and sets the salmon drop mark.
        /// </summary>
        [AvaloniaFact]
        public async Task DragOver_Internal_Reorder_With_AutoSort_Cancels_AutoSort_And_Sets_DropMark()
        {
            var dir = _context.CreateTempDir();
            var alphaPath = Path.Combine(dir, "alpha.txt");
            var betaPath = Path.Combine(dir, "beta.md");
            File.WriteAllText(alphaPath, "a");
            File.WriteAllText(betaPath, "b");

            var renameListViewModel = _context.CreateRenameListViewModel(dir);
            renameListViewModel.ApplySession(RenameListTestHelpers.SortSession(RenameListTestHelpers.FullFileNameKey));
            await renameListViewModel.AddPathsAsync([alphaPath, betaPath]);
            Assert.True(renameListViewModel.IsAutoSort);

            var (view, window) = _context.Show(renameListViewModel);
            var grid = view.FindControl<DataGrid>("RenameGrid");
            Assert.NotNull(grid);
            Dispatcher.UIThread.RunJobs();
            renameListViewModel.SetSelectedEntries([renameListViewModel.Entries[0]]);

            var pointOnView = _PointOverEntry(view, grid, renameListViewModel.Entries[1]);
            var dataTransfer = RenameListTestHelpers.CreateInternalReorderDataTransfer();
            view.RaiseEvent(
                new DragEventArgs(DragDrop.DragOverEvent, dataTransfer, view, pointOnView, KeyModifiers.None)
            );

            Assert.False(renameListViewModel.IsAutoSort);
            Assert.Equal(1, renameListViewModel.DropMarkIndex);
            var markedRow = grid.GetVisualDescendants().OfType<DataGridRow>().First(row => row.Index == 1);
            Assert.Contains("drop-mark", markedRow.Classes);

            window.Close();
        }

        /// <summary>
        /// Verifies Alt during internal reorder does not clear the list.
        /// </summary>
        [AvaloniaFact]
        public async Task DragOver_Internal_Reorder_Alt_Does_Not_Clear()
        {
            var dir = _context.CreateTempDir();
            var alphaPath = Path.Combine(dir, "alpha.txt");
            var betaPath = Path.Combine(dir, "beta.md");
            File.WriteAllText(alphaPath, "a");
            File.WriteAllText(betaPath, "b");

            var renameListViewModel = _context.CreateRenameListViewModel(dir);
            await renameListViewModel.AddPathsAsync([alphaPath, betaPath]);

            var (view, window) = _context.Show(renameListViewModel);
            renameListViewModel.SetSelectedEntries([renameListViewModel.Entries[0]]);
            var dataTransfer = RenameListTestHelpers.CreateInternalReorderDataTransfer();
            view.RaiseEvent(new DragEventArgs(DragDrop.DragOverEvent, dataTransfer, view, default, KeyModifiers.Alt));

            Assert.Equal(2, renameListViewModel.Entries.Count);
            Assert.Equal(["alpha.txt", "beta.md"], renameListViewModel.Entries.Select(e => e.FullFileName));

            window.Close();
        }

        /// <summary>
        /// Verifies DragOver on the last row lower half sets append mark and shows the salmon line.
        /// </summary>
        [AvaloniaFact]
        public async Task DragOver_Last_Row_Lower_Half_Sets_Append_Mark()
        {
            var dir = _context.CreateTempDir();
            var alphaPath = Path.Combine(dir, "alpha.txt");
            var betaPath = Path.Combine(dir, "beta.md");
            var dragPath = Path.Combine(dir, "drag.txt");
            File.WriteAllText(alphaPath, "a");
            File.WriteAllText(betaPath, "b");
            File.WriteAllText(dragPath, "d");

            var renameListViewModel = _context.CreateRenameListViewModel(dir);
            await renameListViewModel.AddPathsAsync([alphaPath, betaPath]);

            var (view, window) = _context.Show(renameListViewModel);
            var grid = view.FindControl<DataGrid>("RenameGrid");
            Assert.NotNull(grid);
            Dispatcher.UIThread.RunJobs();

            var pointOnView = _PointOverEntry(view, grid, renameListViewModel.Entries[1], rowFractionY: 0.75);
            var dataTransfer = await RenameListTestHelpers.CreateFileDataTransferAsync(window, [dragPath]);
            view.RaiseEvent(
                new DragEventArgs(DragDrop.DragOverEvent, dataTransfer, view, pointOnView, KeyModifiers.None)
            );
            Dispatcher.UIThread.RunJobs();

            Assert.Equal(2, renameListViewModel.DropMarkIndex);
            var lastRow = grid.GetVisualDescendants().OfType<DataGridRow>().First(row => row.Index == 1);
            Assert.DoesNotContain("drop-mark", lastRow.Classes);
            Assert.NotNull(AdornerLayer.GetAdorner(grid));

            window.Close();
        }

        /// <summary>
        /// Verifies DragOver below the last row sets append mark and shows the salmon line.
        /// </summary>
        [AvaloniaFact]
        public async Task DragOver_Below_Last_Row_Sets_Append_Mark()
        {
            var dir = _context.CreateTempDir();
            var alphaPath = Path.Combine(dir, "alpha.txt");
            var betaPath = Path.Combine(dir, "beta.md");
            var dragPath = Path.Combine(dir, "drag.txt");
            File.WriteAllText(alphaPath, "a");
            File.WriteAllText(betaPath, "b");
            File.WriteAllText(dragPath, "d");

            var renameListViewModel = _context.CreateRenameListViewModel(dir);
            await renameListViewModel.AddPathsAsync([alphaPath, betaPath]);

            var (view, window) = _context.Show(renameListViewModel);
            var grid = view.FindControl<DataGrid>("RenameGrid");
            Assert.NotNull(grid);
            Dispatcher.UIThread.RunJobs();

            var pointOnView = _PointBelowLastEntry(view, grid, renameListViewModel.Entries[1]);
            var dataTransfer = await RenameListTestHelpers.CreateFileDataTransferAsync(window, [dragPath]);
            view.RaiseEvent(
                new DragEventArgs(DragDrop.DragOverEvent, dataTransfer, view, pointOnView, KeyModifiers.None)
            );
            Dispatcher.UIThread.RunJobs();

            Assert.Equal(2, renameListViewModel.DropMarkIndex);
            Assert.NotNull(AdornerLayer.GetAdorner(grid));

            window.Close();
        }

        private static Point _PointOverEntry(
            RenameListView view,
            DataGrid grid,
            RenameListEntry entry,
            double rowFractionY = 0.25
        )
        {
            var row = grid.GetVisualDescendants()
                .OfType<DataGridRow>()
                .FirstOrDefault(item => ReferenceEquals(item.DataContext, entry));
            Assert.NotNull(row);

            var localY = Math.Max(4, row.Bounds.Height * rowFractionY);
            var local = new Point(Math.Max(8, row.Bounds.Width / 2), localY);
            var pointOnView = row.TranslatePoint(local, view);
            Assert.True(pointOnView.HasValue);
            return pointOnView.Value;
        }

        private static Point _PointBelowLastEntry(RenameListView view, DataGrid grid, RenameListEntry entry)
        {
            var row = grid.GetVisualDescendants()
                .OfType<DataGridRow>()
                .FirstOrDefault(item => ReferenceEquals(item.DataContext, entry));
            Assert.NotNull(row);

            var local = new Point(Math.Max(8, row.Bounds.Width / 2), row.Bounds.Height + 4);
            var pointOnView = row.TranslatePoint(local, view);
            Assert.True(pointOnView.HasValue);
            return pointOnView.Value;
        }

        private static async Task _WaitUntil(Func<bool> condition)
        {
            for (var i = 0; i < 200; i++)
            {
                if (condition())
                {
                    return;
                }

                await Task.Delay(10).ConfigureAwait(true);
                Dispatcher.UIThread.RunJobs();
            }

            Assert.Fail("Timed out waiting for condition.");
        }
    }
}
