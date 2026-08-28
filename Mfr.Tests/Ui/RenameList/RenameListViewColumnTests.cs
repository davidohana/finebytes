using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Headless.XUnit;
using Avalonia.Media;
using Avalonia.Threading;
using Avalonia.VisualTree;
using Mfr.App.Ui.ViewModels.RenameList;
using Mfr.App.Ui.Views.RenameList;
using Mfr.Models.RenameList.Fields.AudioTag;
using Mfr.Models.RenameList.Fields.Basic;
using Mfr.Tests.Models.Filters;

namespace Mfr.Tests.Ui.RenameList
{
    /// <summary>
    /// Headless tests for Rename List dynamic grid columns.
    /// </summary>
    public sealed class RenameListViewColumnTests : IDisposable
    {
        private readonly RenameListUiTestContext _context = new(pinAddPolicy: true);

        /// <inheritdoc />
        public void Dispose()
        {
            _context.Dispose();
        }

        /// <summary>
        /// Verifies the default visible column list produces four grid columns.
        /// </summary>
        [AvaloniaFact]
        public async Task Default_visible_columns_produce_four_grid_columns()
        {
            var (renameListViewModel, window, grid) = await _context.ShowWithRowsAsync(rowCount: 2);

            Assert.Equal(4, renameListViewModel.VisibleColumns.Count);
            Assert.Equal(4, grid.Columns.Count);

            window.Close();
        }

        /// <summary>
        /// Verifies changing visible columns rebuilds the grid column count.
        /// </summary>
        [AvaloniaFact]
        public async Task SetVisibleColumns_rebuilds_grid_columns()
        {
            var (renameListViewModel, window, grid) = await _context.ShowWithRowsAsync(rowCount: 2);
            var twoColumns = new List<RenameListVisibleColumn>
            {
                new(RenameListFieldKey.Original(BasicRenameListField.Group, BasicRenameListFields.Key.Folder)),
                new(RenameListFieldKey.Original(BasicRenameListField.Group, BasicRenameListFields.Key.FullName)),
            };

            renameListViewModel.SetVisibleColumns(twoColumns);
            window.UpdateLayout();
            Dispatcher.UIThread.RunJobs();

            Assert.Equal(2, grid.Columns.Count);

            window.Close();
        }

        /// <summary>
        /// Verifies default grid columns use catalog or header-fit pixel widths, including the last preview column.
        /// </summary>
        [AvaloniaFact]
        public async Task Default_columns_use_catalog_or_header_widths()
        {
            var (renameListViewModel, window, grid) = await _context.ShowWithRowsAsync(rowCount: 2);

            Assert.Null(renameListViewModel.VisibleColumns[0].ResolveCatalogWidth());
            Assert.Equal(240, renameListViewModel.VisibleColumns[1].ResolveCatalogWidth());
            Assert.Equal(180, renameListViewModel.VisibleColumns[2].ResolveCatalogWidth());

            var fileFolderMinWidth = RenameListGridColumnWidths.GetMinimumHeaderWidth(
                "File/Folder",
                reserveSortGlyph: true
            );
            var fileFolderHeaderOnlyWidth = RenameListGridColumnWidths.GetMinimumHeaderWidth("File/Folder");
            var parentFolderMinWidth = RenameListGridColumnWidths.GetMinimumHeaderWidth(
                "Parent Folder",
                reserveSortGlyph: true
            );
            var fullFileNameMinWidth = RenameListGridColumnWidths.GetMinimumHeaderWidth(
                "Full File Name",
                reserveSortGlyph: true
            );
            var previewFullFileNameMinWidth = RenameListGridColumnWidths.GetMinimumHeaderWidth(
                "Full File Name",
                reservePreviewGlyph: true
            );
            Assert.True(fileFolderMinWidth > fileFolderHeaderOnlyWidth);
            Assert.Equal(fileFolderMinWidth, grid.Columns[0].Width.Value);
            Assert.Equal(DataGridLengthUnitType.Pixel, grid.Columns[0].Width.UnitType);
            Assert.Equal(240, grid.Columns[1].Width.Value);
            Assert.Equal(DataGridLengthUnitType.Pixel, grid.Columns[1].Width.UnitType);
            Assert.Equal(Math.Max(180, fullFileNameMinWidth), grid.Columns[2].Width.Value);
            Assert.Equal(DataGridLengthUnitType.Pixel, grid.Columns[2].Width.UnitType);
            Assert.Equal(Math.Max(180, previewFullFileNameMinWidth), grid.Columns[3].Width.Value);
            Assert.Equal(DataGridLengthUnitType.Pixel, grid.Columns[3].Width.UnitType);
            Assert.Equal(fileFolderMinWidth, grid.Columns[0].MinWidth);
            Assert.Equal(parentFolderMinWidth, grid.Columns[1].MinWidth);
            Assert.Equal(fullFileNameMinWidth, grid.Columns[2].MinWidth);
            Assert.Equal(previewFullFileNameMinWidth, grid.Columns[3].MinWidth);

            window.Close();
        }

