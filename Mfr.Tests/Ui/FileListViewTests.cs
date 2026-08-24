using System.Collections;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Presenters;
using Avalonia.Controls.Primitives;
using Avalonia.Headless.XUnit;
using Avalonia.Input;
using Avalonia.Threading;
using Avalonia.VisualTree;
using Mfr.App.Ui.Services.FileList;
using Mfr.App.Ui.ViewModels.FileList;
using Mfr.App.Ui.Views.FileList;

namespace Mfr.Tests.Ui
{
    /// <summary>
    /// Headless layout tests for the File List pane.
    /// </summary>
    public sealed class FileListViewTests : IDisposable
    {
        private readonly TempDirectoryFixture _tempDirectoryFixture = new();
        private readonly List<FileListViewModel> _viewModels = [];

        /// <inheritdoc />
        public void Dispose()
        {
            foreach (var viewModel in _viewModels)
            {
                viewModel.Dispose();
            }

            _tempDirectoryFixture.Dispose();
        }

        /// <summary>
        /// Verifies long tile names stay inside the cell instead of painting over neighbors.
        /// </summary>
        [AvaloniaFact]
        public void Tiles_Long_Names_Do_Not_Exceed_Cell_Width()
        {
            const string longName = "Unigine_Heaven_Benchmark_4.0_20241106_2059_extra_long_report_name.html";
            var dir = _tempDirectoryFixture.CreateTempDir();
            File.WriteAllText(Path.Combine(dir, longName), "x");
            File.WriteAllText(Path.Combine(dir, "short.txt"), "y");

            var viewModel = new FileListViewModel(NullSystemIconProvider.Instance, dir);
            _viewModels.Add(viewModel);
            viewModel.SetViewMode(FileListViewMode.Tiles);

            var view = new FileListView { DataContext = viewModel };
            var window = new Window
            {
                Width = 560,
                Height = 360,
                Content = view,
            };
            window.Show();
            window.UpdateLayout();

            var list = view.FindControl<ListBox>("TilesList");
            Assert.NotNull(list);
            Assert.True(list.IsVisible);

            var entry = Assert.Single(viewModel.Entries, item => item.Name == longName);
            var container = list.ContainerFromIndex(list.Items.Cast<FileListEntry>().ToList().IndexOf(entry));
            Assert.NotNull(container);
            Assert.True(container.ClipToBounds);

            var nameBlock = container.GetVisualDescendants().OfType<TextBlock>().First(block => block.Text == longName);
            Assert.True(
                nameBlock.Bounds.Width <= 140,
                $"Name width {nameBlock.Bounds.Width} should stay within the tile text column."
            );
            Assert.True(
                container.Bounds.Width <= 176,
                $"Tile container width {container.Bounds.Width} should match the wrap cell."
            );
        }

        /// <summary>
        /// Verifies thumbnail selection fills the cell even when the caption is narrower than the icon.
        /// </summary>
        [AvaloniaFact]
        public void Thumbnail_Selection_Fills_Cell_For_Short_Names()
        {
            var viewModel = _CreateThumbnailsViewModel(folderCount: 4);
            var (window, list) = _ShowThumbnails(viewModel);
            var entry = Assert.Single(viewModel.Entries, item => item.Name == ".config");
            viewModel.SelectedEntry = entry;
            window.UpdateLayout();

            var presenter = _SelectionPresenter(list, entry);
            Assert.True(
                presenter.Bounds.Width >= viewModel.ThumbnailSize,
                $"Selection width {presenter.Bounds.Width} should cover thumbnail size {viewModel.ThumbnailSize}."
            );
            Assert.Equal(viewModel.ThumbnailCellWidth, presenter.Bounds.Width, precision: 0);
            Assert.Equal(viewModel.ThumbnailCellHeight, presenter.Bounds.Height, precision: 0);
        }

        /// <summary>
        /// Verifies each thumbnail image occupies a square of the current size, including after zoom.
        /// </summary>
        [AvaloniaFact]
        public void Thumbnail_Image_Is_Square_Of_ThumbnailSize()
        {
            var viewModel = _CreateThumbnailsViewModel(folderCount: 4);
            var (window, list) = _ShowThumbnails(viewModel);
            var entry = Assert.Single(viewModel.Entries, item => item.Name == ".config");
            var square = _ThumbnailSquare(list, entry);

            Assert.Equal(viewModel.ThumbnailSize, square.Bounds.Width, precision: 0);
            Assert.Equal(viewModel.ThumbnailSize, square.Bounds.Height, precision: 0);

            viewModel.ZoomThumbnailsIn();
            window.UpdateLayout();

            Assert.Equal(ThumbnailSizes.Large, viewModel.ThumbnailSize);
            Assert.Equal(viewModel.ThumbnailSize, square.Bounds.Width, precision: 0);
            Assert.Equal(viewModel.ThumbnailSize, square.Bounds.Height, precision: 0);
        }

