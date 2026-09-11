using Avalonia.Controls;
using Avalonia.Interactivity;
using Mfr.App.Ui.ViewModels;
using Mfr.App.Ui.ViewModels.Presets;
using Mfr.App.Ui.Views.Presets;
using Mfr.Models.Config;

namespace Mfr.App.Ui.Views.AppliedFilters
{
    public partial class AppliedFiltersView
    {
        private void _WirePresetHandlers()
        {
            SavePresetButton.Click += _OnSavePresetClick;
            SavePresetAsButton.Click += _OnSavePresetAsClick;
        }

        private void _OnSavePresetClick(object? sender, RoutedEventArgs e)
        {
            SavePreset();
        }

        private async void _OnSavePresetAsClick(object? sender, RoutedEventArgs e)
        {
            await ShowSavePresetAsAsync();
        }

        /// <summary>
        /// Saves the last-loaded preset in place when available (no dialog).
        /// </summary>
        public void SavePreset()
        {
            if (_viewModel is null || !_viewModel.CanSavePreset)
            {
                return;
            }

            _viewModel.SavePreset(_CaptureRenameListColumns());
        }

        /// <summary>
        /// Opens Save Preset As, confirms overwrite when the name exists, then upserts.
        /// </summary>
        /// <returns>A task that completes when the dialog flow finishes.</returns>
        public async Task ShowSavePresetAsAsync()
        {
            if (_viewModel is null)
            {
                return;
            }

            if (TopLevel.GetTopLevel(this) is not Window owner)
            {
                return;
            }

            var dialogVm = new SavePresetDialogViewModel(_viewModel.LastLoaded);
            var dialog = new SavePresetDialog(dialogVm);
            var accepted = await dialog.ShowDialog<bool?>(owner);
            if (accepted != true || !dialogVm.CanConfirm)
            {
                return;
            }

            var name = dialogVm.TrimmedName;
            if (_viewModel.PresetManager.NameToPreset.ContainsKey(name))
            {
                var confirm = new ConfirmMessageDialog(
                    "Overwrite Preset",
                    $"A preset named '{name}' already exists. Overwrite it?"
                );
                var overwrite = await confirm.ShowDialog<bool?>(owner);
                if (overwrite != true)
                {
                    return;
                }
            }

            var columns = dialogVm.SaveRenameListColumns ? _CaptureRenameListColumns() : null;
            _viewModel.SavePresetAs(name, dialogVm.TrimmedDescriptionOrNull, columns);
        }

        /// <summary>
        /// Captures Rename List visible columns from the owning main window, or an empty list when unavailable.
        /// </summary>
        /// <returns>Current visible columns for preset storage.</returns>
        private IReadOnlyList<SessionStateRenameListColumn> _CaptureRenameListColumns()
        {
            if (TopLevel.GetTopLevel(this) is Window { DataContext: MainWindowViewModel main })
            {
                return main.RenameListViewModel.CaptureVisibleColumnsForSession();
            }

            return [];
        }
    }
}