        /// <summary>
        /// Verifies narrow catalog defaults expand so long header labels are not truncated.
        /// </summary>
        [AvaloniaFact]
        public async Task Narrow_catalog_columns_expand_to_fit_header_text()
        {
            var (renameListViewModel, window, grid) = await _context.ShowWithRowsAsync(rowCount: 2);
            renameListViewModel.SetVisibleColumns([
                new RenameListVisibleColumn(
                    RenameListFieldKey.Original(BasicRenameListField.Group, BasicRenameListFields.Key.FullPathLength)
                ),
                new RenameListVisibleColumn(
                    RenameListFieldKey.Preview(BasicRenameListField.Group, BasicRenameListFields.Key.FileNameLength)
                ),
            ]);
            window.UpdateLayout();
            Dispatcher.UIThread.RunJobs();

            var fullPathLengthMin = RenameListGridColumnWidths.GetMinimumHeaderWidth("Full Path Name Length");
            var previewFileNameLengthMin = RenameListGridColumnWidths.GetMinimumHeaderWidth(
                "File Name Length",
                reservePreviewGlyph: true
            );
            var fullPathLengthWithGlyph = RenameListGridColumnWidths.GetMinimumHeaderWidth(
                "Full Path Name Length",
                reserveSortGlyph: true
            );

            Assert.Null(renameListViewModel.VisibleColumns[0].ResolveCatalogWidth());
            Assert.Null(renameListViewModel.VisibleColumns[1].ResolveCatalogWidth());
            Assert.True(fullPathLengthWithGlyph > fullPathLengthMin);
            Assert.True(previewFileNameLengthMin > 0);
            Assert.Equal(fullPathLengthWithGlyph, grid.Columns[0].Width.Value);
            Assert.Equal(fullPathLengthWithGlyph, grid.Columns[0].MinWidth);
            Assert.Equal(previewFileNameLengthMin, grid.Columns[1].Width.Value);
            Assert.Equal(DataGridLengthUnitType.Pixel, grid.Columns[1].Width.UnitType);
            Assert.Equal(previewFileNameLengthMin, grid.Columns[1].MinWidth);

            window.Close();
        }

        /// <summary>
        /// Verifies preview column headers show the preview badge without red header styling.
        /// </summary>
        [AvaloniaFact]
        public async Task Preview_column_header_uses_preview_style_class()
        {
            var (_, window, grid) = await _context.ShowWithRowsAsync(rowCount: 2);
            window.UpdateLayout();
            Dispatcher.UIThread.RunJobs();

            var previewKey = RenameListFieldKey.Preview(BasicRenameListField.Group, BasicRenameListFields.Key.FullName);
            var previewHeader = grid.GetVisualDescendants()
                .OfType<DataGridColumnHeader>()
                .FirstOrDefault(header => RenameListGridColumns.TryResolveFieldKey(header) == previewKey);
            Assert.NotNull(previewHeader);

            var previewTitle = previewHeader
                .GetVisualDescendants()
                .OfType<TextBlock>()
                .FirstOrDefault(textBlock => textBlock.Text == "Full File Name");
            Assert.NotNull(previewTitle);
            Assert.DoesNotContain("rename-list-preview-header", previewTitle.Classes);

            var previewBadge = previewHeader
                .GetVisualDescendants()
                .OfType<Border>()
                .FirstOrDefault(border => border.Classes.Contains("rename-list-preview-glyph"));
            Assert.NotNull(previewBadge);

            window.Close();
        }

