using Avalonia.Headless.XUnit;
using Avalonia.Threading;
using Mfr.App.Ui.ViewModels.RenameList;

namespace Mfr.Tests.Ui.RenameList
{
    /// <summary>
    /// Headless tests for Rename List fixed-width font display mode.
    /// </summary>
    public sealed class RenameListViewDisplayTests : IDisposable
    {
        private readonly RenameListUiTestContext _context = new();

        /// <inheritdoc />
        public void Dispose()
        {
            _context.Dispose();
        }

        /// <summary>
        /// Verifies toggling <see cref="RenameListViewModel.ToggleUseFixedWidthFontCommand"/> applies the grid style class.
        /// </summary>
        [AvaloniaFact]
        public async Task ToggleUseFixedWidthFont_toggles_grid_style_class()
        {
            var (renameListViewModel, window, grid) = await _context.ShowWithRowsAsync(rowCount: 1);

            Assert.DoesNotContain("fixed-width-font", grid.Classes);

            renameListViewModel.ToggleUseFixedWidthFontCommand.Execute(null);
            window.UpdateLayout();
            Dispatcher.UIThread.RunJobs();

            Assert.Contains("fixed-width-font", grid.Classes);

            renameListViewModel.ToggleUseFixedWidthFontCommand.Execute(null);
            window.UpdateLayout();
            Dispatcher.UIThread.RunJobs();

            Assert.DoesNotContain("fixed-width-font", grid.Classes);

            window.Close();
        }
    }
}
