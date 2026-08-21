using Avalonia.Controls;
using Avalonia.Interactivity;
using Mfr.App.Ui.ViewModels;

namespace Mfr.App.Ui.Views
{
    /// <summary>
    /// Modal dialog for File List exclude masks (MFR 7 Exclude Masks window).
    /// </summary>
    public partial class ExcludeMasksDialog : Window
    {
        /// <summary>
        /// Initializes the dialog (designer / default).
        /// </summary>
        public ExcludeMasksDialog()
        {
            InitializeComponent();
        }

        /// <summary>
        /// Initializes the dialog with a view model.
        /// </summary>
        /// <param name="viewModel">Draft enable flag and mask lines.</param>
        public ExcludeMasksDialog(ExcludeMasksDialogViewModel viewModel)
            : this()
        {
            DataContext = viewModel;
        }

        private void _OnOkClick(object? sender, RoutedEventArgs e)
        {
            Close(true);
        }

        private void _OnCancelClick(object? sender, RoutedEventArgs e)
        {
            Close(false);
        }
    }
}