        /// <summary>
        /// Verifies header context-menu resolution targets the clicked column, not a visual-tree index.
        /// </summary>
        [AvaloniaFact]
        public async Task Header_context_menu_resolves_clicked_column_field_key()
        {
            var (_, window, grid) = await _context.ShowWithRowsAsync(rowCount: 1);
            window.UpdateLayout();
            Dispatcher.UIThread.RunJobs();

            var parentFolderKey = RenameListFieldKey.Original(
                BasicRenameListField.Group,
                BasicRenameListFields.Key.Folder
            );
            var parentFolderHeader = grid.GetVisualDescendants()
                .OfType<DataGridColumnHeader>()
                .First(header => RenameListGridColumns.TryResolveFieldKey(header) == parentFolderKey);
            var previewHeader = grid.GetVisualDescendants()
                .OfType<DataGridColumnHeader>()
                .First(header =>
                    RenameListGridColumns.TryResolveFieldKey(header)
                    == RenameListFieldKey.Preview(BasicRenameListField.Group, BasicRenameListFields.Key.FullName)
                );

            Assert.Equal(parentFolderKey, RenameListGridColumns.TryResolveFieldKey(parentFolderHeader));
            Assert.Equal(
                RenameListFieldKey.Preview(BasicRenameListField.Group, BasicRenameListFields.Key.FullName),
                RenameListGridColumns.TryResolveFieldKey(previewHeader)
            );

            var renameListViewModel = (RenameListViewModel)((RenameListView)window.Content!).DataContext!;
            var resolvedKey = RenameListGridColumns.TryResolveFieldKey(parentFolderHeader);
            Assert.NotNull(resolvedKey);
            renameListViewModel.HideColumn(resolvedKey.Value);

            Assert.DoesNotContain(renameListViewModel.VisibleColumns, column => column.Key == parentFolderKey);
            Assert.Equal(3, renameListViewModel.VisibleColumns.Count);

            window.Close();
        }

        /// <summary>
        /// Verifies session column widths survive the initial grid layout pass.
        /// </summary>
        [AvaloniaFact]
        public async Task Session_column_widths_survive_initial_layout()
        {
            const int savedWidth = 400;
            var folderKey = RenameListFieldKey.Original(BasicRenameListField.Group, BasicRenameListFields.Key.Folder);
            var dir = _context.CreateTempDir();
            var path = Path.Combine(dir, "row.txt");
            await File.WriteAllTextAsync(path, "x");

            var renameListViewModel = _context.CreateRenameListViewModel(dir);
            await renameListViewModel.AddPathsAsync([path]);
            renameListViewModel.ApplyVisibleColumnsFromSession([
                new SessionStateRenameListColumn(folderKey, Width: savedWidth),
            ]);

            var view = new RenameListView { DataContext = renameListViewModel };
            var window = new Window
            {
                Width = 600,
                Height = 180,
                Content = view,
            };
            window.Show();
            window.UpdateLayout();
            Dispatcher.UIThread.RunJobs();
            await Dispatcher.UIThread.InvokeAsync(() => { }, DispatcherPriority.Loaded);
            Dispatcher.UIThread.RunJobs();
            window.UpdateLayout();

            var grid = view.GetVisualDescendants().OfType<DataGrid>().Single();

            Assert.Equal(savedWidth, renameListViewModel.VisibleColumns[0].Width);
            Assert.Equal(savedWidth, grid.Columns[0].Width.Value);

            window.Close();
        }

