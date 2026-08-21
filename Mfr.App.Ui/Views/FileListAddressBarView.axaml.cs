using System.Collections.ObjectModel;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Threading;
using Avalonia.VisualTree;
using Mfr.App.Ui.ViewModels;

namespace Mfr.App.Ui.Views
{
    /// <summary>
    /// File List address bar: breadcrumbs, path edit, history, Up, and Refresh.
    /// </summary>
    public partial class FileListAddressBarView : UserControl
    {
        private bool _isHistoryOpen;

        /// <summary>
        /// Initializes the address bar.
        /// </summary>
        public FileListAddressBarView()
        {
            InitializeComponent();
            PathEditBox.PropertyChanged += _OnPathEditBoxPropertyChanged;
            BreadcrumbItems.LayoutUpdated += _OnBreadcrumbLayoutUpdated;
        }

        /// <summary>
        /// Gets ancestor folders hidden from the address bar when it is too narrow.
        /// </summary>
        public ObservableCollection<PathBreadcrumbSegment> OverflowBreadcrumbSegments { get; } = [];

        private void _OnPathEditBoxPropertyChanged(object? sender, AvaloniaPropertyChangedEventArgs e)
        {
            if (e.Property != IsVisibleProperty || !PathEditBox.IsVisible)
            {
                return;
            }

            _FocusAndSelectPathEdit();
        }

        private void _FocusAndSelectPathEdit()
        {
            Dispatcher.UIThread.Post(
                () =>
                {
                    if (!PathEditBox.IsVisible)
                    {
                        return;
                    }

                    PathEditBox.Focus();
                    PathEditBox.SelectAll();
                },
                DispatcherPriority.Input
            );
        }

        private void _OnAddressBarPointerPressed(object? sender, PointerPressedEventArgs e)
        {
            if (DataContext is not FileListViewModel viewModel || viewModel.IsPathEditing)
            {
                return;
            }

            if (e.Source is Visual visual && visual.FindAncestorOfType<Button>(includeSelf: true) is not null)
            {
                return;
            }

            viewModel.BeginPathEdit();
            e.Handled = true;
        }

        private void _OnHistoryClick(object? sender, RoutedEventArgs e)
        {
            if (DataContext is FileListViewModel viewModel)
            {
                viewModel.BeginPathEdit();
            }
        }

        private void _OnHistoryOpened(object? sender, EventArgs e)
        {
            _isHistoryOpen = true;
        }

        private void _OnHistoryClosed(object? sender, EventArgs e)
        {
            _isHistoryOpen = false;
            Dispatcher.UIThread.Post(_CommitPathIfAddressBarInactive, DispatcherPriority.Input);
        }

        private void _OnHistoryTapped(object? sender, TappedEventArgs e)
        {
            var path = _HistoryPathFromTap(e.Source);
            if (path is null || DataContext is not FileListViewModel viewModel)
            {
                return;
            }

            viewModel.NavigateTo(path);
            HistoryButton.Flyout?.Hide();
        }

        private static string? _HistoryPathFromTap(object? source)
        {
            if (source is StyledElement { DataContext: string path })
            {
                return path;
            }

            if (source is Visual visual && visual.FindAncestorOfType<ListBoxItem>()?.DataContext is string itemPath)
            {
                return itemPath;
            }

            return null;
        }

        private void _OnBreadcrumbLayoutUpdated(object? sender, EventArgs e)
        {
            _SyncBreadcrumbOverflow();
        }

        private void _SyncBreadcrumbOverflow()
        {
            if (BreadcrumbItems.ItemsPanelRoot is not BreadcrumbTrailPanel trail)
            {
                return;
            }

            OverflowButton.IsVisible = trail.HasOverflow;
            var hiddenSegments = _HiddenBreadcrumbSegments(trail.VisibleStartIndex);
            if (_SameOverflowSegments(hiddenSegments))
            {
                return;
            }

            OverflowBreadcrumbSegments.Clear();
            foreach (var segment in hiddenSegments)
            {
                OverflowBreadcrumbSegments.Add(segment);
            }
        }

        private List<PathBreadcrumbSegment> _HiddenBreadcrumbSegments(int visibleStartIndex)
        {
            if (DataContext is not FileListViewModel viewModel || visibleStartIndex <= 0)
            {
                return [];
            }

            var hiddenCount = Math.Min(visibleStartIndex, viewModel.BreadcrumbSegments.Count);
            return [.. viewModel.BreadcrumbSegments.Take(hiddenCount)];
        }

        private bool _SameOverflowSegments(List<PathBreadcrumbSegment> hiddenSegments)
        {
            if (hiddenSegments.Count != OverflowBreadcrumbSegments.Count)
            {
                return false;
            }

            for (var i = 0; i < hiddenSegments.Count; i++)
            {
                if (hiddenSegments[i].TargetPath != OverflowBreadcrumbSegments[i].TargetPath)
                {
                    return false;
                }
            }

            return true;
        }

        private void _OnOverflowTapped(object? sender, TappedEventArgs e)
        {
            var segment = _OverflowSegmentFromTap(e.Source);
            if (segment is null || DataContext is not FileListViewModel viewModel)
            {
                return;
            }

            viewModel.NavigateTo(segment.TargetPath);
            OverflowButton.Flyout?.Hide();
        }

        private static PathBreadcrumbSegment? _OverflowSegmentFromTap(object? source)
        {
            if (source is StyledElement { DataContext: PathBreadcrumbSegment segment })
            {
                return segment;
            }

            if (
                source is Visual visual
                && visual.FindAncestorOfType<ListBoxItem>()?.DataContext is PathBreadcrumbSegment item
            )
            {
                return item;
            }

            return null;
        }

        private void _OnPathKeyDown(object? sender, KeyEventArgs e)
        {
            if (DataContext is not FileListViewModel viewModel)
            {
                return;
            }

            if (e.Key == Key.Escape)
            {
                viewModel.CancelPathEdit();
                e.Handled = true;
                return;
            }

            if (e.Key != Key.Enter)
            {
                return;
            }

            viewModel.CommitPath();
            e.Handled = true;
        }

        private void _OnPathLostFocus(object? sender, RoutedEventArgs e)
        {
            Dispatcher.UIThread.Post(_CommitPathIfAddressBarInactive, DispatcherPriority.Input);
        }

        private void _CommitPathIfAddressBarInactive()
        {
            if (DataContext is not FileListViewModel viewModel || !viewModel.IsPathEditing)
            {
                return;
            }

            if (_isHistoryOpen || AddressBar.IsPointerOver || PathEditBox.IsFocused)
            {
                return;
            }

            viewModel.CommitPath();
        }
    }
}
