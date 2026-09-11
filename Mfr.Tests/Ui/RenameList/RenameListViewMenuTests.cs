using Avalonia.Controls;
using Avalonia.Headless.XUnit;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Threading;
using Avalonia.VisualTree;
using Mfr.Models.RenameList.Fields.Basic;

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
            Assert.Contains("Manual Override Field", headers);
            Assert.Contains("Cancel Manual Override", headers);

            var overrideIndex = headers.IndexOf("Manual Override Field");
            var cancelIndex = headers.IndexOf("Cancel Manual Override");
            var locateIndex = headers.IndexOf("Locate in File List");
            var refreshIndex = headers.IndexOf("Refresh");
            Assert.True(overrideIndex < cancelIndex);
            Assert.True(cancelIndex < locateIndex);
            Assert.True(locateIndex < refreshIndex);

            Assert.False(viewModel.CanShowCancelManualOverride);

            var nameKey = RenameListFieldKey.Preview(BasicRenameListField.Group, BasicRenameListFields.Key.Name);
            viewModel.SetSelectedEntries([viewModel.Entries[0]]);
            Dispatcher.UIThread.RunJobs();
            viewModel.Entries[0].EngineItem.SetOverride(nameKey, "x");
            viewModel.SetFocusedFieldKey(nameKey);
            Assert.True(viewModel.CanShowCancelManualOverride);

            window.Close();
        }

        /// <summary>
        /// Verifies a context request on an unselected row selects that row.
        /// </summary>
        [AvaloniaFact]
        public async Task ContextRequest_On_Unselected_Row_Selects_That_Row()
        {
            var (viewModel, window, grid) = await _context.ShowWithRowsAsync(rowCount: 2);
            viewModel.SetSelectedEntries([viewModel.Entries[1]]);
            Dispatcher.UIThread.RunJobs();

            var row = grid.GetVisualDescendants().OfType<DataGridRow>().First(r => r.Index == 0);
            var content = row.GetVisualDescendants().OfType<TextBlock>().First();
            content.RaiseEvent(new ContextRequestedEventArgs { RoutedEvent = Control.ContextRequestedEvent });
            Dispatcher.UIThread.RunJobs();

            Assert.Equal([viewModel.Entries[0]], viewModel.SelectedEntries);
            window.Close();
        }

        /// <summary>
        /// Verifies a context request on a row already in a multi-selection keeps that selection.
        /// </summary>
        [AvaloniaFact]
        public async Task ContextRequest_On_Selected_Row_Keeps_MultiSelection()
        {
            var (viewModel, window, grid) = await _context.ShowWithRowsAsync(rowCount: 2);
            viewModel.SetSelectedEntries([viewModel.Entries[0], viewModel.Entries[1]]);
            Dispatcher.UIThread.RunJobs();

            var row = grid.GetVisualDescendants().OfType<DataGridRow>().First(r => r.Index == 1);
            var content = row.GetVisualDescendants().OfType<TextBlock>().First();
            content.RaiseEvent(new ContextRequestedEventArgs { RoutedEvent = Control.ContextRequestedEvent });
            Dispatcher.UIThread.RunJobs();

            Assert.Equal([viewModel.Entries[0], viewModel.Entries[1]], viewModel.SelectedEntries);
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
