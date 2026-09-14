using Avalonia.Controls;
using Avalonia.Controls.Primitives;
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
    /// Headless tests for A/B Mode grid chrome (toolbar side, projection rebuild, header menus).
    /// </summary>
    public sealed class RenameListAbModeChromeTests : IDisposable
    {
        private readonly RenameListUiTestContext _context = new();

        /// <inheritdoc />
        public void Dispose()
        {
            _context.Dispose();
        }

        /// <summary>
        /// Verifies the Original|Preview toolbar is hidden until A/B Mode is on.
        /// </summary>
        [AvaloniaFact]
        public async Task Ab_side_toolbar_hidden_when_ab_mode_off()
        {
            var (renameListViewModel, window, view) = await _ShowAsync();
            var abSidePanel = view.FindControl<StackPanel>("AbSidePanel");
            Assert.NotNull(abSidePanel);
            Assert.False(renameListViewModel.IsAbModeEnabled);
            Assert.False(abSidePanel.IsVisible);

            window.Close();
        }

        /// <summary>
        /// Verifies clicking Original|Preview rebuilds the grid from ProjectedColumns.
        /// </summary>
        [AvaloniaFact]
        public async Task Ab_side_toolbar_click_rebuilds_projected_grid_columns()
        {
            var (renameListViewModel, window, view) = await _ShowAsync();
            var grid = view.FindControl<DataGrid>("RenameGrid");
            var abSidePanel = view.FindControl<StackPanel>("AbSidePanel");
            var originalRadio = view.FindControl<ToggleButton>("AbSideOriginalRadio");
            var previewRadio = view.FindControl<ToggleButton>("AbSidePreviewRadio");
            Assert.NotNull(grid);
            Assert.NotNull(abSidePanel);
            Assert.NotNull(originalRadio);
            Assert.NotNull(previewRadio);

            var fullNameKey = RenameListFieldKey.Original(
                BasicRenameListField.Group,
                BasicRenameListFields.Key.FullName
            );
            renameListViewModel.SetVisibleColumns([new RenameListVisibleColumn(fullNameKey)]);
            renameListViewModel.IsAbModeEnabled = true;
            renameListViewModel.AbSide = RenameListPrefs.AbSidePreview;
            window.UpdateLayout();
            Dispatcher.UIThread.RunJobs();

            Assert.True(abSidePanel.IsVisible);
            Assert.Equal(3, grid.Columns.Count);
            Assert.Equal(
                [
                    fullNameKey,
                    RenameListFieldKey.Preview(BasicRenameListField.Group, BasicRenameListFields.Key.FullName),
                ],
                RenameListGridColumns.GetDisplayedFieldKeys(grid)
            );

            Assert.NotNull(originalRadio.Command);
            Assert.True(originalRadio.Command.CanExecute(originalRadio.CommandParameter));
            Assert.Equal(RenameListPrefs.AbSideOriginal, originalRadio.CommandParameter);

            // Bound Command path (headless pointer hits are unreliable on these text ToggleButtons).
            originalRadio.Command.Execute(originalRadio.CommandParameter);
            window.UpdateLayout();
            Dispatcher.UIThread.RunJobs();

            Assert.Equal(RenameListPrefs.AbSideOriginal, renameListViewModel.AbSide);
            Assert.True(originalRadio.IsChecked);
            Assert.Equal(2, grid.Columns.Count);
            Assert.Equal([fullNameKey], RenameListGridColumns.GetDisplayedFieldKeys(grid));

            Assert.NotNull(previewRadio.Command);
            previewRadio.Command.Execute(previewRadio.CommandParameter);
            window.UpdateLayout();
            Dispatcher.UIThread.RunJobs();

            Assert.Equal(RenameListPrefs.AbSidePreview, renameListViewModel.AbSide);
            Assert.Equal(3, grid.Columns.Count);

            window.Close();
        }

        /// <summary>
        /// Verifies derived Preview headers omit Hide Field but keep Remove Unchanged and override Cancel.
        /// </summary>
        [AvaloniaFact]
        public async Task Derived_preview_header_omits_hide_keeps_remove_unchanged_and_cancel()
        {
            var (renameListViewModel, window, view) = await _ShowAsync();
            var grid = view.FindControl<DataGrid>("RenameGrid");
            Assert.NotNull(grid);

            var fullNameKey = RenameListFieldKey.Original(
                BasicRenameListField.Group,
                BasicRenameListFields.Key.FullName
            );
            var previewKey = RenameListFieldKey.Preview(BasicRenameListField.Group, BasicRenameListFields.Key.FullName);
            renameListViewModel.SetVisibleColumns([new RenameListVisibleColumn(fullNameKey)]);
            renameListViewModel.IsAbModeEnabled = true;
            renameListViewModel.AbSide = RenameListPrefs.AbSidePreview;
            window.UpdateLayout();
            Dispatcher.UIThread.RunJobs();

            var originalHeader = grid.GetVisualDescendants()
                .OfType<DataGridColumnHeader>()
                .First(header => RenameListGridColumns.TryResolveFieldKey(header) == fullNameKey);
            var previewHeader = grid.GetVisualDescendants()
                .OfType<DataGridColumnHeader>()
                .First(header => RenameListGridColumns.TryResolveFieldKey(header) == previewKey);

            _RaiseHeaderContextMenu(originalHeader);
            Assert.Contains("Hide Field", _MenuHeaders(originalHeader.ContextMenu));
            Assert.DoesNotContain("Remove Unchanged Items", _MenuHeaders(originalHeader.ContextMenu));

            _RaiseHeaderContextMenu(previewHeader);
            Assert.DoesNotContain("Hide Field", _MenuHeaders(previewHeader.ContextMenu));
            Assert.Contains("Remove Unchanged Items", _MenuHeaders(previewHeader.ContextMenu));
            Assert.DoesNotContain("Cancel Manual Override", _MenuHeaders(previewHeader.ContextMenu));

            renameListViewModel.Entries[0].EngineItem.SetOverride(previewKey, "forced");
            _RaiseHeaderContextMenu(previewHeader);
            Assert.Contains("Cancel Manual Override", _MenuHeaders(previewHeader.ContextMenu));
            Assert.Equal(
                [
                    "(Full File Name)",
                    "Remove Unchanged Items",
                    "Select Fields...",
                    "Edit as Name List",
                    "Cancel Manual Override",
                    "Export",
                ],
                _MenuHeaders(previewHeader.ContextMenu)
            );

            window.Close();
        }

        /// <summary>
        /// Verifies original-side override blue stays on the original column across Preview flips.
        /// </summary>
        [AvaloniaFact]
        public async Task Original_override_blue_survives_preview_side_flip()
        {
            var (renameListViewModel, window, view) = await _ShowAsync();
            var grid = view.FindControl<DataGrid>("RenameGrid");
            Assert.NotNull(grid);

            var nameKey = RenameListFieldKey.Original(BasicRenameListField.Group, BasicRenameListFields.Key.Name);
            var namePreview = RenameListFieldKey.Preview(BasicRenameListField.Group, BasicRenameListFields.Key.Name);
            renameListViewModel.SetVisibleColumns([new RenameListVisibleColumn(nameKey)]);
            renameListViewModel.IsAbModeEnabled = true;
            renameListViewModel.AbSide = RenameListPrefs.AbSideOriginal;
            window.UpdateLayout();
            Dispatcher.UIThread.RunJobs();

            var entry = Assert.Single(renameListViewModel.Entries);
            entry.EngineItem.SetOverride(nameKey, "original-forced");
            entry.EngineItem.SetOverride(namePreview, "preview-forced");

            renameListViewModel.SetAbSide(RenameListPrefs.AbSidePreview);
            window.UpdateLayout();
            Dispatcher.UIThread.RunJobs();

            Assert.Contains(
                grid.GetVisualDescendants().OfType<TextBlock>(),
                text => text.Text == "original-forced" && text.Classes.Contains("rename-list-manual-override")
            );
            Assert.Contains(
                grid.GetVisualDescendants().OfType<TextBlock>(),
                text => text.Text == "preview-forced" && text.Classes.Contains("rename-list-manual-override")
            );

            renameListViewModel.SetAbSide(RenameListPrefs.AbSideOriginal);
            window.UpdateLayout();
            Dispatcher.UIThread.RunJobs();

            Assert.Contains(
                grid.GetVisualDescendants().OfType<TextBlock>(),
                text => text.Text == "original-forced" && text.Classes.Contains("rename-list-manual-override")
            );
            Assert.DoesNotContain(
                grid.GetVisualDescendants().OfType<TextBlock>(),
                text => text.Text == "preview-forced"
            );

            window.Close();
        }

        private async Task<(RenameListViewModel ViewModel, Window Window, RenameListView View)> _ShowAsync()
        {
            var (renameListViewModel, window, _) = await _context.ShowWithRowsAsync(rowCount: 1);
            var view = Assert.IsType<RenameListView>(window.Content);
            return (renameListViewModel, window, view);
        }

        private static void _RaiseHeaderContextMenu(DataGridColumnHeader header)
        {
            header.RaiseEvent(new ContextRequestedEventArgs { RoutedEvent = InputElement.ContextRequestedEvent });
            Dispatcher.UIThread.RunJobs();
        }

        private static IReadOnlyList<string> _MenuHeaders(ContextMenu? menu)
        {
            Assert.NotNull(menu);
            return [.. menu.Items.OfType<MenuItem>().Select(item => item.Header?.ToString() ?? string.Empty)];
        }
    }
}
