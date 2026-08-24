using System.Collections;
using System.ComponentModel;
using Avalonia.Controls;
using Mfr.App.Ui.ViewModels.RenameList;

namespace Mfr.App.Ui.Views.RenameList
{
    /// <summary>
    /// Rename List pane host.
    /// </summary>
    public partial class RenameListView : UserControl
    {
        private RenameListViewModel? _viewModel;
        private bool _isSyncingSelection;
        private bool _selectionChangeFromView;
        private AddProgressDialog? _addProgressDialog;

        /// <summary>
        /// Initializes the Rename List pane.
        /// </summary>
        public RenameListView()
        {
            InitializeComponent();
            DataContextChanged += _OnDataContextChanged;
            RenameGrid.SelectionChanged += _OnSelectionChanged;
        }

        private void _OnDataContextChanged(object? sender, EventArgs e)
        {
            _viewModel?.PropertyChanged -= _OnViewModelPropertyChanged;

            _viewModel = DataContext as RenameListViewModel;
            if (_viewModel is null)
            {
                return;
            }

            _viewModel.PropertyChanged += _OnViewModelPropertyChanged;
            _SyncSelectionToGrid();
        }

        private void _OnSelectionChanged(object? sender, SelectionChangedEventArgs e)
        {
            if (_isSyncingSelection || _viewModel is null)
            {
                return;
            }

            _selectionChangeFromView = true;
            try
            {
                var selected = _ReadSelectedEntries();
                _viewModel.SetSelectedEntries(selected);
            }
            finally
            {
                _selectionChangeFromView = false;
            }
        }

        private void _OnViewModelPropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            if (_selectionChangeFromView)
            {
                return;
            }

            if (e.PropertyName is nameof(RenameListViewModel.SelectedEntries))
            {
                _SyncSelectionToGrid();
                return;
            }

            if (e.PropertyName is nameof(RenameListViewModel.IsAddProgressVisible))
            {
                _ = _SyncAddProgressDialogAsync();
            }
        }

        private async Task _SyncAddProgressDialogAsync()
        {
            if (_viewModel is null)
            {
                return;
            }

            if (!_viewModel.IsAddProgressVisible)
            {
                _addProgressDialog?.Close();
                return;
            }

            if (_addProgressDialog is not null)
            {
                return;
            }

            if (TopLevel.GetTopLevel(this) is not Window owner)
            {
                return;
            }

            var dialog = new AddProgressDialog(_viewModel);
            _addProgressDialog = dialog;
            try
            {
                await dialog.ShowDialog(owner);
            }
            finally
            {
                if (ReferenceEquals(_addProgressDialog, dialog))
                {
                    _addProgressDialog = null;
                }
            }
        }

        private void _SyncSelectionToGrid()
        {
            if (_isSyncingSelection || _viewModel is null)
            {
                return;
            }

            if (RenameGrid.SelectedItems is not IList selectedItems)
            {
                return;
            }

            if (_SelectionMatches(selectedItems, _viewModel.SelectedEntries))
            {
                return;
            }

            _isSyncingSelection = true;
            try
            {
                selectedItems.Clear();
                foreach (var entry in _viewModel.SelectedEntries)
                {
                    selectedItems.Add(entry);
                }
            }
            finally
            {
                _isSyncingSelection = false;
            }
        }

        private IReadOnlyList<RenameListEntry> _ReadSelectedEntries()
        {
            if (RenameGrid.SelectedItems is not IList items)
            {
                return [];
            }

            return [.. items.OfType<RenameListEntry>()];
        }

        private static bool _SelectionMatches(IList selectedItems, IReadOnlyList<RenameListEntry> expected)
        {
            if (selectedItems.Count != expected.Count)
            {
                return false;
            }

            for (var i = 0; i < expected.Count; i++)
            {
                if (!ReferenceEquals(selectedItems[i], expected[i]))
                {
                    return false;
                }
            }

            return true;
        }
    }
}
