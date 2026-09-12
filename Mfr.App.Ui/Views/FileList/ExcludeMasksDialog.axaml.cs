using Avalonia.Controls;
using Mfr.App.Ui.ViewModels.FileList;

namespace Mfr.App.Ui.Views.FileList
{
    /// <summary>
    /// Modal dialog for File List exclude masks.
    /// </summary>
    public partial class ExcludeMasksDialog : Window
    {
        /// <summary>
        /// Initializes the dialog (designer / default).
        /// </summary>
        public ExcludeMasksDialog()
        {
            InitializeComponent();
            ModalDialogKeyboard.Attach(this);
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
    }
}