        /// <summary>
        /// Verifies pixel column widths can exceed the viewport instead of being compressed.
        /// </summary>
        [AvaloniaFact]
        public async Task Pixel_column_widths_can_overflow_viewport()
        {
            const int wideWidth = 500;
            var folderKey = RenameListFieldKey.Original(BasicRenameListField.Group, BasicRenameListFields.Key.Folder);
            var nameKey = RenameListFieldKey.Original(BasicRenameListField.Group, BasicRenameListFields.Key.FullName);
            var (renameListViewModel, window, grid) = await _context.ShowWithRowsAsync(rowCount: 2);
            window.Width = 420;
            renameListViewModel.SetVisibleColumns([
                new RenameListVisibleColumn(folderKey, wideWidth),
                new RenameListVisibleColumn(nameKey, wideWidth),
            ]);
            window.UpdateLayout();
            Dispatcher.UIThread.RunJobs();
            await Dispatcher.UIThread.InvokeAsync(() => { }, DispatcherPriority.Loaded);
            Dispatcher.UIThread.RunJobs();

            Assert.Equal(wideWidth, grid.Columns[0].Width.Value);
            Assert.Equal(wideWidth, grid.Columns[1].Width.Value);
            Assert.Equal(DataGridLengthUnitType.Pixel, grid.Columns[0].Width.UnitType);
            Assert.Equal(DataGridLengthUnitType.Pixel, grid.Columns[1].Width.UnitType);
            Assert.Equal(ScrollBarVisibility.Auto, grid.HorizontalScrollBarVisibility);

            window.Close();
        }

        /// <summary>
        /// Verifies user-expanded column widths do not raise MinWidth above the header-fit minimum.
        /// </summary>
        [AvaloniaFact]
        public async Task User_expanded_column_width_allows_shrink_below_saved_width()
        {
            const int expandedWidth = 400;
            var folderKey = RenameListFieldKey.Original(BasicRenameListField.Group, BasicRenameListFields.Key.Folder);
            var (renameListViewModel, window, grid) = await _context.ShowWithRowsAsync(rowCount: 2);
            renameListViewModel.SetVisibleColumns([new RenameListVisibleColumn(folderKey, expandedWidth)]);
            window.UpdateLayout();
            Dispatcher.UIThread.RunJobs();

            var parentFolderMinWidth = RenameListGridColumnWidths.GetMinimumHeaderWidth(
                "Parent Folder",
                reserveSortGlyph: true
            );

            Assert.Equal(expandedWidth, grid.Columns[0].Width.Value);
            Assert.Equal(parentFolderMinWidth, grid.Columns[0].MinWidth);
            Assert.True(parentFolderMinWidth < expandedWidth);

            window.Close();
        }

        /// <summary>
        /// Verifies reordering visible columns keeps pixel widths so columns can overflow the viewport.
        /// </summary>
        [AvaloniaFact]
        public async Task Reordered_visible_columns_keep_pixel_widths()
        {
            var (renameListViewModel, window, grid) = await _context.ShowWithRowsAsync(rowCount: 2);
            var previewKey = RenameListFieldKey.Preview(BasicRenameListField.Group, BasicRenameListFields.Key.FullName);
            var folderKey = RenameListFieldKey.Original(BasicRenameListField.Group, BasicRenameListFields.Key.Folder);
            renameListViewModel.SetVisibleColumns([
                new RenameListVisibleColumn(previewKey),
                new RenameListVisibleColumn(folderKey),
            ]);
            window.UpdateLayout();
            Dispatcher.UIThread.RunJobs();

            Assert.Equal(DataGridLengthUnitType.Pixel, grid.Columns[0].Width.UnitType);
            Assert.Equal(DataGridLengthUnitType.Pixel, grid.Columns[1].Width.UnitType);

            renameListViewModel.ReorderVisibleColumns([folderKey, previewKey]);
            window.UpdateLayout();
            Dispatcher.UIThread.RunJobs();

            Assert.Equal(DataGridLengthUnitType.Pixel, grid.Columns[0].Width.UnitType);
            Assert.Equal(DataGridLengthUnitType.Pixel, grid.Columns[1].Width.UnitType);

            window.Close();
        }

