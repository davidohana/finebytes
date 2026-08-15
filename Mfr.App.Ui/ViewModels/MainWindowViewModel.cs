using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

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
        public MainWindowViewModel()
        {
            FileList = new FileListViewModel();
            FilterPalette = new FilterPaletteViewModel();
            AppliedFilters = new AppliedFiltersViewModel();
            FilterEditor = new FilterEditorViewModel();
            RenameList = new RenameListViewModel();
        }

        /// <summary>
        /// Gets the File Explorer pane.
        /// </summary>
        public FileListViewModel FileList { get; }

        /// <summary>
        /// Gets the Available Filters pane.
        /// </summary>
        public FilterPaletteViewModel FilterPalette { get; }

        /// <summary>
        /// Gets the Applied Filters pane.
        /// </summary>
        public AppliedFiltersViewModel AppliedFilters { get; }

        /// <summary>
        /// Gets the Filter Configuration pane.
        /// </summary>
        public FilterEditorViewModel FilterEditor { get; }

        /// <summary>
        /// Gets the Rename List pane.
        /// </summary>
        public RenameListViewModel RenameList { get; }

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
        public void Go()
        {
        }

        /// <summary>
        /// Undoes the last GO. Placeholder until undo is implemented.
        /// </summary>
        [RelayCommand(CanExecute = nameof(_CanExecuteUnimplemented))]
        public void UndoLast()
        {
        }

        /// <summary>
        /// Opens the log window. Placeholder until the log is implemented.
        /// </summary>
        [RelayCommand(CanExecute = nameof(_CanExecuteUnimplemented))]
        public void ShowLog()
        {
        }

        /// <summary>
        /// Opens Options. Placeholder until the options window is implemented.
        /// </summary>
        [RelayCommand(CanExecute = nameof(_CanExecuteUnimplemented))]
        public void ShowOptions()
        {
        }

        /// <summary>
        /// Shuts down the application.
        /// </summary>
        [RelayCommand]
        public void Exit()
        {
            if (Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
                desktop.Shutdown();
        }

        private static bool _CanExecuteUnimplemented()
        {
            return false;
        }
    }
}