        /// <summary>
        /// Verifies recycled thumbnail containers keep a full-cell selection after a long scroll.
        /// </summary>
        [AvaloniaFact]
        public void Thumbnail_Selection_Fills_Cell_After_Fast_Scroll()
        {
            var viewModel = _CreateThumbnailsViewModel(folderCount: 80);
            var (window, list) = _ShowThumbnails(viewModel);
            var scrollViewer = list.GetVisualDescendants().OfType<ScrollViewer>().First();
            var entry = Assert.Single(viewModel.Entries, item => item.Name == ".config");
            viewModel.SelectedEntry = entry;
            window.UpdateLayout();

            scrollViewer.Offset = new Vector(0, scrollViewer.Extent.Height);
            window.UpdateLayout();
            scrollViewer.Offset = new Vector(0, 0);
            window.UpdateLayout();

            var presenter = _SelectionPresenter(list, entry);
            Assert.Equal(viewModel.ThumbnailCellWidth, presenter.Bounds.Width, precision: 0);
            Assert.Equal(viewModel.ThumbnailCellHeight, presenter.Bounds.Height, precision: 0);
        }

        /// <summary>
        /// Verifies List view multi-select from the view model syncs to the listing control.
        /// </summary>
        [AvaloniaFact]
        public void List_View_Syncs_Multi_Select_From_ViewModel()
        {
            var dir = _tempDirectoryFixture.CreateTempDir();
            Directory.CreateDirectory(Path.Combine(dir, "zeta-folder"));
            File.WriteAllText(Path.Combine(dir, "alpha.txt"), "a");
            File.WriteAllText(Path.Combine(dir, "beta.md"), "b");

            var viewModel = new FileListViewModel(NullSystemIconProvider.Instance, dir);
            _viewModels.Add(viewModel);
            viewModel.SetViewMode(FileListViewMode.List);

            var view = new FileListView { DataContext = viewModel };
            var window = new Window
            {
                Width = 420,
                Height = 360,
                Content = view,
            };
            window.Show();
            window.UpdateLayout();

            var list = view.FindControl<ListBox>("ListViewList");
            Assert.NotNull(list);
            Assert.True(list.IsVisible);

            var alpha = viewModel.Entries.First(entry => entry.Name == "alpha.txt");
            var beta = viewModel.Entries.First(entry => entry.Name == "beta.md");
            viewModel.SetSelectedEntries([alpha, beta], beta);
            window.UpdateLayout();

            Assert.Equal(2, list.SelectedItems!.Count);
            var selected = list.SelectedItems.Cast<FileListEntry>().ToList();
            Assert.Contains(selected, entry => entry.FullPath == alpha.FullPath);
            Assert.Contains(selected, entry => entry.FullPath == beta.FullPath);
        }

        /// <summary>
        /// Verifies Report (details) view multi-select from the view model syncs to the DataGrid.
        /// </summary>
        [AvaloniaFact]
        public void Report_View_Syncs_Multi_Select_From_ViewModel()
        {
            var dir = _tempDirectoryFixture.CreateTempDir();
            Directory.CreateDirectory(Path.Combine(dir, "zeta-folder"));
            File.WriteAllText(Path.Combine(dir, "alpha.txt"), "a");
            File.WriteAllText(Path.Combine(dir, "beta.md"), "b");

            var viewModel = new FileListViewModel(NullSystemIconProvider.Instance, dir);
            _viewModels.Add(viewModel);
            Assert.True(viewModel.IsReportView);

            var view = new FileListView { DataContext = viewModel };
            var window = new Window
            {
                Width = 560,
                Height = 360,
                Content = view,
            };
            window.Show();
            window.UpdateLayout();

            var grid = view.FindControl<DataGrid>("ReportGrid");
            Assert.NotNull(grid);
            Assert.True(grid.IsVisible);

            var alpha = viewModel.Entries.First(entry => entry.Name == "alpha.txt");
            var beta = viewModel.Entries.First(entry => entry.Name == "beta.md");
            viewModel.SetSelectedEntries([alpha, beta], beta);
            window.UpdateLayout();

            Assert.Equal(2, grid.SelectedItems.Count);
            var selected = grid.SelectedItems.Cast<FileListEntry>().ToList();
            Assert.Contains(selected, entry => entry.FullPath == alpha.FullPath);
            Assert.Contains(selected, entry => entry.FullPath == beta.FullPath);
        }

