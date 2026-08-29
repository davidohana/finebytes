using Avalonia.Controls;
using Avalonia.Interactivity;
using Mfr.App.Ui.ViewModels.RenameList;

namespace Mfr.App.Ui.Views.RenameList
{
    /// <summary>
    /// Modal dialog for Rename List display options.
    /// </summary>
    public partial class RenameListDisplayOptionsDialog : Window
    {
        /// <summary>
        /// Initializes the dialog (designer / default).
        /// </summary>
        public RenameListDisplayOptionsDialog()
        {
            InitializeComponent();
        }

        /// <summary>
        /// Initializes the dialog with a view model.
        /// </summary>
        /// <param name="viewModel">Draft display options.</param>
        public RenameListDisplayOptionsDialog(RenameListDisplayOptionsDialogViewModel viewModel)
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
