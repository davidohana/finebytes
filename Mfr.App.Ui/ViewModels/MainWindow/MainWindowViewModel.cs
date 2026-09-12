using System.ComponentModel;
using System.Reflection;
using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Mfr.App.Ui.Services.Session;
using Mfr.App.Ui.ViewModels.AppliedFilters;
using Mfr.App.Ui.ViewModels.FileList;
using Mfr.App.Ui.ViewModels.FilterEditors;
using Mfr.App.Ui.ViewModels.FilterPalette;
using Mfr.App.Ui.ViewModels.RenameList;
using Mfr.Engine.Presets;
using Mfr.Models.Config;
using Mfr.Utils;

namespace Mfr.App.Ui.ViewModels.MainWindow
{
    /// <summary>
    /// Root view model for the main window shell (menus, toolbar, status, pane hosts).
    /// </summary>
    public partial class MainWindowViewModel : ViewModelBase
    {
        private StyledTextDisplay _lastStatusHint = StyledTextDisplay.Empty;
        private StyledTextDisplay _paneStatusHint = StyledTextDisplay.Empty;
        private bool _previewDirty;
        private bool _previewRunning;
        private Task _previewDrainTask = Task.CompletedTask;

        /// <summary>
        /// Initializes pane view models for the 7.4 layout.
        /// </summary>
        /// <param name="initialFileListPath">
        /// Optional File List start path (e.g. remembered last folder). When null, the File List uses its default.
        /// </param>
        /// <param name="session">
        /// Loaded session to restore onto child panes and persist from this window. When null, panes keep
        /// first-launch defaults and this window does not write <c>session.json</c>.
        /// </param>
        /// <param name="filterDefaults">
        /// Per-type filter add defaults. When null, uses an empty store that does not read AppData
        /// (production passes <see cref="FilterDefaultsStore.OpenDefault"/>).
        /// </param>
        /// <param name="presetManager">
        /// Named presets store. When null, uses an empty manager that does not read AppData
        /// (production passes <see cref="PresetManager.OpenDefault"/>).
        /// </param>
        /// <param name="sessionFilePath">
        /// Path used when the main window writes <c>session.json</c> on close. When null, close-save uses
        /// <see cref="SessionStore.DefaultFilePath"/>. Production passes the same path used for load.
        /// </param>
        public MainWindowViewModel(
            string? initialFileListPath = null,
            SessionState? session = null,
            FilterDefaultsStore? filterDefaults = null,
            PresetManager? presetManager = null,
            string? sessionFilePath = null
        )
        {
            Session = session;
            SessionFilePath = sessionFilePath;
            AppliedFiltersViewModel = new AppliedFiltersViewModel(
                filterDefaults ?? FilterDefaultsStore.CreateEmpty(),
                presetManager ?? PresetManager.CreateEmpty()
            );
            FileListViewModel = new FileListViewModel(iconProvider: null, initialPath: initialFileListPath);
            RenameListViewModel = new RenameListViewModel(FileListViewModel, appliedFilters: AppliedFiltersViewModel);
            AppliedFiltersViewModel.SetRenameListColumnSource(
                RenameListViewModel.CaptureVisibleColumnSpecs,
                RenameListViewModel.ApplyVisibleColumnSpecs
            );
            FilterEditorViewModel = new FilterEditorViewModel();
            FilterEditorViewModel.ApplySession(session);
            FilterEditorViewModel.SetSampleRenameItemSource(
                () => [.. RenameListViewModel.Entries.Select(entry => entry.EngineItem)],
                fullPath =>
                    RenameListViewModel
                        .Entries.Select(entry => entry.EngineItem)
                        .FirstOrDefault(item => PathComparers.Os.Equals(item.Original.FullPath, fullPath))
            );
            if (session is not null)
            {
                FileListViewModel.ApplySession(FileListSessionSnapshot.FromSessionState(session));
                RenameListViewModel.ApplySessionSection(session.RenameList);
            }

            RenameListViewModel.PropertyChanged += _OnRenameListPropertyChanged;
            RenameListViewModel.MembershipChanged += _OnPreviewInputsChanged;
            RenameListViewModel.OriginalsRefreshed += _OnPreviewInputsChanged;
            RenameListViewModel.ManualOverridesChanged += _OnPreviewInputsChanged;
            AppliedFiltersViewModel.PropertyChanged += _OnAppliedFiltersPropertyChanged;
            AppliedFiltersViewModel.FilterOptionsApplied += _OnFilterOptionsApplied;
            AppliedFiltersViewModel.ChainChanged += _OnPreviewInputsChanged;
            FilterPaletteViewModel.PropertyChanged += _OnFilterPalettePropertyChanged;
            ItemCount = RenameListViewModel.ItemCount;
            FilterCount = AppliedFiltersViewModel.Count;
            ChangeCount = RenameListViewModel.ChangeCount;
            PreviewErrorCount = RenameListViewModel.PreviewErrorCount;
            WindowTitle = $"Magic File Renamer {_GetDisplayVersion()}";
        }

