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
                for (var index = 0; index < visibleColumns.Count; index++)
                {
                    RenameGrid.Columns.Add(_CreateGridColumn(visibleColumns[index], index, visibleColumns.Count));
                }
            }
            finally
            {
                _isRebuildingColumns = false;
            }

            // Ignore layout-driven width changes until the first pass completes.
            Dispatcher.UIThread.Post(() => _columnWidthSyncEnabled = true, DispatcherPriority.Loaded);
        }

        private DataGridTextColumn _CreateGridColumn(RenameListVisibleColumn visibleColumn, int index, int columnCount)
        {
            var key = visibleColumn.Key;
            var field = RenameListFieldCatalog.GetField(key);
            var headerText = RenameListFieldDisplay.GetColumnHeaderText(field, key.IsPreview);
            var sortMemberPath = _GetSortMemberPath(key);
            var canUserSort = sortMemberPath is not null;

            var catalogWidth = visibleColumn.ResolveCatalogWidth();
            var reserveSortGlyph = !key.IsPreview;
            var minHeaderWidth = RenameListGridColumnWidths.GetMinimumHeaderWidth(
                headerText,
                reserveSortGlyph: reserveSortGlyph,
                reservePreviewGlyph: key.IsPreview
            );
            var effectiveWidth = catalogWidth is int catalogPixelWidth
                ? Math.Max(catalogPixelWidth, minHeaderWidth)
                : minHeaderWidth;
            var width = _ResolveColumnWidth(visibleColumn, index, columnCount, effectiveWidth);

            var column = new DataGridTextColumn
            {
                Header = headerText,
                Binding = new Binding { Converter = RenameListFieldTextConverter.Instance, ConverterParameter = key },
                SortMemberPath = sortMemberPath ?? string.Empty,
                CanUserSort = canUserSort,
                Width = width,
                MinWidth = effectiveWidth,
            };

            RenameListGridColumns.SetFieldKey(column, key);

            if (canUserSort && field.SortColumn is { } sortColumn)
            {
                column.HeaderTemplate = new FuncDataTemplate<object>(
                    (_, _) => _BuildSortableHeader(_viewModel!, headerText, key.IsPreview, sortColumn)
                );
            }
            else if (key.IsPreview)
            {
                column.HeaderTemplate = new FuncDataTemplate<object>(
                    (_, _) => RenameListPreviewGlyph.CreateLabelRow(headerText, isPreview: true)
                );
            }

            column.PropertyChanged += (_, args) => _OnGridColumnPropertyChanged(column, args);
            return column;
        }

        private static DataGridLength _ResolveColumnWidth(
            RenameListVisibleColumn visibleColumn,
            int index,
            int columnCount,
            int effectiveWidth
        )
        {
            var isLastColumn = index == columnCount - 1;
            if (isLastColumn && visibleColumn.Key.IsPreview)
            {
                return new DataGridLength(1, DataGridLengthUnitType.Star);
            }

            return new DataGridLength(effectiveWidth, DataGridLengthUnitType.Pixel);
        }

        private static string? _GetSortMemberPath(RenameListFieldKey key)
        {
            if (key.IsPreview || !RenameListFieldCatalog.TryMapFieldKeyToSortColumn(key, out var sortColumn))
            {
                return null;
            }

            return sortColumn switch
            {
                RenameListSortColumn.FileFolder => nameof(RenameListEntry.FileFolder),
                RenameListSortColumn.ParentFolder => nameof(RenameListEntry.ParentFolder),
                RenameListSortColumn.FullFileName => nameof(RenameListEntry.FullFileName),
                RenameListSortColumn.FullPath => null,
                _ => null,
            };
        }

        private static Grid _BuildSortableHeader(
            RenameListViewModel viewModel,
            string headerText,
            bool isPreview,
            RenameListSortColumn sortColumn
        )
        {
            var grid = new Grid { ColumnDefinitions = new ColumnDefinitions("*,Auto") };

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

            if (args.OldValue is not DataGridLength oldWidth || oldWidth.IsStar || oldWidth.IsAuto)
            {
                return;
            }

            var width = column.Width;
            if (width.IsStar || width.IsAuto)
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
