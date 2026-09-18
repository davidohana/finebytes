using System.ComponentModel;
using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Mfr.App.Ui.Services;
using Mfr.App.Ui.Services.Help;
using Mfr.App.Ui.Services.Shell;
using Mfr.App.Ui.ViewModels.FileList;
using Mfr.App.Ui.ViewModels.FilterChainPane;
using Mfr.App.Ui.ViewModels.FilterEditors;
using Mfr.App.Ui.ViewModels.FilterPalette;
using Mfr.App.Ui.ViewModels.RenameList;
using Mfr.Engine.Config;
using Mfr.Engine.Presets;
using Mfr.Engine.RenameLog;
using Mfr.Engine.RenameScript;
using Mfr.Models.Config;
using Mfr.Models.Rename;
using Mfr.Utils;

namespace Mfr.App.Ui.ViewModels.MainWindow
{
    /// <summary>
    /// Root view model for the main window shell (menus, toolbar, status, pane hosts).
    /// </summary>
    public partial class MainWindowViewModel : ViewModelBase
    {
        private readonly HelpHost _helpHost;
        private bool _previewDirty;
        private bool _previewRunning;
        private Task _previewDrainTask = Task.CompletedTask;

        /// <summary>
        /// Initializes pane view models for the 7.4 layout.
        /// </summary>
        /// <param name="initialFileListPath">
        /// Optional File List start path (e.g. remembered last folder). When null, the File List uses its default.
        /// </param>
        /// <param name="persistSession">
        /// When <see langword="true"/>, restore child panes from <see cref="ConfigStore"/> and persist
        /// session sections on close / Options. When <see langword="false"/>, panes keep first-launch
        /// defaults and this window does not write session into <c>config.json</c>.
        /// </param>
        /// <param name="filterDefaults">
        /// Per-type filter add defaults. When null, uses an empty store that does not read AppData
        /// (production passes <see cref="FilterDefaultsStore.FromConfigStore"/>).
        /// </param>
        /// <param name="presetManager">
        /// Named presets store. When null, uses an empty manager that does not read AppData
        /// (production passes <see cref="PresetManager.OpenDefault"/>).
        /// </param>
        /// <param name="helpHost">
        /// Opens Index / Tips Help HTML. When null, uses a default <see cref="HelpHost"/>.
        /// </param>
        /// <param name="shellOpener">
        /// Shared shell opener for File List and Rename List, or <see langword="null"/> for the OS default.
        /// Tests pass <see cref="NullFileShellOpener"/> so export/reveal does not open Explorer.
        /// </param>
        public MainWindowViewModel(
            string? initialFileListPath = null,
            bool persistSession = false,
            FilterDefaultsStore? filterDefaults = null,
            PresetManager? presetManager = null,
            HelpHost? helpHost = null,
            IFileShellOpener? shellOpener = null
        )
        {
            PersistSession = persistSession;
            _helpHost = helpHost ?? new HelpHost();
            FilterChainViewModel = new FilterChainViewModel(
                filterDefaults ?? FilterDefaultsStore.CreateEmpty(),
                presetManager ?? PresetManager.CreateEmpty()
            );
            FileListViewModel = new FileListViewModel(
                iconProvider: null,
                initialPath: initialFileListPath,
                shellOpener: shellOpener,
                deferInitialListing: persistSession
            );
            RenameListViewModel = new RenameListViewModel(
                FileListViewModel,
                shellOpener: shellOpener,
                filterChain: FilterChainViewModel
            );
            FilterChainViewModel.SetRenameListColumnSource(
                RenameListViewModel.CaptureVisibleColumnSpecs,
                RenameListViewModel.ApplyVisibleColumnSpecs
            );
            FilterEditorViewModel = new FilterEditorViewModel();
            FilterEditorViewModel.ApplySession(persistSession);
            FilterEditorViewModel.SetSampleRenameItemSource(
                () => [.. RenameListViewModel.Entries.Select(entry => entry.EngineItem)],
                fullPath =>
                    RenameListViewModel
                        .Entries.Select(entry => entry.EngineItem)
                        .FirstOrDefault(item => PathComparers.Os.Equals(item.Original.FullPath, fullPath))
            );
            if (persistSession)
            {
                FileListViewModel.ApplySession(ConfigStore.FileList);
                RenameListViewModel.ApplySessionSection(ConfigStore.RenameList);
            }

            RenameListViewModel.PropertyChanged += _OnRenameListPropertyChanged;
            RenameListViewModel.MembershipChanged += _OnPreviewInputsChanged;
            RenameListViewModel.OriginalsRefreshed += _OnPreviewInputsChanged;
            RenameListViewModel.ManualOverridesChanged += _OnPreviewInputsChanged;
            FilterChainViewModel.PropertyChanged += _OnFilterChainPropertyChanged;
            FilterChainViewModel.FilterOptionsApplied += _OnFilterOptionsApplied;
            FilterChainViewModel.ChainChanged += _OnPreviewInputsChanged;
            FileListViewModel.PropertyChanged += _OnFileListPropertyChanged;
            FilterPaletteViewModel.PropertyChanged += _OnFilterPalettePropertyChanged;
            // File List may set LastStatusMessage during construction (e.g. remembered folder fallback)
            // before this handler was wired — seed the bar once.
            _ApplyStatusHintIfPresent(FileListViewModel.LastStatusMessage);
            ItemCount = RenameListViewModel.ItemCount;
            FilterCount = FilterChainViewModel.Count;
            ChangeCount = RenameListViewModel.ChangeCount;
            PreviewErrorCount = RenameListViewModel.PreviewErrorCount;
            WindowTitle = $"{AppProductInfo.GetProductName()} {AppProductInfo.GetDisplayVersion()}";
        }