        /// <summary>
        /// Verifies Report grid selection changes sync to the view model without collapsing prior rows.
        /// </summary>
        [AvaloniaFact]
        public void Report_Grid_Multi_Select_From_Control_Syncs_To_ViewModel()
        {
            var (viewModel, grid, window) = _ShowReportGrid();
            var alpha = viewModel.Entries.First(entry => entry.Name == "alpha.txt");
            var beta = viewModel.Entries.First(entry => entry.Name == "beta.md");

            _SetGridSelection(grid, [alpha], alpha);
            window.UpdateLayout();

            Assert.Single(viewModel.SelectedEntries);
            Assert.Equal(alpha.FullPath, viewModel.SelectedEntry!.FullPath);

            _SetGridSelection(grid, [alpha, beta], beta);
            window.UpdateLayout();

            Assert.Equal(2, viewModel.SelectedEntries.Count);
            Assert.Equal(2, grid.SelectedItems!.Count);
            var selectedPaths = viewModel.SelectedEntries.Select(entry => entry.FullPath).ToHashSet();
            Assert.Contains(alpha.FullPath, selectedPaths);
            Assert.Contains(beta.FullPath, selectedPaths);
        }

        /// <summary>
        /// Verifies adding a second Report row does not get wiped by view-model sync-back.
        /// </summary>
        [AvaloniaFact]
        public void Report_Grid_Multi_Select_Does_Not_Collapse_On_Second_Add()
        {
            var (viewModel, grid, window) = _ShowReportGrid();
            var alpha = viewModel.Entries.First(entry => entry.Name == "alpha.txt");
            var beta = viewModel.Entries.First(entry => entry.Name == "beta.md");

            _SetGridSelection(grid, [alpha], alpha);
            window.UpdateLayout();
            _SetGridSelection(grid, [alpha, beta], beta);
            window.UpdateLayout();

            Assert.Equal(2, grid.SelectedItems.Count);
            var selectedPaths = grid.SelectedItems.Cast<FileListEntry>().Select(entry => entry.FullPath).ToHashSet();
            Assert.Contains(alpha.FullPath, selectedPaths);
            Assert.Contains(beta.FullPath, selectedPaths);
        }

        /// <summary>
        /// Verifies Down arrow on the Report grid replaces multi-select with the next row.
        /// </summary>
        [AvaloniaFact]
        public void Report_Grid_Arrow_Down_Replaces_Multi_Select()
        {
            var (viewModel, grid, window) = _ShowReportGrid();
            var alpha = viewModel.Entries.First(entry => entry.Name == "alpha.txt");
            var beta = viewModel.Entries.First(entry => entry.Name == "beta.md");
            viewModel.SetSelectedEntries([alpha, beta], alpha);
            window.UpdateLayout();

            _RaiseKeyDown(grid, Key.Down);
            window.UpdateLayout();

            Assert.Single(viewModel.SelectedEntries);
            Assert.Equal("beta.md", viewModel.SelectedEntry!.Name);
            Assert.Single(grid.SelectedItems!.Cast<FileListEntry>());
            Assert.Equal("beta.md", grid.SelectedItems.Cast<FileListEntry>().Single().Name);
        }

        /// <summary>
        /// Verifies List view selection changes sync to the view model.
        /// </summary>
        [AvaloniaFact]
        public void List_View_Multi_Select_From_Control_Syncs_To_ViewModel()
        {
            var (viewModel, view, window) = _ShowListView();
            var list = view.FindControl<ListBox>("ListViewList");
            Assert.NotNull(list);

            var alpha = viewModel.Entries.First(entry => entry.Name == "alpha.txt");
            var beta = viewModel.Entries.First(entry => entry.Name == "beta.md");

            _SetListSelection(list, [alpha], alpha);
            window.UpdateLayout();
            _SetListSelection(list, [alpha, beta], beta);
            window.UpdateLayout();

            Assert.Equal(2, viewModel.SelectedEntries.Count);
            Assert.Equal(2, list.SelectedItems!.Count);
        }

        /// <summary>
        /// Verifies Tiles view multi-select from the view model syncs to the listing control.
        /// </summary>
        [AvaloniaFact]
        public void Tiles_View_Syncs_Multi_Select_From_ViewModel()
        {
            var (viewModel, view, window) = _ShowTilesView();
            var list = view.FindControl<ListBox>("TilesList");
            Assert.NotNull(list);

            var alpha = viewModel.Entries.First(entry => entry.Name == "alpha.txt");
            var beta = viewModel.Entries.First(entry => entry.Name == "beta.md");
            viewModel.SetSelectedEntries([alpha, beta], beta);
            window.UpdateLayout();

            Assert.Equal(2, list.SelectedItems!.Count);
            var selectedPaths = list.SelectedItems.Cast<FileListEntry>().Select(entry => entry.FullPath).ToHashSet();
            Assert.Contains(alpha.FullPath, selectedPaths);
            Assert.Contains(beta.FullPath, selectedPaths);
        }

