using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.VisualTree;
using Mfr.Models.RenameList;

namespace Mfr.App.Ui.Views.RenameList
{
    /// <summary>
    /// Double-click column header splitter auto-fit for <see cref="RenameListView"/>.
    /// </summary>
    public partial class RenameListView
    {
        private void _WireColumnAutoFit()
        {
            // Tunnel so we can auto-fit before DataGridColumnHeader starts a resize drag.
            RenameGrid.AddHandler(PointerPressedEvent, _OnGridPointerPressedForAutoFit, RoutingStrategies.Tunnel);
        }

        private void _OnGridPointerPressedForAutoFit(object? sender, PointerPressedEventArgs e)
        {
            if (_viewModel is null || e.Handled || e.ClickCount != 2)
            {
                return;
            }

            if (e.Source is not Visual source)
            {
                return;
            }

            var header = source.FindAncestorOfType<DataGridColumnHeader>() ?? source as DataGridColumnHeader;
            if (header is null)
            {
                return;
            }

            if (!e.GetCurrentPoint(header).Properties.IsLeftButtonPressed)
            {
                return;
            }

            if (
                !RenameListColumnAutoFit.TryResolveAutoFitFieldKey(
                    header,
                    RenameGrid,
                    e.GetPosition(header),
                    out var fieldKey
                )
            )
            {
                return;
            }

            var column = _FindGridColumn(fieldKey);
            if (column is null)
            {
                return;
            }

            e.Handled = true;
            _AutoFitColumnWidth(column, fieldKey);
        }

        private DataGridColumn? _FindGridColumn(RenameListFieldKey fieldKey)
        {
            foreach (var column in RenameGrid.Columns)
            {
                if (RenameListGridColumns.GetFieldKey(column) == fieldKey)
                {
                    return column;
                }
            }

            return null;
        }

        private void _AutoFitColumnWidth(DataGridColumn column, RenameListFieldKey fieldKey)
        {
            if (_viewModel is null)
            {
                return;
            }

            var fitWidth = RenameListColumnAutoFit.ResolveAutoFitWidth(_viewModel.Entries, fieldKey);
            column.Width = new DataGridLength(fitWidth, DataGridLengthUnitType.Pixel);
        }
    }
}
