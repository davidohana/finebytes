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
                RenameGrid.Columns.Add(_CreateRowStatusColumn(_viewModel));
                var visibleColumns = _viewModel.VisibleColumns;
                foreach (var visibleColumn in visibleColumns)
                {
                    RenameGrid.Columns.Add(_CreateGridColumn(_viewModel, visibleColumn));
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

        /// <summary>
        /// Builds the leading status column whose cells listen for list-level field display refreshes.
        /// </summary>
        private DataGridTemplateColumn _CreateRowStatusColumn(RenameListViewModel listViewModel)
        {
            const int width = 20;
            var column = new DataGridTemplateColumn
            {
                CanUserSort = false,
                Width = new DataGridLength(width, DataGridLengthUnitType.Pixel),
                MinWidth = width,
                MaxWidth = width,
                Header = string.Empty,
                CellTemplate = new FuncDataTemplate<RenameListEntry>(
                    (_, _) => RenameListRowErrorGlyph.Create(listViewModel)
                ),
            };
            RenameListGridColumns.MarkAsRowStatusColumn(column);
            return column;
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

        private DataGridTemplateColumn _CreateGridColumn(
            RenameListViewModel listViewModel,
            RenameListVisibleColumn visibleColumn
        )
        {
            var key = visibleColumn.Key;
            var field = RenameListFieldCatalog.GetField(key);
            var headerText = field.DisplayName;
            var canUserSort = RenameListFieldCatalog.IsSortableKey(key);

            var minHeaderWidth = RenameListGridColumnWidths.GetMinimumHeaderWidth(key, listViewModel.UseFixedWidthFont);
            var pixelWidth = _ResolveEffectivePixelWidth(visibleColumn, minHeaderWidth);

            var column = new DataGridTemplateColumn
            {
                CanUserSort = canUserSort,
                Width = new DataGridLength(pixelWidth, DataGridLengthUnitType.Pixel),
                MinWidth = minHeaderWidth,
                CellTemplate = new FuncDataTemplate<RenameListEntry>(
                    (entry, _) => _CreateFieldCell(entry, key, listViewModel)
                ),
            };

            RenameListGridColumns.SetFieldKey(column, key);

            column.HeaderTemplate = canUserSort
                ? new FuncDataTemplate<object>((_, _) => _BuildSortableHeader(listViewModel, headerText, key))
                : new FuncDataTemplate<object>((_, _) => _CreateHeaderContent(headerText, key));

            column.PropertyChanged += (_, args) => _OnGridColumnPropertyChanged(column, args);
            return column;
        }

        /// <summary>
        /// Builds one field cell and re-applies text when the row recycles or field values change.
        /// </summary>
        private TextBlock _CreateFieldCell(
            RenameListEntry? entry,
            RenameListFieldKey key,
            RenameListViewModel listViewModel
        )
        {
            var textBlock = new TextBlock { VerticalAlignment = VerticalAlignment.Center };

            void ApplyCurrent()
            {
                _ApplyFieldCell(
                    textBlock,
                    textBlock.DataContext as RenameListEntry ?? entry,
                    key,
                    highlightPreviewChanges: listViewModel.IsAutoPreview
                );
            }

            textBlock.DataContextChanged += (_, _) => ApplyCurrent();
            ListenToFieldDisplayRevision(textBlock, listViewModel, ApplyCurrent);
            ApplyCurrent();
            return textBlock;
        }

        /// <summary>
        /// Re-runs <paramref name="apply"/> when catalog field text or row-error state may have changed.
        /// </summary>
        /// <param name="control">Cell or glyph that should refresh while attached to the visual tree.</param>
        /// <param name="listViewModel">List that bumps <see cref="RenameListViewModel.FieldDisplayRevision"/>.</param>
        /// <param name="apply">Refresh callback for the current row data context.</param>
        internal static void ListenToFieldDisplayRevision(
            Control control,
            RenameListViewModel listViewModel,
            Action apply
        )
        {
            void OnListPropertyChanged(object? sender, PropertyChangedEventArgs e)
            {
                if (e.PropertyName is not (nameof(RenameListViewModel.FieldDisplayRevision) or null or ""))
                {
                    return;
                }

                apply();
            }

            void OnAttached(object? sender, VisualTreeAttachmentEventArgs e)
            {
                listViewModel.PropertyChanged += OnListPropertyChanged;
            }

            void OnDetached(object? sender, VisualTreeAttachmentEventArgs e)
            {
                listViewModel.PropertyChanged -= OnListPropertyChanged;
            }

            control.AttachedToVisualTree += OnAttached;
            control.DetachedFromVisualTree += OnDetached;
        }

        /// <summary>
        /// Sets catalog text; missing/load-error/changed-preview cells get style classes.
        /// </summary>
        /// <remarks>
        /// Preview-changed red text follows MFR7: only while Auto-Preview is on.
        /// </remarks>
        private static void _ApplyFieldCell(
            TextBlock textBlock,
            RenameListEntry? entry,
            RenameListFieldKey key,
            bool highlightPreviewChanges
        )
        {
            textBlock.Text = entry?.GetFieldText(key) ?? string.Empty;
            var isMissing = entry?.IsMissingFromDisk == true;
            var isLoadError = !isMissing && entry?.IsLoadError(key) == true;
            var isPreviewChanged =
                highlightPreviewChanges && !isMissing && !isLoadError && entry?.IsPreviewChanged(key) == true;
            textBlock.Classes.Set("rename-list-missing-on-disk", isMissing);
            textBlock.Classes.Set("rename-list-load-error", isLoadError);
            textBlock.Classes.Set("rename-list-preview-changed", isPreviewChanged);
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
