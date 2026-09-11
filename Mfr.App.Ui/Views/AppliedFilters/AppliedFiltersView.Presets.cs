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
        /// <summary>
        /// Gets the ▾ quick-pick flyout (tests).
        /// </summary>
        internal MenuFlyout PresetsQuickPickFlyout { get; } = new() { Placement = PlacementMode.BottomEdgeAlignedLeft };

        private void _WirePresetHandlers()
        {
            PresetsButton.Click += _OnPresetsClick;
            PresetsQuickPickButton.Click += _OnPresetsQuickPickClick;
            SavePresetButton.Click += _OnSavePresetClick;
        }

        private async void _OnPresetsClick(object? sender, RoutedEventArgs e)
        {
            await ShowPresetManagerAsync();
        }

        private async void _OnSavePresetClick(object? sender, RoutedEventArgs e)
        {
            await ShowSavePresetAsync();
        }

        /// <summary>
        /// Rebuilds ▾ items then shows the menu (items must exist before the presenter is created).
        /// </summary>
        /// <param name="sender">The ▾ button.</param>
        /// <param name="e">Event args.</param>
        private void _OnPresetsQuickPickClick(object? sender, RoutedEventArgs e)
        {
            if (PresetsQuickPickFlyout.IsOpen)
            {
                PresetsQuickPickFlyout.Hide();
                return;
            }

            _RebuildPresetsQuickPick(PresetsQuickPickFlyout);
            PresetsQuickPickFlyout.ShowAt(PresetsQuickPickButton);
        }

        /// <summary>
        /// Rebuilds the ▾ quick-pick menu with sorted preset names (or a disabled empty placeholder).
        /// </summary>
        /// <param name="flyout">Quick-pick flyout to refill.</param>
        private void _RebuildPresetsQuickPick(MenuFlyout flyout)
        {
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

            PresetsQuickPickFlyout.Hide();
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
        /// Opens Save Preset (Update or Save as new), confirms overwrite when needed, then saves.
        /// </summary>
        /// <returns>A task that completes when the dialog flow finishes.</returns>
        public async Task ShowSavePresetAsync()
        {
            if (_viewModel is null)
            {
                return;
            }

            if (TopLevel.GetTopLevel(this) is not Window owner)
            {
                return;
            }

            var dialogVm = new SavePresetDialogViewModel(_viewModel.LastLoaded, canUpdate: _viewModel.CanSavePreset);
            var dialog = new SavePresetDialog(dialogVm);
            var choice = await dialog.ShowDialog<SavePresetDialogMode?>(owner);
            if (choice is null)
            {
                return;
            }

            var name = dialogVm.TrimmedName;
            if (name.Length == 0)
            {
                return;
            }

            var description = dialogVm.TrimmedDescriptionOrNull;
            var columns = dialogVm.SaveRenameListColumns ? PresetRenameListColumns.Capture(this) : null;

            if (choice == SavePresetDialogMode.Update)
            {
                if (!_viewModel.CanSavePreset)
                {
                    return;
                }

                var currentName = _viewModel.LastLoaded!.Name;
                var isRename = !string.Equals(currentName, name, StringComparison.Ordinal);
                if (isRename && _viewModel.PresetManager.NameToPreset.ContainsKey(name))
                {
                    await new OkMessageDialog("Rename Preset", $"A preset named '{name}' already exists.").ShowDialog(
                        owner
                    );
                    return;
                }

                _viewModel.SavePreset(name, description, columns);
                return;
            }

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

            _viewModel.SavePresetAs(name, description, columns);
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