        /// <summary>
        /// Loaded session document for this window, or <see langword="null"/> when the window was created without one.
        /// </summary>
        internal SessionState? Session { get; }

        /// <summary>
        /// Session JSON path for close save, or <see langword="null"/> to use the default AppData file.
        /// </summary>
        internal string? SessionFilePath { get; }

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
        /// Gets the Applied Filters pane.
        /// </summary>
        public AppliedFiltersViewModel AppliedFiltersViewModel { get; }

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
        /// Count of applied filters.
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
        /// Appends the selected Available Filters row to the Applied list.
        /// </summary>
        [RelayCommand(CanExecute = nameof(_CanAddSelectedFilterFromPalette))]
        public void AddSelectedFilterFromPalette()
        {
            var entry = FilterPaletteViewModel.SelectedFilter;
            if (entry is null)
            {
                return;
            }

            AppliedFiltersViewModel.AppendCommand.Execute(entry);
        }

        /// <summary>
        /// Previews the live filter chain and applies valid rename changes.
        /// </summary>
        [RelayCommand(CanExecute = nameof(_CanGo))]
        public async Task GoAsync()
        {
            await WaitForPendingPreviewAsync().ConfigureAwait(true);
            if (!_CanGo())
            {
                return;
            }

            var commitStarted = await RenameListViewModel
                .GoAsync(AppliedFiltersViewModel.ToChain())
                .ConfigureAwait(true);
            if (!commitStarted || !RenameListViewModel.IsAutoPreview)
            {
                return;
            }

            _RequestPreview();
            await WaitForPendingPreviewAsync().ConfigureAwait(true);
        }

        /// <summary>
        /// Undoes the last GO. Placeholder until undo is implemented.
        /// </summary>
        [RelayCommand(CanExecute = nameof(_CanExecuteUnimplemented))]
        public void UndoLast() { }

        /// <summary>
        /// Opens the log window. Placeholder until the log is implemented.
        /// </summary>
        [RelayCommand(CanExecute = nameof(_CanExecuteUnimplemented))]
        public void ShowLog() { }

        /// <summary>
        /// Opens Options. Placeholder until the options window is implemented.
        /// </summary>
        [RelayCommand(CanExecute = nameof(_CanExecuteUnimplemented))]
        public void ShowOptions() { }

        /// <summary>
        /// When <see langword="true"/>, closing this window does not write <c>session.json</c>
        /// (Reset Configuration after deleting persisted files).
        /// </summary>
        internal bool SuppressSessionSaveOnClose { get; set; }

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
            }

            if (
                e.PropertyName is nameof(RenameListViewModel.LastStatusMessage)
                && !RenameListViewModel.LastStatusMessage.IsEmpty
            )
            {
                _ShowStickyStatusHint(RenameListViewModel.LastStatusMessage);
            }

            if (e.PropertyName is nameof(RenameListViewModel.CellStatusHint))
            {
                _paneStatusHint = RenameListViewModel.CellStatusHint;
                _UpdateStatusHint();
            }
        }

        private void _OnAppliedFiltersPropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName is nameof(AppliedFiltersViewModel.Count))
            {
                FilterCount = AppliedFiltersViewModel.Count;
            }

            if (e.PropertyName is nameof(AppliedFiltersViewModel.SelectedSteps))
            {
                FilterEditorViewModel.SyncSelection(AppliedFiltersViewModel.SelectedSteps);
            }
        }

        private void _OnFilterOptionsApplied(object? sender, EventArgs e)
        {
            FilterEditorViewModel.SyncSelection(AppliedFiltersViewModel.SelectedSteps);
        }

        /// <summary>
        /// Re-runs Rename List preview when the filter chain, list membership, or originals refresh.
        /// </summary>
        private void _OnPreviewInputsChanged(object? sender, EventArgs e)
        {
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
        /// Applies the live Applied Filters chain until the queue is idle or Auto-Preview turns off.
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
                    await RenameListViewModel.PreviewAsync(AppliedFiltersViewModel.ToChain()).ConfigureAwait(true);
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

        private bool _CanGo()
        {
            return RenameListViewModel.ItemCount >= 1 && !RenameListViewModel.IsBusy;
        }

        /// <summary>
        /// Stores a sticky status-bar message until another high-signal outcome replaces it.
        /// </summary>
        private void _ShowStickyStatusHint(StyledTextDisplay message)
        {
            _lastStatusHint = message;
            _UpdateStatusHint();
        }

        private void _UpdateStatusHint()
        {
            StatusHint = !_paneStatusHint.IsEmpty ? _paneStatusHint : _lastStatusHint;
        }

        private static bool _CanExecuteUnimplemented()
        {
            return false;
        }

        private static string _GetDisplayVersion()
        {
            var informational = typeof(MainWindowViewModel)
                .Assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>()
                ?.InformationalVersion;
            if (!string.IsNullOrWhiteSpace(informational))
            {
                return informational;
            }

            return typeof(MainWindowViewModel).Assembly.GetName().Version?.ToString(3) ?? "unknown";
        }
    }
}
