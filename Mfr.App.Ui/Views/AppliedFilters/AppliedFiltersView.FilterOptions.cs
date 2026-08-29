using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Mfr.App.Ui.ViewModels.AppliedFilters;

namespace Mfr.App.Ui.Views.AppliedFilters
{
    public partial class AppliedFiltersView
    {
        private void _WireFilterOptionsHandlers()
        {
            FilterOptionsButton.Click += _OnFilterOptionsClick;
            AppliedFiltersList.DoubleTapped += _OnListDoubleTapped;
        }

        private async void _OnFilterOptionsClick(object? sender, RoutedEventArgs e)
        {
            await ShowFilterOptionsAsync();
        }

        private async void _OnListDoubleTapped(object? sender, TappedEventArgs e)
        {
            await ShowFilterOptionsAsync();
        }

        /// <summary>
        /// Opens Filter Options for the single selected applied-filter row.
        /// </summary>
        /// <returns>A task that completes when the dialog closes.</returns>
        public async Task ShowFilterOptionsAsync()
        {
            if (_viewModel is null || !_viewModel.CanShowFilterOptions)
            {
                return;
            }

            if (TopLevel.GetTopLevel(this) is not Window owner)
            {
                return;
            }

            var step = _viewModel.SelectedSteps[0];
            var dialogVm = new FilterOptionsDialogViewModel(step);
            var dialog = new FilterOptionsDialog(dialogVm);
            var accepted = await dialog.ShowDialog<bool?>(owner);
            if (accepted == true)
            {
                _viewModel.ApplyFilterOptions(dialogVm);
            }
        }
    }
}
