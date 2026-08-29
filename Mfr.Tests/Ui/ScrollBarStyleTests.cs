using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Headless.XUnit;
using Avalonia.Layout;
using Avalonia.Threading;
using Avalonia.VisualTree;
using Mfr.App.Ui.Services.FileList;
using Mfr.App.Ui.ViewModels;
using Mfr.App.Ui.ViewModels.AppliedFilters;
using Mfr.App.Ui.ViewModels.FileList;
using Mfr.App.Ui.Views.AppliedFilters;
using Mfr.App.Ui.Views.FileList;
using Mfr.App.Ui.Views.FilterPalette;
using Mfr.Tests.Ui.AppliedFilters;
using Mfr.Tests.Ui.RenameList;

namespace Mfr.Tests.Ui
{
    /// <summary>
    /// Headless tests for app-wide expanded (wide) scrollbar styling.
    /// </summary>
    public sealed class ScrollBarStyleTests : IDisposable
    {
        private readonly RenameListUiTestContext _renameListContext = new();

        /// <inheritdoc />
        public void Dispose()
        {
            _renameListContext.Dispose();
        }

        /// <summary>
        /// Verifies Filter Palette list scrollbars use expanded Fluent chrome, not overlay thumbs.
        /// </summary>
        [AvaloniaFact]
        public void FilterPalette_uses_expanded_vertical_scrollbar()
        {
            var (window, scrollBar) = _ShowFilterPaletteVerticalScrollBar();
            _AssertExpandedVerticalScrollBar(scrollBar);
            window.Close();
        }

        /// <summary>
        /// Verifies Applied Filters list scrollbars use expanded Fluent chrome.
        /// </summary>
        [AvaloniaFact]
        public void AppliedFilters_uses_expanded_vertical_scrollbar()
        {
            var viewModel = new AppliedFiltersViewModel();
            for (var index = 0; index < 30; index++)
            {
                viewModel.AddCommand.Execute(
                    AppliedFiltersTestUi.Entry(index % 2 == 0 ? "ShrinkSpaces" : "LettersCase")
                );
            }

            var view = new AppliedFiltersView { DataContext = viewModel };
            var window = new Window
            {
                Width = 280,
                Height = 120,
                Content = view,
            };
            window.Show();
            window.UpdateLayout();
            Dispatcher.UIThread.RunJobs();

            var list = view.FindControl<ListBox>("AppliedFiltersList");
            Assert.NotNull(list);
            var scrollBar = _FindVisibleVerticalScrollBar(list);
            _AssertExpandedVerticalScrollBar(scrollBar);
            window.Close();
        }

        /// <summary>
        /// Verifies File List report DataGrid scrollbars use expanded Fluent chrome.
        /// </summary>
        [AvaloniaFact]
        public void FileListReport_uses_expanded_vertical_scrollbar()
        {
            var viewModel = new FileListViewModel(
                NullSystemIconProvider.Instance,
                _CreateOverflowDir(),
                NullFileShellOpener.Instance
            );
            var view = new FileListView { DataContext = viewModel };
            var window = new Window
            {
                Width = 560,
                Height = 120,
                Content = view,
            };

            window.Show();
            window.UpdateLayout();
            Dispatcher.UIThread.RunJobs();

            var grid = view.FindControl<DataGrid>("ReportGrid");
            Assert.NotNull(grid);
            var scrollBar = _FindVisibleVerticalScrollBar(grid);
            _AssertExpandedVerticalScrollBar(scrollBar);

            window.Close();
        }

        /// <summary>
        /// Verifies Rename List DataGrid scrollbars use expanded Fluent chrome.
        /// </summary>
        [AvaloniaFact]
        public async Task RenameList_uses_expanded_vertical_scrollbar()
        {
            var (renameListViewModel, window, grid) = await _renameListContext.ShowWithRowsAsync(rowCount: 40);
            window.Height = 120;
            window.UpdateLayout();
            Dispatcher.UIThread.RunJobs();

            Assert.True(renameListViewModel.Entries.Count > 10);
            var scrollBar = _FindVisibleVerticalScrollBar(grid);
            _AssertExpandedVerticalScrollBar(scrollBar);

            window.Close();
        }

        private static (Window Window, ScrollBar ScrollBar) _ShowFilterPaletteVerticalScrollBar()
        {
            var mainViewModel = new MainWindowViewModel();
            var paletteView = new FilterPaletteView
            {
                DataContext = mainViewModel.FilterPaletteViewModel,
                AddSelectedToAppliedCommand = mainViewModel.AddSelectedFilterFromPaletteCommand,
                RemoveAppliedStepsCommand = mainViewModel.AppliedFiltersViewModel.RemoveStepsAtIndicesCommand,
            };

            var window = new Window
            {
                Width = 240,
                Height = 120,
                Content = paletteView,
            };

            window.Show();
            window.UpdateLayout();
            Dispatcher.UIThread.RunJobs();

            var list = paletteView.FindControl<ListBox>("FilterList");
            Assert.NotNull(list);
            return (window, _FindVisibleVerticalScrollBar(list));
        }

        private static ScrollBar _FindVisibleVerticalScrollBar(Control root)
        {
            return root.GetVisualDescendants()
                .OfType<ScrollBar>()
                .First(bar => bar.Orientation == Orientation.Vertical && bar.IsVisible);
        }

        private static void _AssertExpandedVerticalScrollBar(ScrollBar verticalScrollBar)
        {
            var scrollBarSize = (double)Application.Current!.FindResource("ScrollBarSize")!;

            Assert.False(verticalScrollBar.AllowAutoHide);
            Assert.True(verticalScrollBar.IsExpanded);
            Assert.Equal(scrollBarSize, verticalScrollBar.Bounds.Width, precision: 0);
        }

        private static string _CreateOverflowDir()
        {
            var dir = Path.Combine(Path.GetTempPath(), "mfr-scrollbar-test-" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(dir);
            for (var index = 0; index < 40; index++)
            {
                File.WriteAllText(Path.Combine(dir, $"file-{index:D2}.txt"), "x");
            }

            return dir;
        }
    }
}