        /// <summary>
        /// When <see langword="true"/>, this window restores and persists <see cref="ConfigStore"/> session sections.
        /// </summary>
        internal bool PersistSession { get; }

        /// <summary>
        /// Gets the main window title, including the product version.
        /// </summary>
        public string WindowTitle { get; }

        /// <summary>
        /// Gets the File List pane.
        /// </summary>
        public FileListViewModel FileListViewModel { get; }

        /// <summary>
        /// Gets the Available Filters pane.
        /// </summary>
        public FilterPaletteViewModel FilterPaletteViewModel { get; } = new FilterPaletteViewModel();

        /// <summary>
        /// Gets the Filter Chain pane.
        /// </summary>
        public FilterChainViewModel FilterChainViewModel { get; }

        /// <summary>
        /// Gets the Filter Configuration pane.
        /// </summary>
        public FilterEditorViewModel FilterEditorViewModel { get; }

        /// <summary>
        /// Gets the Rename List pane.
        /// </summary>
        public RenameListViewModel RenameListViewModel { get; }

        /// <summary>
        /// Status-bar hint content. Plain text or a rich Rename List cell hint.
        /// </summary>
        [ObservableProperty]
        private StyledTextDisplay _statusHint = StyledTextDisplay.Empty;

        /// <summary>
        /// Count of items in the rename list.
        /// </summary>
        [ObservableProperty]
        private int _itemCount;

        /// <summary>
        /// Count of Filter Chain.
        /// </summary>
        [ObservableProperty]
        private int _filterCount;

        /// <summary>
        /// Count of items whose preview name differs from the original.
        /// </summary>
        [ObservableProperty]
        private int _changeCount;