        /// <summary>
        /// Verifies displayed field keys follow grid display index order after reorder.
        /// </summary>
        [AvaloniaFact]
        public async Task GetDisplayedFieldKeys_follows_display_index_order()
        {
            var (_, window, grid) = await _context.ShowWithRowsAsync(rowCount: 2);
            window.UpdateLayout();
            Dispatcher.UIThread.RunJobs();

            var firstColumn = grid.Columns[0];
            var secondColumn = grid.Columns[1];
            firstColumn.DisplayIndex = 1;
            secondColumn.DisplayIndex = 0;
            window.UpdateLayout();
            Dispatcher.UIThread.RunJobs();

            var orderedKeys = RenameListGridColumns.GetDisplayedFieldKeys(grid);

            Assert.Equal(RenameListGridColumns.GetFieldKey(secondColumn), orderedKeys[0]);
            Assert.Equal(RenameListGridColumns.GetFieldKey(firstColumn), orderedKeys[1]);

            window.Close();
        }

        /// <summary>
        /// Verifies the column drop marker reports a non-zero size for the header height.
        /// </summary>
        [AvaloniaFact]
        public void Column_drop_indicator_has_header_height()
        {
            const double headerHeight = 22;
            var indicator = new RenameListColumnDropIndicator(headerHeight);
            indicator.Measure(new Size(double.PositiveInfinity, double.PositiveInfinity));

            Assert.Equal(8, indicator.DesiredSize.Width);
            Assert.Equal(headerHeight, indicator.DesiredSize.Height);
        }

        /// <summary>
        /// Verifies auto-fit width respects header minimum and caps at the configured maximum.
        /// </summary>
        [AvaloniaFact]
        public void GetAutoFitWidth_clamps_to_max_and_respects_header_minimum()
        {
            var minHeaderWidth = RenameListGridColumnWidths.GetMinimumHeaderWidth(
                "Full File Name",
                reserveSortGlyph: true
            );
            var key = RenameListFieldKey.Original(BasicRenameListField.Group, BasicRenameListFields.Key.FullName);
            var shortEntry = RenameListEntry.ToEntry(
                FilterTestHelpers.CreateRenameItem(prefix: "short", directory: @"C:\folder")
            );
            var longEntry = RenameListEntry.ToEntry(
                FilterTestHelpers.CreateRenameItem(prefix: new string('x', 500), directory: @"C:\folder")
            );

            var shortFit = RenameListGridColumnWidths.GetAutoFitWidth([shortEntry], key, minHeaderWidth);
            var longFit = RenameListGridColumnWidths.GetAutoFitWidth([longEntry], key, minHeaderWidth);

            Assert.True(shortFit >= minHeaderWidth);
            Assert.Equal(RenameListGridColumnWidths.MaxAutoFitWidth, longFit);
            Assert.True(longFit > shortFit);
        }

