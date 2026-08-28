using System.ComponentModel;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Templates;
using Avalonia.Data;
using Avalonia.Layout;
using Avalonia.Threading;
using Mfr.App.Ui.ViewModels.RenameList;
using Mfr.Models.RenameList;

namespace Mfr.App.Ui.Views.RenameList
{
    /// <summary>
    /// Dynamic Avalonia DataGrid columns for <see cref="RenameListView"/>.
    /// </summary>
    public partial class RenameListView
    {
        private bool _isRebuildingColumns;
        private bool _columnWidthSyncEnabled;

        private void _WireColumnReorder()
        {
            RenameGrid.ColumnReordering += _OnColumnReordering;
            RenameGrid.ColumnReordered += _OnColumnReordered;
        }

        private void _OnColumnReordering(object? sender, DataGridColumnReorderingEventArgs e)
        {
            var headerHeight = RenameGrid.ColumnHeaderHeight;
            if (double.IsNaN(headerHeight) || headerHeight <= 0)
            {
                headerHeight = 22;
            }

            e.DropLocationIndicator = new RenameListColumnDropIndicator(headerHeight);
        }

        private void _OnColumnReordered(object? sender, DataGridColumnEventArgs e)
        {
            if (_isRebuildingColumns || _viewModel is null)
            {
                return;
            }

            var orderedKeys = RenameListGridColumns.GetDisplayedFieldKeys(RenameGrid);
            if (orderedKeys.Count == 0)
            {
                return;
            }

            _viewModel.ReorderVisibleColumns(orderedKeys);
        }

        private void _RebuildColumns()
        {
            if (_viewModel is null)
            {
                RenameGrid.Columns.Clear();
                return;
            }

            _isRebuildingColumns = true;
            _columnWidthSyncEnabled = false;
            try
            {
                RenameGrid.Columns.Clear();
                var visibleColumns = _viewModel.VisibleColumns;
                foreach (var visibleColumn in visibleColumns)
                {
                    RenameGrid.Columns.Add(_CreateGridColumn(visibleColumn));
                }
            }
            finally
            {
                _isRebuildingColumns = false;
            }

            // Ignore layout-driven width changes until the first pass completes.
            Dispatcher.UIThread.Post(() => _columnWidthSyncEnabled = true, DispatcherPriority.Loaded);
        }

        private DataGridTextColumn _CreateGridColumn(RenameListVisibleColumn visibleColumn)
        {
            var key = visibleColumn.Key;
            var field = RenameListFieldCatalog.GetField(key);
            var headerText = RenameListFieldDisplay.GetColumnHeaderText(field, key.IsPreview);
            var sortColumn = field.SortColumn;
            var canUserSort = !key.IsPreview && sortColumn is not null;

            var minHeaderWidth = RenameListGridColumnWidths.GetMinimumHeaderWidth(
                headerText,
                reserveSortGlyph: !key.IsPreview,
                reservePreviewGlyph: key.IsPreview
            );
            var pixelWidth = _ResolveEffectivePixelWidth(visibleColumn, minHeaderWidth);

            var column = new DataGridTextColumn
            {
                Binding = new Binding { Converter = RenameListFieldTextConverter.Instance, ConverterParameter = key },
                CanUserSort = canUserSort,
                Width = new DataGridLength(pixelWidth, DataGridLengthUnitType.Pixel),
                MinWidth = minHeaderWidth,
            };

            RenameListGridColumns.SetFieldKey(column, key);

            column.HeaderTemplate = canUserSort
                ? new FuncDataTemplate<object>(
                    (_, _) => _BuildSortableHeader(_viewModel!, headerText, key.IsPreview, sortColumn!.Value, key)
                )
                : new FuncDataTemplate<object>((_, _) => _CreateHeaderContent(headerText, key));

            column.PropertyChanged += (_, args) => _OnGridColumnPropertyChanged(column, args);
            return column;
        }