        /// <summary>
        /// Verifies committing a mask picked from the combo keeps the displayed text.
        /// </summary>
        [AvaloniaFact]
        public void Mask_Combo_Keeps_Text_After_Commit()
        {
            var viewModel = new FileListViewModel(NullSystemIconProvider.Instance, _CreateSampleDir());
            _viewModels.Add(viewModel);

            var view = new FileListView { DataContext = viewModel };
            var window = new Window
            {
                Width = 420,
                Height = 360,
                Content = view,
            };
            window.Show();
            window.UpdateLayout();

            var combo = view.FindControl<ComboBox>("MaskCombo");
            Assert.NotNull(combo);

            const string mask = "*.txt";
            var existingIndex = viewModel.MaskSuggestions.IndexOf(mask);
            Assert.True(existingIndex > 0, "Test mask should start below the front of suggestions.");

            combo.SelectedItem = viewModel.MaskSuggestions[existingIndex];
            combo.Text = mask;
            viewModel.Mask = mask;
            window.UpdateLayout();

            var grid = view.FindControl<DataGrid>("ReportGrid");
            Assert.NotNull(grid);
            combo.Focus();
            window.UpdateLayout();
            grid.Focus();
            Dispatcher.UIThread.RunJobs();
            window.UpdateLayout();

            Assert.Equal(mask, viewModel.Mask);
            Assert.Equal(mask, combo.Text);
            Assert.Equal(mask, viewModel.MaskSuggestions[0]);
        }

        /// <summary>
        /// Verifies Thumbnails view multi-select from the view model syncs to the listing control.
        /// </summary>
        [AvaloniaFact]
        public void Thumbnails_View_Syncs_Multi_Select_From_ViewModel()
        {
            var dir = _CreateSampleDir();
            var viewModel = new FileListViewModel(NullSystemIconProvider.Instance, dir);
            _viewModels.Add(viewModel);
            viewModel.SetViewMode(FileListViewMode.Thumbnails);

            var view = new FileListView { DataContext = viewModel };
            var window = new Window
            {
                Width = 420,
                Height = 360,
                Content = view,
            };
            window.Show();
            window.UpdateLayout();

            var list = view.FindControl<ListBox>("ThumbnailsList");
            Assert.NotNull(list);

            var alpha = viewModel.Entries.First(entry => entry.Name == "alpha.txt");
            var beta = viewModel.Entries.First(entry => entry.Name == "beta.md");
            viewModel.SetSelectedEntries([alpha, beta], beta);
            window.UpdateLayout();

            Assert.Equal(2, list.SelectedItems!.Count);
            var selectedPaths = list.SelectedItems.Cast<FileListEntry>().Select(entry => entry.FullPath).ToHashSet();
            Assert.Contains(alpha.FullPath, selectedPaths);
            Assert.Contains(beta.FullPath, selectedPaths);
        }

        private string _CreateSampleDir()
        {
            var dir = _tempDirectoryFixture.CreateTempDir();
            Directory.CreateDirectory(Path.Combine(dir, "zeta-folder"));
            File.WriteAllText(Path.Combine(dir, "alpha.txt"), "a");
            File.WriteAllText(Path.Combine(dir, "beta.md"), "b");
            return dir;
        }

        private (FileListViewModel ViewModel, DataGrid Grid, Window Window) _ShowReportGrid()
        {
            var viewModel = new FileListViewModel(NullSystemIconProvider.Instance, _CreateSampleDir());
            _viewModels.Add(viewModel);
            Assert.True(viewModel.IsReportView);

            var view = new FileListView { DataContext = viewModel };
            var window = new Window
            {
                Width = 560,
                Height = 360,
                Content = view,
            };
            window.Show();
            window.UpdateLayout();

            var grid = view.FindControl<DataGrid>("ReportGrid");
            Assert.NotNull(grid);
            Assert.True(grid.IsVisible);
            return (viewModel, grid, window);
        }

        private (FileListViewModel ViewModel, FileListView View, Window Window) _ShowListView()
        {
            var viewModel = new FileListViewModel(NullSystemIconProvider.Instance, _CreateSampleDir());
            _viewModels.Add(viewModel);
            viewModel.SetViewMode(FileListViewMode.List);

            var view = new FileListView { DataContext = viewModel };
            var window = new Window
            {
                Width = 420,
                Height = 360,
                Content = view,
            };
            window.Show();
            window.UpdateLayout();
            return (viewModel, view, window);
        }