        /// <summary>
        /// Verifies auto-fit of an empty short header (Disc) still leaves room for the full label.
        /// </summary>
        [AvaloniaFact]
        public async Task Auto_fit_empty_disc_column_does_not_truncate_header()
        {
            var discKey = RenameListFieldKey.Original(AudioTagRenameListFields.Group, "Disc");
            var fullNameKey = RenameListFieldKey.Original(
                BasicRenameListField.Group,
                BasicRenameListFields.Key.FullName
            );
            var (renameListViewModel, window, grid) = await _context.ShowWithRowsAsync(rowCount: 2);
            Assert.All(renameListViewModel.Entries, entry => Assert.Equal(string.Empty, entry.GetFieldText(discKey)));

            renameListViewModel.SetVisibleColumns([
                new RenameListVisibleColumn(discKey),
                new RenameListVisibleColumn(fullNameKey, 400),
            ]);
            window.UpdateLayout();
            Dispatcher.UIThread.RunJobs();
            await Dispatcher.UIThread.InvokeAsync(() => { }, DispatcherPriority.Loaded);
            Dispatcher.UIThread.RunJobs();

            var minHeaderWidth = RenameListGridColumnWidths.GetMinimumHeaderWidth("Disc", reserveSortGlyph: true);
            var fitWidth = RenameListColumnAutoFit.ResolveAutoFitWidth(renameListViewModel.Entries, discKey);
            Assert.Equal(minHeaderWidth, fitWidth);

            grid.Columns[0].Width = new DataGridLength(fitWidth, DataGridLengthUnitType.Pixel);
            window.UpdateLayout();
            Dispatcher.UIThread.RunJobs();

            var (Header, Title) = grid.GetVisualDescendants()
                .OfType<DataGridColumnHeader>()
                .Select(item =>
                    (
                        Header: item,
                        Title: item.GetVisualDescendants()
                            .OfType<TextBlock>()
                            .FirstOrDefault(text => text.Text == "Disc")
                    )
                )
                .First(item => item.Title is not null);
            var title = Title!;
            Assert.True(Header.Bounds.Width > 0);
            Assert.True(title.Bounds.Width > 0);

            var unconstrained = new TextBlock
            {
                Text = "Disc",
                FontFamily = title.FontFamily,
                FontSize = title.FontSize,
                FontWeight = title.FontWeight,
            };
            unconstrained.Measure(new Size(double.PositiveInfinity, double.PositiveInfinity));
            Assert.True(
                title.Bounds.Width + 0.5 >= unconstrained.DesiredSize.Width,
                $"Disc header truncated: available={title.Bounds.Width}, needed={unconstrained.DesiredSize.Width}, column={fitWidth}"
            );

            window.Close();
        }

        /// <summary>
        /// Verifies auto-fit widths for typical music-library paths exceed catalog defaults.
        /// </summary>
        [AvaloniaFact]
        public void GetAutoFitWidth_fits_typical_music_library_paths()
        {
            const string parentFolder = @"D:\Music\General\QRS\Supergrass - 2005 - Road To Rouen";
            const string fullPath =
                @"D:\Music\General\QRS\Supergrass - 2005 - Road To Rouen\01 - Tales of Endurance (Part 1).mp3";

            var parentFolderKey = RenameListFieldKey.Original(
                BasicRenameListField.Group,
                BasicRenameListFields.Key.Folder
            );
            var fullPathKey = RenameListFieldKey.Original(
                BasicRenameListField.Group,
                BasicRenameListFields.Key.FullPath
            );
            var parentEntry = RenameListEntry.ToEntry(
                FilterTestHelpers.CreateRenameItem(prefix: "01 - Tales of Endurance (Part 1)", directory: parentFolder)
            );
            var fullPathEntry = RenameListEntry.ToEntry(
                FilterTestHelpers.CreateRenameItem(prefix: "01 - Tales of Endurance (Part 1)", directory: parentFolder)
            );

            var parentMin = RenameListGridColumnWidths.GetMinimumHeaderWidth("Parent Folder", reserveSortGlyph: true);
            var fullPathMin = RenameListGridColumnWidths.GetMinimumHeaderWidth(
                "Full File Path",
                reserveSortGlyph: true
            );
            var parentFit = RenameListGridColumnWidths.GetAutoFitWidth([parentEntry], parentFolderKey, parentMin);
            var fullPathFit = RenameListGridColumnWidths.GetAutoFitWidth([fullPathEntry], fullPathKey, fullPathMin);

            Assert.True(parentFit > 240);
            Assert.True(parentFit < RenameListGridColumnWidths.MaxAutoFitWidth);
            Assert.True(fullPathFit > parentFit);
            Assert.True(fullPathFit <= RenameListGridColumnWidths.MaxAutoFitWidth);
            Assert.Equal(parentFolder, parentEntry.GetFieldText(parentFolderKey));
            Assert.Equal(fullPath, fullPathEntry.GetFieldText(fullPathKey));
        }

