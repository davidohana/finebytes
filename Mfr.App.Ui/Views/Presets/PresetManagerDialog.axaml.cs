using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Mfr.App.Ui.ViewModels;
using Mfr.App.Ui.ViewModels.AppliedFilters;
using Mfr.App.Ui.ViewModels.Presets;
using Mfr.Models;
using Mfr.Models.Filters;

namespace Mfr.App.Ui.Views.Presets
{
    /// <summary>
    /// Modal Preset Manager: load, delete, edit description, and rename named presets.
    /// <para>
    /// Closes with <see langword="true"/> after a successful Load; <see langword="false"/> when closed
    /// without loading.
    /// </para>
    /// </summary>
    public partial class PresetManagerDialog : Window
    {
        private readonly AppliedFiltersViewModel? _appliedFilters;
        private readonly Func<Task<bool>>? _confirmReplaceAsync;
        private readonly Action<FilterPreset>? _applyColumns;

        /// <summary>
        /// Initializes an empty dialog (designer / XAML loader).
        /// </summary>
        public PresetManagerDialog()
        {
            InitializeComponent();
            ModalDialogKeyboard.Attach(this);
        }

        /// <summary>
        /// Initializes the dialog with list state and load-side host callbacks.
        /// </summary>
        /// <param name="viewModel">Sorted preset list and selection.</param>
        /// <param name="appliedFilters">Pane that mutates presets and the Applied Filters chain.</param>
        /// <param name="confirmReplaceAsync">
        /// Returns <see langword="true"/> when Load may replace the current chain (after optional confirm).
        /// </param>
        /// <param name="applyColumns">Applies optional Rename List columns after a successful load.</param>
        public PresetManagerDialog(
            PresetManagerDialogViewModel viewModel,
            AppliedFiltersViewModel appliedFilters,
            Func<Task<bool>> confirmReplaceAsync,
            Action<FilterPreset> applyColumns
        )
            : this()
        {
            ArgumentNullException.ThrowIfNull(viewModel);
            ArgumentNullException.ThrowIfNull(appliedFilters);
            ArgumentNullException.ThrowIfNull(confirmReplaceAsync);
            ArgumentNullException.ThrowIfNull(applyColumns);

            DataContext = viewModel;
            _appliedFilters = appliedFilters;
            _confirmReplaceAsync = confirmReplaceAsync;
            _applyColumns = applyColumns;
        }

        /// <inheritdoc />
        protected override void OnOpened(EventArgs e)
        {
            base.OnOpened(e);
            PresetsList.Focus();
        }

        private PresetManagerDialogViewModel? _ViewModel => DataContext as PresetManagerDialogViewModel;

        private async void _OnLoadClick(object? sender, RoutedEventArgs e)
        {
            await _TryLoadSelectedAsync();
        }

        private async void _OnPresetsListDoubleTapped(object? sender, TappedEventArgs e)
        {
            await _TryLoadSelectedAsync();
        }

        private async void _OnPresetsListKeyDown(object? sender, KeyEventArgs e)
        {
            if (e.Key != Key.Enter)
            {
                return;
            }

            e.Handled = true;
            await _TryLoadSelectedAsync();
        }

        /// <summary>
        /// Confirms replace when needed, loads the selection, applies columns, and closes OK.
        /// </summary>
        /// <returns>A task that completes when load UI finishes (success, cancel, or error dialog).</returns>
        private async Task _TryLoadSelectedAsync()
        {
            if (_ViewModel?.SelectedPreset is not { } preset || _appliedFilters is null)
            {
                return;
            }

            if (_confirmReplaceAsync is not null && !await _confirmReplaceAsync())
            {
                return;
            }

            try
            {
                _appliedFilters.LoadPreset(preset);
                _applyColumns?.Invoke(preset);
                Close(true);
            }
            catch (Exception ex)
            {
                await _ShowErrorAsync("Load Preset", ex);
            }
        }

