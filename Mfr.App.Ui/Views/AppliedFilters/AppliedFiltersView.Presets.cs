using Avalonia.Controls;
using Avalonia.Interactivity;
using Mfr.App.Ui.ViewModels.Presets;
using Mfr.App.Ui.Views.Presets;
using Mfr.Models;
using Mfr.Models.Config;
using Mfr.Models.Filters;

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
        /// Rebuilds the ▾ quick-pick menu with sorted preset names (or a disabled empty placeholder).
        /// </summary>
        /// <param name="sender">The <see cref="MenuFlyout"/> opening.</param>
        /// <param name="e">Event args.</param>
        private void _OnPresetsQuickPickOpening(object? sender, EventArgs e)
        {
            if (sender is not MenuFlyout flyout)
            {
                return;
            }

            flyout.Items.Clear();
            if (_viewModel is null)
            {
                _AddNoPresetsPlaceholder(flyout);
                return;
            }

            var presets = _viewModel
                .PresetManager.NameToPreset.Values.OrderBy(preset => preset.Name, StringComparer.OrdinalIgnoreCase)
                .ThenBy(preset => preset.Name, StringComparer.Ordinal)
                .ToList();
            if (presets.Count == 0)
            {
                _AddNoPresetsPlaceholder(flyout);
                return;
            }

            foreach (var preset in presets)
            {
                var item = new MenuItem { Header = preset.Name, Tag = preset };
                item.Click += _OnPresetsQuickPickItemClick;
                flyout.Items.Add(item);
            }
        }

        /// <summary>
        /// Loads the preset tagged on the clicked ▾ menu item (same path as Manager Load).
        /// </summary>
        /// <param name="sender">The clicked <see cref="MenuItem"/>.</param>
        /// <param name="e">Event args.</param>
        private async void _OnPresetsQuickPickItemClick(object? sender, RoutedEventArgs e)
        {
            if (sender is not MenuItem { Tag: FilterPreset preset })
            {
                return;
            }

            PresetsQuickPickButton.Flyout?.Hide();
            await TryLoadPresetAsync(preset);
        }

        /// <summary>
        /// Adds the disabled empty-state row used when no presets are available.
        /// </summary>
        /// <param name="flyout">Quick-pick flyout being rebuilt.</param>
        private static void _AddNoPresetsPlaceholder(MenuFlyout flyout)
        {
            flyout.Items.Add(new MenuItem { Header = "No presets", IsEnabled = false });
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
            var dialog = new PresetManagerDialog(dialogVm, _viewModel, tryLoadAsync: TryLoadPresetAsync);
            await dialog.ShowDialog<bool?>(owner);
        }

        /// <summary>
        /// Confirms replace when needed, loads <paramref name="preset"/>, and applies optional columns.
        /// <para>
        /// Shared by Preset Manager Load and the toolbar ▾ quick-pick. On failure shows an error dialog
        /// and returns <see langword="false"/>.
        /// </para>
        /// </summary>
        /// <param name="preset">Preset to load.</param>
        /// <returns>
        /// <see langword="true"/> when the preset was loaded; <see langword="false"/> when cancelled,
        /// unavailable, or load failed after the error dialog.
        /// </returns>
        public async Task<bool> TryLoadPresetAsync(FilterPreset preset)
        {
            ArgumentNullException.ThrowIfNull(preset);

            if (_viewModel is null)
            {
                return false;
            }

            if (TopLevel.GetTopLevel(this) is not Window owner)
            {
                return false;
            }

            if (!await _TryConfirmReplaceAsync(owner))
            {
                return false;
            }

            try
            {
                _viewModel.LoadPreset(preset);
                PresetRenameListColumns.ApplyIfPresent(this, preset.VisibleColumns);
                return true;
            }
            catch (Exception ex)
            {
                var message = ex is UserException userEx ? userEx.Message : ex.Message;
                await new OkMessageDialog("Load Preset", message).ShowDialog(owner);
                return false;
            }
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
