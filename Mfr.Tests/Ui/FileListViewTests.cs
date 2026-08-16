using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Presenters;
using Avalonia.Headless.XUnit;
using Avalonia.VisualTree;
using Mfr.App.Ui.Services;
using Mfr.App.Ui.ViewModels;
using Mfr.App.Ui.Views;

namespace Mfr.Tests.Ui
{
    /// <summary>
    /// Headless layout tests for the File Explorer pane.
    /// </summary>
    public sealed class FileListViewTests : IDisposable
    {
        private readonly TempDirectoryFixture _tempDirectoryFixture = new();
        private readonly List<FileListViewModel> _viewModels = [];

        /// <inheritdoc />
        public void Dispose()
        {
            foreach (var viewModel in _viewModels)
                viewModel.Dispose();

            _tempDirectoryFixture.Dispose();
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
                $"Selection width {presenter.Bounds.Width} should cover thumbnail size {viewModel.ThumbnailSize}.");
            Assert.Equal(viewModel.ThumbnailCellWidth, presenter.Bounds.Width, precision: 0);
            Assert.Equal(viewModel.ThumbnailCellHeight, presenter.Bounds.Height, precision: 0);
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
                Directory.CreateDirectory(Path.Combine(dir, $"folder{i:D3}"));

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

        private static ContentPresenter _SelectionPresenter(ListBox list, FileListEntry entry)
        {
            var index = list.Items.Cast<FileListEntry>().ToList().IndexOf(entry);
            Assert.True(index >= 0);
            var container = list.ContainerFromIndex(index) as ListBoxItem;
            Assert.NotNull(container);

            var presenter = container.GetVisualDescendants()
                .OfType<ContentPresenter>()
                .FirstOrDefault(item => item.Name == "PART_ContentPresenter");
            Assert.NotNull(presenter);
            return presenter;
        }
    }
}
