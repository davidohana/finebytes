using Avalonia.Controls;
using Avalonia.Interactivity;
using Mfr.App.Ui.ViewModels.AppliedFilters;

namespace Mfr.App.Ui.Views.AppliedFilters
{
    /// <summary>
    /// Modal dialog for applied-filter name and Apply-To targets.
    /// </summary>
    public partial class FilterOptionsDialog : Window
    {
        /// <summary>
        /// Initializes the dialog (designer / default).
        /// </summary>
        public FilterOptionsDialog()
        {
            InitializeComponent();
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
