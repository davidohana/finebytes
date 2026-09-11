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
    /// Modal Preset Manager: load, delete, and rename named presets.
    /// <para>
    /// Closes with <see langword="true"/> after a successful Load; <see langword="false"/> when closed
    /// without loading.
    /// </para>
    /// </summary>
    public partial class PresetManagerDialog : Window
    {
        private readonly AppliedFiltersViewModel? _appliedFilters;
        private readonly Func<Task>? _importSamplesAsync;
        private readonly Func<FilterPreset, Task<bool>>? _tryLoadAsync;

        /// <summary>
        /// Initializes an empty dialog (designer / XAML loader).
        /// </summary>
        public PresetManagerDialog()
        {
            InitializeComponent();
            ModalDialogKeyboard.Attach(this);
        }

        /// <summary>
        /// Initializes the dialog with list state and the shared host load path.
        /// </summary>
        /// <param name="viewModel">Sorted preset list and selection.</param>
        /// <param name="appliedFilters">Pane that mutates presets and the Applied Filters chain.</param>
        /// <param name="tryLoadAsync">
        /// Shared load path (confirm → replace → optional columns). Returns <see langword="true"/> on success.
        /// </param>
        /// <param name="importSamplesAsync">
        /// Optional import-dialog host override for headless tests.
        /// </param>
        public PresetManagerDialog(
            PresetManagerDialogViewModel viewModel,
            AppliedFiltersViewModel appliedFilters,
            Func<FilterPreset, Task<bool>> tryLoadAsync,
            Func<Task>? importSamplesAsync = null
        )
            : this()
        {
            ArgumentNullException.ThrowIfNull(viewModel);
            ArgumentNullException.ThrowIfNull(appliedFilters);
            ArgumentNullException.ThrowIfNull(tryLoadAsync);

            DataContext = viewModel;
            _appliedFilters = appliedFilters;
            _tryLoadAsync = tryLoadAsync;
            _importSamplesAsync = importSamplesAsync ?? _ShowImportSamplesAsync;
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

        private async void _OnImportSamplesClick(object? sender, RoutedEventArgs e)
        {
            if (_importSamplesAsync is not null)
            {
                await _importSamplesAsync();
            }
        }

        /// <summary>
        /// Shows the sample checklist, imports the accepted names, refreshes the manager, and reports counts.
        /// </summary>
        /// <returns>A task that completes after the import flow closes.</returns>
        private async Task _ShowImportSamplesAsync()
        {
            if (_ViewModel is null || _appliedFilters is null)
            {
                return;
            }

            var dialogViewModel = new ImportSamplePresetsDialogViewModel();
            var accepted = await new ImportSamplePresetsDialog(dialogViewModel).ShowDialog<bool?>(this);
            if (accepted != true || !dialogViewModel.CanImport)
            {
                return;
            }

            try
            {
                var (AddedCount, SkippedCount) = _appliedFilters.ImportSamplePresets(dialogViewModel.SelectedNames());
                _ViewModel.Refresh();
                await new OkMessageDialog(
                    "Import Sample Presets",
                    $"Added {AddedCount}. Skipped {SkippedCount} (name already exists)."
                ).ShowDialog(this);
            }
            catch (Exception ex)
            {
                await _ShowErrorAsync("Import Sample Presets", ex);
            }
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
        /// Runs the shared host load path for the selection and closes OK on success.
        /// </summary>
        /// <returns>A task that completes when load UI finishes (success, cancel, or error dialog).</returns>
        private async Task _TryLoadSelectedAsync()
        {
            if (_ViewModel?.SelectedPreset is not { } preset || _tryLoadAsync is null)
            {
                return;
            }

            if (!await _tryLoadAsync(preset))
            {
                return;
            }

            Close(true);
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
                        _ViewModel.Refresh(preferredName: rename.Preset?.Name);
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
