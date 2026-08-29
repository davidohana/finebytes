using Avalonia.Controls;
using Avalonia.Threading;
using Mfr.App.Ui.ViewModels.AppliedFilters;
using Mfr.App.Ui.Views.DragAndDrop;

namespace Mfr.App.Ui.Views.AppliedFilters
{
    public partial class AppliedFiltersView
    {
        private bool _isSyncingSelection;
        private AppliedFiltersViewModel? _viewModel;

        private void _WireSelectionHandlers()
        {
            AppliedFiltersList.SelectionChanged += (_, _) => _OnListSelectionChanged();
            Loaded += (_, _) => _QueueRestoreSelectionFromViewModel();
        }

        private void _OnDataContextAttached(AppliedFiltersViewModel viewModel)
        {
            if (ReferenceEquals(_viewModel, viewModel))
            {
                return;
            }

            _viewModel?.PropertyChanged -= _OnViewModelPropertyChanged;

            _viewModel = viewModel;
            _viewModel.PropertyChanged += _OnViewModelPropertyChanged;
            _QueueRestoreSelectionFromViewModel();
        }

        private void _OnViewModelPropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            if (e.PropertyName is nameof(AppliedFiltersViewModel.SelectedSteps))
            {
                _QueueRestoreSelectionFromViewModel();
            }
        }

        private void _QueueRestoreSelectionFromViewModel()
        {
            if (_viewModel is null)
            {
                return;
            }

            Dispatcher.UIThread.Post(_RestoreSelectionFromViewModel, DispatcherPriority.Loaded);
        }

        private void _OnListSelectionChanged()
        {
            if (_viewModel is null || _isSyncingSelection)
            {
                return;
            }

            if (_TryKeepMultiSelectionForDrag())
            {
                return;
            }

            _viewModel.SetSelectedSteps(_ReadSelectedSteps(AppliedFiltersList));
        }

        private bool _TryKeepMultiSelectionForDrag()
        {
            if (_dragSession.SelectionSnapshot is not { Count: > 0 } snapshot)
            {
                return false;
            }

            var anchor = _dragSession.HitIndex is int hit && snapshot.Contains(hit) ? hit : snapshot[^1];
            _RestoreListSelection(AppliedFiltersList, snapshot, anchor);
            return true;
        }

        private void _RestoreSelectionFromViewModel()
        {
            if (_viewModel is null)
            {
                return;
            }

            var indices = _viewModel
                .SelectedSteps.Select(_viewModel.Steps.IndexOf)
                .Where(index => index >= 0)
                .OrderBy(index => index)
                .ToList();
            var anchorIndex = indices.Count > 0 ? indices[0] : -1;
            _RestoreListSelection(AppliedFiltersList, indices, anchorIndex);
        }

        private static IReadOnlyList<AppliedFilterStepViewModel> _ReadSelectedSteps(ListBox listBox)
        {
            return
            [
                .. ListBoxDrag
                    .ReadSelectedIndices(listBox)
                    .Where(index => index < listBox.ItemCount)
                    .Select(index => (AppliedFilterStepViewModel)listBox.Items[index]!),
            ];
        }

        private void _RestoreListSelection(ListBox listBox, IReadOnlyList<int> indices, int anchorIndex)
        {
            var wasSyncing = _isSyncingSelection;
            _isSyncingSelection = true;
            try
            {
                ListBoxDrag.RestoreSelection(listBox, indices, anchorIndex);
            }
            finally
            {
                _isSyncingSelection = wasSyncing;
            }
        }
    }
}
