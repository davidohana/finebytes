using Avalonia.Controls;
using Avalonia.Threading;
using Mfr.App.Ui.ViewModels.Presets;
using Mfr.App.Ui.Views.DragAndDrop;
using Mfr.Models.Filters;

namespace Mfr.App.Ui.Views.Presets
{
    public partial class PresetManagerDialog
    {
        private bool _isSyncingSelection;
        private PresetManagerDialogViewModel? _selectionViewModel;

        private void _WireSelectionHandlers()
        {
            PresetsList.SelectionChanged += (_, _) => _OnListSelectionChanged();
            Loaded += (_, _) => _QueueRestoreSelectionFromViewModel();
            DataContextChanged += (_, _) =>
            {
                if (DataContext is PresetManagerDialogViewModel viewModel)
                {
                    _OnDataContextAttached(viewModel);
                }
            };
        }

        private void _OnDataContextAttached(PresetManagerDialogViewModel viewModel)
        {
            if (ReferenceEquals(_selectionViewModel, viewModel))
            {
                return;
            }

            _selectionViewModel?.PropertyChanged -= _OnViewModelPropertyChanged;

            _selectionViewModel = viewModel;
            _selectionViewModel.PropertyChanged += _OnViewModelPropertyChanged;
            _QueueRestoreSelectionFromViewModel();
        }

        private void _OnViewModelPropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            if (e.PropertyName is nameof(PresetManagerDialogViewModel.SelectedPresets))
            {
                _QueueRestoreSelectionFromViewModel();
            }
        }

        private void _QueueRestoreSelectionFromViewModel()
        {
            if (_selectionViewModel is null)
            {
                return;
            }

            Dispatcher.UIThread.Post(_RestoreSelectionFromViewModel, DispatcherPriority.Loaded);
        }

        private void _OnListSelectionChanged()
        {
            if (_selectionViewModel is null || _isSyncingSelection)
            {
                return;
            }

            _selectionViewModel.SetSelectedPresets(_ReadSelectedPresets(PresetsList));
        }

        private void _RestoreSelectionFromViewModel()
        {
            if (_selectionViewModel is null)
            {
                return;
            }

            var indices = _selectionViewModel
                .SelectedPresets.Select(_selectionViewModel.Presets.IndexOf)
                .Where(index => index >= 0)
                .OrderBy(index => index)
                .ToList();
            var anchorIndex = indices.Count > 0 ? indices[0] : -1;
            _RestoreListSelection(PresetsList, indices, anchorIndex);
        }

        private static IReadOnlyList<FilterPreset> _ReadSelectedPresets(ListBox listBox)
        {
            return
            [
                .. ListBoxDrag
                    .ReadSelectedIndices(listBox)
                    .Where(index => index < listBox.ItemCount)
                    .Select(index => (FilterPreset)listBox.Items[index]!),
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
