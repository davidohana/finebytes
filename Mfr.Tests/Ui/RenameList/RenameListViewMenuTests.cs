using Avalonia.Controls;
using Avalonia.Headless.XUnit;
using Avalonia.Input;
using Avalonia.Threading;
using Avalonia.VisualTree;

namespace Mfr.Tests.Ui.RenameList
{
    /// <summary>
    /// Headless tests for Rename List row context menu chrome.
    /// </summary>
    public sealed class RenameListViewMenuTests : IDisposable
    {
        private readonly RenameListUiTestContext _context = new();

        /// <inheritdoc />
        public void Dispose()
        {
            _context.Dispose();
        }

        /// <summary>
        /// Verifies the row context menu includes Properties and Show in Explorer.
        /// </summary>
        [AvaloniaFact]
        public async Task Row_Context_Menu_Includes_Properties_And_Show_In_Explorer()
        {
            var (viewModel, window, grid) = await _context.ShowWithRowsAsync(rowCount: 1);
            Dispatcher.UIThread.RunJobs();

            Assert.NotNull(grid.ContextMenu);
            var headers = grid.ContextMenu.Items.OfType<MenuItem>().Select(item => item.Header?.ToString()).ToList();

            Assert.Contains("Locate in File List", headers);
            Assert.Contains("Show in Explorer", headers);
            Assert.Contains("Properties", headers);

            _ = viewModel;
            window.Close();
        }

        /// <summary>
        /// Verifies Alt+Enter on the Rename List grid shows Properties for a single selected row.
        /// </summary>
        [AvaloniaFact]
        public async Task Grid_Alt_Enter_Shows_Properties()
        {
            var shell = new RecordingFileShellOpener();
            var dir = _context.CreateTempDir();
            var path = Path.Combine(dir, "alpha.txt");
            File.WriteAllText(path, "x");
            var viewModel = _context.CreateRenameListViewModel(dir, shell);
            await viewModel.AddPathsAsync([path]);

            var (view, window) = _context.Show(viewModel);
            Dispatcher.UIThread.RunJobs();
            viewModel.SetSelectedEntries([viewModel.Entries[0]]);
            Dispatcher.UIThread.RunJobs();

            var grid = view.GetVisualDescendants().OfType<DataGrid>().Single();
            grid.RaiseEvent(
                new KeyEventArgs
                {
                    RoutedEvent = InputElement.KeyDownEvent,
                    Key = Key.Enter,
                    KeyModifiers = KeyModifiers.Alt,
                }
            );

            Assert.Equal([path], shell.ShownProperties);
            window.Close();
        }
    }
}
