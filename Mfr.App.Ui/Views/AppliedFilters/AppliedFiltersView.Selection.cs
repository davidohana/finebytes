using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Threading;
using Avalonia.VisualTree;
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
            AppliedFiltersList.AddHandler(ContextRequestedEvent, _OnListContextRequested, RoutingStrategies.Tunnel);
            Loaded += (_, _) => _QueueRestoreSelectionFromViewModel();
        }

        private void _OnListContextRequested(object? sender, ContextRequestedEventArgs e)
        {
            if (_viewModel is null || e.Source is not Visual source)
            {
                return;
            }

            var item = source.FindAncestorOfType<ListBoxItem>() ?? source as ListBoxItem;
            if (item?.DataContext is not AppliedFilterStepViewModel hit)
            {
                return;
            }

            _SelectStepForContextMenu(hit);
        }

        /// <summary>
        /// Selects <paramref name="hit"/> for the context menu unless it is already selected
        /// (sole selection or part of a multi-selection).
        /// </summary>
        /// <param name="hit">Step under the pointer.</param>
        private void _SelectStepForContextMenu(AppliedFilterStepViewModel hit)
        {
            if (_viewModel is null)
            {
                return;
            }

            ContextMenuHitSelection.SelectHitIfNeeded(
                _viewModel.SelectedSteps,
                hit,
                () =>
                {
                    _viewModel.SetSelectedSteps([hit]);
                    _RestoreSelectionFromViewModel();
                }
            );
        }

        private void _OnDataContextAttached(AppliedFiltersViewModel viewModel)
        {
            if (ReferenceEquals(_viewModel, viewModel))
            {
                return;
            }

            if (_viewModel is not null)
            {
                _viewModel.PropertyChanged -= _OnViewModelPropertyChanged;
                _ClearUiHooks(_viewModel);
            }

            _viewModel = viewModel;
            _viewModel.PropertyChanged += _OnViewModelPropertyChanged;
            _WireUiHooks(_viewModel);
            _QueueRestoreSelectionFromViewModel();
        }

        private void _WireUiHooks(AppliedFiltersViewModel viewModel)
        {
            viewModel.UiHooks = new AppliedFiltersUiHooks { ConfirmClearAsync = _ConfirmClearAsync };
        }

        private static void _ClearUiHooks(AppliedFiltersViewModel viewModel)
        {
            viewModel.UiHooks = null;
        }

        private async Task<bool> _ConfirmClearAsync()
        {
            if (TopLevel.GetTopLevel(this) is not Window owner)
            {
                return false;
            }

            var dialog = new ConfirmMessageDialog(title: "Remove All Filters", message: "Remove all Applied Filters?");
            return await dialog.ShowDialog<bool>(owner).ConfigureAwait(true);
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