        /// <summary>
        /// Count of items with a preview error.
        /// </summary>
        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(HasPreviewErrors))]
        private int _previewErrorCount;

        /// <summary>
        /// Gets whether the Preview Errors count should use the error brush.
        /// </summary>
        public bool HasPreviewErrors => PreviewErrorCount > 0;

        /// <summary>
        /// Refreshes original Rename List fields when that grid has focus; otherwise reloads the File List.
        /// </summary>
        [RelayCommand]
        public async Task RefreshFocusedPaneAsync()
        {
            if (RenameListViewModel.IsGridFocused)
            {
                // Auto-Preview after membership changes holds IsBusy via the shared progress runner;
                // wait so F5 is not skipped while that pass is still finishing.
                await WaitForPendingPreviewAsync().ConfigureAwait(true);
                if (RenameListViewModel.RefreshCommand.CanExecute(null))
                {
                    await RenameListViewModel.RefreshCommand.ExecuteAsync(null).ConfigureAwait(true);
                }

                return;
            }

            FileListViewModel.RefreshCommand.Execute(null);
        }

        /// <summary>
        /// Appends the selected Available Filters row to the Filter Chain.
        /// </summary>
        [RelayCommand(CanExecute = nameof(_CanAddSelectedFilterFromPalette))]
        public void AddSelectedFilterFromPalette()
        {
            var entry = FilterPaletteViewModel.SelectedFilter;
            if (entry is null)
            {
                return;
            }

            FilterChainViewModel.AppendCommand.Execute(entry);
        }

        /// <summary>
        /// Previews the live filter chain and applies valid rename changes.
        /// </summary>
        /// <remarks>
        /// Reloads the File List after commit so renamed/moved names appear without a manual refresh.
        /// </remarks>
        [RelayCommand(CanExecute = nameof(_CanGo))]
        public async Task GoAsync()
        {
            await WaitForPendingPreviewAsync().ConfigureAwait(true);
            if (!_CanGo())
            {
                return;
            }

            var commitStarted = await RenameListViewModel.GoAsync(FilterChainViewModel.ToChain()).ConfigureAwait(true);
            if (!commitStarted)
            {
                return;
            }

            UndoLastCommand.NotifyCanExecuteChanged();
            await _RefreshFileListAfterRenameCommitAsync().ConfigureAwait(true);
        }

        /// <summary>
        /// Prepares an undo session from the last GO (preview only; user presses GO to apply).
        /// </summary>
        /// <remarks>
        /// Does not refresh the File List — disk is unchanged until a later GO.
        /// </remarks>
        [RelayCommand(CanExecute = nameof(_CanUndoLast))]
        public async Task UndoLastAsync()
        {
            await WaitForPendingPreviewAsync().ConfigureAwait(true);
            if (RenameListViewModel.IsBusy)
            {
                return;
            }

            await RenameListViewModel.PrepareUndoLastAsync().ConfigureAwait(true);
            UndoLastCommand.NotifyCanExecuteChanged();
        }

        /// <summary>
        /// Prepares an undo session from a rename log chosen in the Rename Log dialog (same confirm as Undo Last).
        /// </summary>
        /// <param name="log">Log selected in the dialog (last op or disk).</param>
        internal async Task UndoFromLogAsync(RenameLog log)
        {
            ArgumentNullException.ThrowIfNull(log);

            await WaitForPendingPreviewAsync().ConfigureAwait(true);
            if (RenameListViewModel.IsBusy)
            {
                return;
            }

            await RenameListViewModel.PrepareUndoAsync(log).ConfigureAwait(true);
            UndoLastCommand.NotifyCanExecuteChanged();
        }

        /// <summary>
        /// Reloads the File List after GO and re-previews when Auto-Preview is on.
        /// </summary>
        private async Task _RefreshFileListAfterRenameCommitAsync()
        {
            FileListViewModel.Refresh();

            if (!RenameListViewModel.IsAutoPreview)
            {
                return;
            }

            _RequestPreview();
            await WaitForPendingPreviewAsync().ConfigureAwait(true);
        }

        /// <summary>
        /// Opens the Rename Log dialog (disk history + last operation).
        /// </summary>
        [RelayCommand]
        public void ShowLog()
        {
            LogRequested?.Invoke(this, EventArgs.Empty);
        }

        /// <summary>
        /// Opens the Options dialog (remember flags and preset confirm).
        /// </summary>
        [RelayCommand]
        public void ShowOptions()
        {
            OptionsRequested?.Invoke(this, EventArgs.Empty);
        }

        /// <summary>
        /// Opens Help Index (<c>help/index.html</c>) in the default browser.
        /// </summary>
        [RelayCommand]
        public void ShowHelp()
        {
            _OpenHelpFile("index.html");
        }

        /// <summary>
        /// Opens Tips (<c>help/guide/tips.html</c>) in the default browser.
        /// </summary>
        [RelayCommand]
        public void ShowTips()
        {
            _OpenHelpFile("tips.html");
        }

        /// <summary>
        /// Opens the About dialog.
        /// </summary>
        [RelayCommand]
        public void ShowAbout()
        {
            AboutRequested?.Invoke(this, EventArgs.Empty);
        }

        /// <summary>
        /// When <see langword="true"/>, closing this window does not write session into <c>config.json</c>
        /// (Reset Configuration after deleting persisted files).
        /// </summary>
        internal bool SuppressSessionSaveOnClose { get; set; }

        /// <summary>
        /// Raised when the user chooses Tools → Options; the main window hosts the dialog.
        /// </summary>
        internal event EventHandler? OptionsRequested;

        /// <summary>
        /// Raised when Help Index or Tips HTML is missing; the main window shows the missing-help dialog.
        /// </summary>
        internal event EventHandler<string>? HelpMissing;

        /// <summary>
        /// Raised when the user chooses Help → About; the main window hosts the dialog.
        /// </summary>
        internal event EventHandler? AboutRequested;

        /// <summary>
        /// Raised when the user chooses MFR → Log; the main window hosts the dialog.
        /// </summary>
        internal event EventHandler? LogRequested;

        /// <summary>
        /// Raised when the user chooses Tools → Reset Configuration; the main window confirms and restarts.
        /// </summary>
        internal event EventHandler? ResetConfigurationRequested;

        /// <summary>
        /// Requests Reset Configuration (confirm / delete AppData / restart) via the main window.
        /// </summary>
        [RelayCommand]
        public void ResetConfiguration()
        {
            ResetConfigurationRequested?.Invoke(this, EventArgs.Empty);
        }

        /// <summary>
        /// Confirm when the Filter Chain has filters that cannot be fully emitted in a rename script
        /// (wired by the main window; set in tests). Argument is unsupported step display names.
        /// </summary>
        /// <remarks>
        /// <para>
        /// When null while confirmation is required, Generate Rename Script aborts after the empty gate.
        /// </para>
        /// </remarks>
        internal Func<IReadOnlyList<string>, Task<bool>>? ConfirmRenameScriptUnsupportedFiltersAsync { get; set; }

        /// <summary>
        /// Info dialog when there are no path/attribute changes to put in a rename script
        /// (wired by the main window; set in tests).
        /// </summary>
        internal Func<Task>? ShowRenameScriptEmptyAsync { get; set; }

        /// <summary>
        /// Exports pending path and RAHS attribute changes as a <c>.bat</c> or <c>.ps1</c> script.
        /// </summary>
        /// <remarks>
        /// <para>
        /// Gates empty Collect before the save dialog (OK dialog + status). Warns when enabled Filter Chain
        /// steps write dates or tags (not emitted). Uses Rename List
        /// <see cref="RenameListViewModel.UiHooks"/> <c>PickSavePathAsync</c>. Format follows the chosen
        /// extension. Outcomes land on <see cref="RenameListViewModel.LastStatusMessage"/>.
        /// </para>
        /// </remarks>
        [RelayCommand(CanExecute = nameof(_CanGo))]
        public async Task GenerateRenameScriptAsync()
        {
            if (!_CanGo())
            {
                return;
            }

            var renameList = RenameListViewModel;
            if (renameList.CountRenameScriptItems() == 0)
            {
                await _NotifyRenameScriptEmptyAsync(renameList).ConfigureAwait(true);
                return;
            }

            var unsupportedNames = RenameScriptFilterSupport.GetUnsupportedEnabledStepNames(
                FilterChainViewModel.Steps.Select(step => (step.Enabled, step.Filter, step.DisplayName))
            );
            if (
                unsupportedNames.Count > 0
                && ConfirmationPolicy.ShouldConfirm(ConfirmationKind.GenerateRenameScriptUnsupportedFilters)
            )
            {
                var confirm = ConfirmRenameScriptUnsupportedFiltersAsync;
                if (confirm is null)
                {
                    return;
                }

                var accepted = await confirm(unsupportedNames).ConfigureAwait(true);
                if (!accepted)
                {
                    renameList.LastStatusMessage = StatusBarText.Neutral("Generate Rename Script cancelled.");
                    return;
                }
            }

            var pick = renameList.UiHooks?.PickSavePathAsync;
            if (pick is null)
            {
                return;
            }

            var path = await pick(_RenameScriptSaveOptions).ConfigureAwait(true);
            if (string.IsNullOrWhiteSpace(path))
            {
                renameList.LastStatusMessage = StatusBarText.Neutral("Generate Rename Script cancelled.");
                return;
            }

            if (!_TryInferRenameScriptFormat(path, out var format))
            {
                renameList.LastStatusMessage = StatusBarText.Error("Choose a .bat or .ps1 file.");
                return;
            }

            if (renameList.IsBusy)
            {
                return;
            }

            try
            {
                var count = renameList.ExportRenameScript(path, format);
                if (count == 0)
                {
                    await _NotifyRenameScriptEmptyAsync(renameList).ConfigureAwait(true);
                    return;
                }

                renameList.LastStatusMessage = StatusBarText.Neutral($"Generated rename script for {count} item(s).");
                renameList.RevealExportedPath(path);
            }
            catch (Exception ex)
            {
                renameList.LastStatusMessage = StatusBarText.Error($"Failed to generate rename script: {ex.Message}");
            }
        }

        /// <summary>
        /// Status + optional OK dialog when Collect finds no path/RAHS ops.
        /// </summary>
        private async Task _NotifyRenameScriptEmptyAsync(RenameListViewModel renameList)
        {
            renameList.LastStatusMessage = StatusBarText.Warning("No scriptable changes to export.");
            if (ShowRenameScriptEmptyAsync is { } show)
            {
                await show().ConfigureAwait(true);
            }
        }

        private static readonly SaveFilePickOptions _RenameScriptSaveOptions = new()
        {
            Title = "Generate Rename Script",
            DefaultExtension = "bat",
            SuggestedFileName = "rename",
            FileTypes =
            [
                new SaveFilePickType("Batch files", ["*.bat"]),
                new SaveFilePickType("PowerShell scripts", ["*.ps1"]),
                new SaveFilePickType("All files", ["*.*"]),
            ],
        };

        /// <summary>
        /// Maps a save path extension to bat or PowerShell.
        /// </summary>
        private static bool _TryInferRenameScriptFormat(string path, out RenameScriptFormat format)
        {
            var extension = Path.GetExtension(path);
            if (extension.Equals(".bat", StringComparison.OrdinalIgnoreCase))
            {
                format = RenameScriptFormat.Bat;
                return true;
            }

            if (extension.Equals(".ps1", StringComparison.OrdinalIgnoreCase))
            {
                format = RenameScriptFormat.PowerShell;
                return true;
            }

            format = default;
            return false;
        }

        /// <summary>
        /// Shuts down the application.
        /// </summary>
        [RelayCommand]
        public void Exit()
        {
            if (Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
            {
                desktop.Shutdown();
            }
        }

        private void _OnRenameListPropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName is nameof(RenameListViewModel.ItemCount))
            {
                ItemCount = RenameListViewModel.ItemCount;
                GoCommand.NotifyCanExecuteChanged();
                GenerateRenameScriptCommand.NotifyCanExecuteChanged();
            }

            if (e.PropertyName is nameof(RenameListViewModel.ChangeCount))
            {
                ChangeCount = RenameListViewModel.ChangeCount;
            }

            if (e.PropertyName is nameof(RenameListViewModel.PreviewErrorCount))
            {
                PreviewErrorCount = RenameListViewModel.PreviewErrorCount;
            }

            if (e.PropertyName is nameof(RenameListViewModel.IsAutoPreview) && RenameListViewModel.IsAutoPreview)
            {
                _RequestPreview();
            }

            if (e.PropertyName is nameof(RenameListViewModel.IsBusy) && !RenameListViewModel.IsBusy && _previewDirty)
            {
                _RequestPreview();
            }

            if (e.PropertyName is nameof(RenameListViewModel.IsBusy))
            {
                GoCommand.NotifyCanExecuteChanged();
                UndoLastCommand.NotifyCanExecuteChanged();
                GenerateRenameScriptCommand.NotifyCanExecuteChanged();
            }

            if (e.PropertyName is nameof(RenameListViewModel.LastStatusMessage))
            {
                // Empty is applied so Clear can wipe a prior GO/add message.
                StatusHint = RenameListViewModel.LastStatusMessage;
            }

            if (e.PropertyName is nameof(RenameListViewModel.CellStatusHint))
            {
                // Last write wins — empty clears the bar (no restore of a prior operation message).
                StatusHint = RenameListViewModel.CellStatusHint;
            }
        }

        private void _OnFilterChainPropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName is nameof(FilterChainViewModel.Count))
            {
                FilterCount = FilterChainViewModel.Count;
            }

            if (e.PropertyName is nameof(FilterChainViewModel.SelectedSteps))
            {
                FilterEditorViewModel.SyncSelection(FilterChainViewModel.SelectedSteps);
            }

            if (e.PropertyName is nameof(FilterChainViewModel.LastStatusMessage))
            {
                _ApplyStatusHintIfPresent(FilterChainViewModel.LastStatusMessage);
            }
        }

        private void _OnFileListPropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName is nameof(FileListViewModel.LastStatusMessage))
            {
                _ApplyStatusHintIfPresent(FileListViewModel.LastStatusMessage);
            }
        }

        private void _OnFilterOptionsApplied(object? sender, EventArgs e)
        {
            FilterEditorViewModel.SyncSelection(FilterChainViewModel.SelectedSteps);
        }

        /// <summary>
        /// Re-runs Rename List preview when the filter chain, list membership, or originals refresh.
        /// </summary>
        private void _OnPreviewInputsChanged(object? sender, EventArgs e)
        {
            if (RenameListViewModel.ArePreviewInputsSuspended)
            {
                return;
            }

            _RequestPreview();
            FilterEditorViewModel.RefreshTrimHelperRenameItems();
        }

        /// <summary>
        /// Queues a preview pass when Auto-Preview is on (coalesces overlapping requests).
        /// </summary>
        private void _RequestPreview()
        {
            if (!RenameListViewModel.IsAutoPreview)
            {
                return;
            }

            _previewDirty = true;
            if (_previewRunning)
            {
                return;
            }

            _previewDrainTask = _DrainPreviewAsync();
        }

        /// <summary>
        /// Waits for any in-flight Auto-Preview drain started by this window (tests).
        /// </summary>
        /// <returns>A task that completes when the current drain finishes.</returns>
        internal Task WaitForPendingPreviewAsync()
        {
            return _previewDrainTask;
        }

        /// <summary>
        /// Applies the live Filter Chain until the queue is idle or Auto-Preview turns off.
        /// </summary>
        private async Task _DrainPreviewAsync()
        {
            if (_previewRunning)
            {
                return;
            }

            _previewRunning = true;
            try
            {
                while (_previewDirty && RenameListViewModel.IsAutoPreview)
                {
                    if (RenameListViewModel.IsBusy)
                    {
                        break;
                    }

                    _previewDirty = false;
                    await RenameListViewModel.PreviewAsync(FilterChainViewModel.ToChain()).ConfigureAwait(true);
                }
            }
            finally
            {
                _previewRunning = false;
            }
        }

        private void _OnFilterPalettePropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName is nameof(FilterPaletteViewModel.SelectedFilter))
            {
                AddSelectedFilterFromPaletteCommand.NotifyCanExecuteChanged();
            }
        }

        private bool _CanAddSelectedFilterFromPalette()
        {
            return FilterPaletteViewModel.SelectedFilter is not null;
        }

        /// <summary>
        /// Whether GO / Generate Rename Script may run (non-empty idle Rename List).
        /// </summary>
        private bool _CanGo()
        {
            return RenameListViewModel.ItemCount >= 1 && !RenameListViewModel.IsBusy;
        }

        private bool _CanUndoLast()
        {
            var last = RenameLogStore.LastOperation;
            return last is { HasUndoableEntries: true } && !RenameListViewModel.IsBusy;
        }

        /// <summary>
        /// Applies a non-empty pane status to the status-bar hint (last write wins).
        /// </summary>
        /// <param name="message">Status published by Filter Chain or File List.</param>
        /// <remarks>
        /// Empty is ignored so producers can reset their property without wiping the bar.
        /// Rename List assigns <see cref="StatusHint"/> directly (including Empty) so Clear can wipe.
        /// </remarks>
        private void _ApplyStatusHintIfPresent(StyledTextDisplay message)
        {
            if (message.IsEmpty)
            {
                return;
            }

            StatusHint = message;
        }

        /// <summary>
        /// Opens <paramref name="helpFileName"/> via the help host, or raises <see cref="HelpMissing"/>.
        /// </summary>
        /// <param name="helpFileName">Help HTML basename under the app <c>help/</c> folder.</param>
        private void _OpenHelpFile(string helpFileName)
        {
            if (_helpHost.TryOpen(helpFileName, out _))
            {
                return;
            }

            HelpMissing?.Invoke(this, helpFileName);
        }
    }
}
