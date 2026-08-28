using Avalonia.Controls;
using Avalonia.Headless.XUnit;
using Avalonia.Threading;
using Avalonia.VisualTree;
using Mfr.App.Ui.Services.FileList;
using Mfr.App.Ui.ViewModels.FileList;
using Mfr.App.Ui.ViewModels.RenameList;
using Mfr.App.Ui.Views.RenameList;
using Mfr.Models.RenameList.Fields.Basic;

namespace Mfr.Tests.Ui
{
    /// <summary>
    /// Headless tests for Rename List dynamic grid columns.
    /// </summary>
    public sealed class RenameListViewColumnTests : IDisposable
    {
        private readonly TempDirectoryFixture _tempDirectoryFixture = new();
        private readonly List<FileListViewModel> _fileListViewModels = [];
        private readonly UiConfig _originalUiConfig;

        /// <summary>
        /// Snapshots UI add-policy config for tests that may change it.
        /// </summary>
        public RenameListViewColumnTests()
        {
            _originalUiConfig = new UiConfig
            {
                AddMode = ConfigStore.Config.Ui.AddMode,
                AddFolderContents = ConfigStore.Config.Ui.AddFolderContents,
                RememberWindowState = ConfigStore.Config.Ui.RememberWindowState,
                RememberLastFolder = ConfigStore.Config.Ui.RememberLastFolder,
            };
            ConfigStore.Config.Ui.AddMode = RenameListAddMode.Files;
            ConfigStore.Config.Ui.AddFolderContents = true;
        }

        /// <inheritdoc />
        public void Dispose()
        {
            ConfigStore.Config.Ui.AddMode = _originalUiConfig.AddMode;
            ConfigStore.Config.Ui.AddFolderContents = _originalUiConfig.AddFolderContents;
            ConfigStore.Config.Ui.RememberWindowState = _originalUiConfig.RememberWindowState;
            ConfigStore.Config.Ui.RememberLastFolder = _originalUiConfig.RememberLastFolder;

            foreach (var fileListViewModel in _fileListViewModels)
            {
                fileListViewModel.Dispose();
            }

            _tempDirectoryFixture.Dispose();
        }

        /// <summary>
        /// Verifies the default visible column list produces four grid columns.
        /// </summary>
        [AvaloniaFact]
        public async Task Default_visible_columns_produce_four_grid_columns()
        {
            var (renameListViewModel, window, grid) = await _ShowWithRowsAsync(rowCount: 2);

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
            var (renameListViewModel, window, grid) = await _ShowWithRowsAsync(rowCount: 2);
            var twoColumns = new List<RenameListVisibleColumn>
            {
                new(RenameListFieldKey.Original(BasicRenameListField.Group, BasicFolderField.Key)),
                new(RenameListFieldKey.Original(BasicRenameListField.Group, BasicFullNameField.Key)),
            };

            renameListViewModel.SetVisibleColumns(twoColumns);
            window.UpdateLayout();
            Dispatcher.UIThread.RunJobs();

            Assert.Equal(2, grid.Columns.Count);

            window.Close();
        }

        /// <summary>
        /// Verifies default grid columns use at least catalog or header-fit widths, and a star preview column.
        /// </summary>
        [AvaloniaFact]
        public async Task Default_columns_use_catalog_or_header_widths()
        {
            var (renameListViewModel, window, grid) = await _ShowWithRowsAsync(rowCount: 2);

            Assert.Null(renameListViewModel.VisibleColumns[0].ResolveCatalogWidth());
            Assert.Equal(240, renameListViewModel.VisibleColumns[1].ResolveCatalogWidth());
            Assert.Equal(180, renameListViewModel.VisibleColumns[2].ResolveCatalogWidth());

            var fileFolderMinWidth = RenameListGridColumnWidths.GetMinimumHeaderWidth("File/Folder", reserveSortGlyph: true);
            var fileFolderHeaderOnlyWidth = RenameListGridColumnWidths.GetMinimumHeaderWidth("File/Folder");
            var fullFileNameMinWidth = RenameListGridColumnWidths.GetMinimumHeaderWidth(
                "Full File Name",
                reserveSortGlyph: true
            );
            Assert.True(fileFolderMinWidth > fileFolderHeaderOnlyWidth);
            Assert.Equal(fileFolderMinWidth, grid.Columns[0].Width.Value);
            Assert.Equal(DataGridLengthUnitType.Pixel, grid.Columns[0].Width.UnitType);
            Assert.Equal(240, grid.Columns[1].Width.Value);
            Assert.Equal(DataGridLengthUnitType.Pixel, grid.Columns[1].Width.UnitType);
            Assert.Equal(Math.Max(180, fullFileNameMinWidth), grid.Columns[2].Width.Value);
            Assert.Equal(DataGridLengthUnitType.Pixel, grid.Columns[2].Width.UnitType);
            Assert.True(grid.Columns[3].Width.IsStar);
            Assert.Equal(fileFolderMinWidth, grid.Columns[0].MinWidth);
            Assert.Equal(240, grid.Columns[1].MinWidth);
            Assert.Equal(Math.Max(180, fullFileNameMinWidth), grid.Columns[2].MinWidth);

            window.Close();
        }

