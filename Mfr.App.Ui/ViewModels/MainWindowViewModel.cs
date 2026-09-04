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
using Mfr.Models.Config;

namespace Mfr.App.Ui.ViewModels
{
    /// <summary>
    /// Root view model for the main window shell (menus, toolbar, status, pane hosts).
    /// </summary>
    public partial class MainWindowViewModel : ViewModelBase
    {
        private const int StatusHintClearMilliseconds = 8_000;

        private CancellationTokenSource? _statusHintClearCts;
        private string _transientStatusHint = string.Empty;
        private StatusHintDisplay _paneStatusHintDisplay = StatusHintDisplay.Empty;
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
        public MainWindowViewModel(string? initialFileListPath = null, SessionState? session = null)
        {
            Session = session;
            FileListViewModel = new FileListViewModel(iconProvider: null, initialPath: initialFileListPath);
            RenameListViewModel = new RenameListViewModel(FileListViewModel);
            if (session is not null)
            {
                FileListViewModel.ApplySession(FileListSessionSnapshot.FromSessionState(session));
                RenameListViewModel.ApplySessionSection(session.RenameList);
            }

            RenameListViewModel.PropertyChanged += _OnRenameListPropertyChanged;
            RenameListViewModel.MembershipChanged += _OnPreviewInputsChanged;
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
        public AppliedFiltersViewModel AppliedFiltersViewModel { get; } = new AppliedFiltersViewModel();

        /// <summary>
        /// Gets the Filter Configuration pane.
        /// </summary>
        public FilterEditorViewModel FilterEditorViewModel { get; } = new FilterEditorViewModel();

        /// <summary>
        /// Gets the Rename List pane.
        /// </summary>
        public RenameListViewModel RenameListViewModel { get; }

        /// <summary>
        /// Status-bar hint content. Plain text or a rich Rename List cell hint.
        /// </summary>
        [ObservableProperty]
        private StatusHintDisplay _statusHintDisplay = StatusHintDisplay.Empty;

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
        private int _previewErrorCount;

        /// <summary>
        /// Refreshes original Rename List fields when that grid has focus; otherwise reloads the File List.
        /// </summary>
        [RelayCommand]
        public async Task RefreshFocusedPaneAsync()
        {
            if (RenameListViewModel.IsGridFocused)
            {
                // Auto-Preview after membership changes holds IsAdding via the shared progress runner;
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
        /// Applies pending rename changes. Disabled until preview/GO is implemented.
        /// </summary>
        [RelayCommand(CanExecute = nameof(_CanExecuteUnimplemented))]
        public void Go() { }

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
                _PreviewRenameList();
            }

            if (
                e.PropertyName is nameof(RenameListViewModel.IsAdding)
                && !RenameListViewModel.IsAdding
                && _previewDirty
            )
            {
                _RequestPreview();
            }

            if (
                e.PropertyName is nameof(RenameListViewModel.LastAddError)
                && !string.IsNullOrEmpty(RenameListViewModel.LastAddError)
            )
            {
                _ShowTransientStatusHint(RenameListViewModel.LastAddError);
            }

            if (
                e.PropertyName is nameof(RenameListViewModel.LastLocateError)
                && !string.IsNullOrEmpty(RenameListViewModel.LastLocateError)
            )
            {
                _ShowTransientStatusHint(RenameListViewModel.LastLocateError);
            }

            if (e.PropertyName is nameof(RenameListViewModel.CellStatusHintDisplay))
            {
                _paneStatusHintDisplay = RenameListViewModel.CellStatusHintDisplay;
                _UpdateStatusHintDisplay();
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
        /// Re-runs Rename List preview when the filter chain or list membership changes.
        /// </summary>
        private void _OnPreviewInputsChanged(object? sender, EventArgs e)
        {
            _RequestPreview();
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
                    if (RenameListViewModel.IsAdding)
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
                if (_previewDirty && RenameListViewModel.IsAutoPreview && !RenameListViewModel.IsAdding)
                {
                    _previewDrainTask = _DrainPreviewAsync();
                    await _previewDrainTask.ConfigureAwait(true);
                }
            }
        }

        /// <summary>
        /// Applies the live Applied Filters chain to the Rename List when Auto-Preview is on.
        /// </summary>
        private void _PreviewRenameList()
        {
            _RequestPreview();
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
        /// Shows a status-bar message and clears it after a short delay unless replaced sooner.
        /// </summary>
        private void _ShowTransientStatusHint(string message)
        {
            _statusHintClearCts?.Cancel();
            _statusHintClearCts?.Dispose();
            _transientStatusHint = message;
            _UpdateStatusHintDisplay();

            _statusHintClearCts = new CancellationTokenSource();
            var token = _statusHintClearCts.Token;
            _ = _ClearStatusHintAfterDelayAsync(message, token);
        }

        private void _UpdateStatusHintDisplay()
        {
            StatusHintDisplay = !string.IsNullOrEmpty(_transientStatusHint)
                ? StatusHintDisplay.FromPlain(_transientStatusHint)
                : _paneStatusHintDisplay;
        }

        private async Task _ClearStatusHintAfterDelayAsync(string message, CancellationToken token)
        {
            try
            {
                await Task.Delay(StatusHintClearMilliseconds, token).ConfigureAwait(true);
                if (string.Equals(_transientStatusHint, message, StringComparison.Ordinal))
                {
                    _transientStatusHint = string.Empty;
                    _UpdateStatusHintDisplay();
                }
            }
            catch (OperationCanceledException) { }
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
