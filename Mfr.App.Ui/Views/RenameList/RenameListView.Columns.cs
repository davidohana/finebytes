using System.ComponentModel;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Templates;
using Avalonia.Layout;
using Avalonia.Media;
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
            _EnsureRowStatusColumnFirst();
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
                RenameGrid.Columns.Add(_CreateRowStatusColumn());
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
            Dispatcher.UIThread.Post(
                () =>
                {
                    _columnWidthSyncEnabled = true;
                    _EnsureRowStatusColumnFirst();
                },
                DispatcherPriority.Loaded
            );
        }

        private DataGridTemplateColumn _CreateRowStatusColumn()
        {
            const int width = 20;
            var column = new DataGridTemplateColumn
            {
                CanUserSort = false,
                Width = new DataGridLength(width, DataGridLengthUnitType.Pixel),
                MinWidth = width,
                MaxWidth = width,
                Header = string.Empty,
                CellTemplate = new FuncDataTemplate<RenameListEntry>((entry, _) => _CreateRowStatusCell(entry)),
            };
            RenameListGridColumns.MarkAsRowStatusColumn(column);
            return column;
        }

        /// <summary>
        /// Builds the fixed status cell and re-applies the error mark when rows are recycled.
        /// </summary>
        private static Grid _CreateRowStatusCell(RenameListEntry? entry)
        {
            var mark = RenameListRowErrorGlyph.Create();
            var grid = new Grid
            {
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center,
                Children = { mark },
            };

            void _SyncMark()
            {
                var current = grid.DataContext as RenameListEntry ?? entry;
                RenameListRowErrorGlyph.Apply(mark, current);
            }

            _SyncMark();
            grid.DataContextChanged += (_, _) => _SyncMark();
            return grid;
        }

        private void _EnsureRowStatusColumnFirst()
        {
            var statusColumn = RenameGrid.Columns.FirstOrDefault(RenameListGridColumns.IsRowStatusColumn);
            if (statusColumn is null || statusColumn.DisplayIndex == 0)
            {
                return;
            }

            statusColumn.DisplayIndex = 0;
        }

        private DataGridTemplateColumn _CreateGridColumn(RenameListVisibleColumn visibleColumn)
        {
            var key = visibleColumn.Key;
            var field = RenameListFieldCatalog.GetField(key);
            var headerText = field.DisplayName;
            var canUserSort = RenameListFieldCatalog.IsSortableKey(key);

            var minHeaderWidth = RenameListGridColumnWidths.GetMinimumHeaderWidth(
                key,
                _viewModel?.UseFixedWidthFont == true
            );
            var pixelWidth = _ResolveEffectivePixelWidth(visibleColumn, minHeaderWidth);

            var column = new DataGridTemplateColumn
            {
                CanUserSort = canUserSort,
                Width = new DataGridLength(pixelWidth, DataGridLengthUnitType.Pixel),
                MinWidth = minHeaderWidth,
                CellTemplate = new FuncDataTemplate<RenameListEntry>((entry, _) => _CreateFieldCell(entry, key)),
            };

            RenameListGridColumns.SetFieldKey(column, key);

            column.HeaderTemplate = canUserSort
                ? new FuncDataTemplate<object>((_, _) => _BuildSortableHeader(_viewModel!, headerText, key))
                : new FuncDataTemplate<object>((_, _) => _CreateHeaderContent(headerText, key));

            column.PropertyChanged += (_, args) => _OnGridColumnPropertyChanged(column, args);
            return column;
        }

        /// <summary>
        /// Builds one field cell and re-applies text and foreground when DataGrid recycles the row.
        /// </summary>
        private static TextBlock _CreateFieldCell(RenameListEntry? entry, RenameListFieldKey key)
        {
            var textBlock = new TextBlock { VerticalAlignment = VerticalAlignment.Center };
            _ApplyFieldCell(textBlock, entry, key);
            textBlock.DataContextChanged += (_, _) =>
            {
                if (textBlock.DataContext is not RenameListEntry current)
                {
                    return;
                }

                _ApplyFieldCell(textBlock, current, key);
            };
            return textBlock;
        }

        /// <summary>
        /// Sets catalog text; missing rows use whole-row gray, load failures use per-cell styling.
        /// </summary>
        private static void _ApplyFieldCell(TextBlock textBlock, RenameListEntry? entry, RenameListFieldKey key)
        {
            textBlock.Text = entry?.GetFieldText(key) ?? string.Empty;
            var isMissing = entry?.IsMissingFromDisk == true;
            var isLoadError = !isMissing && entry?.IsLoadError(key) == true;
            textBlock.Classes.Set("rename-list-missing-on-disk", isMissing);
            textBlock.Classes.Set("rename-list-load-error", isLoadError);
            textBlock.ClearValue(TextBlock.ForegroundProperty);
            textBlock.ClearValue(TextBlock.FontStyleProperty);
        }

        private static int _ResolveEffectivePixelWidth(RenameListVisibleColumn visibleColumn, int minHeaderWidth)
        {
            var catalogWidth = visibleColumn.ResolveCatalogWidth();
            return catalogWidth is int catalogPixelWidth ? Math.Max(catalogPixelWidth, minHeaderWidth) : minHeaderWidth;
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
            RenameListFieldKey fieldKey
        )
        {
            var grid = new Grid { ColumnDefinitions = new ColumnDefinitions("*,Auto") };
            RenameListGridColumns.StampHeaderFieldKey(grid, fieldKey);

            var title = new TextBlock
            {
                Text = headerText,
                VerticalAlignment = VerticalAlignment.Center,
                HorizontalAlignment = HorizontalAlignment.Left,
                TextTrimming = TextTrimming.CharacterEllipsis,
            };
            title[Grid.ColumnProperty] = 0;

            var glyph = new Border { [Grid.ColumnProperty] = 1, Classes = { "rename-list-sort-glyph" } };

            var priority = new TextBlock();
            var direction = new TextBlock();
            glyph.Child = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                Spacing = 1,
                Children = { priority, direction },
            };

            grid.Children.Add(title);
            grid.Children.Add(glyph);

            _ApplySortGlyphState(glyph, priority, direction, viewModel, fieldKey);
            _WireSortGlyphUpdates(grid, glyph, priority, direction, viewModel, fieldKey);

            return grid;
        }

        private static void _WireSortGlyphUpdates(
            Grid headerRoot,
            Border glyph,
            TextBlock priority,
            TextBlock direction,
            RenameListViewModel viewModel,
            RenameListFieldKey fieldKey
        )
        {
            void _OnViewModelPropertyChanged(object? sender, PropertyChangedEventArgs args)
            {
                if (args.PropertyName is nameof(RenameListViewModel.ColumnSortStates))
                {
                    _ApplySortGlyphState(glyph, priority, direction, viewModel, fieldKey);
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
            RenameListFieldKey fieldKey
        )
        {
            var state = viewModel.ColumnSortStates[fieldKey];
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

        private void _RefreshColumnMinimumWidths()
        {
            if (_viewModel is null)
            {
                return;
            }

            var useFixedWidthFont = _viewModel.UseFixedWidthFont;
            foreach (var column in RenameGrid.Columns)
            {
                var fieldKey = RenameListGridColumns.GetFieldKey(column);
                if (fieldKey is null)
                {
                    continue;
                }

                var minHeaderWidth = RenameListGridColumnWidths.GetMinimumHeaderWidth(
                    fieldKey.Value,
                    useFixedWidthFont
                );
                column.MinWidth = minHeaderWidth;
                if (column.Width.IsAbsolute && column.Width.Value < minHeaderWidth)
                {
                    column.Width = new DataGridLength(minHeaderWidth, DataGridLengthUnitType.Pixel);
                }
            }
        }
    }
}