        /// <summary>
        /// Verifies narrow catalog defaults expand so long header labels are not truncated.
        /// </summary>
        [AvaloniaFact]
        public async Task Narrow_catalog_columns_expand_to_fit_header_text()
        {
            var (renameListViewModel, window, grid) = await _ShowWithRowsAsync(rowCount: 2);
            renameListViewModel.SetVisibleColumns([
                new RenameListVisibleColumn(
                    RenameListFieldKey.Original(BasicRenameListField.Group, BasicFullPathLengthField.Key)
                ),
                new RenameListVisibleColumn(
                    RenameListFieldKey.Preview(BasicRenameListField.Group, BasicFileNameLengthField.Key)
                ),
            ]);
            window.UpdateLayout();
            Dispatcher.UIThread.RunJobs();

            var fullPathLengthMin = RenameListGridColumnWidths.GetMinimumHeaderWidth("Full Path Name Length");
            var previewFileNameLengthMin = RenameListGridColumnWidths.GetMinimumHeaderWidth(
                "File Name Length (Preview)"
            );

            Assert.Null(renameListViewModel.VisibleColumns[0].ResolveCatalogWidth());
            Assert.Null(renameListViewModel.VisibleColumns[1].ResolveCatalogWidth());
            Assert.True(fullPathLengthMin > 0);
            Assert.True(previewFileNameLengthMin > 0);
            Assert.Equal(fullPathLengthMin, grid.Columns[0].Width.Value);
            Assert.Equal(fullPathLengthMin, grid.Columns[0].MinWidth);
            Assert.True(grid.Columns[1].Width.IsStar);
            Assert.Equal(previewFileNameLengthMin, grid.Columns[1].MinWidth);

            window.Close();
        }

        /// <summary>
        /// Verifies preview column headers use the MFR7 red preview style class.
        /// </summary>
        [AvaloniaFact]
        public async Task Preview_column_header_uses_preview_style_class()
        {
            var (_, window, grid) = await _ShowWithRowsAsync(rowCount: 2);
            window.UpdateLayout();
            Dispatcher.UIThread.RunJobs();

            var previewHeader = grid.GetVisualDescendants()
                .OfType<DataGridColumnHeader>()
                .LastOrDefault(header => header.Content is not null);
            Assert.NotNull(previewHeader);

            var previewTitle = previewHeader
                .GetVisualDescendants()
                .OfType<TextBlock>()
                .FirstOrDefault(textBlock => textBlock.Classes.Contains("rename-list-preview-header"));
            Assert.NotNull(previewTitle);
            Assert.Equal("Full File Name (Preview)", previewTitle.Text);

            window.Close();
        }

        private async Task<(RenameListViewModel ViewModel, Window Window, DataGrid Grid)> _ShowWithRowsAsync(
            int rowCount
        )
        {
            var dir = _tempDirectoryFixture.CreateTempDir();
            var paths = new List<string>(rowCount);
            for (var i = 0; i < rowCount; i++)
            {
                var path = Path.Combine(dir, $"row-{i:00}.txt");
                File.WriteAllText(path, "x");
                paths.Add(path);
            }

            var fileListViewModel = _CreateFileListViewModel(dir);
            var renameListViewModel = new RenameListViewModel(fileListViewModel);
            await renameListViewModel.AddPathsAsync(paths);

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

            var grid = view.GetVisualDescendants().OfType<DataGrid>().Single();
            return (renameListViewModel, window, grid);
        }

        private FileListViewModel _CreateFileListViewModel(string path)
        {
            var fileListViewModel = new FileListViewModel(
                NullSystemIconProvider.Instance,
                path,
                NullFileShellOpener.Instance
            );
            _fileListViewModels.Add(fileListViewModel);
            return fileListViewModel;
        }
    }
}