        /// <summary>
        /// Verifies header splitter hit-testing matches Avalonia resize regions.
        /// </summary>
        [AvaloniaFact]
        public async Task Header_splitter_hit_test_matches_resize_regions()
        {
            var (_, window, grid) = await _context.ShowWithRowsAsync(rowCount: 1);
            window.UpdateLayout();
            Dispatcher.UIThread.RunJobs();

            var headers = grid.GetVisualDescendants()
                .OfType<DataGridColumnHeader>()
                .Select(header => (header, key: RenameListGridColumns.TryResolveFieldKey(header)))
                .Where(item => item.key is not null)
                .ToList();
            Assert.True(headers.Count >= 2);

            var firstHeader = headers[0].header;
            var secondHeader = headers[1].header;
            var firstKey = headers[0].key!.Value;
            var secondKey = headers[1].key!.Value;
            Assert.NotEqual(firstKey, secondKey);

            Assert.True(firstHeader.Bounds.Width > RenameListColumnAutoFit.HeaderResizeHitWidth);
            Assert.True(secondHeader.Bounds.Width > RenameListColumnAutoFit.HeaderResizeHitWidth);

            Assert.True(
                RenameListColumnAutoFit.TryResolveAutoFitFieldKey(
                    firstHeader,
                    grid,
                    new Point(firstHeader.Bounds.Width - 1, 11),
                    out var rightEdgeKey
                )
            );
            Assert.Equal(firstKey, rightEdgeKey);

            Assert.True(
                RenameListColumnAutoFit.TryResolveAutoFitFieldKey(
                    secondHeader,
                    grid,
                    new Point(1, 11),
                    out var leftEdgeKey
                )
            );
            Assert.Equal(firstKey, leftEdgeKey);

            Assert.False(
                RenameListColumnAutoFit.TryResolveAutoFitFieldKey(
                    firstHeader,
                    grid,
                    new Point(firstHeader.Bounds.Width / 2, 11),
                    out _
                )
            );

            window.Close();
        }

        /// <summary>
        /// Verifies auto-fit width is applied to the grid column and synced to the view model.
        /// </summary>
        [AvaloniaFact]
        public async Task Auto_fit_width_updates_grid_column_and_view_model()
        {
            var dir = _context.CreateTempDir();
            var path = Path.Combine(dir, "a-very-long-sample-file-name-for-autofit.txt");
            await File.WriteAllTextAsync(path, "x");

            var renameListViewModel = _context.CreateRenameListViewModel(dir);
            await renameListViewModel.AddPathsAsync([path]);
            var fullNameKey = RenameListFieldKey.Original(
                BasicRenameListField.Group,
                BasicRenameListFields.Key.FullName
            );
            var minHeaderWidth = RenameListGridColumnWidths.GetMinimumHeaderWidth(
                "Full File Name",
                reserveSortGlyph: true
            );
            renameListViewModel.SetVisibleColumns([new RenameListVisibleColumn(fullNameKey, minHeaderWidth)]);

            var view = new RenameListView { DataContext = renameListViewModel };
            var window = new Window
            {
                Width = 800,
                Height = 180,
                Content = view,
            };
            window.Show();
            window.UpdateLayout();
            Dispatcher.UIThread.RunJobs();
            await Dispatcher.UIThread.InvokeAsync(() => { }, DispatcherPriority.Loaded);
            Dispatcher.UIThread.RunJobs();

            var grid = view.GetVisualDescendants().OfType<DataGrid>().Single();
            var expectedWidth = RenameListColumnAutoFit.ResolveAutoFitWidth(renameListViewModel.Entries, fullNameKey);

            Assert.Equal(minHeaderWidth, grid.Columns[0].Width.Value);
            Assert.True(expectedWidth > minHeaderWidth);

            grid.Columns[0].Width = new DataGridLength(expectedWidth, DataGridLengthUnitType.Pixel);
            window.UpdateLayout();
            Dispatcher.UIThread.RunJobs();

            Assert.Equal(expectedWidth, grid.Columns[0].Width.Value);
            Assert.Equal(expectedWidth, renameListViewModel.VisibleColumns[0].Width);

            window.Close();
        }