        private static int _ResolveEffectivePixelWidth(
            RenameListVisibleColumn visibleColumn,
            int? minHeaderWidth = null
        )
        {
            var key = visibleColumn.Key;
            var field = RenameListFieldCatalog.GetField(key);
            var headerText = RenameListFieldDisplay.GetColumnHeaderText(field, key.IsPreview);
            var resolvedMinHeaderWidth =
                minHeaderWidth
                ?? RenameListGridColumnWidths.GetMinimumHeaderWidth(
                    headerText,
                    reserveSortGlyph: !key.IsPreview,
                    reservePreviewGlyph: key.IsPreview
                );
            var catalogWidth = visibleColumn.ResolveCatalogWidth();
            return catalogWidth is int catalogPixelWidth
                ? Math.Max(catalogPixelWidth, resolvedMinHeaderWidth)
                : resolvedMinHeaderWidth;
        }

        private static Control _CreateHeaderContent(string headerText, RenameListFieldKey key)
        {
            var root = RenameListPreviewGlyph.CreateLabelRow(headerText, key.IsPreview);
            RenameListGridColumns.StampHeaderFieldKey(root, key);
            return root;
        }

        private static Grid _BuildSortableHeader(
            RenameListViewModel viewModel,
            string headerText,
            bool isPreview,
            RenameListSortColumn sortColumn,
            RenameListFieldKey key
        )
        {
            var grid = new Grid { ColumnDefinitions = new ColumnDefinitions("*,Auto") };
            RenameListGridColumns.StampHeaderFieldKey(grid, key);

            var titleHost = RenameListPreviewGlyph.CreateLabelRow(headerText, isPreview);
            titleHost[Grid.ColumnProperty] = 0;
            if (titleHost is TextBlock titleTextBlock)
            {
                titleTextBlock.HorizontalAlignment = HorizontalAlignment.Left;
            }
            else if (titleHost is StackPanel titlePanel)
            {
                titlePanel.HorizontalAlignment = HorizontalAlignment.Left;
            }

            var glyph = new Border { [Grid.ColumnProperty] = 1, Classes = { "rename-list-sort-glyph" } };

            var priority = new TextBlock();
            var direction = new TextBlock();
            glyph.Child = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                Spacing = 1,
                Children = { priority, direction },
            };

            grid.Children.Add(titleHost);
            grid.Children.Add(glyph);

            _ApplySortGlyphState(glyph, priority, direction, viewModel, sortColumn);
            _WireSortGlyphUpdates(grid, glyph, priority, direction, viewModel, sortColumn);

            return grid;
        }

        private static void _WireSortGlyphUpdates(
            Grid headerRoot,
            Border glyph,
            TextBlock priority,
            TextBlock direction,
            RenameListViewModel viewModel,
            RenameListSortColumn sortColumn
        )
        {
            void _OnViewModelPropertyChanged(object? sender, PropertyChangedEventArgs args)
            {
                if (args.PropertyName is nameof(RenameListViewModel.ColumnSortStates))
                {
                    _ApplySortGlyphState(glyph, priority, direction, viewModel, sortColumn);
                }
            }

            viewModel.PropertyChanged += _OnViewModelPropertyChanged;
            headerRoot.DetachedFromVisualTree += (_, _) =>
            {
                viewModel.PropertyChanged -= _OnViewModelPropertyChanged;
            };
        }

        private static void _ApplySortGlyphState(
            Border glyph,
            TextBlock priority,
            TextBlock direction,
            RenameListViewModel viewModel,
            RenameListSortColumn sortColumn
        )
        {
            var state = viewModel.ColumnSortStates[sortColumn];
            glyph.IsVisible = state.IsActive;
            priority.Text = state.Priority?.ToString() ?? string.Empty;
            direction.Text = state.IsActive ? state.DirectionGlyph : string.Empty;
        }

        private void _OnGridColumnPropertyChanged(DataGridColumn column, AvaloniaPropertyChangedEventArgs args)
        {
            if (
                _isRebuildingColumns
                || !_columnWidthSyncEnabled
                || _viewModel is null
                || args.Property != DataGridColumn.WidthProperty
            )
            {
                return;
            }

            var width = column.Width;
            if (!width.IsAbsolute)
            {
                return;
            }

            var fieldKey = RenameListGridColumns.GetFieldKey(column);
            if (fieldKey is null)
            {
                return;
            }

            _viewModel.UpdateVisibleColumnWidth(fieldKey.Value, (int)Math.Round(width.Value));
        }
    }
}