        private async void _OnDeleteClick(object? sender, RoutedEventArgs e)
        {
            if (_ViewModel?.SelectedPreset is not { } preset || _appliedFilters is null)
            {
                return;
            }

            var confirm = new ConfirmMessageDialog(
                "Delete Preset",
                $"Delete preset '{preset.Name}'? This cannot be undone."
            );
            if (await confirm.ShowDialog<bool?>(this) != true)
            {
                return;
            }

            try
            {
                _appliedFilters.DeletePreset(preset.Name);
                _ViewModel.Refresh();
            }
            catch (Exception ex)
            {
                await _ShowErrorAsync("Delete Preset", ex);
            }
        }

        private async void _OnEditDescriptionClick(object? sender, RoutedEventArgs e)
        {
            if (_ViewModel?.SelectedPreset is not { } preset || _appliedFilters is null)
            {
                return;
            }

            var prompt = new TextInputPrompt
            {
                Title = "Edit Description",
                Prompt = StyledTextDisplay.FromPlain($"Description for '{preset.Name}':"),
                DefaultValue = preset.Description ?? string.Empty,
                Multiline = true,
            };
            var result = await new TextInputDialog(prompt).ShowDialog<string?>(this);
            if (result is null)
            {
                return;
            }

            try
            {
                _appliedFilters.SetPresetDescription(preset.Name, result);
                _ViewModel.Refresh();
            }
            catch (Exception ex)
            {
                await _ShowErrorAsync("Edit Description", ex);
            }
        }

        private async void _OnRenameClick(object? sender, RoutedEventArgs e)
        {
            if (_ViewModel?.SelectedPreset is not { } preset || _appliedFilters is null)
            {
                return;
            }

            var prompt = new TextInputPrompt
            {
                Title = "Rename Preset",
                Prompt = StyledTextDisplay.FromPlain("New preset name:"),
                DefaultValue = preset.Name,
            };
            var result = await new TextInputDialog(prompt).ShowDialog<string?>(this);
            if (result is null)
            {
                return;
            }

            try
            {
                var rename = _appliedFilters.RenamePreset(preset.Name, result);
                switch (rename.Status)
                {
                    case PresetRenameStatus.Unchanged:
                        return;
                    case PresetRenameStatus.BlankName:
                        await new OkMessageDialog("Rename Preset", "Preset name cannot be blank.").ShowDialog(this);
                        return;
                    case PresetRenameStatus.NameTaken:
                        await new OkMessageDialog(
                            "Rename Preset",
                            $"A preset named '{result.Trim()}' already exists."
                        ).ShowDialog(this);
                        return;
                    case PresetRenameStatus.NotFound:
                        await new OkMessageDialog(
                            "Rename Preset",
                            "The selected preset is no longer available."
                        ).ShowDialog(this);
                        _ViewModel.Refresh();
                        return;
                    case PresetRenameStatus.Success:
                        if (rename.Preset is not null)
                        {
                            _ViewModel.SelectedPreset = rename.Preset;
                        }

                        _ViewModel.Refresh();
                        return;
                    default:
                        return;
                }
            }
            catch (Exception ex)
            {
                await _ShowErrorAsync("Rename Preset", ex);
            }
        }

        private void _OnCloseClick(object? sender, RoutedEventArgs e)
        {
            Close(false);
        }

        /// <summary>
        /// Shows a modal error for a failed preset mutation (prefers <see cref="UserException"/> text).
        /// </summary>
        /// <param name="title">Dialog title (action name).</param>
        /// <param name="ex">Caught failure from load / persist.</param>
        /// <returns>A task that completes when the user dismisses the dialog.</returns>
        private async Task _ShowErrorAsync(string title, Exception ex)
        {
            var message = ex is UserException userEx ? userEx.Message : ex.Message;
            await new OkMessageDialog(title, message).ShowDialog(this);
        }
    }
}
