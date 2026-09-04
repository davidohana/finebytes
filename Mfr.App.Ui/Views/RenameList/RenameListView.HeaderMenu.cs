using System.Windows.Input;
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

            var isOnlySelected = _viewModel.SelectedEntries.Count == 1 && _viewModel.SelectedEntries[0] == hit;
            if (isOnlySelected)
            {
                return;
            }

            var keepMultiSelection = _viewModel.SelectedEntries.Count > 1 && _viewModel.SelectedEntries.Contains(hit);
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

            var menu = _BuildColumnHeaderContextMenu(_viewModel, fieldKey);
            header.ContextMenu = menu;
            menu.PlacementTarget = header;
            menu.Open(header);
        }

        /// <summary>
        /// Builds the column header menu in MFR7 order for the given field.
        /// </summary>
        /// <param name="viewModel">Bound Rename List view model.</param>
        /// <param name="fieldKey">Field for the clicked header.</param>
        /// <returns>A new context menu (caller opens it).</returns>
        /// <remarks>
        /// <para>
        /// Order: title → Hide Field → (preview) Remove Unchanged → Export Name List (14b) →
        /// Free Names Edit (14c, writable) → Select Visible Fields → Select Sort Fields.
        /// Insert 14b/14c items in this method so order stays one place.
        /// </para>
        /// </remarks>
        private static ContextMenu _BuildColumnHeaderContextMenu(
            RenameListViewModel viewModel,
            RenameListFieldKey fieldKey
        )
        {
            var field = RenameListFieldCatalog.GetField(fieldKey);
            var menu = new ContextMenu { DataContext = viewModel };
            menu.Items.Add(new MenuItem { Header = $"({field.DisplayName})", IsEnabled = false });
            menu.Items.Add(new Separator());

            var hideField = new MenuItem
            {
                Header = "Hide Field",
                IsEnabled = viewModel.VisibleColumns.Count > 1,
            };
            hideField.Click += (_, _) => viewModel.HideColumn(fieldKey);
            menu.Items.Add(hideField);

            if (fieldKey.IsPreview)
            {
                menu.Items.Add(
                    _CreateTipMenuItem(
                        "Remove Unchanged Items",
                        AppTips.RemoveUnchangedItems,
                        () => viewModel.RemoveUnchanged(fieldKey)
                    )
                );
            }

            menu.Items.Add(
                _CreateCommandMenuItem(
                    "Select Visible Fields...",
                    AppTips.SelectRenameListFields,
                    viewModel.OpenFieldShuttleCommand
                )
            );
            menu.Items.Add(
                _CreateCommandMenuItem(
                    "Select Sort Fields...",
                    AppTips.EditRenameListSortFields,
                    viewModel.OpenEditSortFieldsCommand
                )
            );

            return menu;
        }

        /// <summary>
        /// Creates a menu item that runs an action on click and shows a tip.
        /// </summary>
        private static MenuItem _CreateTipMenuItem(string header, string tip, Action onClick)
        {
            var item = new MenuItem { Header = header };
            ToolTip.SetTip(item, tip);
            item.Click += (_, _) => onClick();
            return item;
        }

        /// <summary>
        /// Creates a menu item bound to a command with a tip.
        /// </summary>
        private static MenuItem _CreateCommandMenuItem(
            string header,
            string tip,
            ICommand command
        )
        {
            var item = new MenuItem { Header = header, Command = command };
            ToolTip.SetTip(item, tip);
            return item;
        }
    }
}
