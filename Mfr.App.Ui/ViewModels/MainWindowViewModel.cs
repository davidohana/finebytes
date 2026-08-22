using System.Reflection;
using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Mfr.App.Ui.ViewModels.FileList;
using Mfr.App.Ui.ViewModels.FilterPalette;
using Mfr.App.Ui.ViewModels.RenameList;

namespace Mfr.App.Ui.ViewModels
{
    /// <summary>
    /// Root view model for the main window shell (menus, toolbar, status, pane hosts).
    /// </summary>
    public partial class MainWindowViewModel : ViewModelBase
    {
        /// <summary>
        /// Initializes pane view models for the 7.4 layout.
        /// </summary>
        /// <param name="initialFileListPath">
        /// Optional File List start path (e.g. remembered last folder). When null, the File List uses its default.
        /// </param>
        public MainWindowViewModel(string? initialFileListPath = null)
        {
            FileListViewModel = new FileListViewModel(iconProvider: null, initialPath: initialFileListPath);
            RenameListViewModel = new RenameListViewModel(FileListViewModel);
            WindowTitle = $"Magic File Renamer {_GetDisplayVersion()}";
        }

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
        /// Status-bar hover hint. Empty until panes publish hints.
        /// </summary>
        [ObservableProperty]
        private string _statusHint = string.Empty;

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
