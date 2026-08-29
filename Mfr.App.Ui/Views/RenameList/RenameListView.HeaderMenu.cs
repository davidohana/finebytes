using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.VisualTree;
using Mfr.App.Ui.Resources;
using Mfr.App.Ui.ViewModels.RenameList;
using Mfr.Models.RenameList;

namespace Mfr.App.Ui.Views.RenameList
{
    /// <summary>
    /// Column header context menu for <see cref="RenameListView"/>.
    /// </summary>
    public partial class RenameListView
    {
        private void _WireHeaderContextMenu()
        {
            RenameGrid.AddHandler(ContextRequestedEvent, _OnGridContextRequested, RoutingStrategies.Tunnel);
        }

        private void _OnGridContextRequested(object? sender, ContextRequestedEventArgs e)
        {
            if (_viewModel is null || e.Handled)
            {
                return;
            }

            if (e.Source is not Visual source)
            {
                return;
            }

            var header = source.FindAncestorOfType<DataGridColumnHeader>() ?? source as DataGridColumnHeader;
            if (header is not null)
            {
                var fieldKey = RenameListGridColumns.TryResolveFieldKey(header);
                if (fieldKey is null)
                {
                    return;
                }

                e.Handled = true;
                _ShowColumnHeaderContextMenu(header, fieldKey.Value);
                return;
            }

            var row = source.FindAncestorOfType<DataGridRow>();
            if (row?.DataContext is not RenameListEntry hit)
            {
                return;
            }

            _SelectRowForContextMenu(hit);
        }

        private void _SelectRowForContextMenu(RenameListEntry hit)
        {
            if (_viewModel is null)
            {
                return;
            }

            var isOnlySelected =
                _viewModel.SelectedEntries.Count == 1 && _viewModel.SelectedEntries[0] == hit;
            if (isOnlySelected)
            {
                return;
            }

            var keepMultiSelection =
                _viewModel.SelectedEntries.Count > 1 && _viewModel.SelectedEntries.Contains(hit);
            if (keepMultiSelection)
            {
                return;
            }

            _selectionChangeFromView = true;
            try
            {
                _viewModel.SetSelectedEntries([hit]);
            }
            finally
            {
                _selectionChangeFromView = false;
            }

            _isSyncingSelection = true;
            try
            {
                _SyncSelectionToGrid();
            }
            finally
            {
                _isSyncingSelection = false;
            }
        }

        private void _ShowColumnHeaderContextMenu(DataGridColumnHeader header, RenameListFieldKey fieldKey)
        {
            if (_viewModel is null)
            {
                return;
            }

            var field = RenameListFieldCatalog.GetField(fieldKey);
            var headerText = field.DisplayName;
            var canHide = _viewModel.VisibleColumns.Count > 1;

            var menu = new ContextMenu { DataContext = _viewModel };
            menu.Items.Add(new MenuItem { Header = $"({headerText})", IsEnabled = false });
            menu.Items.Add(new Separator());
            var selectFields = new MenuItem
            {
                Header = "Select Visible Fields...",
                Command = _viewModel.OpenFieldShuttleCommand,
            };
            ToolTip.SetTip(selectFields, AppTips.SelectRenameListFields);
            menu.Items.Add(selectFields);

            var selectSortFields = new MenuItem
            {
                Header = "Select Sort Fields...",
                Command = _viewModel.OpenEditSortFieldsCommand,
            };
            ToolTip.SetTip(selectSortFields, AppTips.EditRenameListSortFields);
            menu.Items.Add(selectSortFields);

            var hideField = new MenuItem { Header = "Hide Field", IsEnabled = canHide };
            hideField.Click += (_, _) => _viewModel.HideColumn(fieldKey);
            menu.Items.Add(hideField);

            menu.PlacementTarget = header;
            menu.Open(header);
        }
    }
}
