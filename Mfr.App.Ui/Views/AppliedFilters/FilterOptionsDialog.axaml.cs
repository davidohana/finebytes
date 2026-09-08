using System.ComponentModel;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Threading;
using Mfr.App.Ui.ViewModels.AppliedFilters;

namespace Mfr.App.Ui.Views.AppliedFilters
{
    /// <summary>
    /// Modal dialog for applied-filter name and Apply-To targets.
    /// </summary>
    /// <remarks>
    /// Opens height-to-content, then locks height so only width remains resizable. Relocks when
    /// Apply-on scope panels show or hide.
    /// </remarks>
    public partial class FilterOptionsDialog : Window
    {
        private INotifyPropertyChanged? _heightRelockSource;

        /// <summary>
        /// Initializes the dialog (designer / default).
        /// </summary>
        public FilterOptionsDialog()
        {
            InitializeComponent();
            ModalDialogKeyboard.Attach(this);
            ModalDialogHorizontalResize.Attach(this);
            DataContextChanged += _OnDataContextChanged;
        }

        /// <summary>
        /// Initializes the dialog with a view model.
        /// </summary>
        /// <param name="viewModel">Draft name and Apply-To fields.</param>
        public FilterOptionsDialog(FilterOptionsDialogViewModel viewModel)
            : this()
        {
            DataContext = viewModel;
        }

        private void _OnDataContextChanged(object? sender, EventArgs e)
        {
            _heightRelockSource?.PropertyChanged -= _OnViewModelPropertyChanged;
            _heightRelockSource = DataContext as INotifyPropertyChanged;
            _heightRelockSource?.PropertyChanged += _OnViewModelPropertyChanged;
        }

        private void _OnViewModelPropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            if (
                e.PropertyName
                is not (
                    nameof(FilterOptionsDialogViewModel.ShowSubstringOptions)
                    or nameof(FilterOptionsDialogViewModel.ShowTokenOptions)
                    or nameof(FilterOptionsDialogViewModel.HasId3v2MultiInstanceFields)
                    or nameof(FilterOptionsDialogViewModel.HasId3v2Language)
                    or nameof(FilterOptionsDialogViewModel.HasAncestorFolderLevel)
                )
            )
            {
                return;
            }

            Dispatcher.UIThread.Post(
                () =>
                {
                    UpdateLayout();
                    ModalDialogHorizontalResize.LockHeightToContent(this);
                },
                DispatcherPriority.Loaded
            );
        }

        private void _OnOkClick(object? sender, RoutedEventArgs e)
        {
            if (DataContext is FilterOptionsDialogViewModel { CanConfirm: false })
            {
                return;
            }

            Close(true);
        }

        private void _OnCancelClick(object? sender, RoutedEventArgs e)
        {
            Close(false);
        }
    }
}