        private (FileListViewModel ViewModel, FileListView View, Window Window) _ShowTilesView()
        {
            var viewModel = new FileListViewModel(NullSystemIconProvider.Instance, _CreateSampleDir());
            _viewModels.Add(viewModel);
            viewModel.SetViewMode(FileListViewMode.Tiles);

            var view = new FileListView { DataContext = viewModel };
            var window = new Window
            {
                Width = 560,
                Height = 360,
                Content = view,
            };
            window.Show();
            window.UpdateLayout();
            return (viewModel, view, window);
        }

        private static void _SetGridSelection(
            DataGrid grid,
            IReadOnlyList<FileListEntry> entries,
            FileListEntry focused
        )
        {
            grid.SelectedItems.Clear();
            foreach (var entry in entries)
            {
                grid.SelectedItems.Add(entry);
            }

            if (entries.Count == 1)
            {
                grid.SelectedItem = focused;
            }

            _RaiseSelectionChanged(grid, Array.Empty<object>(), entries.Cast<object>().ToArray());
        }

        private static void _SetListSelection(ListBox list, IReadOnlyList<FileListEntry> entries, FileListEntry focused)
        {
            list.SelectedItems!.Clear();
            FileListEntry? focusedEntry = null;
            foreach (var entry in entries)
            {
                if (ReferenceEquals(entry, focused))
                {
                    focusedEntry = entry;
                    continue;
                }

                list.SelectedItems.Add(entry);
            }

            if (focusedEntry is not null)
            {
                list.SelectedItems.Add(focusedEntry);
            }

            _RaiseSelectionChanged(list, Array.Empty<object>(), entries.Cast<object>().ToArray());
        }

        private static void _RaiseSelectionChanged(Control control, IList removed, IList added)
        {
            control.RaiseEvent(
                new SelectionChangedEventArgs(SelectingItemsControl.SelectionChangedEvent, removed, added)
            );
        }

        private static void _RaiseKeyDown(Control control, Key key, KeyModifiers modifiers = KeyModifiers.None)
        {
            control.RaiseEvent(
                new KeyEventArgs
                {
                    RoutedEvent = InputElement.KeyDownEvent,
                    Key = key,
                    KeyModifiers = modifiers,
                }
            );
        }

        private FileListViewModel _CreateThumbnailsViewModel(int folderCount)
        {
            var dir = _tempDirectoryFixture.CreateTempDir();
            Directory.CreateDirectory(Path.Combine(dir, ".config"));
            for (var i = 0; i < folderCount; i++)
            {
                Directory.CreateDirectory(Path.Combine(dir, $"folder{i:D3}"));
            }

            var viewModel = new FileListViewModel(NullSystemIconProvider.Instance, dir);
            _viewModels.Add(viewModel);
            viewModel.SetViewMode(FileListViewMode.Thumbnails);
            return viewModel;
        }

        private static (Window Window, ListBox List) _ShowThumbnails(FileListViewModel viewModel)
        {
            var view = new FileListView { DataContext = viewModel };
            var window = new Window
            {
                Width = 420,
                Height = 360,
                Content = view,
            };
            window.Show();
            window.UpdateLayout();

            var list = view.FindControl<ListBox>("ThumbnailsList");
            Assert.NotNull(list);
            Assert.True(list.IsVisible);
            return (window, list);
        }

        private static ListBoxItem _ThumbnailContainer(ListBox list, FileListEntry entry)
        {
            var index = list.Items.Cast<FileListEntry>().ToList().IndexOf(entry);
            Assert.True(index >= 0);
            var container = list.ContainerFromIndex(index) as ListBoxItem;
            Assert.NotNull(container);
            return container;
        }

        private static ContentPresenter _SelectionPresenter(ListBox list, FileListEntry entry)
        {
            var presenter = _ThumbnailContainer(list, entry)
                .GetVisualDescendants()
                .OfType<ContentPresenter>()
                .FirstOrDefault(item => item.Name == "PART_ContentPresenter");
            Assert.NotNull(presenter);
            return presenter;
        }

        private static Panel _ThumbnailSquare(ListBox list, FileListEntry entry)
        {
            var square = _ThumbnailContainer(list, entry)
                .GetVisualDescendants()
                .OfType<Panel>()
                .FirstOrDefault(item => item.Name == "ThumbnailSquare");
            Assert.NotNull(square);
            return square;
        }
    }
}
