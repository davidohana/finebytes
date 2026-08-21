using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Presenters;
using Avalonia.Headless.XUnit;
using Avalonia.VisualTree;
using Mfr.App.Ui.Services.FileList;
using Mfr.App.Ui.ViewModels;
using Mfr.App.Ui.Views;

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
