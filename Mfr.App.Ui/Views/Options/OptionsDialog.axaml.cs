using Avalonia.Controls;
using Avalonia.Interactivity;
using Mfr.App.Ui.ViewModels.Options;

namespace Mfr.App.Ui.Views.Options
{
    /// <summary>
    /// Modal dialog for app-wide Options (remember flags and preset confirm).
    /// </summary>
    public partial class OptionsDialog : Window
    {
        /// <summary>
        /// Initializes the dialog (designer / default).
        /// </summary>
        public OptionsDialog()
        {
            InitializeComponent();
            ModalDialogKeyboard.Attach(this);
        }

        /// <summary>
        /// Initializes the dialog with a view model.
        /// </summary>
        /// <param name="viewModel">Draft options values.</param>
        public OptionsDialog(OptionsDialogViewModel viewModel)
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
