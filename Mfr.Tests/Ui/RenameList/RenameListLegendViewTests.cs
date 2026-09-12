using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Headless.XUnit;
using Avalonia.Threading;
using Mfr.App.Ui.Views.RenameList;

namespace Mfr.Tests.Ui.RenameList
{
    /// <summary>
    /// Tests for the Rename List color legend toolbar toggle and panel.
    /// </summary>
    public sealed class RenameListLegendViewTests : IDisposable
    {
        private readonly RenameListUiTestContext _context = new();

        /// <inheritdoc />
        public void Dispose()
        {
            _context.Dispose();
        }

        /// <summary>
        /// Verifies ToggleLegend flips IsLegendVisible (default off).
        /// </summary>
        [Fact]
        public void ToggleLegend_flips_IsLegendVisible()
        {
            var renameListViewModel = _context.CreateRenameListViewModel();
            Assert.False(renameListViewModel.IsLegendVisible);

            renameListViewModel.ToggleLegend();
            Assert.True(renameListViewModel.IsLegendVisible);

            renameListViewModel.ToggleLegendCommand.Execute(null);
            Assert.False(renameListViewModel.IsLegendVisible);
        }

        /// <summary>
        /// Verifies the legend starts hidden and the toolbar toggle shows the panel.
        /// </summary>
        [AvaloniaFact]
        public async Task Toolbar_legend_toggle_shows_and_hides_panel()
        {
            var (renameListViewModel, window, _) = await _context.ShowWithRowsAsync(rowCount: 1);
            var view = Assert.IsType<RenameListView>(window.Content);
            var toggle = view.FindControl<ToggleButton>("LegendToggle");
            var panel = view.FindControl<Border>("ColorLegendPanel");
            Assert.NotNull(toggle);
            Assert.NotNull(panel);
            Assert.False(renameListViewModel.IsLegendVisible);
            Assert.False(toggle.IsChecked);
            Assert.False(panel.IsVisible);
            Assert.NotNull(toggle.Command);
            Assert.True(toggle.Command.CanExecute(null));

            toggle.Command.Execute(null);
            Dispatcher.UIThread.RunJobs();

            Assert.True(renameListViewModel.IsLegendVisible);
            Assert.True(toggle.IsChecked);
            Assert.True(panel.IsVisible);

            toggle.Command.Execute(null);
            Dispatcher.UIThread.RunJobs();

            Assert.False(renameListViewModel.IsLegendVisible);
            Assert.False(toggle.IsChecked);
            Assert.False(panel.IsVisible);
            window.Close();
        }
    }
}