        /// <summary>
        /// Verifies default grid cells render basic field text (not blank/invisible).
        /// </summary>
        [AvaloniaFact]
        public async Task Grid_cells_show_basic_field_text()
        {
            var (renameListViewModel, window, grid) = await _context.ShowWithRowsAsync(rowCount: 2);

            window.UpdateLayout();
            Dispatcher.UIThread.RunJobs();
            await Dispatcher.UIThread.InvokeAsync(() => { }, DispatcherPriority.Loaded);
            Dispatcher.UIThread.RunJobs();

            var firstRow = grid.GetVisualDescendants().OfType<DataGridRow>().First();
            var expected = renameListViewModel.Entries[0].FullFileName;
            var rowTexts = firstRow
                .GetVisualDescendants()
                .OfType<TextBlock>()
                .Select(textBlock => textBlock.Text)
                .Where(text => !string.IsNullOrEmpty(text))
                .ToList();

            Assert.Contains(expected, rowTexts);

            window.Close();
        }

        /// <summary>
        /// Verifies basic columns still render when a sibling metadata column failed (6b grid regression).
        /// </summary>
        [AvaloniaFact]
        public async Task Grid_shows_basic_text_and_Error_for_metadata_failure_on_same_row()
        {
            var dir = _context.CreateTempDir();
            var path = Path.Combine(dir, "info.htm");
            await File.WriteAllTextAsync(path, "<html></html>");
            var titleKey = RenameListFieldKey.Original(AudioTagRenameListFields.Group, "Title");
            var fullNameKey = RenameListFieldKey.Original(
                BasicRenameListField.Group,
                BasicRenameListFields.Key.FullName
            );

            var renameListViewModel = _context.CreateRenameListViewModel(dir);
            renameListViewModel.SetVisibleColumns([
                new RenameListVisibleColumn(fullNameKey),
                new RenameListVisibleColumn(titleKey),
            ]);
            await renameListViewModel.AddPathsAsync([path]).ConfigureAwait(true);

            var view = new RenameListView { DataContext = renameListViewModel };
            var window = new Window
            {
                Width = 900,
                Height = 200,
                Content = view,
            };
            window.Show();
            window.UpdateLayout();
            Dispatcher.UIThread.RunJobs();
            await Dispatcher.UIThread.InvokeAsync(() => { }, DispatcherPriority.Loaded);
            Dispatcher.UIThread.RunJobs();

            var grid = view.GetVisualDescendants().OfType<DataGrid>().Single();
            var entry = Assert.Single(renameListViewModel.Entries);
            Assert.Equal("info.htm", entry.GetFieldText(fullNameKey));
            Assert.Equal(RenameListMetadataLoadErrors.DisplayText, entry.GetFieldText(titleKey));

            var row = Assert.Single(grid.GetVisualDescendants().OfType<DataGridRow>());
            var rowTexts = row.GetVisualDescendants()
                .OfType<TextBlock>()
                .Select(textBlock => textBlock.Text)
                .Where(text => !string.IsNullOrEmpty(text))
                .ToList();
            Assert.Contains("info.htm", rowTexts);
            Assert.Contains(RenameListMetadataLoadErrors.DisplayText, rowTexts);

            var errorTextBlock = row.GetVisualDescendants()
                .OfType<TextBlock>()
                .First(textBlock => textBlock.Text == RenameListMetadataLoadErrors.DisplayText);
            var errorBrush = Assert.IsType<SolidColorBrush>(errorTextBlock.Foreground);
            Assert.Equal(Color.Parse("#808080"), errorBrush.Color);

            var fullNameTextBlock = row.GetVisualDescendants()
                .OfType<TextBlock>()
                .First(textBlock => textBlock.Text == "info.htm");
            Assert.NotSame(RenameListFieldForegroundConverter.ErrorBrush, fullNameTextBlock.Foreground);

            window.Close();
        }
    }
}
