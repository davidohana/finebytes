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
    /// Headless tests for A/B Mode grid chrome (toolbar side, projection remap, header menus).
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
        /// Verifies the Before/After toolbar toggle is hidden until Before/After Mode is on.
        /// </summary>
        [AvaloniaFact]
        public async Task Ab_side_toolbar_hidden_when_ab_mode_off()
        {
            var (renameListViewModel, window, view) = await _ShowAsync();
            var sideToggle = view.FindControl<ToggleButton>("BeforeAfterSideToggle");
            Assert.NotNull(sideToggle);
            Assert.False(renameListViewModel.IsAbModeEnabled);
            Assert.False(sideToggle.IsVisible);

            window.Close();
        }

        /// <summary>
        /// Verifies toolbar view toggles keep Auto-Preview → Before/After → Color Legend order.
        /// </summary>
        [AvaloniaFact]
        public async Task Toolbar_view_toggle_order_auto_preview_before_after_legend()
        {
            var (_, window, view) = await _ShowAsync();
            var autoPreview = view.FindControl<ToggleButton>("AutoPreviewToggle");
            var beforeAfter = view.FindControl<ToggleButton>("BeforeAfterSideToggle");
            var legend = view.FindControl<ToggleButton>("LegendToggle");
            Assert.NotNull(autoPreview);
            Assert.NotNull(beforeAfter);
            Assert.NotNull(legend);

            var rail = Assert.IsType<StackPanel>(autoPreview.Parent);
            var children = rail.Children.ToList();
            Assert.True(children.IndexOf(autoPreview) < children.IndexOf(beforeAfter));
            Assert.True(children.IndexOf(beforeAfter) < children.IndexOf(legend));
            Assert.Equal(legend, children[^1]);

            window.Close();
        }

        /// <summary>
        /// Verifies the Before/After toolbar toggle remaps projected field keys without dropping columns.
        /// </summary>
        [AvaloniaFact]
        public async Task Ab_side_toolbar_click_remaps_projected_grid_column_keys()
        {
            var (renameListViewModel, window, view) = await _ShowAsync();
            var grid = view.FindControl<DataGrid>("RenameGrid");
            var sideToggle = view.FindControl<ToggleButton>("BeforeAfterSideToggle");
            Assert.NotNull(grid);
            Assert.NotNull(sideToggle);

            var fullNameKey = RenameListFieldKey.Original(
                BasicRenameListField.Group,
                BasicRenameListFields.Key.FullName
            );
            renameListViewModel.SetVisibleColumns([new RenameListVisibleColumn(fullNameKey)]);
            renameListViewModel.IsAbModeEnabled = true;
            renameListViewModel.AbSide = RenameListPrefs.AbSidePreview;
            window.UpdateLayout();
            Dispatcher.UIThread.RunJobs();

            Assert.True(sideToggle.IsVisible);
            Assert.True(sideToggle.IsChecked);
            Assert.Equal(2, grid.Columns.Count);
            var fieldColumn = Assert.Single(grid.Columns, static c => !RenameListGridColumns.IsRowStatusColumn(c));
            Assert.Equal(
                [RenameListFieldKey.Preview(BasicRenameListField.Group, BasicRenameListFields.Key.FullName)],
                RenameListGridColumns.GetDisplayedFieldKeys(grid)
            );

            Assert.NotNull(sideToggle.Command);
            Assert.True(sideToggle.Command.CanExecute(null));

            // Bound Command path (headless pointer hits are unreliable on these ToggleButtons).
            sideToggle.Command.Execute(null);
            window.UpdateLayout();
            Dispatcher.UIThread.RunJobs();

            Assert.Equal(RenameListPrefs.AbSideOriginal, renameListViewModel.AbSide);
            Assert.False(sideToggle.IsChecked);
            Assert.Equal(2, grid.Columns.Count);
            Assert.Same(
                fieldColumn,
                Assert.Single(grid.Columns, static c => !RenameListGridColumns.IsRowStatusColumn(c))
            );
            Assert.Equal([fullNameKey], RenameListGridColumns.GetDisplayedFieldKeys(grid));

            sideToggle.Command.Execute(null);
            window.UpdateLayout();
            Dispatcher.UIThread.RunJobs();

            Assert.Equal(RenameListPrefs.AbSidePreview, renameListViewModel.AbSide);
            Assert.True(sideToggle.IsChecked);
            Assert.Equal(2, grid.Columns.Count);
            Assert.Same(
                fieldColumn,
                Assert.Single(grid.Columns, static c => !RenameListGridColumns.IsRowStatusColumn(c))
            );
            Assert.Equal(
                [RenameListFieldKey.Preview(BasicRenameListField.Group, BasicRenameListFields.Key.FullName)],
                RenameListGridColumns.GetDisplayedFieldKeys(grid)
            );

            window.Close();
        }

        /// <summary>
        /// Verifies After-side cells show preview values after an in-place A/B side remap (not a baked original key).
        /// </summary>
        [AvaloniaFact]
        public async Task Ab_side_flip_updates_cell_text_to_projected_values()
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
            entry.EngineItem.SetOverride(nameKey, "before-name");
            entry.EngineItem.SetOverride(namePreview, "after-name");
            renameListViewModel.RefreshFieldDisplayAfterColumnRemap();
            window.UpdateLayout();
            Dispatcher.UIThread.RunJobs();

            Assert.Contains(grid.GetVisualDescendants().OfType<TextBlock>(), text => text.Text == "before-name");

            renameListViewModel.SetAbSide(RenameListPrefs.AbSidePreview);
            window.UpdateLayout();
            Dispatcher.UIThread.RunJobs();

            Assert.Contains(grid.GetVisualDescendants().OfType<TextBlock>(), text => text.Text == "after-name");
            Assert.DoesNotContain(grid.GetVisualDescendants().OfType<TextBlock>(), text => text.Text == "before-name");

            window.Close();
        }

        /// <summary>
        /// Verifies After-side preview headers keep Hide Field, Remove Unchanged, and override Cancel.
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

            var previewHeader = grid.GetVisualDescendants()
                .OfType<DataGridColumnHeader>()
                .First(header => RenameListGridColumns.TryResolveFieldKey(header) == previewKey);

            _RaiseHeaderContextMenu(previewHeader);
            Assert.Contains("Hide field", _MenuHeaders(previewHeader.ContextMenu));
            Assert.Contains("Remove unchanged items", _MenuHeaders(previewHeader.ContextMenu));
            Assert.DoesNotContain("Cancel manual override", _MenuHeaders(previewHeader.ContextMenu));

            renameListViewModel.Entries[0].EngineItem.SetOverride(previewKey, "forced");
            _RaiseHeaderContextMenu(previewHeader);
            Assert.Contains("Cancel manual override", _MenuHeaders(previewHeader.ContextMenu));
            Assert.Equal(
                [
                    "(Full File Name)",
                    "Hide field",
                    "Remove unchanged items",
                    "Select fields...",
                    "Edit as Name List",
                    "Cancel manual override",
                    "Export",
                ],
                _MenuHeaders(previewHeader.ContextMenu)
            );

            window.Close();
        }

        /// <summary>
        /// Verifies Before/After flips keep the matching override blue on the on-screen column.
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

            Assert.DoesNotContain(
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
                text => text.Text == "preview-forced" && text.Classes.Contains("rename-list-manual-override")
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
