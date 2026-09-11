using Avalonia.Controls;
using Avalonia.Interactivity;
using Mfr.App.Ui.ViewModels.Presets;
using Mfr.App.Ui.Views.Presets;
using Mfr.Models.Config;

namespace Mfr.App.Ui.Views.AppliedFilters
{
    public partial class AppliedFiltersView
    {
        private void _WirePresetHandlers()
        {
            PresetsButton.Click += _OnPresetsClick;
            SavePresetButton.Click += _OnSavePresetClick;
            SavePresetAsButton.Click += _OnSavePresetAsClick;
        }

        private async void _OnPresetsClick(object? sender, RoutedEventArgs e)
        {
            await ShowPresetManagerAsync();
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
        /// Opens the Preset Manager (Load / Delete / Edit Description / Rename).
        /// </summary>
        /// <returns>A task that completes when the dialog closes.</returns>
        public async Task ShowPresetManagerAsync()
        {
            if (_viewModel is null)
            {
                return;
            }

            if (TopLevel.GetTopLevel(this) is not Window owner)
            {
                return;
            }

            var dialogVm = new PresetManagerDialogViewModel(_viewModel);
            var dialog = new PresetManagerDialog(
                dialogVm,
                _viewModel,
                confirmReplaceAsync: () => _TryConfirmReplaceAsync(owner),
                applyColumns: preset => PresetRenameListColumns.ApplyIfPresent(this, preset.VisibleColumns)
            );
            await dialog.ShowDialog<bool?>(owner);
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

            _viewModel.SavePreset(PresetRenameListColumns.Capture(this));
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

            var columns = dialogVm.SaveRenameListColumns ? PresetRenameListColumns.Capture(this) : null;
            _viewModel.SavePresetAs(name, dialogVm.TrimmedDescriptionOrNull, columns);
        }

        /// <summary>
        /// Confirms replacing a non-empty Applied Filters chain when the config flag is enabled.
        /// </summary>
        /// <param name="owner">Owner window for the confirm dialog.</param>
        /// <returns>
        /// <see langword="true"/> when load may proceed; <see langword="false"/> when the user cancels.
        /// </returns>
        private async Task<bool> _TryConfirmReplaceAsync(Window owner)
        {
            if (_viewModel is null)
            {
                return false;
            }

            if (!_viewModel.NeedsConfirmReplaceOnLoad(ConfigStore.Config.Ui.Presets.ConfirmReplaceAppliedFiltersOnLoad))
            {
                return true;
            }

            var confirm = new ConfirmMessageDialog(
                "Replace Applied Filters",
                "Loading this preset will replace the current Applied Filters list. Continue?"
            );
            return await confirm.ShowDialog<bool?>(owner) == true;
        }
    }
}
